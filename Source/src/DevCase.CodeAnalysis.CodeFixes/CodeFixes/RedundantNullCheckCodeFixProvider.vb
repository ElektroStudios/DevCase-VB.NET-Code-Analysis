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

#Region " RedundantNullCheckCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.RedundantNullCheckBeforeTypeOf"/>: removes the redundant null check 
    ''' and its <see langword="AndAlso"/>/<see langword="OrElse"/> operator.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(RedundantNullCheckCodeFixProvider)), [Shared]>
    Public NotInheritable Class RedundantNullCheckCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.RedundantNullCheckBeforeTypeOf)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RedundantNullCheckCodeFixProvider"/> class.
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
                    CodeAction.Create("Remove the redundant null check",
                                      Function(ct As CancellationToken) RedundantNullCheckCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(RedundantNullCheckCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the null check reported by the specified diagnostic.
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
        ''' Thrown when the diagnostic no longer points to the operand of an <see langword="AndAlso"/> or <see langword="OrElse"/> expression.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim nullCheck As ExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of ExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim parent As BinaryExpressionSyntax = If(nullCheck Is Nothing, Nothing, TryCast(nullCheck.Parent, BinaryExpressionSyntax))
            If parent Is Nothing OrElse (Not parent.IsKind(SyntaxKind.AndAlsoExpression) AndAlso Not parent.IsKind(SyntaxKind.OrElseExpression)) Then
                Throw New InvalidOperationException("The diagnostic does not point to the operand of an AndAlso or OrElse expression.")
            End If

            Dim newRoot As SyntaxNode
            If parent.Left Is nullCheck Then
                ' "check AndAlso TypeOf ..." -> "TypeOf ..."
                newRoot = root.ReplaceNode(parent, parent.Right.WithLeadingTrivia(parent.GetLeadingTrivia()).WithTrailingTrivia(parent.GetTrailingTrivia()))
            Else
                ' "(a AndAlso check) AndAlso TypeOf ..." -> "a AndAlso TypeOf ..."
                newRoot = root.ReplaceNode(parent, parent.Left.WithTrailingTrivia(parent.GetTrailingTrivia()))
            End If
            Return document.WithSyntaxRoot(newRoot)
        End Function

#End Region

    End Class

End Namespace

#End Region
