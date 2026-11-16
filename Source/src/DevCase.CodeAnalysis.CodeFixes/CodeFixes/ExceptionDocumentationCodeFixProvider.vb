#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Text
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

#Region " ExceptionDocumentationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MissingExceptionDocumentation"/>:
    ''' adds an empty <c>&lt;exception cref="..."&gt;</c> section for every undocumented exception.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The sections are intentionally left empty: the condition under which an exception is thrown is not inferred.
    ''' They are inserted after the last <c>&lt;summary&gt;</c>, <c>&lt;remarks&gt;</c>, <c>&lt;typeparam&gt;</c>,
    ''' <c>&lt;param&gt;</c>, <c>&lt;returns&gt;</c>, <c>&lt;value&gt;</c> or <c>&lt;exception&gt;</c> section (so before <c>&lt;example&gt;</c>).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ExceptionDocumentationCodeFixProvider)), [Shared]>
    Public NotInheritable Class ExceptionDocumentationCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add <exception> tags"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The documentation sections after which the exception sections may be inserted.
        ''' </summary>
        Private Shared ReadOnly AnchorElementNames As String() = {"summary", "remarks", "typeparam", "param", "returns", "value", "exception"}

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingExceptionDocumentation)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ExceptionDocumentationCodeFixProvider"/> class.
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
                    CodeAction.Create(ExceptionDocumentationCodeFixProvider.Title,
                                      Function(ct As CancellationToken) ExceptionDocumentationCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(ExceptionDocumentationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the member reported by the specified diagnostic.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic, which carries the undocumented exception type names.
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
        ''' Thrown when the diagnostic no longer points to a documented declaration.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            Dim declaration As StatementSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType(Of StatementSyntax)().
                FirstOrDefault(Function(s As StatementSyntax) TypeOf s Is MethodBaseSyntax)
            Dim docComment As DocumentationCommentTriviaSyntax = If(declaration Is Nothing, Nothing, UtilAnalyzerSyntax.GetDocumentationComment(declaration))
            If docComment Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a documented declaration.")
            End If

            Dim missing As String() = diagnostic.Properties(ExceptionDocumentationAnalyzer.MissingExceptionsPropertyKey).
                Split({","c}, StringSplitOptions.RemoveEmptyEntries)

            Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(declaration.GetFirstToken())
            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim separator As String = UtilCodeFix.BuildDocumentationSeparator(indentation, eol)

            ' The new tags form one compact group: one single-line tag per exception, without separators between them.
            Dim block As New StringBuilder()
            For Each typeName As String In missing
                block.Append(UtilCodeFix.BuildDocumentationSection(indentation, $"<exception cref=""{typeName}"">", "</exception>", String.Empty, eol))
            Next typeName

            Dim lines As TextLineCollection = text.Lines
            Dim declarationLine As Integer = lines.GetLineFromPosition(declaration.GetFirstToken().SpanStart).LineNumber
            Dim commentFirstLine As Integer = lines.GetLineFromPosition(docComment.SpanStart).LineNumber

            Dim anchor As XmlNodeSyntax = docComment.Content.LastOrDefault(
                Function(n As XmlNodeSyntax) ExceptionDocumentationCodeFixProvider.AnchorElementNames.Contains(UtilAnalyzerSyntax.GetXmlElementName(n), StringComparer.Ordinal))

            ' The line after which the group is inserted: the end of the anchor section, or else the last non-blank comment line.
            Dim anchorLine As Integer
            If anchor IsNot Nothing Then
                anchorLine = lines.GetLineFromPosition(anchor.Span.End).LineNumber
            Else
                anchorLine = declarationLine - 1
                While anchorLine > commentFirstLine AndAlso ExceptionDocumentationCodeFixProvider.IsBlankDocumentationLine(lines(anchorLine))
                    anchorLine -= 1
                End While
            End If

            Dim blankCount As Integer = 0
            While anchorLine + 1 + blankCount < declarationLine AndAlso ExceptionDocumentationCodeFixProvider.IsBlankDocumentationLine(lines(anchorLine + 1 + blankCount))
                blankCount += 1
            End While
            Dim hasFollowingContent As Boolean = anchorLine + 1 + blankCount < declarationLine

            Dim anchorIsSingleLineException As Boolean =
                anchor IsNot Nothing AndAlso
                String.Equals(UtilAnalyzerSyntax.GetXmlElementName(anchor), "exception", StringComparison.Ordinal) AndAlso
                lines.GetLineFromPosition(anchor.SpanStart).LineNumber = anchorLine

            Dim changes As New List(Of TextChange)()
            If anchorIsSingleLineException Then
                ' Append to the existing group of single-line exception tags.
                changes.Add(New TextChange(New TextSpan(lines(anchorLine).EndIncludingLineBreak, 0), block.ToString()))

            ElseIf blankCount > 0 Then
                ' Reuse the existing blank line as the separator before the group.
                Dim insertion As String = If(hasFollowingContent AndAlso blankCount = 1, $"{block}{separator}", block.ToString())
                changes.Add(New TextChange(New TextSpan(lines(anchorLine + 1).EndIncludingLineBreak, 0), insertion))

                If Not hasFollowingContent AndAlso blankCount > 1 Then
                    ' Remove dangling blank lines between the group and the declaration.
                    Dim removalStart As Integer = lines(anchorLine + 2).Start
                    Dim removalEnd As Integer = lines(anchorLine + blankCount).EndIncludingLineBreak
                    changes.Add(New TextChange(TextSpan.FromBounds(removalStart, removalEnd), String.Empty))
                End If

            Else
                Dim insertion As String = If(hasFollowingContent, $"{separator}{block}{separator}", $"{separator}{block}")
                changes.Add(New TextChange(New TextSpan(lines(anchorLine).EndIncludingLineBreak, 0), insertion))
            End If

            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Determines whether a text line is an empty documentation comment line (only <c>'''</c> and white-space).
        ''' </summary>
        '''
        ''' <param name="line">
        ''' The text line.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the line is an empty documentation comment line; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsBlankDocumentationLine(line As TextLine) As Boolean

            Return String.Equals(line.ToString().Trim(), UtilAnalyzerSyntax.DocumentationCommentPrefix, StringComparison.Ordinal)
        End Function

#End Region

    End Class

End Namespace

#End Region
