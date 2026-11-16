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

#Region " DateTimeParseCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.DateTimeParseWithoutProvider"/>: two lightbulb actions that insert 
    ''' <c>CultureInfo.CurrentCulture</c> or <c>CultureInfo.InvariantCulture</c> (followed by <c>DateTimeStyles.None</c> or 
    ''' <c>TimeSpanStyles.None</c> when the overload requires it).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>CultureInfo.CurrentCulture</c> is offered first because it is what the call already uses: it keeps the current behavior 
    ''' and only makes it explicit. <c>CultureInfo.InvariantCulture</c> is the right choice for data written by programs 
    ''' (files, logs, configuration, network protocols).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(DateTimeParseCodeFixProvider)), [Shared]>
    Public NotInheritable Class DateTimeParseCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The <see cref="CultureInfo"/> property names offered by the fix, in order.
        ''' </summary>
        Private Shared ReadOnly CultureNames As String() = {NameOf(CultureInfo.CurrentCulture), NameOf(CultureInfo.InvariantCulture)}

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.DateTimeParseWithoutProvider)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DateTimeParseCodeFixProvider"/> class.
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
                For Each cultureName As String In DateTimeParseCodeFixProvider.CultureNames
                    Dim capturedCulture As String = cultureName
                    Dim title As String = If(capturedCulture.Equals(NameOf(CultureInfo.CurrentCulture), StringComparison.Ordinal),
                                             $"Add 'CultureInfo.{capturedCulture}' (keeps the current behavior)", $"Add 'CultureInfo.{capturedCulture}'")
                    context.RegisterCodeFix(
                        CodeAction.Create(title,
                                          Function(ct As CancellationToken) DateTimeParseCodeFixProvider.FixAsync(document, diagnostic, capturedCulture, ct),
                                          equivalenceKey:=$"{NameOf(DateTimeParseCodeFixProvider)}.{capturedCulture}"),
                        diagnostic)
                Next cultureName
            Next diagnostic
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
        ''' <param name="cultureName">
        ''' The <see cref="CultureInfo"/> property name (<c>CurrentCulture</c> or <c>InvariantCulture</c>).
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
        ''' Thrown when the diagnostic no longer points to a parsing call with arguments, or the required types cannot be resolved.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cultureName As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim argumentList As ArgumentListSyntax = UtilCodeFix.FindArgumentList(root, diagnostic.Location.SourceSpan)
            Dim cultureType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(GetType(CultureInfo).FullName)
            If argumentList Is Nothing OrElse argumentList.Arguments.Count = 0 OrElse cultureType Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a parsing call with arguments.")
            End If

            Dim properties As ImmutableDictionary(Of String, String) = diagnostic.Properties
            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = argumentList.Arguments
            Dim named As Boolean = arguments.Any(Function(a As ArgumentSyntax) a.IsNamed)
            Dim insertIndex As Integer = Integer.Parse(properties(DateTimeParseAnalyzer.InsertIndexPropertyKey), CultureInfo.InvariantCulture)
            Dim appended As Boolean = named OrElse insertIndex >= arguments.Count
            Dim position As Integer = If(appended, arguments.Last().Span.End, arguments(insertIndex).SpanStart)

            Dim names As New List(Of String) From {properties(DateTimeParseAnalyzer.ProviderParameterPropertyKey)}
            Dim values As New List(Of String) From {$"{cultureType.ToMinimalDisplayString(model, position)}.{cultureName}"}
            Dim stylesTypeName As String = properties(DateTimeParseAnalyzer.StylesTypePropertyKey)
            If Not String.IsNullOrEmpty(stylesTypeName) Then
                Dim stylesType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(stylesTypeName)
                If stylesType Is Nothing Then
                    Throw New InvalidOperationException($"The type '{stylesTypeName}' cannot be resolved.")
                End If
                names.Add(properties(DateTimeParseAnalyzer.StylesParameterPropertyKey))
                values.Add($"{stylesType.ToMinimalDisplayString(model, position)}.None")
            End If

            Dim insertion As String = UtilCodeFix.FormatInsertedArguments(names, values, named, appended)
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), insertion)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
