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

#Region " IndexerCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.LinqInsteadOfIndexer"/>: replaces <c>items.First()</c> with <c>items(0)</c>, 
    ''' <c>items.Last()</c> with <c>items(items.Count - 1)</c> (<c>Length</c> for arrays) and <c>items.ElementAt(i)</c> with <c>items(i)</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' When the receiver is a method called without parentheses (e.g. <c>GetItems.First()</c>), the parentheses are added 
    ''' (<c>GetItems()(0)</c>) so that the index is not passed to the method.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(IndexerCodeFixProvider)), [Shared]>
    Public NotInheritable Class IndexerCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.LinqInsteadOfIndexer)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="IndexerCodeFixProvider"/> class.
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
                If Not String.Equals(diagnostic.Properties(IndexerAnalyzer.CanFixPropertyKey), Boolean.TrueString, StringComparison.OrdinalIgnoreCase) Then
                    Continue For
                End If
                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Use the indexer",
                                      Function(ct As CancellationToken) IndexerCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(IndexerCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the invocation reported by the specified diagnostic.
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
        ''' Thrown when the diagnostic no longer points to a <c>First</c>, <c>Last</c> or <c>ElementAt</c> call.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim invocation As InvocationExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of InvocationExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim access As MemberAccessExpressionSyntax = If(invocation Is Nothing, Nothing, TryCast(invocation.Expression, MemberAccessExpressionSyntax))
            If access Is Nothing OrElse access.Expression Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a First, Last or ElementAt call.")
            End If

            Dim receiver As String = access.Expression.WithoutTrivia().ToFullString()
            If TypeOf access.Expression IsNot InvocationExpressionSyntax AndAlso
               TypeOf model.GetSymbolInfo(access.Expression, cancellationToken).Symbol Is IMethodSymbol Then
                receiver = $"{receiver}()" ' Otherwise the index would be passed to the method.
            End If

            Dim index As String
            Select Case access.Name.Identifier.ValueText
                Case "First"
                    index = "0"
                Case "Last"
                    index = $"{receiver}.{diagnostic.Properties(IndexerAnalyzer.CountPropertyPropertyKey)} - 1"
                Case Else
                    index = invocation.ArgumentList.Arguments(0).GetExpression().WithoutTrivia().ToFullString()
            End Select

            Dim newExpression As ExpressionSyntax = SyntaxFactory.ParseExpression($"{receiver}({index})").WithTriviaFrom(invocation)
            Return document.WithSyntaxRoot(root.ReplaceNode(invocation, newExpression))
        End Function

#End Region

    End Class

End Namespace

#End Region
