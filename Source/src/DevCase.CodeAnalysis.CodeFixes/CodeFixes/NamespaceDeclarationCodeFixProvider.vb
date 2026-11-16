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
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " NamespaceDeclarationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.TypeOutsideNamespace"/>: wraps the type inside a <see langword="Namespace"/> block.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The namespace name is chosen deterministically, in this order:
    ''' <list type="number">
    '''   <item><description>The <c>devcase_analyzers.default_namespace</c> option, when it is set to a valid name.</description></item>
    '''   <item><description>The first top-level <see langword="Namespace"/> block already declared in the same file, 
    '''   so that the file stays consistent.</description></item>
    '''   <item><description>The assembly name of the project (the same default Visual Studio assigns to new projects), 
    '''   with invalid identifier characters replaced by underscores.</description></item>
    ''' </list>
    ''' When no valid name can be determined, no fix is offered.
    ''' <para></para>
    ''' Every partial declaration of the type (in every document of the solution) is wrapped, so that the parts keep forming a single type. 
    ''' When a file contains only top-level type declarations, all of them are wrapped in one block. 
    ''' Region directives that precede the type stay outside the new block, and its content is re-indented by the formatter.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(NamespaceDeclarationCodeFixProvider)), [Shared]>
    Public NotInheritable Class NamespaceDeclarationCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.TypeOutsideNamespace)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="NamespaceDeclarationCodeFixProvider"/> class.
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
            Dim namespaceName As String = NamespaceDeclarationCodeFixProvider.GetNamespaceName(context.Document, DirectCast(root, CompilationUnitSyntax))
            If namespaceName Is Nothing Then
                Return ' No valid namespace name can be determined: no fix is offered.
            End If

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim document As Document = context.Document
                Dim span As TextSpan = diagnostic.Location.SourceSpan
                context.RegisterCodeFix(
                    CodeAction.Create($"Wrap in 'Namespace {namespaceName}'",
                                      Function(ct As CancellationToken) NamespaceDeclarationCodeFixProvider.FixAsync(document, span, namespaceName, ct),
                                      equivalenceKey:=NameOf(NamespaceDeclarationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the namespace name used to wrap the types of a document (see the class remarks for the resolution order).
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document.
        ''' </param>
        '''
        ''' <param name="root">
        ''' The compilation unit of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The namespace name, or <see langword="Nothing"/> when no valid name can be determined.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="document"/> or <paramref name="root"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetNamespaceName(document As Document, root As CompilationUnitSyntax) As String

            If document Is Nothing Then
                Throw New ArgumentNullException(NameOf(document))
            End If
            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If

            ' 1. Explicit configuration.
            Dim configured As String = DevCaseAnalyzerOptions.GetString(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree,
                                                                        DevCaseAnalyzerOptions.DefaultNamespaceKey, Nothing)
            If NamespaceDeclarationCodeFixProvider.IsValidNamespaceName(configured) Then
                Return configured
            End If

            ' 2. A namespace already declared in the same file.
            Dim existing As NamespaceBlockSyntax = root.Members.OfType(Of NamespaceBlockSyntax)().FirstOrDefault()
            If existing IsNot Nothing Then
                Dim existingName As String = existing.NamespaceStatement.Name.WithoutTrivia().ToString()
                If NamespaceDeclarationCodeFixProvider.IsValidNamespaceName(existingName) Then
                    Return existingName
                End If
            End If

            ' 3. The assembly name.
            Dim assemblyName As String = document.Project.AssemblyName
            Return If(String.IsNullOrWhiteSpace(assemblyName),
                      Nothing,
                      NamespaceDeclarationCodeFixProvider.GetNamespaceFromAssemblyName(assemblyName))
        End Function

        ''' <summary>
        ''' Converts an assembly name into a valid namespace name: invalid identifier characters become underscores, 
        ''' segments that start with a digit are prefixed with an underscore, and keywords are escaped with brackets.
        ''' </summary>
        '''
        ''' <param name="assemblyName">
        ''' The assembly name.
        ''' </param>
        '''
        ''' <returns>
        ''' The namespace name (e.g. <c>"My-App.2026"</c> → <c>"My_App._2026"</c>), or <see langword="Nothing"/> if none can be derived 
        ''' (e.g. <c>".."</c>).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="assemblyName"/> is <see langword="Nothing"/>, empty, or consists only of white-space characters.
        ''' </exception>
        Friend Shared Function GetNamespaceFromAssemblyName(assemblyName As String) As String

            If String.IsNullOrWhiteSpace(assemblyName) Then
                Throw New ArgumentNullException(NameOf(assemblyName))
            End If

            Dim segments As New List(Of String)()
            For Each rawSegment As String In assemblyName.Split({"."c}, StringSplitOptions.RemoveEmptyEntries)
                Dim builder As New System.Text.StringBuilder()
                For Each c As Char In rawSegment.Trim()
                    builder.Append(If(Char.IsLetterOrDigit(c) OrElse c = "_"c, c, "_"c))
                Next c
                If builder.Length = 0 Then
                    Continue For
                End If
                If Char.IsDigit(builder(0)) Then
                    builder.Insert(0, "_"c)
                End If

                Dim segment As String = builder.ToString()
                If SyntaxFacts.GetKeywordKind(segment) <> SyntaxKind.None Then
                    segment = $"[{segment}]"
                End If
                segments.Add(segment)
            Next rawSegment

            Dim result As String = String.Join(".", segments)
            Return If(NamespaceDeclarationCodeFixProvider.IsValidNamespaceName(result), result, Nothing)
        End Function

        ''' <summary>
        ''' Determines whether a text is a valid namespace name for a <see langword="Namespace"/> statement.
        ''' </summary>
        '''
        ''' <param name="name">
        ''' The candidate name. May be <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the name parses as a qualified name without errors; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsValidNamespaceName(name As String) As Boolean

            If String.IsNullOrWhiteSpace(name) Then
                Return False
            End If

            Dim parsed As NameSyntax = SyntaxFactory.ParseName(name)
            Return Not parsed.ContainsDiagnostics AndAlso parsed.ToFullString().Equals(name, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Wraps groups of top-level declarations of a compilation unit inside <see langword="Namespace"/> blocks.
        ''' </summary>
        '''
        ''' <param name="root">
        ''' The compilation unit.
        ''' </param>
        '''
        ''' <param name="targets">
        ''' The top-level declarations that must be wrapped.
        ''' </param>
        '''
        ''' <param name="namespaceName">
        ''' The namespace name.
        ''' </param>
        '''
        ''' <returns>
        ''' The new compilation unit. The new blocks carry the <see cref="Formatter.Annotation"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="root"/> or <paramref name="targets"/> is <see langword="Nothing"/>, 
        ''' or <paramref name="namespaceName"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        Friend Shared Function WrapInNamespace(root As CompilationUnitSyntax, targets As IEnumerable(Of StatementSyntax), namespaceName As String) As CompilationUnitSyntax

            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If
            If targets Is Nothing Then
                Throw New ArgumentNullException(NameOf(targets))
            End If
            If String.IsNullOrWhiteSpace(namespaceName) Then
                Throw New ArgumentNullException(NameOf(namespaceName))
            End If

            Dim targetSet As New HashSet(Of StatementSyntax)(targets)
            Dim wrapEverything As Boolean = root.Members.Count > 0 AndAlso
                                            root.Members.All(Function(m As StatementSyntax) UtilTypeDeclarations.GetDeclarationNode(m) Is m) AndAlso
                                            root.Members.Skip(1).All(Function(m As StatementSyntax) Not m.GetLeadingTrivia().Any(Function(t As SyntaxTrivia) t.IsDirective))

            Dim eol As SyntaxTrivia = SyntaxFactory.EndOfLine(UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree))
            Dim newMembers As New List(Of StatementSyntax)()

            If wrapEverything Then
                newMembers.Add(NamespaceDeclarationCodeFixProvider.BuildNamespaceBlock(root.Members.ToList(), namespaceName, eol))
            Else
                For Each member As StatementSyntax In root.Members
                    If targetSet.Contains(member) Then
                        newMembers.Add(NamespaceDeclarationCodeFixProvider.BuildNamespaceBlock({member}.ToList(), namespaceName, eol))
                    Else
                        newMembers.Add(member)
                    End If
                Next member
            End If

            Return root.WithMembers(SyntaxFactory.List(newMembers))
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to every declaration of the type whose identifier is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document that contains the diagnostic.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the type identifier).
        ''' </param>
        '''
        ''' <param name="namespaceName">
        ''' The namespace name.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed solution.
        ''' </returns>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the syntax at the diagnostic location no longer matches the reported issue.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, namespaceName As String,
                                               cancellationToken As CancellationToken) As Task(Of Solution)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim declaration As StatementSyntax = root.FindToken(span.Start).Parent.AncestorsAndSelf().
                Select(Function(n As SyntaxNode) UtilTypeDeclarations.GetDeclarationNode(n)).
                FirstOrDefault(Function(d As StatementSyntax) d IsNot Nothing)
            If declaration Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a type declaration.")
            End If

            Dim type As INamedTypeSymbol = TryCast(semanticModel.GetDeclaredSymbol(UtilTypeDeclarations.GetHeader(declaration), cancellationToken), INamedTypeSymbol)
            If type Is Nothing Then
                Throw New InvalidOperationException("The type symbol cannot be resolved.")
            End If

            Dim solution As Solution = document.Project.Solution
            For Each treeGroup As IGrouping(Of SyntaxTree, SyntaxReference) In type.DeclaringSyntaxReferences.GroupBy(Function(r As SyntaxReference) r.SyntaxTree)
                Dim targetDocument As Document = solution.GetDocument(treeGroup.Key)
                If targetDocument Is Nothing Then
                    Continue For ' The declaration lives outside the solution (e.g. metadata-as-source).
                End If

                Dim targetRoot As CompilationUnitSyntax = DirectCast(Await targetDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False), CompilationUnitSyntax)
                Dim targets As New List(Of StatementSyntax)()
                For Each reference As SyntaxReference In treeGroup
                    Dim node As StatementSyntax = UtilTypeDeclarations.GetDeclarationNode(targetRoot.FindNode(reference.Span, getInnermostNodeForTie:=True))
                    If node IsNot Nothing AndAlso TypeOf node.Parent Is CompilationUnitSyntax Then
                        targets.Add(node)
                    End If
                Next reference
                If targets.Count = 0 Then
                    Continue For
                End If

                Dim newRoot As CompilationUnitSyntax = NamespaceDeclarationCodeFixProvider.WrapInNamespace(targetRoot, targets, namespaceName)
                Dim newDocument As Document = targetDocument.WithSyntaxRoot(newRoot)
                newDocument = Await Formatter.FormatAsync(newDocument, Formatter.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
                newDocument = Await UtilCodeFix.NormalizeLineEndingsAsync(targetDocument, newDocument, cancellationToken).ConfigureAwait(False)
                solution = newDocument.Project.Solution
            Next treeGroup

            Return solution
        End Function

        ''' <summary>
        ''' Builds a <see langword="Namespace"/> block containing the specified contiguous declarations.
        ''' </summary>
        '''
        ''' <param name="members">
        ''' The declarations to wrap, in source order.
        ''' </param>
        '''
        ''' <param name="namespaceName">
        ''' The namespace name.
        ''' </param>
        '''
        ''' <param name="eol">
        ''' The end-of-line trivia of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The namespace block, annotated for formatting.
        ''' </returns>
        Private Shared Function BuildNamespaceBlock(members As List(Of StatementSyntax), namespaceName As String, eol As SyntaxTrivia) As NamespaceBlockSyntax

            Dim head As SyntaxTriviaList = Nothing
            Dim tail As SyntaxTriviaList = Nothing
            UtilCodeFix.SplitLeadingTrivia(members(0).GetLeadingTrivia(), head, tail)

            Dim wrappedMembers As New List(Of StatementSyntax)(members)
            wrappedMembers(0) = members(0).WithLeadingTrivia(SyntaxFactory.TriviaList(eol).AddRange(tail))

            Dim lastIndex As Integer = wrappedMembers.Count - 1
            If Not wrappedMembers(lastIndex).GetTrailingTrivia().Any(SyntaxKind.EndOfLineTrivia) Then
                wrappedMembers(lastIndex) = wrappedMembers(lastIndex).WithTrailingTrivia(wrappedMembers(lastIndex).GetTrailingTrivia().Add(eol))
            End If

            Dim namespaceStatement As NamespaceStatementSyntax =
                SyntaxFactory.NamespaceStatement(SyntaxFactory.Token(SyntaxKind.NamespaceKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                                                 SyntaxFactory.ParseName(namespaceName)).
                              WithLeadingTrivia(head).
                              WithTrailingTrivia(eol)

            Dim endNamespaceStatement As EndBlockStatementSyntax =
                SyntaxFactory.EndNamespaceStatement(SyntaxFactory.Token(SyntaxKind.EndKeyword).WithTrailingTrivia(SyntaxFactory.Space),
                                                    SyntaxFactory.Token(SyntaxKind.NamespaceKeyword)).
                              WithLeadingTrivia(eol).
                              WithTrailingTrivia(eol)

            Return SyntaxFactory.NamespaceBlock(namespaceStatement, SyntaxFactory.List(wrappedMembers), endNamespaceStatement).
                                 WithAdditionalAnnotations(Formatter.Annotation)
        End Function

#End Region

    End Class

End Namespace

#End Region
