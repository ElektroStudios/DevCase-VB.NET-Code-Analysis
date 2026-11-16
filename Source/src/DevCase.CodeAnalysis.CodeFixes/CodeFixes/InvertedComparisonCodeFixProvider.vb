#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Linq
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

#Region " InvertedComparisonCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.InvertedComparison"/>: replaces <c>Not (a = b)</c> with <c>a &lt;&gt; b</c>, 
    ''' <c>Not (a &lt; b)</c> with <c>a &gt;= b</c>, and so on.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(InvertedComparisonCodeFixProvider)), [Shared]>
    Public NotInheritable Class InvertedComparisonCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.InvertedComparison)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="InvertedComparisonCodeFixProvider"/> class.
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
                context.RegisterCodeFix(
                    CodeAction.Create("Use the inverse operator",
                                      Function(ct As CancellationToken) InvertedComparisonCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(InvertedComparisonCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the expression reported by the specified diagnostic.
        ''' </summary>
        ''' 
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        ''' 
        ''' <param name="diagnostic">
        ''' The diagnostic to fix.
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
        ''' Thrown when the diagnostic no longer points to a negated comparison.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim notExpression As UnaryExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of UnaryExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim comparison As BinaryExpressionSyntax = If(notExpression Is Nothing, Nothing, TryCast(UtilAnalyzerSyntax.SkipParentheses(notExpression.Operand), BinaryExpressionSyntax))
            Dim inverseKind As SyntaxKind = If(comparison Is Nothing, SyntaxKind.None, InvertedComparisonAnalyzer.GetInverseKind(comparison.Kind()))
            If inverseKind = SyntaxKind.None Then
                Throw New InvalidOperationException("The diagnostic does not point to a negated comparison.")
            End If

            Dim operatorToken As SyntaxToken = SyntaxFactory.Token(InvertedComparisonAnalyzer.GetOperatorTokenKind(inverseKind)).
                                                             WithLeadingTrivia(comparison.OperatorToken.LeadingTrivia).
                                                             WithTrailingTrivia(comparison.OperatorToken.TrailingTrivia)
            Dim inverse As BinaryExpressionSyntax = SyntaxFactory.BinaryExpression(inverseKind, comparison.Left, operatorToken, comparison.Right).
                                                                  WithLeadingTrivia(notExpression.GetLeadingTrivia()).
                                                                  WithTrailingTrivia(notExpression.GetTrailingTrivia())
            Return document.WithSyntaxRoot(root.ReplaceNode(notExpression, inverse))
        End Function

#End Region

    End Class

End Namespace

#End Region
