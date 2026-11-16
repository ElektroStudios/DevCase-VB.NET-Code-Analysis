#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

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

#Region " RegexTimeoutCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.RegexWithoutTimeout"/>:
    ''' two lightbulb actions that append a match timeout argument (and <c>RegexOptions.None</c> when the overload requires it).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <list type="number">
    '''   <item><description><b>Add match timeout</b>: a finite timeout, 30 seconds by default
    '''   (configurable with <c>devcase_analyzers.dca017_timeout_ms</c>), e.g. <c>TimeSpan.FromSeconds(30)</c>.</description></item>
    '''   <item><description><b>Add <c>Regex.InfiniteMatchTimeout</c></b>: an explicit, documented opt-out for patterns that only
    '''   process trusted input. It removes the diagnostic but gives no protection.</description></item>
    ''' </list>
    ''' Arguments are appended as text after the last argument, so nested calls fixed together (Fix All) never conflict.
    ''' Named arguments are used when the call already uses named arguments.
    ''' Type names are written in their shortest form valid at that position (e.g. <c>TimeSpan</c> or <c>Text.RegularExpressions.RegexOptions</c>).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(RegexTimeoutCodeFixProvider)), [Shared]>
    Public NotInheritable Class RegexTimeoutCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The equivalence key of the finite timeout action.
        ''' </summary>
        Friend Const FiniteEquivalenceKey As String = "RegexTimeoutCodeFixProvider.Finite"

        ''' <summary>
        ''' The equivalence key of the <c>Regex.InfiniteMatchTimeout</c> action.
        ''' </summary>
        Friend Const InfiniteEquivalenceKey As String = "RegexTimeoutCodeFixProvider.Infinite"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.RegexWithoutTimeout)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RegexTimeoutCodeFixProvider"/> class.
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
            Dim milliseconds As Integer = RegexTimeoutCodeFixProvider.GetTimeoutMilliseconds(context.Document, root.SyntaxTree)

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim argumentList As ArgumentListSyntax = UtilCodeFix.FindArgumentList(root, diagnostic.Location.SourceSpan)
                If argumentList Is Nothing OrElse argumentList.Arguments.Count = 0 Then
                    Continue For ' Omitted arguments (Regex.Match(a, , b)) cannot be extended safely.
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create($"Add match timeout ({RegexTimeoutCodeFixProvider.FormatTimeout(milliseconds)})",
                                      Function(ct As CancellationToken) RegexTimeoutCodeFixProvider.FixAsync(document, diagnostic, infinite:=False, ct),
                                      equivalenceKey:=RegexTimeoutCodeFixProvider.FiniteEquivalenceKey),
                    diagnostic)
                context.RegisterCodeFix(
                    CodeAction.Create("Add 'Regex.InfiniteMatchTimeout' (explicit opt-out, no protection)",
                                      Function(ct As CancellationToken) RegexTimeoutCodeFixProvider.FixAsync(document, diagnostic, infinite:=True, ct),
                                      equivalenceKey:=RegexTimeoutCodeFixProvider.InfiniteEquivalenceKey),
                    diagnostic)
            Next diagnostic
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the match timeout configured for the specified syntax tree.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document.
        ''' </param>
        '''
        ''' <param name="tree">
        ''' The syntax tree of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The configured timeout in milliseconds, or <see cref="DevCaseAnalyzerOptions.RegexMatchTimeoutValue"/>
        ''' when the option is absent or is not a positive integer.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="document"/> or <paramref name="tree"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetTimeoutMilliseconds(document As Document, tree As SyntaxTree) As Integer

            If document Is Nothing Then
                Throw New ArgumentNullException(NameOf(document))
            End If
            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If

            Dim configured As String = DevCaseAnalyzerOptions.GetString(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, tree,
                                                                        DevCaseAnalyzerOptions.RegexMatchTimeoutKey, Nothing)
            Dim value As Integer
            Dim isValid As Boolean = configured IsNot Nothing AndAlso
                                     Integer.TryParse(configured, NumberStyles.None, CultureInfo.InvariantCulture, value) AndAlso
                                     value > 0
            Return If(isValid, value, DevCaseAnalyzerOptions.RegexMatchTimeoutValue)
        End Function

        ''' <summary>
        ''' Formats a timeout as the <see cref="TimeSpan"/> factory call written by the fix
        ''' (<c>FromSeconds</c> for whole seconds, otherwise <c>FromMilliseconds</c>).
        ''' </summary>
        '''
        ''' <param name="milliseconds">
        ''' The timeout in milliseconds.
        ''' </param>
        '''
        ''' <param name="typeName">
        ''' The name written for the <see cref="TimeSpan"/> type.
        ''' </param>
        '''
        ''' <returns>
        ''' The expression text, e.g. <c>TimeSpan.FromSeconds(1)</c> or <c>TimeSpan.FromMilliseconds(250)</c>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentOutOfRangeException">
        ''' Thrown when <paramref name="milliseconds"/> is not positive.
        ''' </exception>
        Friend Shared Function FormatTimeout(milliseconds As Integer, Optional typeName As String = NameOf(TimeSpan)) As String

            If milliseconds <= 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(milliseconds), milliseconds, "The timeout must be positive.")
            End If

            Return If(milliseconds Mod 1000 = 0,
                      $"{typeName}.{NameOf(TimeSpan.FromSeconds)}({(milliseconds \ 1000).ToString(CultureInfo.InvariantCulture)})",
                      $"{typeName}.{NameOf(TimeSpan.FromMilliseconds)}({milliseconds.ToString(CultureInfo.InvariantCulture)})")
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
        ''' <param name="infinite">
        ''' <see langword="True"/> to add <c>Regex.InfiniteMatchTimeout</c>; <see langword="False"/> to add the configured finite timeout.
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
        ''' Thrown when the diagnostic no longer points to a <c>Regex</c> call with arguments, or the regular expression types cannot be resolved.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, infinite As Boolean,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim argumentList As ArgumentListSyntax = UtilCodeFix.FindArgumentList(root, diagnostic.Location.SourceSpan)
            If argumentList Is Nothing OrElse argumentList.Arguments.Count = 0 Then
                Throw New InvalidOperationException("The diagnostic does not point to a Regex call with arguments.")
            End If

            Dim regexType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(RegexTimeoutAnalyzer.RegexMetadataName)
            Dim optionsType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(RegexTimeoutAnalyzer.RegexOptionsMetadataName)
            Dim timeSpanType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(GetType(TimeSpan).FullName)
            If regexType Is Nothing OrElse optionsType Is Nothing OrElse timeSpanType Is Nothing Then
                Throw New InvalidOperationException("The regular expression types cannot be resolved.")
            End If

            Dim lastArgument As ArgumentSyntax = argumentList.Arguments.Last()
            Dim position As Integer = lastArgument.Span.End
            Dim useNamedArguments As Boolean = argumentList.Arguments.Any(Function(a As ArgumentSyntax) a.IsNamed)

            Dim timeoutText As String =
                If(infinite,
                   $"{regexType.ToMinimalDisplayString(model, position)}.InfiniteMatchTimeout",
                   RegexTimeoutCodeFixProvider.FormatTimeout(RegexTimeoutCodeFixProvider.GetTimeoutMilliseconds(document, root.SyntaxTree),
                                                             timeSpanType.ToMinimalDisplayString(model, position)))

            Dim insertion As New System.Text.StringBuilder()
            If String.Equals(diagnostic.Properties(RegexTimeoutAnalyzer.AddsOptionsPropertyKey), Boolean.TrueString, StringComparison.OrdinalIgnoreCase) Then
                Dim optionsText As String = $"{optionsType.ToMinimalDisplayString(model, position)}.None"
                insertion.Append(RegexTimeoutCodeFixProvider.FormatArgument(diagnostic.Properties(RegexTimeoutAnalyzer.OptionsParameterPropertyKey), optionsText, useNamedArguments))
            End If
            insertion.Append(RegexTimeoutCodeFixProvider.FormatArgument(diagnostic.Properties(RegexTimeoutAnalyzer.TimeoutParameterPropertyKey), timeoutText, useNamedArguments))

            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), insertion.ToString())}, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Formats an argument appended to an argument list.
        ''' </summary>
        '''
        ''' <param name="parameterName">
        ''' The parameter name, used when <paramref name="named"/> is <see langword="True"/>.
        ''' </param>
        '''
        ''' <param name="expression">
        ''' The argument expression.
        ''' </param>
        '''
        ''' <param name="named">
        ''' <see langword="True"/> to write a named argument (<c>name:=value</c>).
        ''' </param>
        '''
        ''' <returns>
        ''' The argument text, including the leading separator (e.g. <c>", matchTimeout:=TimeSpan.FromSeconds(30)"</c>).
        ''' </returns>
        Private Shared Function FormatArgument(parameterName As String, expression As String, named As Boolean) As String

            Return If(named AndAlso Not String.IsNullOrEmpty(parameterName), $", {parameterName}:={expression}", $", {expression}")
        End Function

#End Region

    End Class

End Namespace

#End Region
