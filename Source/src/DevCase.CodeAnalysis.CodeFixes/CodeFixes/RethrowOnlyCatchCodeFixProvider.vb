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
                Dim trimmed As TryBlockSyntax = tryBlock.WithCatchBlocks(SyntaxFactory.List(tryBlock.CatchBlocks.Take(index)))
                Return document.WithSyntaxRoot(root.ReplaceNode(tryBlock, trimmed))
            End If

            If tryBlock.Statements.Count = 0 Then
                Return document.WithSyntaxRoot(root.RemoveNode(tryBlock, SyntaxRemoveOptions.KeepNoTrivia))
            End If

            ' Replace the statement by its body; the formatter re-indents the moved statements.
            Dim statements As New List(Of StatementSyntax)(tryBlock.Statements)
            Dim firstLeading As IEnumerable(Of SyntaxTrivia) = statements(0).GetLeadingTrivia().SkipWhile(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.WhitespaceTrivia))
            statements(0) = statements(0).WithLeadingTrivia(tryBlock.GetLeadingTrivia().AddRange(firstLeading))
            For index = 0 To statements.Count - 1
                statements(index) = statements(index).WithAdditionalAnnotations(Formatter.Annotation)
            Next index

            Dim newDocument As Document = document.WithSyntaxRoot(root.ReplaceNode(tryBlock, statements))
            Dim formatted As Document = Await Formatter.FormatAsync(newDocument, Formatter.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Return Await UtilCodeFix.NormalizeLineEndingsAsync(document, formatted, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
