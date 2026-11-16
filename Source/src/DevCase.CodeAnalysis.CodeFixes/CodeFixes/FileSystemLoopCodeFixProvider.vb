#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " FileSystemLoopCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.FileSystemLoopWithoutMessagePumping"/>: 
    ''' inserts <c>Thread.CurrentThread.Join(0)</c> (with an explanatory comment) as the first statement of the loop body.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(FileSystemLoopCodeFixProvider)), [Shared]>
    Public NotInheritable Class FileSystemLoopCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add 'Thread.CurrentThread.Join(0)'"

        ''' <summary>
        ''' The comment appended to the inserted statement.
        ''' </summary>
        Friend Const ExplanatoryComment As String = "' Pumps pending messages to prevent ContextSwitchDeadlock during long-running file system enumerations."

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.FileSystemLoopWithoutMessagePumping)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FileSystemLoopCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return WellKnownFixAllProviders.BatchFixer
        End Function

        ''' <inheritdoc/>
        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim document As Document = context.Document
                Dim span As TextSpan = diagnostic.Location.SourceSpan
                context.RegisterCodeFix(
                    CodeAction.Create(FileSystemLoopCodeFixProvider.Title,
                                      Function(ct As CancellationToken) FileSystemLoopCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(FileSystemLoopCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the forEachBlock whose <see langword="For Each"/> statement is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the <see langword="For Each"/> statement).
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the diagnostic does not point to a <see langword="For Each"/> forEachBlock.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim forEachStatement As ForEachStatementSyntax = UtilCodeFix.FindNodeBySpan(Of ForEachStatementSyntax)(root, span)
            Dim forEachBlock As ForEachBlockSyntax = TryCast(forEachStatement.Parent, ForEachBlockSyntax)
            If forEachBlock Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a For Each loop.")
            End If

            Dim eol As SyntaxTrivia = SyntaxFactory.EndOfLine(UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree))
            Dim dot As SyntaxToken = SyntaxFactory.Token(SyntaxKind.DotToken)

            ' Global.System.Threading.Thread.CurrentThread.Join(0), reduced by the simplifier to the shortest valid form.
            Dim threadType As ExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                        SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.GlobalName(), dot, SyntaxFactory.IdentifierName("System")),
                        dot, SyntaxFactory.IdentifierName("Threading")),
                    dot, SyntaxFactory.IdentifierName("Thread")).
                WithAdditionalAnnotations(Simplifier.Annotation)

            Dim currentThread As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, threadType, dot, SyntaxFactory.IdentifierName("CurrentThread"))
            Dim join As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, currentThread, dot, SyntaxFactory.IdentifierName("Join"))
            Dim zero As ArgumentSyntax = SyntaxFactory.SimpleArgument(SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal(0)))
            Dim invocation As InvocationExpressionSyntax =
                SyntaxFactory.InvocationExpression(join, SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(zero)))

            Dim statement As StatementSyntax = SyntaxFactory.ExpressionStatement(invocation).
                WithTrailingTrivia(SyntaxFactory.Space, SyntaxFactory.CommentTrivia(FileSystemLoopCodeFixProvider.ExplanatoryComment), eol).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newForEachBlock As ForEachBlockSyntax = forEachBlock.WithStatements(forEachBlock.Statements.Insert(0, statement))
            Dim newDocument As Document = document.WithSyntaxRoot(root.ReplaceNode(forEachBlock, newForEachBlock))
            newDocument = Await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Dim formatted As Document = Await Formatter.FormatAsync(newDocument, Formatter.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Return Await UtilCodeFix.NormalizeLineEndingsAsync(document, formatted, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
