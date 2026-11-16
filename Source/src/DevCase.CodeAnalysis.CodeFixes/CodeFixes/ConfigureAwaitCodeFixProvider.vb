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

#Region " ConfigureAwaitCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MissingConfigureAwait"/>: 
    ''' injects <c>.ConfigureAwait(False)</c> (or <c>.ConfigureAwait(continueOnCapturedContext:=False)</c>) into the awaited expression.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ConfigureAwaitCodeFixProvider)), [Shared]>
    Public NotInheritable Class ConfigureAwaitCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add '.ConfigureAwait(False)'"

        ''' <summary>
        ''' The identifier name of the <c>continueOnCapturedContext</c> parameter.
        ''' </summary>
        Private Const ContinueOnCapturedContextIdentifierName As String = "continueOnCapturedContext"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingConfigureAwait)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ConfigureAwaitCodeFixProvider"/> class.
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
                    CodeAction.Create(ConfigureAwaitCodeFixProvider.Title,
                                      Function(ct As CancellationToken) ConfigureAwaitCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(ConfigureAwaitCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Builds the configured awaited expression: <c>expr.ConfigureAwait(False)</c>, 
        ''' parenthesizing <paramref name="awaitedExpression"/> when required.
        ''' </summary>
        '''
        ''' <param name="awaitedExpression">
        ''' The original awaited expression.
        ''' </param>
        '''
        ''' <param name="includeNamedParameter">
        ''' <see langword="True"/> to emit <c>continueOnCapturedContext:=False</c>; <see langword="False"/> to emit <c>False</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The new expression, carrying the trivia of <paramref name="awaitedExpression"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="awaitedExpression"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function BuildConfiguredExpression(awaitedExpression As ExpressionSyntax, includeNamedParameter As Boolean) As ExpressionSyntax

            If awaitedExpression Is Nothing Then
                Throw New ArgumentNullException(NameOf(awaitedExpression))
            End If

            Dim cleanExpr As ExpressionSyntax = awaitedExpression.WithoutLeadingTrivia().WithoutTrailingTrivia()
            If UtilAnalyzerSyntax.RequiresParenthesesForMemberAccess(cleanExpr) Then
                cleanExpr = SyntaxFactory.ParenthesizedExpression(cleanExpr)
            End If

            Dim memberAccess As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                                     cleanExpr,
                                                     SyntaxFactory.Token(SyntaxKind.DotToken),
                                                     SyntaxFactory.IdentifierName(UtilAnalyzerSyntax.ConfigureAwaitIdentifierName))

            Dim falseLiteral As LiteralExpressionSyntax =
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression, SyntaxFactory.Token(SyntaxKind.FalseKeyword))

            Dim argument As ArgumentSyntax
            If includeNamedParameter Then
                Dim nameColonEq As NameColonEqualsSyntax =
                    SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(ConfigureAwaitCodeFixProvider.ContinueOnCapturedContextIdentifierName))
                argument = SyntaxFactory.SimpleArgument(nameColonEq, falseLiteral)
            Else
                argument = SyntaxFactory.SimpleArgument(falseLiteral)
            End If

            Dim argumentList As ArgumentListSyntax =
                SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(argument))

            Return SyntaxFactory.InvocationExpression(memberAccess, argumentList).
                                 WithLeadingTrivia(awaitedExpression.GetLeadingTrivia()).
                                 WithTrailingTrivia(awaitedExpression.GetTrailingTrivia())
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the <see langword="Await"/> expression located at the specified span.
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
            Dim awaitExpression As AwaitExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of AwaitExpressionSyntax)(root, span)

            Dim includeNamedParameter As Boolean =
                DevCaseAnalyzerOptions.GetBoolean(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree,
                                                  DevCaseAnalyzerOptions.ConfigureAwaitNamedArgumentKey, defaultValue:=False)

            Dim newExpression As ExpressionSyntax = ConfigureAwaitCodeFixProvider.BuildConfiguredExpression(awaitExpression.Expression, includeNamedParameter)
            Dim newAwait As AwaitExpressionSyntax = awaitExpression.WithExpression(newExpression)
            Return document.WithSyntaxRoot(root.ReplaceNode(awaitExpression, newAwait))
        End Function

#End Region

    End Class

End Namespace

#End Region
