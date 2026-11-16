#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " ArrayRankOnNameCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.ArrayRankOnVariable"/>: moves the array designator from the name to the type 
    ''' (<c>Dim values() As Integer</c> becomes <c>Dim values As Integer()</c>).
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ArrayRankOnNameCodeFixProvider)), [Shared]>
    Public NotInheritable Class ArrayRankOnNameCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.ArrayRankOnVariable)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ArrayRankOnNameCodeFixProvider"/> class.
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
                    CodeAction.Create("Move the array designator to the type",
                                      Function(ct As CancellationToken) ArrayRankOnNameCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(ArrayRankOnNameCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Moves the array designator of the name reported by the specified diagnostic to its type.
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
        ''' Thrown when the diagnostic no longer points to a name whose array designator can be moved.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim identifier As ModifiedIdentifierSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.FirstAncestorOrSelf(Of ModifiedIdentifierSyntax)()
            Dim target As TypeSyntax = If(identifier Is Nothing, Nothing, ArrayRankOnNameAnalyzer.GetMovableTarget(identifier))
            If target Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a name whose array designator can be moved.")
            End If

            Dim designator As String = String.Concat(identifier.ArrayRankSpecifiers.Select(Function(r As ArrayRankSpecifierSyntax) r.WithoutTrivia().ToString()))
            Dim changes As TextChange() = {
                New TextChange(TextSpan.FromBounds(identifier.Identifier.Span.End, identifier.ArrayRankSpecifiers.Last().Span.End), String.Empty),
                New TextChange(New TextSpan(target.Span.End, 0), designator)}
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
