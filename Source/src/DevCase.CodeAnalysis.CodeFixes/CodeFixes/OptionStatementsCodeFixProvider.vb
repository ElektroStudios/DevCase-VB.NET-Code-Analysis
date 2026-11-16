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

#Region " OptionStatementsCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MissingOptionStatements"/>: 
    ''' writes <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c> inside an 
    ''' <c>#Region " Option Statements "</c> block at the top of the file.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Conflicting declarations (e.g. <c>Option Strict Off</c>) are replaced; other <see langword="Option"/> statements 
    ''' (e.g. <c>Option Compare Text</c>) and comments are preserved. An existing region that already wraps the 
    ''' <see langword="Option"/> statements is reused.
    ''' <para></para>
    ''' The edit is performed on the source text because it injects preprocessor directives, which are trivia 
    ''' that must be placed on their own lines regardless of the surrounding nodes.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(OptionStatementsCodeFixProvider)), [Shared]>
    Public NotInheritable Class OptionStatementsCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add 'Option Strict On', 'Option Explicit On' and 'Option Infer Off'"

        ''' <summary>
        ''' The name of the injected region.
        ''' </summary>
        Friend Const RegionName As String = " Option Statements "

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingOptionStatements)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="OptionStatementsCodeFixProvider"/> class.
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
                    CodeAction.Create(OptionStatementsCodeFixProvider.Title,
                                      Function(ct As CancellationToken) OptionStatementsCodeFixProvider.FixAsync(document, ct),
                                      equivalenceKey:=NameOf(OptionStatementsCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the specified document.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function FixAsync(document As Document, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As CompilationUnitSyntax = DirectCast(Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), CompilationUnitSyntax)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            If OptionStatementsAnalyzer.GetMissingStatements(root).Count = 0 Then
                Return document ' Already fixed (e.g. by a previous Fix All pass).
            End If

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim requiredLines As String = String.Join(eol, {OptionStatementsAnalyzer.OptionStrictOn,
                                                            OptionStatementsAnalyzer.OptionExplicitOn,
                                                            OptionStatementsAnalyzer.OptionInferOff})
            Dim regionStart As String = $"#Region ""{OptionStatementsCodeFixProvider.RegionName}"""
            Const RegionEnd As String = "#End Region"

            ' Case 1: no Option statement at all -> inject the whole region at the very top of the file.
            If root.Options.Count = 0 Then
                Dim header As String = $"{regionStart}{eol}{eol}{requiredLines}{eol}{eol}{RegionEnd}{eol}{eol}"
                Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(0, 0), header)}, cancellationToken).ConfigureAwait(False)
            End If

            ' Case 2: rewrite the lines that contain Option statements.
            Dim firstLine As TextLine = text.Lines.GetLineFromPosition(root.Options.First().SpanStart)
            Dim lastLine As TextLine = text.Lines.GetLineFromPosition(root.Options.Last().Span.End)

            ' Lines that declare Strict/Explicit/Infer (whatever their value) are rebuilt in canonical order.
            Dim replacedLineNumbers As New HashSet(Of Integer)()
            For Each statement As OptionStatementSyntax In root.Options
                Select Case statement.NameKeyword.Kind()
                    Case SyntaxKind.StrictKeyword, SyntaxKind.ExplicitKeyword, SyntaxKind.InferKeyword
                        replacedLineNumbers.Add(text.Lines.GetLineFromPosition(statement.SpanStart).LineNumber)
                End Select
            Next statement

            Dim builder As New StringBuilder()
            Dim isWrapped As Boolean = root.Options.First().GetLeadingTrivia().Any(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.RegionDirectiveTrivia))
            If Not isWrapped Then
                builder.Append(regionStart).Append(eol).Append(eol)
            End If

            Dim requiredWritten As Boolean = False
            For lineNumber As Integer = firstLine.LineNumber To lastLine.LineNumber
                Dim line As TextLine = text.Lines(lineNumber)
                If replacedLineNumbers.Contains(lineNumber) Then
                    If Not requiredWritten Then
                        builder.Append(requiredLines).Append(eol)
                        requiredWritten = True
                    End If
                    Continue For
                End If

                If Not requiredWritten AndAlso lineNumber = firstLine.LineNumber Then
                    ' Only non-required Option statements (e.g. Option Compare) exist: required ones go first.
                    builder.Append(requiredLines).Append(eol)
                    requiredWritten = True
                End If
                builder.Append(text.ToString(line.Span)).Append(eol)
            Next lineNumber

            If Not isWrapped Then
                builder.Append(eol).Append(RegionEnd).Append(eol)

                ' Keep a blank line between the region and the following code.
                Dim nextLineNumber As Integer = lastLine.LineNumber + 1
                If nextLineNumber < text.Lines.Count AndAlso Not String.IsNullOrWhiteSpace(text.ToString(text.Lines(nextLineNumber).Span)) Then
                    builder.Append(eol)
                End If
            End If

            Dim replacedSpan As TextSpan = TextSpan.FromBounds(firstLine.Start, lastLine.EndIncludingLineBreak)
            Dim newText As String = builder.ToString()
            If lastLine.EndIncludingLineBreak = lastLine.End AndAlso newText.EndsWith(eol, StringComparison.Ordinal) Then
                ' The original block was the last line of the file without a trailing line break.
                newText = newText.Substring(0, newText.Length - eol.Length)
            End If

            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(replacedSpan, newText)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
