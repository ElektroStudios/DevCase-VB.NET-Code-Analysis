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

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " PlusConcatenationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.StringConcatenationWithPlus"/>: replaces <c>+</c> with <c>&amp;</c> 
    ''' and <c>+=</c> with <c>&amp;=</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' A space is added around the new operator when the original one had none: <c>a&amp;b</c> would be read as the type character 
    ''' of <c>a</c>, and <c>&amp;H</c> as the start of a hexadecimal literal.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(PlusConcatenationCodeFixProvider)), [Shared]>
    Public NotInheritable Class PlusConcatenationCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.StringConcatenationWithPlus)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="PlusConcatenationCodeFixProvider"/> class.
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
                    CodeAction.Create("Use '&' to concatenate strings",
                                      Function(ct As CancellationToken) PlusConcatenationCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(PlusConcatenationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the operator reported by the specified diagnostic.
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
        ''' Thrown when the diagnostic no longer points to a <c>+</c> or <c>+=</c> operator.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim token As SyntaxToken = root.FindToken(diagnostic.Location.SourceSpan.Start)
            Dim replacement As String
            If token.IsKind(SyntaxKind.PlusToken) Then
                replacement = "&"
            ElseIf token.IsKind(SyntaxKind.PlusEqualsToken) Then
                replacement = "&="
            Else
                Throw New InvalidOperationException("The diagnostic does not point to a '+' or '+=' operator.")
            End If

            If token.SpanStart > 0 AndAlso Not Char.IsWhiteSpace(text(token.SpanStart - 1)) Then
                replacement = $" {replacement}"
            End If
            If token.Span.End < text.Length AndAlso Not Char.IsWhiteSpace(text(token.Span.End)) Then
                replacement = $"{replacement} "
            End If
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(token.Span, replacement)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
