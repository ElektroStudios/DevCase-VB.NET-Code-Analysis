#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Rename
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " UtilCodeFix "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides helper methods shared by the DevCase code fix providers.
    ''' </summary>
    Friend NotInheritable Class UtilCodeFix

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilCodeFix"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the innermost node whose span exactly matches the diagnostic span and that is of the requested type.
        ''' </summary>
        '''
        ''' <typeparam name="TNode">
        ''' The type of node to find.
        ''' </typeparam>
        '''
        ''' <param name="root">
        ''' The syntax root.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span.
        ''' </param>
        '''
        ''' <returns>
        ''' The matching node.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="root"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when no node of type <typeparamref name="TNode"/> matches the span (the document changed since the diagnostic was computed).
        ''' </exception>
        Friend Shared Function FindNodeBySpan(Of TNode As SyntaxNode)(root As SyntaxNode, span As TextSpan) As TNode

            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If

            Dim node As SyntaxNode = root.FindNode(span, findInsideTrivia:=False, getInnermostNodeForTie:=True)
            For Each candidate As SyntaxNode In node.AncestorsAndSelf()
                If TypeOf candidate Is TNode AndAlso candidate.Span = span Then
                    Return DirectCast(candidate, TNode)
                End If
                If candidate.Span.Length > span.Length Then
                    Exit For
                End If
            Next candidate

            Throw New InvalidOperationException($"No {GetType(TNode).Name} node matches the diagnostic span {span}.")
        End Function

        ''' <summary>
        ''' Applies a set of text changes to a document.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to change.
        ''' </param>
        '''
        ''' <param name="changes">
        ''' The text changes. They are sorted by position before being applied.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The changed document.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="document"/> or <paramref name="changes"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Async Function ApplyTextChangesAsync(document As Document, changes As IEnumerable(Of TextChange),
                                                           cancellationToken As CancellationToken) As Task(Of Document)

            If document Is Nothing Then
                Throw New ArgumentNullException(NameOf(document))
            End If
            If changes Is Nothing Then
                Throw New ArgumentNullException(NameOf(changes))
            End If

            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim ordered As List(Of TextChange) = changes.OrderBy(Function(c As TextChange) c.Span.Start).ToList()
            Return document.WithText(text.WithChanges(ordered))
        End Function

        ''' <summary>
        ''' Splits a leading trivia list into the part that must stay before a new enclosing construct
        ''' (everything up to, and including, the last preprocessor directive and the blank lines after it)
        ''' and the part that belongs to the declaration itself (documentation comments, comments, indentation).
        ''' </summary>
        '''
        ''' <param name="trivia">
        ''' The leading trivia of the declaration.
        ''' </param>
        '''
        ''' <param name="head">
        ''' Receives the trivia that must precede the new construct.
        ''' </param>
        '''
        ''' <param name="tail">
        ''' Receives the trivia that stays attached to the declaration.
        ''' </param>
        Friend Shared Sub SplitLeadingTrivia(trivia As SyntaxTriviaList, ByRef head As SyntaxTriviaList, ByRef tail As SyntaxTriviaList)

            Dim items As List(Of SyntaxTrivia) = trivia.ToList()
            Dim splitIndex As Integer = 0

            Dim lastDirectiveIndex As Integer = -1
            For index As Integer = 0 To items.Count - 1
                If items(index).IsDirective Then
                    lastDirectiveIndex = index
                End If
            Next index

            If lastDirectiveIndex >= 0 Then
                splitIndex = lastDirectiveIndex + 1
            End If

            ' Blank lines that follow the split point stay in the head.
            While splitIndex < items.Count AndAlso
                  (items(splitIndex).IsKind(SyntaxKind.EndOfLineTrivia) OrElse
                   (items(splitIndex).IsKind(SyntaxKind.WhitespaceTrivia) AndAlso
                    splitIndex + 1 < items.Count AndAlso items(splitIndex + 1).IsKind(SyntaxKind.EndOfLineTrivia)))
                splitIndex += 1
            End While

            head = SyntaxFactory.TriviaList(items.Take(splitIndex))
            tail = SyntaxFactory.TriviaList(items.Skip(splitIndex))
        End Sub

        ''' <summary>
        ''' Builds the lines of an XML documentation section (e.g. <c>&lt;summary&gt;</c>) following the DevCase layout.
        ''' </summary>
        '''
        ''' <param name="indentation">
        ''' The indentation of the documented declaration.
        ''' </param>
        '''
        ''' <param name="startTag">
        ''' The start tag, e.g. <c>&lt;param name="value"&gt;</c>.
        ''' </param>
        '''
        ''' <param name="endTag">
        ''' The end tag, e.g. <c>&lt;/param&gt;</c>.
        ''' </param>
        '''
        ''' <param name="content">
        ''' The content line, or an empty string (the section is then written on a single line: <c>''' &lt;tag&gt;&lt;/tag&gt;</c>).
        ''' </param>
        '''
        ''' <param name="endOfLine">
        ''' The end-of-line sequence.
        ''' </param>
        '''
        ''' <returns>
        ''' The section text, terminated by <paramref name="endOfLine"/>.
        ''' </returns>
        Friend Shared Function BuildDocumentationSection(indentation As String, startTag As String, endTag As String,
                                                         content As String, endOfLine As String) As String

            Dim prefix As String = $"{indentation}{UtilAnalyzerSyntax.DocumentationCommentPrefix} "

            ' An empty section is written on a single line (e.g. <exception cref="X"></exception>), never as a block of blank lines.
            Return If(String.IsNullOrWhiteSpace(content),
                      $"{prefix}{startTag}{endTag}{endOfLine}",
                      $"{prefix}{startTag}{endOfLine}{prefix}{content}{endOfLine}{prefix}{endTag}{endOfLine}")
        End Function

        ''' <summary>
        ''' Builds an empty XML documentation separator line (<c>''' </c>).
        ''' </summary>
        '''
        ''' <param name="indentation">
        ''' The indentation of the documented declaration.
        ''' </param>
        '''
        ''' <param name="endOfLine">
        ''' The end-of-line sequence.
        ''' </param>
        '''
        ''' <returns>
        ''' The separator line, terminated by <paramref name="endOfLine"/>.
        ''' </returns>
        Friend Shared Function BuildDocumentationSeparator(indentation As String, endOfLine As String) As String

            Return $"{indentation}{UtilAnalyzerSyntax.DocumentationCommentPrefix}{endOfLine}" ' No trailing white-space.
        End Function

        ''' <summary>
        ''' Gets the default description of a parameter, resolving its type through the semantic model.
        ''' </summary>
        '''
        ''' <param name="parameter">
        ''' The parameter syntax.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model of the tree that contains <paramref name="parameter"/>.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The description (see <see cref="UtilDocumentationText.GetParameterDescription"/>).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="parameter"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetParameterDescription(parameter As ParameterSyntax, semanticModel As SemanticModel,
                                                       cancellationToken As CancellationToken) As String

            If parameter Is Nothing Then
                Throw New ArgumentNullException(NameOf(parameter))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim symbol As IParameterSymbol = semanticModel.GetDeclaredSymbol(parameter, cancellationToken)
            Return UtilDocumentationText.GetParameterDescription(parameter.Identifier.Identifier.ValueText,
                                                                 symbol?.Type)
        End Function

        ''' <summary>
        ''' Gets the argument list of the invocation or object creation expression that has exactly the specified span.
        ''' </summary>
        ''' 
        ''' <param name="root">
        ''' The syntax root.
        ''' </param>
        ''' 
        ''' <param name="span">
        ''' The span of the reported expression.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The argument list, or <see langword="Nothing"/> if it cannot be found or it contains omitted arguments 
        ''' (e.g. <c>M(a, , b)</c>), which cannot be extended safely.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="root"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindArgumentList(root As SyntaxNode, span As TextSpan) As ArgumentListSyntax

            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If

            Dim argumentList As ArgumentListSyntax = Nothing
            For Each node As SyntaxNode In root.FindNode(span, getInnermostNodeForTie:=True).AncestorsAndSelf()
                If node.Span <> span Then
                    Continue For
                End If
                If TypeOf node Is InvocationExpressionSyntax Then
                    argumentList = DirectCast(node, InvocationExpressionSyntax).ArgumentList
                    Exit For
                End If
                If TypeOf node Is ObjectCreationExpressionSyntax Then
                    argumentList = DirectCast(node, ObjectCreationExpressionSyntax).ArgumentList
                    Exit For
                End If
            Next node

            Return If(argumentList Is Nothing OrElse
               argumentList.Arguments.Any(Function(a As ArgumentSyntax) a.IsMissing OrElse TypeOf a Is OmittedArgumentSyntax),
                Nothing,
                argumentList)
        End Function

        ''' <summary>
        ''' Formats arguments that are inserted into an existing argument list.
        ''' </summary>
        ''' 
        ''' <param name="names">
        ''' The parameter names (used for named arguments).
        ''' </param>
        ''' 
        ''' <param name="values">
        ''' The argument expressions, in the same order as <paramref name="names"/>.
        ''' </param>
        ''' 
        ''' <param name="named">
        ''' <see langword="True"/> to write named arguments (<c>name:=value</c>).
        ''' </param>
        ''' 
        ''' <param name="appended">
        ''' <see langword="True"/> when the text is appended after the last argument (it starts with <c>", "</c>); 
        ''' <see langword="False"/> when it is inserted before an existing argument (it ends with <c>", "</c>).
        ''' </param>
        ''' 
        ''' <returns>
        ''' The text to insert.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="names"/> or <paramref name="values"/> is <see langword="Nothing"/>.
        ''' </exception>
        ''' 
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="names"/> and <paramref name="values"/> have a different number of elements.
        ''' </exception>
        Friend Shared Function FormatInsertedArguments(names As IList(Of String), values As IList(Of String), named As Boolean, appended As Boolean) As String

            If names Is Nothing Then
                Throw New ArgumentNullException(NameOf(names))
            End If
            If values Is Nothing Then
                Throw New ArgumentNullException(NameOf(values))
            End If
            If names.Count <> values.Count Then
                Throw New ArgumentException("The number of names and values must be equal.", NameOf(values))
            End If

            Dim parts As New List(Of String)()
            For index As Integer = 0 To values.Count - 1
                parts.Add(If(named AndAlso Not String.IsNullOrEmpty(names(index)), $"{names(index)}:={values(index)}", values(index)))
            Next index

            Dim joined As String = String.Join(", ", parts)
            Return If(appended, $", {joined}", $"{joined}, ")
        End Function

        ''' <summary>
        ''' Makes the line breaks introduced by a code fix match the line breaks of the original document.
        ''' </summary>
        '''
        ''' <remarks>
        ''' The Roslyn formatter writes new line breaks with the line ending of the operating system (CRLF on Windows, LF elsewhere),
        ''' which would mix line endings in a document that uses the other style. Only the text changed by the fix is normalized,
        ''' and line breaks inside string literals are never modified, because they are part of the string value.
        ''' </remarks>
        '''
        ''' <param name="original">
        ''' The document before the fix.
        ''' </param>
        '''
        ''' <param name="changed">
        ''' The document after the fix.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The changed document, with consistent line endings.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="original"/> or <paramref name="changed"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Async Function NormalizeLineEndingsAsync(original As Document, changed As Document,
                                                               cancellationToken As CancellationToken) As Task(Of Document)

            If original Is Nothing Then
                Throw New ArgumentNullException(NameOf(original))
            End If
            If changed Is Nothing Then
                Throw New ArgumentNullException(NameOf(changed))
            End If

            Dim originalTree As SyntaxTree = Await original.GetSyntaxTreeAsync(cancellationToken).ConfigureAwait(False)
            Dim endOfLine As String = UtilAnalyzerSyntax.GetEndOfLine(originalTree)
            Dim newText As SourceText = Await changed.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot As SyntaxNode = Await changed.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            ' Spans of the changed text, in the coordinates of the new document.
            Dim textChanges As IEnumerable(Of TextChange) = Await changed.GetTextChangesAsync(original, cancellationToken).ConfigureAwait(False)
            Dim changedSpans As New List(Of TextSpan)()
            Dim offset As Integer = 0
            For Each change As TextChange In textChanges.OrderBy(Function(c As TextChange) c.Span.Start)
                changedSpans.Add(New TextSpan(change.Span.Start + offset, change.NewText.Length))
                offset += change.NewText.Length - change.Span.Length
            Next change

            ' Line breaks inside string literals belong to the string value.
            Dim literalSpans As List(Of TextSpan) = newRoot.DescendantTokens(descendIntoTrivia:=False).
                Where(Function(t As SyntaxToken) t.IsKind(SyntaxKind.StringLiteralToken) OrElse t.IsKind(SyntaxKind.InterpolatedStringTextToken)).
                Select(Function(t As SyntaxToken) t.Span).ToList()

            Dim edits As New List(Of TextChange)()
            For Each span As TextSpan In changedSpans
                Dim position As Integer = span.Start
                While position < span.End
                    Dim character As Char = newText(position)
                    If character <> UtilAnalyzerSyntax.CarriageReturn AndAlso character <> UtilAnalyzerSyntax.LineFeed Then
                        position += 1
                        Continue While
                    End If

                    Dim isCrLf As Boolean = character = UtilAnalyzerSyntax.CarriageReturn AndAlso position + 1 < newText.Length AndAlso
                                            newText(position + 1) = UtilAnalyzerSyntax.LineFeed
                    Dim lineBreak As New TextSpan(position, If(isCrLf, 2, 1))
                    Dim breakPosition As Integer = position
                    Dim insideLiteral As Boolean = literalSpans.Exists(Function(l As TextSpan) l.Contains(breakPosition))
                    If Not insideLiteral AndAlso Not newText.ToString(lineBreak).Equals(endOfLine, StringComparison.Ordinal) Then
                        edits.Add(New TextChange(lineBreak, endOfLine))
                    End If
                    position = lineBreak.End
                End While
            Next span

            Return If(edits.Count = 0, changed, changed.WithText(newText.WithChanges(edits)))
        End Function

        ''' <summary>
        ''' Gets the part of a leading trivia list that belongs to the code before the node: everything up to the last preprocessor 
        ''' directive (<c>#Region</c>, <c>#End Region</c>, <c>#If</c>... with the blank lines, comments and conditional code 
        ''' around them). What follows the last directive (blank lines, comments, XML documentation and indentation) belongs to the node.
        ''' </summary>
        '''
        ''' <param name="leadingTrivia">
        ''' The leading trivia of a node or token.
        ''' </param>
        '''
        ''' <returns>
        ''' The trivia up to the last directive (in Visual Basic, a directive includes its line break), or an empty list if there is none.
        ''' </returns>
        Friend Shared Function GetDirectiveSegment(leadingTrivia As SyntaxTriviaList) As SyntaxTriviaList

            Dim lastDirective As Integer = -1
            For index As Integer = 0 To leadingTrivia.Count - 1
                If leadingTrivia(index).IsDirective Then
                    lastDirective = index
                End If
            Next index
            Return If(lastDirective < 0, SyntaxFactory.TriviaList(), SyntaxFactory.TriviaList(leadingTrivia.Take(lastDirective + 1)))
        End Function

        ''' <summary>
        ''' Removes a node (a member or a statement) without removing the preprocessor directives around it: the directives of its 
        ''' leading trivia (for example the <c>#Region</c> that the node opens, or the <c>#End Region</c> that closes the previous 
        ''' region) move to the token that follows the node, and the directives inside the node that are not balanced within it are kept.
        ''' </summary>
        '''
        ''' <remarks>
        ''' <see cref="SyntaxRemoveOptions.KeepNoTrivia"/> removes every directive of the leading trivia, which breaks the regions 
        ''' (<c>#End Region</c> without <c>#Region</c>), and <see cref="SyntaxRemoveOptions.KeepUnbalancedDirectives"/> alone still 
        ''' removes a complete region that precedes the node (<c>#Region " Fields "</c> / <c>#End Region</c>). The XML documentation, 
        ''' comments and blank lines after the last directive belong to the node and are removed with it.
        ''' </remarks>
        '''
        ''' <typeparam name="TRoot">
        ''' The type of the root node.
        ''' </typeparam>
        '''
        ''' <param name="root">
        ''' The root node that contains <paramref name="node"/>.
        ''' </param>
        '''
        ''' <param name="node">
        ''' The node to remove.
        ''' </param>
        '''
        ''' <returns>
        ''' The new root.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="root"/> or <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function RemoveNodeKeepingDirectives(Of TRoot As SyntaxNode)(root As TRoot, node As SyntaxNode) As TRoot

            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If
            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim kept As SyntaxTriviaList = UtilCodeFix.GetDirectiveSegment(node.GetLeadingTrivia())
            If kept.Count = 0 Then
                Return root.RemoveNode(node, SyntaxRemoveOptions.KeepUnbalancedDirectives)
            End If

            ' The directives move to the next token (never inside the removed node), in their original order.
            Dim annotation As New SyntaxAnnotation()
            Dim nextToken As SyntaxToken = node.GetLastToken(includeZeroWidth:=True).GetNextToken(includeZeroWidth:=True)
            Dim updated As TRoot = root.ReplaceSyntax(
                {node}, Function(original As SyntaxNode, rewritten As SyntaxNode) rewritten.WithLeadingTrivia(rewritten.GetLeadingTrivia().Skip(kept.Count)).
                                                                                   WithAdditionalAnnotations(annotation),
                {nextToken}, Function(original As SyntaxToken, rewritten As SyntaxToken) rewritten.WithLeadingTrivia(kept.AddRange(rewritten.LeadingTrivia)),
                Array.Empty(Of SyntaxTrivia)(), Function(original As SyntaxTrivia, rewritten As SyntaxTrivia) rewritten)
            Return updated.RemoveNode(updated.GetAnnotatedNodes(annotation).Single(), SyntaxRemoveOptions.KeepUnbalancedDirectives)
        End Function

        ''' <summary>
        ''' Renames the parameter whose identifier is reported by a diagnostic, in the whole solution 
        ''' (references, <see langword="NameOf"/>, named arguments of callers and XML documentation included).
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document that declares the parameter.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic, located on the identifier of the parameter.
        ''' </param>
        '''
        ''' <param name="newName">
        ''' The new parameter name.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The solution with the renamed parameter.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="document"/> or <paramref name="diagnostic"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="newName"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the diagnostic no longer points to a parameter.
        ''' </exception>
        Friend Shared Async Function RenameParameterAsync(document As Document, diagnostic As Diagnostic, newName As String,
                                                          cancellationToken As CancellationToken) As Task(Of Solution)

            If document Is Nothing Then
                Throw New ArgumentNullException(NameOf(document))
            End If
            If diagnostic Is Nothing Then
                Throw New ArgumentNullException(NameOf(diagnostic))
            End If
            If String.IsNullOrEmpty(newName) Then
                Throw New ArgumentException("The new name cannot be null or empty.", NameOf(newName))
            End If

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim node As SyntaxNode = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent
            Dim parameter As IParameterSymbol = TryCast(model.GetDeclaredSymbol(node, cancellationToken), IParameterSymbol)
            If parameter Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a parameter.")
            End If

            Return Await Renamer.RenameSymbolAsync(document.Project.Solution, parameter, New SymbolRenameOptions(), newName,
                                                   cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
