#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
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

#Region " SilentGuardClauseCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.SilentGuardClause"/>: 
    ''' replaces the silent <see langword="Return"/>/<see langword="Exit"/> with an explicit argument exception.
    ''' </summary>
    '''
    ''' <remarks>
    ''' A guard that combines several parameter checks with <see langword="OrElse"/> is split into one guard per check, 
    ''' so that every exception reports the exact offending parameter.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(SilentGuardClauseCodeFixProvider)), [Shared]>
    Public NotInheritable Class SilentGuardClauseCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Throw an argument exception instead of exiting silently"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.SilentGuardClause)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SilentGuardClauseCodeFixProvider"/> class.
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
                    CodeAction.Create(SilentGuardClauseCodeFixProvider.Title,
                                      Function(ct As CancellationToken) SilentGuardClauseCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(SilentGuardClauseCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Builds the <see langword="Throw"/> statement that corresponds to a guard condition.
        ''' </summary>
        '''
        ''' <param name="guard">
        ''' The guard condition.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="ThrowStatementSyntax"/> such as <c>Throw New ArgumentNullException(NameOf(value))</c>, 
        ''' with a fully qualified exception type annotated for simplification.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="guard"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the exception kind of <paramref name="guard"/> is not supported.
        ''' </exception>
        Friend Shared Function BuildThrowStatement(guard As GuardCondition) As ThrowStatementSyntax

            If guard Is Nothing Then
                Throw New ArgumentNullException(NameOf(guard))
            End If

            Dim exceptionTypeName As String
            Select Case guard.ExceptionKind
                Case GuardExceptionKind.ArgumentNull
                    exceptionTypeName = NameOf(ArgumentNullException)
                Case GuardExceptionKind.Argument
                    exceptionTypeName = NameOf(ArgumentException)
                Case GuardExceptionKind.ArgumentOutOfRange
                    exceptionTypeName = NameOf(ArgumentOutOfRangeException)
                Case Else
                    Throw New InvalidOperationException($"Unexpected exception kind: {guard.ExceptionKind}.")
            End Select

            Dim exceptionType As TypeSyntax =
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(SyntaxFactory.GlobalName(), SyntaxFactory.IdentifierName(NameOf(System))),
                    SyntaxFactory.IdentifierName(exceptionTypeName)).
                WithAdditionalAnnotations(Simplifier.Annotation)

            Dim nameOfArgument As ArgumentSyntax =
                SyntaxFactory.SimpleArgument(SyntaxFactory.NameOfExpression(SyntaxFactory.IdentifierName(guard.ParameterName)))

            Dim arguments As New List(Of ArgumentSyntax)()
            If guard.ExceptionKind = GuardExceptionKind.Argument Then
                ' ArgumentException(message, paramName)
                Dim messageLiteral As LiteralExpressionSyntax =
                    SyntaxFactory.StringLiteralExpression(SyntaxFactory.StringLiteralToken($"""{guard.Message}""", guard.Message))
                arguments.Add(SyntaxFactory.SimpleArgument(messageLiteral))
                arguments.Add(nameOfArgument)
            Else
                arguments.Add(nameOfArgument)
            End If

            Dim creation As ObjectCreationExpressionSyntax =
                SyntaxFactory.ObjectCreationExpression(exceptionType).
                              WithArgumentList(SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList(arguments)))

            Return SyntaxFactory.ThrowStatement(creation)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the guard clause whose silent exit is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the silent exit statement).
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
        ''' Thrown when the syntax at the diagnostic location no longer matches the reported issue.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim reportedExit As StatementSyntax = UtilCodeFix.FindNodeBySpan(Of StatementSyntax)(root, span)
            Dim ifNode As SyntaxNode = reportedExit.Parent
            If ifNode Is Nothing OrElse Not (ifNode.IsKind(SyntaxKind.MultiLineIfBlock) OrElse ifNode.IsKind(SyntaxKind.SingleLineIfStatement)) Then
                Throw New InvalidOperationException("The diagnostic does not point to the silent exit of an If statement.")
            End If

            Dim exitStatement As StatementSyntax = Nothing
            Dim conditions As ImmutableArray(Of GuardCondition) = Nothing
            If Not UtilGuardClauses.TryAnalyze(ifNode, semanticModel, cancellationToken, exitStatement, conditions) Then
                Throw New InvalidOperationException("The If statement is no longer a silent guard clause.")
            End If

            Dim newRoot As SyntaxNode
            ' A guard nested in a single-line construct (e.g. "If a Then If b Is Nothing Then Return") cannot host 
            ' multi-line blocks; in that case the first checked parameter is reported.
            Dim isNestedInSingleLine As Boolean = TypeOf ifNode.Parent Is SingleLineIfStatementSyntax OrElse
                                                  TypeOf ifNode.Parent Is SingleLineElseClauseSyntax

            If conditions.Length = 1 OrElse isNestedInSingleLine Then
                ' Keep the original If layout (single-line stays single-line); only the exit statement changes.
                Dim throwStatement As ThrowStatementSyntax =
                    SilentGuardClauseCodeFixProvider.BuildThrowStatement(conditions(0)).WithTriviaFrom(exitStatement)
                newRoot = root.ReplaceNode(exitStatement, throwStatement)
            Else
                newRoot = root.ReplaceNode(ifNode, SilentGuardClauseCodeFixProvider.BuildSplitGuards(ifNode, conditions))
            End If

            Dim newDocument As Document = document.WithSyntaxRoot(newRoot)
            newDocument = Await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Dim formatted As Document = Await Formatter.FormatAsync(newDocument, Formatter.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Return Await UtilCodeFix.NormalizeLineEndingsAsync(document, formatted, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Builds one multi-line <see langword="If"/> block per guard condition.
        ''' </summary>
        '''
        ''' <param name="ifNode">
        ''' The original guard clause.
        ''' </param>
        '''
        ''' <param name="conditions">
        ''' The atomic guard conditions.
        ''' </param>
        '''
        ''' <returns>
        ''' The replacement statements, carrying the leading and trailing trivia of <paramref name="ifNode"/>.
        ''' </returns>
        Private Shared Function BuildSplitGuards(ifNode As SyntaxNode, conditions As ImmutableArray(Of GuardCondition)) As IEnumerable(Of SyntaxNode)

            Dim endOfLine As String = UtilAnalyzerSyntax.GetEndOfLine(ifNode.SyntaxTree)
            Dim eolTrivia As SyntaxTrivia = SyntaxFactory.EndOfLine(endOfLine)
            Dim result As New List(Of SyntaxNode)()

            For index As Integer = 0 To conditions.Length - 1
                Dim guard As GuardCondition = conditions(index)

                Dim ifStatement As IfStatementSyntax =
                    SyntaxFactory.IfStatement(SyntaxFactory.Token(SyntaxKind.IfKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                                              guard.Condition.WithoutTrivia(),
                                              SyntaxFactory.Token(SyntaxKind.ThenKeyword).WithLeadingTrivia(SyntaxFactory.Space)).
                                  WithTrailingTrivia(eolTrivia)

                Dim throwStatement As StatementSyntax =
                    SilentGuardClauseCodeFixProvider.BuildThrowStatement(guard).WithTrailingTrivia(eolTrivia)

                Dim endIfStatement As EndBlockStatementSyntax =
                    SyntaxFactory.EndIfStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                                                 SyntaxFactory.Token(SyntaxKind.IfKeyword)).
                                  WithTrailingTrivia(eolTrivia)

                Dim block As MultiLineIfBlockSyntax =
                    SyntaxFactory.MultiLineIfBlock(ifStatement, SyntaxFactory.SingletonList(throwStatement), Nothing, Nothing).
                                  WithEndIfStatement(endIfStatement)

                If index = 0 Then
                    block = block.WithLeadingTrivia(ifNode.GetLeadingTrivia())
                End If
                If index = conditions.Length - 1 Then
                    block = block.WithTrailingTrivia(ifNode.GetTrailingTrivia())
                End If

                result.Add(block.WithAdditionalAnnotations(Formatter.Annotation))
            Next index
            Return result
        End Function

#End Region

    End Class

End Namespace

#End Region
