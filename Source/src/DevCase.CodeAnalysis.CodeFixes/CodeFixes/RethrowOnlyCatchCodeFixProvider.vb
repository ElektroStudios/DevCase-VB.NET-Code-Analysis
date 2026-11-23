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
Imports System.Text
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Formatting

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " RethrowOnlyCatchCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.RethrowOnlyCatch"/>: removes the trailing rethrow-only 
    ''' <see langword="Catch"/> blocks, and removes the whole <see langword="Try"/> statement (keeping its body) when no 
    ''' <see langword="Catch"/> or <see langword="Finally"/> block remains.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The <see langword="Try"/> statement is not unwrapped (and no fix is offered) when its body contains <c>Exit Try</c>, 
    ''' or declares a local variable whose name is also declared elsewhere in the enclosing block, which would cause a conflict.
    ''' <para></para>
    ''' The preprocessor directives are kept: the <c>#If</c> / <c>#End If</c> lines (and the conditional code between them) 
    ''' before a removed <see langword="Catch"/> or <see langword="End Try"/> keyword stay in place, after the remaining code.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(RethrowOnlyCatchCodeFixProvider)), [Shared]>
    Public NotInheritable Class RethrowOnlyCatchCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.RethrowOnlyCatch)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RethrowOnlyCatchCodeFixProvider"/> class.
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
                Dim tryBlock As TryBlockSyntax = RethrowOnlyCatchCodeFixProvider.FindTryBlock(root, diagnostic)
                If tryBlock Is Nothing OrElse
                   (RethrowOnlyCatchCodeFixProvider.RequiresUnwrapping(tryBlock) AndAlso Not RethrowOnlyCatchCodeFixProvider.CanUnwrap(tryBlock)) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Remove the rethrow-only Catch block",
                                      Function(ct As CancellationToken) RethrowOnlyCatchCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(RethrowOnlyCatchCodeFixProvider)),
                    diagnostic)
            Next diagnostic
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets how many indentation characters the body of a <see langword="Try"/> statement has more than the 
        ''' <see langword="Try"/> keyword.
        ''' </summary>
        ''' 
        ''' <param name="tryBlock">
        ''' The <see langword="Try"/> block.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The difference between the column of the first statement of the body (or, when the body only has disabled code, the 
        ''' smallest indentation of its lines) and the column of the <see langword="Try"/> keyword; zero if it is not positive.
        ''' </returns>
        Private Shared Function GetBodyIndentation(tryBlock As TryBlockSyntax) As Integer

            Dim text As SourceText = tryBlock.SyntaxTree.GetText()
            Dim tryColumn As Integer = tryBlock.SpanStart - text.Lines.GetLineFromPosition(tryBlock.SpanStart).Start
            Dim bodyColumn As Integer
            If tryBlock.Statements.Count > 0 Then
                bodyColumn = tryBlock.Statements(0).SpanStart - text.Lines.GetLineFromPosition(tryBlock.Statements(0).SpanStart).Start
            Else
                Dim lines As List(Of String) = tryBlock.DescendantTrivia().
                    Where(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.DisabledTextTrivia)).
                    SelectMany(Function(t As SyntaxTrivia) t.ToString().Split({vbCrLf, vbLf, vbCr}, StringSplitOptions.None)).
                    Where(Function(line As String) line.Trim().Length <> 0).
                    ToList()
                If lines.Count = 0 Then
                    Return 0
                End If
                bodyColumn = lines.Min(Function(line As String) line.Length - line.TrimStart().Length)
            End If
            Return Math.Max(0, bodyColumn - tryColumn)
        End Function

        ''' <summary>
        ''' Removes indentation from the lines of disabled code (the code of an <c>#If</c> block whose condition is false).
        ''' </summary>
        ''' 
        ''' <param name="trivia">
        ''' The trivia; only <see cref="SyntaxKind.DisabledTextTrivia"/> is changed.
        ''' </param>
        ''' 
        ''' <param name="count">
        ''' The number of spaces or tabs to remove from the start of each line (fewer if the line has fewer).
        ''' </param>
        ''' 
        ''' <returns>
        ''' The dedented trivia, or <paramref name="trivia"/> if it is not disabled code or there is nothing to remove.
        ''' </returns>
        Private Shared Function Dedent(trivia As SyntaxTrivia, count As Integer) As SyntaxTrivia

            If count = 0 OrElse Not trivia.IsKind(SyntaxKind.DisabledTextTrivia) Then
                Return trivia
            End If

            Dim result As New StringBuilder()
            Dim removable As Integer = count
            For Each character As Char In trivia.ToString()
                If removable > 0 AndAlso (character = " "c OrElse character = ChrW(9)) Then ' Space or tab.
                    removable -= 1
                    Continue For
                End If
                removable = If(character = ChrW(13) OrElse character = ChrW(10), count, 0) ' After a line break (CR or LF).
                result.Append(character)
            Next character
            Return SyntaxFactory.DisabledTextTrivia(result.ToString())
        End Function

        ''' <summary>
        ''' Finds the <see langword="Try"/> block that contains the reported <see langword="Catch"/> statement.
        ''' </summary>
        ''' 
        ''' <param name="root">
        ''' The syntax root.
        ''' </param>
        ''' 
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The <see langword="Try"/> block, or <see langword="Nothing"/> if it cannot be found.
        ''' </returns>
        Private Shared Function FindTryBlock(root As SyntaxNode, diagnostic As Diagnostic) As TryBlockSyntax

            Return root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType(Of TryBlockSyntax)().FirstOrDefault()
        End Function

        ''' <summary>
        ''' Determines whether removing the rethrow-only blocks leaves the <see langword="Try"/> statement without 
        ''' <see langword="Catch"/> and <see langword="Finally"/> blocks.
        ''' </summary>
        ''' 
        ''' <param name="tryBlock">
        ''' The <see langword="Try"/> block.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the statement must be replaced by its body; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function RequiresUnwrapping(tryBlock As TryBlockSyntax) As Boolean

            Return RethrowOnlyCatchAnalyzer.GetFirstRethrowOnlyIndex(tryBlock) = 0 AndAlso tryBlock.FinallyBlock Is Nothing
        End Function

        ''' <summary>
        ''' Determines whether a <see langword="Try"/> statement can be replaced by its body without changing the meaning of the code.
        ''' </summary>
        ''' 
        ''' <param name="tryBlock">
        ''' The <see langword="Try"/> block.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the body has no <c>Exit Try</c> for this statement and no local name conflicts with the enclosing block; 
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function CanUnwrap(tryBlock As TryBlockSyntax) As Boolean

            Dim exitsThisTry As Boolean = tryBlock.Statements.SelectMany(Function(s As StatementSyntax) s.DescendantNodesAndSelf()).
                OfType(Of ExitStatementSyntax)().
                Any(Function(e As ExitStatementSyntax) e.IsKind(SyntaxKind.ExitTryStatement) AndAlso
                                                       e.Ancestors().OfType(Of TryBlockSyntax)().FirstOrDefault() Is tryBlock)
            If exitsThisTry OrElse tryBlock.Parent Is Nothing Then
                Return False
            End If

            Dim bodyNames As New HashSet(Of String)(
                tryBlock.Statements.OfType(Of LocalDeclarationStatementSyntax)().
                    SelectMany(Function(d As LocalDeclarationStatementSyntax) d.Declarators).
                    SelectMany(Function(v As VariableDeclaratorSyntax) v.Names).
                    Select(Function(n As ModifiedIdentifierSyntax) n.Identifier.ValueText), StringComparer.OrdinalIgnoreCase)

            Return Not tryBlock.Parent.DescendantNodes().OfType(Of ModifiedIdentifierSyntax)().
                Any(Function(n As ModifiedIdentifierSyntax) Not tryBlock.Span.Contains(n.Span) AndAlso bodyNames.Contains(n.Identifier.ValueText))
        End Function

        ''' <summary>
        ''' Applies the fix to the <see langword="Try"/> statement reported by the specified diagnostic.
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
        ''' Thrown when the diagnostic no longer points to a <see langword="Try"/> statement.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim tryBlock As TryBlockSyntax = RethrowOnlyCatchCodeFixProvider.FindTryBlock(root, diagnostic)
            If tryBlock Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a Try statement.")
            End If

            Dim index As Integer = RethrowOnlyCatchAnalyzer.GetFirstRethrowOnlyIndex(tryBlock)
            If Not RethrowOnlyCatchCodeFixProvider.RequiresUnwrapping(tryBlock) Then
                ' Removes the trailing rethrow-only blocks; the directives before their Catch keyword stay.
                Dim removed As List(Of CatchBlockSyntax) = tryBlock.CatchBlocks.Skip(index).ToList()
                Dim trimmedRoot As SyntaxNode = root.TrackNodes(removed)
                For Each catchBlock As CatchBlockSyntax In removed
                    trimmedRoot = UtilCodeFix.RemoveNodeKeepingDirectives(trimmedRoot, trimmedRoot.GetCurrentNode(catchBlock))
                Next catchBlock
                Return document.WithSyntaxRoot(trimmedRoot)
            End If

            ' The Try statement is removed: the directives before its Catch and End Try keywords (with the conditional code between 
            ' them, such as "#If DEBUG Then ... #End If" around the last statements of the body) move after the statement.
            ' The formatter does not re-indent disabled code (the code of an #If block whose condition is false), so it is 
            ' dedented here, as the statements of the body.
            Dim dedentation As Integer = RethrowOnlyCatchCodeFixProvider.GetBodyIndentation(tryBlock)
            Dim annotation As New SyntaxAnnotation()
            Dim innerDirectives As SyntaxTriviaList = SyntaxFactory.TriviaList(
                tryBlock.CatchBlocks.Select(Function(c As CatchBlockSyntax) c.GetLeadingTrivia()).Append(tryBlock.EndTryStatement.GetLeadingTrivia()).
                    SelectMany(Function(t As SyntaxTriviaList) UtilCodeFix.GetDirectiveSegment(t)).
                    Select(Function(t As SyntaxTrivia) RethrowOnlyCatchCodeFixProvider.Dedent(t, dedentation)))
            Dim strippedTry As TryBlockSyntax = tryBlock.ReplaceNodes(
                tryBlock.CatchBlocks.Cast(Of SyntaxNode)().Append(tryBlock.EndTryStatement),
                Function(original As SyntaxNode, rewritten As SyntaxNode) rewritten.WithLeadingTrivia(
                    rewritten.GetLeadingTrivia().Skip(UtilCodeFix.GetDirectiveSegment(original.GetLeadingTrivia()).Count)))
            Dim nextToken As SyntaxToken = tryBlock.GetLastToken(includeZeroWidth:=True).GetNextToken(includeZeroWidth:=True)
            Dim preparedRoot As SyntaxNode = root.ReplaceSyntax(
                {tryBlock}, Function(original As SyntaxNode, rewritten As SyntaxNode) strippedTry.WithAdditionalAnnotations(annotation),
                {nextToken}, Function(original As SyntaxToken, rewritten As SyntaxToken) rewritten.WithLeadingTrivia(innerDirectives.AddRange(rewritten.LeadingTrivia)),
                Array.Empty(Of SyntaxTrivia)(), Function(original As SyntaxTrivia, rewritten As SyntaxTrivia) rewritten)
            Dim preparedTry As TryBlockSyntax = DirectCast(preparedRoot.GetAnnotatedNodes(annotation).Single(), TryBlockSyntax)

            If preparedTry.Statements.Count = 0 Then
                Return document.WithSyntaxRoot(UtilCodeFix.RemoveNodeKeepingDirectives(preparedRoot, preparedTry))
            End If

            ' Replace the statement by its body; the formatter re-indents the moved statements.
            Dim statements As New List(Of StatementSyntax)(preparedTry.Statements)
            For index = 0 To statements.Count - 1
                Dim statement As StatementSyntax = statements(index)
                statements(index) = statement.ReplaceTrivia(statement.DescendantTrivia().Where(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.DisabledTextTrivia)),
                                                            Function(original As SyntaxTrivia, rewritten As SyntaxTrivia) RethrowOnlyCatchCodeFixProvider.Dedent(rewritten, dedentation)).
                                              WithAdditionalAnnotations(Formatter.Annotation)
            Next index

            ' The comments before the Try keyword go before the first statement, without the indentation of the Try keyword: the first 
            ' statement keeps its own leading trivia, so a directive that begins it ("#If DEBUG Then") stays at the start of its line.
            Dim tryLeading As New List(Of SyntaxTrivia)(preparedTry.GetLeadingTrivia())
            Do While tryLeading.Count > 0 AndAlso tryLeading(tryLeading.Count - 1).IsKind(SyntaxKind.WhitespaceTrivia)
                tryLeading.RemoveAt(tryLeading.Count - 1)
            Loop
            statements(0) = statements(0).WithLeadingTrivia(tryLeading.Concat(statements(0).GetLeadingTrivia()))

            Dim newDocument As Document = document.WithSyntaxRoot(preparedRoot.ReplaceNode(preparedTry, statements))
            Dim formatted As Document = Await Formatter.FormatAsync(newDocument, Formatter.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Return Await UtilCodeFix.NormalizeLineEndingsAsync(document, formatted, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
