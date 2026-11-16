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

#Region " DateTimeKindCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.DateTimeWithoutKind"/>: three lightbulb actions that append 
    ''' <c>DateTimeKind.Unspecified</c>, <c>DateTimeKind.Utc</c> or <c>DateTimeKind.Local</c> (preceded by <c>0</c> time components when the overload requires them).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>DateTimeKind.Unspecified</c> is offered first because it is the kind that the constructor without a kind already uses: 
    ''' it keeps the current behavior and only makes it explicit. <c>Utc</c> and <c>Local</c> change the meaning of the value 
    ''' (e.g. the result of <c>ToUniversalTime</c>), so they are a deliberate choice.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(DateTimeKindCodeFixProvider)), [Shared]>
    Public NotInheritable Class DateTimeKindCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The <see cref="DateTimeKind"/> member names offered by the fix, in order.
        ''' </summary>
        Private Shared ReadOnly KindNames As String() = {NameOf(DateTimeKind.Unspecified), NameOf(DateTimeKind.Utc), NameOf(DateTimeKind.Local)}

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.DateTimeWithoutKind)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DateTimeKindCodeFixProvider"/> class.
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
        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            Dim root As SyntaxNode = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim argumentList As ArgumentListSyntax = UtilCodeFix.FindArgumentList(root, diagnostic.Location.SourceSpan)
                If argumentList Is Nothing OrElse argumentList.Arguments.Count = 0 Then
                    Continue For
                End If

                Dim document As Document = context.Document
                For Each kindName As String In DateTimeKindCodeFixProvider.KindNames
                    Dim capturedKind As String = kindName
                    Dim title As String = If(capturedKind.Equals(NameOf(DateTimeKind.Unspecified), StringComparison.Ordinal),
                                             $"Add 'DateTimeKind.{capturedKind}' (keeps the current behavior)", $"Add 'DateTimeKind.{capturedKind}'")
                    context.RegisterCodeFix(
                        CodeAction.Create(title,
                                          Function(ct As CancellationToken) DateTimeKindCodeFixProvider.FixAsync(document, diagnostic, capturedKind, ct),
                                          equivalenceKey:=$"{NameOf(DateTimeKindCodeFixProvider)}.{capturedKind}"),
                        diagnostic)
                Next kindName
            Next diagnostic
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
        ''' <param name="kindName">
        ''' The <see cref="DateTimeKind"/> member name.
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
        ''' Thrown when the diagnostic no longer points to a <c>New DateTime</c> expression with arguments.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, kindName As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim argumentList As ArgumentListSyntax = UtilCodeFix.FindArgumentList(root, diagnostic.Location.SourceSpan)
            Dim kindType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(GetType(DateTimeKind).FullName)
            If argumentList Is Nothing OrElse argumentList.Arguments.Count = 0 OrElse kindType Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a New DateTime expression with arguments.")
            End If

            Dim position As Integer = argumentList.Arguments.Last().Span.End
            Dim names As String() = diagnostic.Properties(DateTimeKindAnalyzer.ParameterNamesPropertyKey).Split(","c)
            Dim values As New List(Of String)()
            For index As Integer = 0 To names.Length - 2
                values.Add("0") ' Time components: the value stays the same.
            Next index
            values.Add($"{kindType.ToMinimalDisplayString(model, position)}.{kindName}")

            Dim named As Boolean = argumentList.Arguments.Any(Function(a As ArgumentSyntax) a.IsNamed)
            Dim insertion As String = UtilCodeFix.FormatInsertedArguments(names, values, named, appended:=True)
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), insertion)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
