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
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " TaskResultCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.TaskResultAccess"/>: 
    ''' replaces <c>.Result</c> (or <c>.Result()</c>) with <c>.GetAwaiter().GetResult()</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(TaskResultCodeFixProvider)), [Shared]>
    Public NotInheritable Class TaskResultCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Replace '.Result' with '.GetAwaiter().GetResult()'"

        ''' <summary>
        ''' The identifier name of the <c>GetAwaiter</c> method.
        ''' </summary>
        Private Const GetAwaiterIdentifierName As String = "GetAwaiter"

        ''' <summary>
        ''' The identifier name of the <c>GetResult</c> method.
        ''' </summary>
        Private Const GetResultIdentifierName As String = "GetResult"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.TaskResultAccess)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TaskResultCodeFixProvider"/> class.
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
                    CodeAction.Create(TaskResultCodeFixProvider.Title,
                                      Function(ct As CancellationToken) TaskResultCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(TaskResultCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Creates a chained <c>.GetAwaiter().GetResult()</c> invocation for the specified base expression.
        ''' </summary>
        '''
        ''' <remarks>
        ''' <paramref name="baseExpression"/> may be <see langword="Nothing"/> when the original access is the 
        ''' <c>WhenNotNull</c> part of a null-conditional expression (<c>task?.Result</c>) or lives in a 
        ''' <see langword="With"/> block (<c>.Result</c>); the original <c>TaskResultRewriter.BuildGetAwaiterGetResult</c> 
        ''' threw an exception in those cases.
        ''' </remarks>
        '''
        ''' <param name="baseExpression">
        ''' The expression that owns the <c>Result</c> property, or <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' A syntax node representing the chained invocations.
        ''' </returns>
        Friend Shared Function BuildGetAwaiterGetResult(baseExpression As ExpressionSyntax) As InvocationExpressionSyntax

            Dim cleanExpr As ExpressionSyntax = baseExpression?.WithoutTrailingTrivia()

            Dim getAwaiterAccess As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                     cleanExpr,
                                                     SyntaxFactory.Token(SyntaxKind.DotToken),
                                                     SyntaxFactory.IdentifierName(TaskResultCodeFixProvider.GetAwaiterIdentifierName))

            Dim getAwaiterInvocation As InvocationExpressionSyntax =
                SyntaxFactory.InvocationExpression(getAwaiterAccess, SyntaxFactory.ArgumentList())

            Dim getResultAccess As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                     getAwaiterInvocation,
                                                     SyntaxFactory.Token(SyntaxKind.DotToken),
                                                     SyntaxFactory.IdentifierName(TaskResultCodeFixProvider.GetResultIdentifierName))

            Return SyntaxFactory.InvocationExpression(getResultAccess, SyntaxFactory.ArgumentList())
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the <c>.Result</c> access located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim reportedNode As ExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of ExpressionSyntax)(root, span)

            ' The diagnostic is reported either on "x.Result" or on the property-call form "x.Result()".
            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(reportedNode, MemberAccessExpressionSyntax)
            If memberAccess Is Nothing Then
                Dim invocation As InvocationExpressionSyntax = DirectCast(reportedNode, InvocationExpressionSyntax)
                memberAccess = DirectCast(invocation.Expression, MemberAccessExpressionSyntax)
            End If

            Dim replacement As InvocationExpressionSyntax =
                TaskResultCodeFixProvider.BuildGetAwaiterGetResult(memberAccess.Expression).WithTriviaFrom(reportedNode)

            Return document.WithSyntaxRoot(root.ReplaceNode(reportedNode, replacement))
        End Function

#End Region

    End Class

End Namespace

#End Region
