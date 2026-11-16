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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " EncodingCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.EncodingNotSpecified"/>: three lightbulb actions that insert 
    ''' <c>New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)</c> (UTF-8 without BOM), <c>Encoding.UTF8</c> (UTF-8 with BOM) 
    ''' or <c>Encoding.Default</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The action that writes the encoding the call already uses implicitly is offered first and marked as keeping the current behavior: 
    ''' UTF-8 without BOM for <c>System.IO</c>, UTF-8 with BOM for <c>Microsoft.VisualBasic.FileIO</c>. 
    ''' <c>Encoding.Default</c> is offered last: it is the system ANSI code page on .NET Framework (UTF-8 on modern .NET), 
    ''' so it changes the bytes written and read on .NET Framework.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(EncodingCodeFixProvider)), [Shared]>
    Public NotInheritable Class EncodingCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The choice that inserts <c>Encoding.Default</c>.
        ''' </summary>
        Private Const DefaultChoice As String = "Default"

        ''' <summary>
        ''' The encoding choices offered by the fix (the implicit one is moved first).
        ''' </summary>
        Private Shared ReadOnly EncodingChoices As String() = {EncodingAnalyzer.Utf8WithoutBomValue, EncodingAnalyzer.Utf8WithBomValue, EncodingCodeFixProvider.DefaultChoice}

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.EncodingNotSpecified)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="EncodingCodeFixProvider"/> class.
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
                If argumentList Is Nothing Then
                    Continue For
                End If

                Dim implicitEncoding As String = diagnostic.Properties(EncodingAnalyzer.ImplicitEncodingPropertyKey)
                Dim ordered As New List(Of String) From {implicitEncoding}
                ordered.AddRange(EncodingCodeFixProvider.EncodingChoices.Where(Function(c As String) Not c.Equals(implicitEncoding, StringComparison.Ordinal)))

                Dim document As Document = context.Document
                For Each choice As String In ordered
                    Dim capturedChoice As String = choice
                    Dim title As String = EncodingCodeFixProvider.GetTitle(capturedChoice)
                    If capturedChoice.Equals(implicitEncoding, StringComparison.Ordinal) Then
                        title = $"{title} (keeps the current behavior)"
                    End If
                    context.RegisterCodeFix(
                        CodeAction.Create(title,
                                          Function(ct As CancellationToken) EncodingCodeFixProvider.FixAsync(document, diagnostic, capturedChoice, ct),
                                          equivalenceKey:=$"{NameOf(EncodingCodeFixProvider)}.{capturedChoice}"),
                        diagnostic)
                Next choice
            Next diagnostic
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the title of the code action for an encoding choice.
        ''' </summary>
        ''' 
        ''' <param name="choice">
        ''' The encoding choice.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The action title.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="choice"/> is not a known encoding choice.
        ''' </exception>
        Private Shared Function GetTitle(choice As String) As String

            Select Case choice
                Case EncodingAnalyzer.Utf8WithoutBomValue
                    Return "Add 'New UTF8Encoding(False)': UTF-8 without BOM"
                Case EncodingAnalyzer.Utf8WithBomValue
                    Return "Add 'Encoding.UTF8': UTF-8 with BOM"
                Case EncodingCodeFixProvider.DefaultChoice
                    Return "Add 'Encoding.Default': ANSI code page on .NET Framework, UTF-8 on .NET"
                Case Else
                    Throw New ArgumentException($"Unknown encoding choice '{choice}'.", NameOf(choice))
            End Select
        End Function

        ''' <summary>
        ''' Applies the fix to the call reported by the specified diagnostic.
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
        ''' <param name="choice">
        ''' The encoding choice.
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
        ''' Thrown when the diagnostic no longer points to a call with an argument list, or the encoding types cannot be resolved.
        ''' </exception>
        ''' 
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="choice"/> is not a known encoding choice.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, choice As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim argumentList As ArgumentListSyntax = UtilCodeFix.FindArgumentList(root, diagnostic.Location.SourceSpan)
            Dim encodingType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(EncodingAnalyzer.EncodingTypeName)
            Dim utf8Type As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName("System.Text.UTF8Encoding")
            If argumentList Is Nothing OrElse encodingType Is Nothing OrElse utf8Type Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a call with an argument list.")
            End If

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = argumentList.Arguments
            Dim named As Boolean = arguments.Any(Function(a As ArgumentSyntax) a.IsNamed)
            Dim insertIndex As Integer = Integer.Parse(diagnostic.Properties(EncodingAnalyzer.InsertIndexPropertyKey), CultureInfo.InvariantCulture)
            Dim appended As Boolean = named OrElse insertIndex >= arguments.Count
            Dim position As Integer
            If arguments.Count = 0 Then
                position = argumentList.OpenParenToken.Span.End
            ElseIf appended Then
                position = arguments.Last().Span.End
            Else
                position = arguments(insertIndex).SpanStart
            End If

            Dim value As String
            Select Case choice
                Case EncodingAnalyzer.Utf8WithoutBomValue
                    value = $"New {utf8Type.ToMinimalDisplayString(model, position)}(encoderShouldEmitUTF8Identifier:=False)"
                Case EncodingAnalyzer.Utf8WithBomValue
                    value = $"{encodingType.ToMinimalDisplayString(model, position)}.UTF8"
                Case EncodingCodeFixProvider.DefaultChoice
                    value = $"{encodingType.ToMinimalDisplayString(model, position)}.Default"
                Case Else
                    Throw New ArgumentException($"Unknown encoding choice '{choice}'.", NameOf(choice))
            End Select

            ' When the overload requires "append As Boolean" before the encoding, False keeps the behavior of the original call.
            Dim names As New List(Of String)()
            Dim values As New List(Of String)()
            Dim appendName As String = diagnostic.Properties(EncodingAnalyzer.AppendParameterPropertyKey)
            If Not String.IsNullOrEmpty(appendName) Then
                names.Add(appendName)
                values.Add("False")
            End If
            names.Add(diagnostic.Properties(EncodingAnalyzer.EncodingParameterPropertyKey))
            values.Add(value)
            Dim insertion As String = UtilCodeFix.FormatInsertedArguments(names, values, named, appended)
            If arguments.Count = 0 Then
                insertion = insertion.Substring(2) ' "M()" -> "M(value)": no separator before the first argument.
            End If
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), insertion)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
