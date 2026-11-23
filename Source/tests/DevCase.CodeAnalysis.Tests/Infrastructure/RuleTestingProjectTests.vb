#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Threading.Tasks
Imports System.Xml.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " RuleTestingProjectTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of the rule testing solution (<c>RuleTesting</c> folder of the repository): every <c>DCAnnn.vb</c> file of its 
    ''' .NET Framework 4.8 and .NET 10 projects must compile and trigger the rule of its name, and no other rule.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The projects are compiled as Visual Studio compiles them: the options, imports, references, <c>MyType</c> and startup 
    ''' object of their project files, the default settings of the SDK for the .NET 10 project, and the options of the 
    ''' <c>.editorconfig</c> file of the solution.
    ''' </remarks>
    Public NotInheritable Class RuleTestingProjectTests

#Region " Constants "

        ''' <summary>
        ''' The name of the .NET Framework 4.8 project directory.
        ''' </summary>
        Private Const NetFrameworkProjectName As String = "DCA Rule Testing (net 48)"

        ''' <summary>
        ''' The name of the .NET 10 project directory.
        ''' </summary>
        Private Const ModernProjectName As String = "DCA Rule Testing (net 10)"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The rules that run only in .NET Framework projects, whose files are only in the .NET Framework project.
        ''' </summary>
        Private Shared ReadOnly NetFrameworkOnlyRules As ImmutableHashSet(Of String) = ImmutableHashSet.Create(StringComparer.Ordinal,
            DiagnosticIds.TypeOutsideNamespace, DiagnosticIds.ThrowHelperMigration, DiagnosticIds.NetFrameworkApiMigration,
            DiagnosticIds.ThrowInFinally, DiagnosticIds.ZeroLengthArrayAllocation, DiagnosticIds.AnyInsteadOfEmptinessCheck,
            DiagnosticIds.StringBuilderArgumentConvertedToString)

        ''' <summary>
        ''' The global imports that the SDK adds to Visual Basic projects (and, with Windows Forms, the Windows Desktop SDK).
        ''' </summary>
        Private Shared ReadOnly SdkImports As ImmutableArray(Of String) = ImmutableArray.Create(
            "Microsoft.VisualBasic", "System", "System.Collections", "System.Collections.Generic", "System.Data", "System.Diagnostics",
            "System.Linq", "System.Xml.Linq", "System.Threading.Tasks", "System.Drawing", "System.Windows.Forms")

        ''' <summary>
        ''' The conditional compilation symbols that the SDK defines for <c>net10.0-windows</c>.
        ''' </summary>
        Private Shared ReadOnly ModernSymbols As ImmutableArray(Of String) = ImmutableArray.Create(
            "NETCOREAPP", "NET", "NET10_0", "NETCOREAPP1_0_OR_GREATER", "NETCOREAPP2_0_OR_GREATER", "NETCOREAPP3_0_OR_GREATER",
            "NETCOREAPP3_1_OR_GREATER", "NET5_0_OR_GREATER", "NET6_0_OR_GREATER", "NET7_0_OR_GREATER", "NET8_0_OR_GREATER",
            "NET9_0_OR_GREATER", "NET10_0_OR_GREATER", "WINDOWS", "WINDOWS7_0_OR_GREATER")

#End Region

#Region " Enumerations "

        ''' <summary>
        ''' The preprocessor directives that <see cref="EveryCodeFix_KeepsTheProjectCompilingAndItsDirectives"/> adds to the 
        ''' <c>DCAnnn.vb</c> files before applying the code fixes.
        ''' </summary>
        Public Enum DirectiveDecoration

            ''' <summary>
            ''' The files are not modified.
            ''' </summary>
            None

            ''' <summary>
            ''' Every type and member is preceded by a complete region (<c>#Region " Extra "</c> / <c>#End Region</c>) and 
            ''' wrapped in another region (<c>#Region " Wrap "</c> / <c>#End Region</c>).
            ''' </summary>
            Regions

            ''' <summary>
            ''' Every type, member and statement is preceded by a block of disabled code (<c>#If False Then</c> / <c>#End If</c>) 
            ''' and wrapped in <c>#If True Then</c> / <c>#End If</c>.
            ''' </summary>
            Conditionals
        End Enum

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RuleTestingProjectTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that the .NET Framework project has a file for every rule, and the .NET 10 project a file for every rule 
        ''' that also runs on modern .NET.
        ''' </summary>
        <Fact>
        Public Sub EveryRule_HasItsFile()
            Dim ruleIds As List(Of String) = GetType(DiagnosticIds).GetFields(BindingFlags.Public Or BindingFlags.Static).
                Select(Function(f As FieldInfo) CStr(f.GetValue(Nothing))).OrderBy(Function(id As String) id, StringComparer.Ordinal).ToList()
            Dim directory As String = RuleTestingProjectTests.FindRuleTestingDirectory()

            Dim netFrameworkFiles As List(Of String) = RuleTestingProjectTests.GetRuleFileIds(Path.Combine(directory, RuleTestingProjectTests.NetFrameworkProjectName))
            Assert.Equal(ruleIds, netFrameworkFiles, StringComparer.Ordinal)

            Dim modernFiles As List(Of String) = RuleTestingProjectTests.GetRuleFileIds(Path.Combine(directory, RuleTestingProjectTests.ModernProjectName))
            Assert.Equal(ruleIds.Where(Function(id As String) Not RuleTestingProjectTests.NetFrameworkOnlyRules.Contains(id)), modernFiles, StringComparer.Ordinal)
        End Sub

        ''' <summary>
        ''' Verifies that the .NET Framework 4.8 project compiles without warnings, and that every <c>DCAnnn.vb</c> file triggers 
        ''' the rule of its name and no other rule (the other files trigger none).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NetFrameworkProject_TriggersOnlyTheRuleOfEachFile() As Task
            Dim compilation As Compilation = RuleTestingProjectTests.CreateNetFrameworkCompilation()
            Await RuleTestingProjectTests.VerifyAsync(compilation, RuleTestingProjectTests.NetFrameworkProjectName).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the .NET 10 project compiles without warnings, and that every <c>DCAnnn.vb</c> file triggers the rule 
        ''' of its name and no other rule (the other files trigger none).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function ModernProject_TriggersOnlyTheRuleOfEachFile() As Task
            Dim compilation As Compilation = RuleTestingProjectTests.CreateModernCompilation()
            Await RuleTestingProjectTests.VerifyAsync(compilation, RuleTestingProjectTests.ModernProjectName).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that every code fix, applied with each of its actions to the <c>DCAnnn.vb</c> file of its rule (as a developer 
        ''' does with <c>Ctrl+.</c>), keeps the project compiling without warnings, never removes a preprocessor directive 
        ''' (<c>#Region</c>, <c>#End Region</c>, <c>#If</c>...), keeps the regions balanced, and fixes the diagnostics of its rule.
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> for the .NET Framework 4.8 project; <see langword="False"/> for the .NET 10 project.
        ''' </param>
        '''
        ''' <param name="decoration">
        ''' The preprocessor directives added to the <c>DCAnnn.vb</c> files before applying the code fixes.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(True, DirectiveDecoration.None)>
        <InlineData(False, DirectiveDecoration.None)>
        <InlineData(True, DirectiveDecoration.Regions)>
        <InlineData(False, DirectiveDecoration.Regions)>
        <InlineData(True, DirectiveDecoration.Conditionals)>
        <InlineData(False, DirectiveDecoration.Conditionals)>
        Public Async Function EveryCodeFix_KeepsTheProjectCompilingAndItsDirectives(targetNetFramework As Boolean, decoration As DirectiveDecoration) As Task
            Dim compilation As Compilation = If(targetNetFramework, RuleTestingProjectTests.CreateNetFrameworkCompilation(), RuleTestingProjectTests.CreateModernCompilation())
            Dim project As Project = Await RuleTestingProjectTests.DecorateAsync(RuleTestingProjectTests.CreateProject(compilation), decoration).ConfigureAwait(True)

            ' The decoration only adds whole lines of directives (the active code is the same), so the project must still compile.
            Dim decoratedCompilation As Compilation = Await project.GetCompilationAsync().ConfigureAwait(True)
            Dim decorationErrors As List(Of String) = decoratedCompilation.GetDiagnostics().
                Where(Function(d As Diagnostic) d.Severity >= DiagnosticSeverity.Warning AndAlso Not d.IsSuppressed).
                Select(Function(d As Diagnostic) d.ToString()).ToList()
            Assert.True(decorationErrors.Count = 0, $"The decorated project does not compile:{Environment.NewLine}{String.Join(Environment.NewLine, decorationErrors)}")
            Dim providers As List(Of CodeFixProvider) = GetType(SenderParameterCodeFixProvider).Assembly.GetTypes().
                Where(Function(t As Type) Not t.IsAbstract AndAlso GetType(CodeFixProvider).IsAssignableFrom(t)).
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), CodeFixProvider)).ToList()

            Dim problems As New List(Of String)()
            Dim appliedActions As Integer = 0
            For Each document As Document In project.Documents.Where(Function(d As Document) RuleTestingProjectTests.IsRuleFileName(Path.GetFileNameWithoutExtension(d.Name))).ToList()
                Dim ruleId As String = Path.GetFileNameWithoutExtension(document.Name)
                Dim provider As CodeFixProvider = providers.SingleOrDefault(Function(p As CodeFixProvider) p.FixableDiagnosticIds.Contains(ruleId, StringComparer.Ordinal))
                If provider Is Nothing Then
                    Continue For ' A rule without a code fix.
                End If

                Dim analyzer As DiagnosticAnalyzer = RuleTestingProjectTests.CreateAnalyzer(ruleId)
                Dim diagnostics As ImmutableArray(Of Diagnostic) = Await RuleTestingProjectTests.GetDocumentDiagnosticsAsync(document, analyzer).ConfigureAwait(True)
                If diagnostics.IsEmpty Then
                    ' DCA011, DCA013 and DCA024 skip conditional code on purpose (the #If blocks added by the decoration).
                    If decoration = DirectiveDecoration.None Then
                        problems.Add($"{document.Name}: the rule reports nothing.")
                    End If
                    Continue For
                End If
                Dim keys As List(Of String) = Await RuleTestingProjectTests.GetEquivalenceKeysAsync(document, provider, diagnostics).ConfigureAwait(True)
                If keys.Count = 0 Then
                    problems.Add($"{document.Name}: no code action is offered for {diagnostics.Length} diagnostic(s).")
                    Continue For
                End If

                For Each key As String In keys
                    Dim fixAllContext As New FixAllContext(document, provider, FixAllScope.Document, key, provider.FixableDiagnosticIds,
                                                           New TestFixAllDiagnosticProvider(diagnostics), Threading.CancellationToken.None)
                    Dim action As CodeAction = Await provider.GetFixAllProvider().GetFixAsync(fixAllContext).ConfigureAwait(True)
                    Dim operations As ImmutableArray(Of CodeActionOperation) = Await action.GetOperationsAsync(Threading.CancellationToken.None).ConfigureAwait(True)
                    Dim fixedDocument As Document = operations.OfType(Of ApplyChangesOperation)().Single().ChangedSolution.GetDocument(document.Id)
                    appliedActions += 1
                    problems.AddRange((Await RuleTestingProjectTests.VerifyFixedDocumentAsync(document, fixedDocument, analyzer, diagnostics.Length).ConfigureAwait(True)).
                                      Select(Function(p As String) $"{document.Name} [{key}]: {p}"))
                Next key
            Next document

            Assert.True(appliedActions > 30, $"Only {appliedActions} code actions were applied.")
            Assert.True(problems.Count = 0, String.Join(Environment.NewLine, problems))
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Verifies a document fixed by a code action: the project compiles without warnings, every preprocessor directive of the 
        ''' original document is still there, the regions are balanced, and the rule reports fewer diagnostics.
        ''' </summary>
        '''
        ''' <param name="original">
        ''' The original document.
        ''' </param>
        '''
        ''' <param name="fixedDocument">
        ''' The fixed document.
        ''' </param>
        '''
        ''' <param name="analyzer">
        ''' The analyzer of the fixed rule.
        ''' </param>
        '''
        ''' <param name="originalCount">
        ''' The number of diagnostics of the rule in the original document.
        ''' </param>
        '''
        ''' <returns>
        ''' The problems found (empty when the fix is correct).
        ''' </returns>
        Private Shared Async Function VerifyFixedDocumentAsync(original As Document, fixedDocument As Document, analyzer As DiagnosticAnalyzer,
                                                               originalCount As Integer) As Task(Of List(Of String))

            Dim problems As New List(Of String)()
            Dim fixedCompilation As Compilation = Await fixedDocument.Project.GetCompilationAsync().ConfigureAwait(True)
            problems.AddRange(fixedCompilation.GetDiagnostics().
                Where(Function(d As Diagnostic) d.Severity >= DiagnosticSeverity.Warning AndAlso Not d.IsSuppressed).
                Select(Function(d As Diagnostic) $"does not compile: {d}"))

            ' A directive can only disappear with the body of a removed member (for example, the #If blocks inside the body of a 
            ' removed constructor); the directives of its leading trivia (#Region, #End Region...) must stay.
            Dim originalRoot As SyntaxNode = Await original.GetSyntaxRootAsync().ConfigureAwait(True)
            Dim fixedRoot As SyntaxNode = Await fixedDocument.GetSyntaxRootAsync().ConfigureAwait(True)
            Dim removedSpans As List(Of TextSpan) = RuleTestingProjectTests.GetRemovedMemberSpans(originalRoot, fixedRoot)
            Dim fixedDirectives As List(Of String) = RuleTestingProjectTests.GetDirectives(fixedRoot).Select(Function(d As SyntaxTrivia) RuleTestingProjectTests.NormalizeDirective(d)).ToList()
            For Each directive As IGrouping(Of String, SyntaxTrivia) In RuleTestingProjectTests.GetDirectives(originalRoot).GroupBy(Function(d As SyntaxTrivia) RuleTestingProjectTests.NormalizeDirective(d), StringComparer.Ordinal)
                Dim removable As Integer = directive.Where(Function(d As SyntaxTrivia) removedSpans.Exists(Function(span As TextSpan) span.Contains(d.Span))).Count()
                Dim remaining As Integer = fixedDirectives.Where(Function(d As String) d.Equals(directive.Key, StringComparison.Ordinal)).Count()
                If remaining < directive.Count() - removable Then
                    problems.Add($"the directive '{directive.Key}' was removed ({directive.Count()} before, {remaining} after, {removable} inside removed members).")
                End If
            Next directive

            Dim regions As Integer = fixedDirectives.Where(Function(d As String) d.StartsWith("#Region", StringComparison.Ordinal)).Count()
            Dim endRegions As Integer = fixedDirectives.Where(Function(d As String) d.Equals("#End Region", StringComparison.Ordinal)).Count()
            If regions <> endRegions Then
                problems.Add($"the regions are not balanced ({regions} #Region, {endRegions} #End Region).")
            End If

            Dim remainingDiagnostics As ImmutableArray(Of Diagnostic) = Await RuleTestingProjectTests.GetDocumentDiagnosticsAsync(fixedDocument, analyzer).ConfigureAwait(True)
            If remainingDiagnostics.Length >= originalCount Then
                problems.Add($"the rule still reports {remainingDiagnostics.Length} diagnostic(s).")
            End If
            Return problems
        End Function

        ''' <summary>
        ''' Adds preprocessor directives around the types, members and statements of the <c>DCAnnn.vb</c> documents of a project.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Only whole lines are added, before the first line of an element (after the directives of its leading trivia, so that its 
        ''' XML documentation stays attached to it) and after its last line: the active code does not change.
        ''' </remarks>
        '''
        ''' <param name="project">
        ''' The project.
        ''' </param>
        '''
        ''' <param name="decoration">
        ''' The directives to add.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task(Of Project)"/> whose result is the project with the modified documents.
        ''' </returns>
        Private Shared Async Function DecorateAsync(project As Project, decoration As DirectiveDecoration) As Task(Of Project)

            If decoration = DirectiveDecoration.None Then
                Return project
            End If

            Dim result As Project = project
            For Each documentId As DocumentId In project.DocumentIds
                Dim document As Document = result.GetDocument(documentId)
                If Not RuleTestingProjectTests.IsRuleFileName(Path.GetFileNameWithoutExtension(document.Name)) Then
                    Continue For
                End If

                Dim text As SourceText = Await document.GetTextAsync().ConfigureAwait(True)
                Dim root As SyntaxNode = Await document.GetSyntaxRootAsync().ConfigureAwait(True)
                Dim eol As String = If(text.ToString().Contains(vbCrLf, StringComparison.Ordinal), vbCrLf, vbLf)

                ' The lines that close an element and the lines that open the next one, by position.
                Dim closings As New SortedDictionary(Of Integer, StringBuilder)()
                Dim openings As New SortedDictionary(Of Integer, StringBuilder)()
                Dim members As New HashSet(Of StatementSyntax)(
                    root.DescendantNodesAndSelf().SelectMany(Function(n As SyntaxNode) RuleTestingProjectTests.GetMembers(n)))
                Dim statements As IEnumerable(Of StatementSyntax) = If(decoration = DirectiveDecoration.Conditionals,
                                                                       root.DescendantNodes().OfType(Of ExecutableStatementSyntax)().Cast(Of StatementSyntax)(),
                                                                       Enumerable.Empty(Of StatementSyntax)())
                For Each element As StatementSyntax In members.Concat(statements).OrderBy(Function(e As StatementSyntax) e.SpanStart)
                    Dim firstLine As TextLine = Nothing
                    Dim lastLine As TextLine = Nothing
                    If Not RuleTestingProjectTests.TryGetWholeLines(text, element, firstLine, lastLine) Then
                        Continue For
                    End If

                    Dim indentation As String = text.ToString(TextSpan.FromBounds(firstLine.Start, RuleTestingProjectTests.GetContentStart(element)))
                    Dim opening As String
                    Dim closing As String
                    If decoration = DirectiveDecoration.Regions Then
                        opening = $"#Region "" Extra ""{eol}#End Region{eol}#Region "" Wrap ""{eol}"
                        closing = $"#End Region{eol}"
                    Else
                        Dim disabled As String = If(members.Contains(element), "Private Disabled As Integer", "Disabled()")
                        opening = $"#If False Then{eol}{indentation}{disabled}{eol}#End If{eol}#If True Then{eol}"
                        closing = $"#End If{eol}"
                    End If
                    RuleTestingProjectTests.AppendAt(openings, firstLine.Start, opening, atStart:=False)
                    RuleTestingProjectTests.AppendAt(closings, lastLine.EndIncludingLineBreak, closing, atStart:=True)
                Next element

                ' At the same position, the lines that close the previous element go before the lines that open the next one.
                Dim changes As List(Of TextChange) = closings.Keys.Union(openings.Keys).OrderBy(Function(position As Integer) position).
                    Select(Function(position As Integer) New TextChange(New TextSpan(position, 0), $"{RuleTestingProjectTests.GetInsertion(closings, position)}{RuleTestingProjectTests.GetInsertion(openings, position)}")).
                    ToList()
                result = document.WithText(text.WithChanges(changes)).Project
            Next documentId
            Return result
        End Function

        ''' <summary>
        ''' Gets the members (types, methods, fields, enumeration members...) declared directly in a node; for a compilation 
        ''' unit, also its <c>Option</c>, <c>Imports</c> and assembly attribute statements.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node: a compilation unit, a namespace, a type or an enumeration.
        ''' </param>
        '''
        ''' <returns>
        ''' The members, or an empty sequence for any other node.
        ''' </returns>
        Private Shared Function GetMembers(node As SyntaxNode) As IEnumerable(Of StatementSyntax)

            Dim compilationUnit As CompilationUnitSyntax = TryCast(node, CompilationUnitSyntax)
            If compilationUnit IsNot Nothing Then
                Return compilationUnit.Options.Cast(Of StatementSyntax)().
                    Concat(compilationUnit.Imports).
                    Concat(compilationUnit.Attributes).
                    Concat(compilationUnit.Members)
            End If
            Dim namespaceBlock As NamespaceBlockSyntax = TryCast(node, NamespaceBlockSyntax)
            If namespaceBlock IsNot Nothing Then
                Return namespaceBlock.Members
            End If
            Dim typeBlock As TypeBlockSyntax = TryCast(node, TypeBlockSyntax)
            If typeBlock IsNot Nothing Then
                Return typeBlock.Members
            End If
            Dim enumBlock As EnumBlockSyntax = TryCast(node, EnumBlockSyntax)
            Return If(enumBlock IsNot Nothing, enumBlock.Members, Enumerable.Empty(Of StatementSyntax)())
        End Function

        ''' <summary>
        ''' Gets the position where the own text of an element begins: its XML documentation, comment, attribute or first keyword, 
        ''' after the preprocessor directives of its leading trivia.
        ''' </summary>
        '''
        ''' <param name="element">
        ''' The type, member or statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The position.
        ''' </returns>
        Private Shared Function GetContentStart(element As SyntaxNode) As Integer

            Dim leading As SyntaxTriviaList = element.GetLeadingTrivia()
            Dim afterDirectives As Integer = 0
            For index As Integer = 0 To leading.Count - 1
                If leading(index).IsDirective OrElse leading(index).IsKind(SyntaxKind.DisabledTextTrivia) Then
                    afterDirectives = index + 1
                End If
            Next index
            Dim content As SyntaxTrivia = leading.Skip(afterDirectives).
                FirstOrDefault(Function(t As SyntaxTrivia) Not t.IsKind(SyntaxKind.WhitespaceTrivia) AndAlso Not t.IsKind(SyntaxKind.EndOfLineTrivia))
            ' FullSpan: the span of an XML documentation comment does not include its first "'''".
            Return If(content.RawKind = 0, element.SpanStart, content.FullSpan.Start)
        End Function

        ''' <summary>
        ''' Gets the lines of an element, if it occupies whole lines: nothing but indentation before its own text, and nothing but 
        ''' a comment after its last token.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text of the document.
        ''' </param>
        '''
        ''' <param name="element">
        ''' The type, member or statement.
        ''' </param>
        '''
        ''' <param name="firstLine">
        ''' When this method returns <see langword="True"/>, the line where the own text of the element begins.
        ''' </param>
        '''
        ''' <param name="lastLine">
        ''' When this method returns <see langword="True"/>, the last line of the element (it has a line break).
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the element occupies whole lines; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function TryGetWholeLines(text As SourceText, element As SyntaxNode, ByRef firstLine As TextLine, ByRef lastLine As TextLine) As Boolean

            Dim contentStart As Integer = RuleTestingProjectTests.GetContentStart(element)
            Dim lastToken As SyntaxToken = element.GetLastToken()
            firstLine = text.Lines.GetLineFromPosition(contentStart)
            lastLine = text.Lines.GetLineFromPosition(lastToken.Span.End)
            Dim before As String = text.ToString(TextSpan.FromBounds(firstLine.Start, contentStart))
            Dim after As String = text.ToString(TextSpan.FromBounds(lastToken.Span.End, lastLine.End)).Trim()
            Return before.Trim().Length = 0 AndAlso
                   (after.Length = 0 OrElse after.StartsWith("'", StringComparison.Ordinal)) AndAlso
                   lastLine.EndIncludingLineBreak > lastLine.End AndAlso
                   Not element.Ancestors().Any(Function(a As SyntaxNode) TypeOf a Is SingleLineIfStatementSyntax OrElse TypeOf a Is SingleLineLambdaExpressionSyntax)
        End Function

        ''' <summary>
        ''' Adds text to the insertions of a position.
        ''' </summary>
        '''
        ''' <param name="insertions">
        ''' The insertions, by position.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position.
        ''' </param>
        '''
        ''' <param name="value">
        ''' The text to add.
        ''' </param>
        '''
        ''' <param name="atStart">
        ''' <see langword="True"/> to add the text before the text already there (closing lines: the inner element closes first); 
        ''' <see langword="False"/> to add it after (opening lines: the outer element opens first).
        ''' </param>
        Private Shared Sub AppendAt(insertions As SortedDictionary(Of Integer, StringBuilder), position As Integer, value As String, atStart As Boolean)

            Dim builder As StringBuilder = Nothing
            If Not insertions.TryGetValue(position, builder) Then
                builder = New StringBuilder()
                insertions.Add(position, builder)
            End If
            If atStart Then
                builder.Insert(0, value)
            Else
                builder.Append(value)
            End If
        End Sub

        ''' <summary>
        ''' Gets the text to insert at a position.
        ''' </summary>
        '''
        ''' <param name="insertions">
        ''' The insertions, by position.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position.
        ''' </param>
        '''
        ''' <returns>
        ''' The text, or an empty string if there is nothing to insert at <paramref name="position"/>.
        ''' </returns>
        Private Shared Function GetInsertion(insertions As SortedDictionary(Of Integer, StringBuilder), position As Integer) As String

            Dim builder As StringBuilder = Nothing
            Return If(insertions.TryGetValue(position, builder), builder.ToString(), String.Empty)
        End Function

        ''' <summary>
        ''' Gets the preprocessor directives of a syntax tree.
        ''' </summary>
        '''
        ''' <param name="root">
        ''' The syntax root.
        ''' </param>
        '''
        ''' <returns>
        ''' The directives, in document order.
        ''' </returns>
        Private Shared Function GetDirectives(root As SyntaxNode) As List(Of SyntaxTrivia)

            Return root.DescendantTrivia(descendIntoTrivia:=True).Where(Function(t As SyntaxTrivia) t.IsDirective).ToList()
        End Function

        ''' <summary>
        ''' Gets the normalized text of a preprocessor directive (single spaces, no comments).
        ''' </summary>
        '''
        ''' <param name="directive">
        ''' The directive.
        ''' </param>
        '''
        ''' <returns>
        ''' The text: <c>#Region " Constructors "</c>, <c>#End Region</c>, <c>#If DEBUG Then</c>...
        ''' </returns>
        Private Shared Function NormalizeDirective(directive As SyntaxTrivia) As String

            Return $"#{String.Join(" ", directive.GetStructure().DescendantTokens().Skip(1).
                                        Select(Function(token As SyntaxToken) token.ToString()).
                                        Where(Function(text As String) text.Length <> 0))}"
        End Function

        ''' <summary>
        ''' Gets the spans of the members of an original syntax tree that a code fix removed: those whose first line (with the first 
        ''' lines of the types and namespaces that contain them) appears fewer times in the fixed tree.
        ''' </summary>
        '''
        ''' <param name="originalRoot">
        ''' The syntax root of the original document.
        ''' </param>
        '''
        ''' <param name="fixedRoot">
        ''' The syntax root of the fixed document.
        ''' </param>
        '''
        ''' <returns>
        ''' The spans (without the leading trivia) of the removed members.
        ''' </returns>
        Private Shared Function GetRemovedMemberSpans(originalRoot As SyntaxNode, fixedRoot As SyntaxNode) As List(Of TextSpan)

            Dim originalMembers As List(Of StatementSyntax) = originalRoot.DescendantNodesAndSelf().SelectMany(Function(n As SyntaxNode) RuleTestingProjectTests.GetMembers(n)).ToList()
            Dim fixedKeys As List(Of String) = fixedRoot.DescendantNodesAndSelf().SelectMany(Function(n As SyntaxNode) RuleTestingProjectTests.GetMembers(n)).
                Select(Function(m As StatementSyntax) RuleTestingProjectTests.GetMemberKey(m)).ToList()
            Dim originalKeys As List(Of String) = originalMembers.Select(Function(m As StatementSyntax) RuleTestingProjectTests.GetMemberKey(m)).ToList()

            Return originalMembers.
                Where(Function(m As StatementSyntax)
                          Dim key As String = RuleTestingProjectTests.GetMemberKey(m)
                          Return fixedKeys.Where(Function(k As String) k.Equals(key, StringComparison.Ordinal)).Count() <
                                 originalKeys.Where(Function(k As String) k.Equals(key, StringComparison.Ordinal)).Count()
                      End Function).
                Select(Function(m As StatementSyntax) m.Span).
                ToList()
        End Function

        ''' <summary>
        ''' Gets a key that identifies a member: its first line, with the first lines of the types and namespaces that contain it.
        ''' </summary>
        '''
        ''' <param name="member">
        ''' The member.
        ''' </param>
        '''
        ''' <returns>
        ''' The key.
        ''' </returns>
        Private Shared Function GetMemberKey(member As StatementSyntax) As String

            Return String.Join(" > ", member.AncestorsAndSelf().
                Where(Function(n As SyntaxNode) n Is member OrElse TypeOf n Is TypeBlockSyntax OrElse TypeOf n Is NamespaceBlockSyntax OrElse TypeOf n Is EnumBlockSyntax).
                Reverse().
                Select(Function(n As SyntaxNode)
                           Dim text As SourceText = n.SyntaxTree.GetText()
                           Dim line As TextLine = text.Lines.GetLineFromPosition(n.SpanStart)
                           Return text.ToString(TextSpan.FromBounds(n.SpanStart, line.End)).Trim()
                       End Function))
        End Function

        ''' <summary>
        ''' Gets the equivalence keys of the code actions that a provider offers for the diagnostics of a document.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider.
        ''' </param>
        '''
        ''' <param name="diagnostics">
        ''' The diagnostics of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The distinct equivalence keys.
        ''' </returns>
        Private Shared Async Function GetEquivalenceKeysAsync(document As Document, provider As CodeFixProvider,
                                                              diagnostics As ImmutableArray(Of Diagnostic)) As Task(Of List(Of String))

            Dim actions As New List(Of CodeAction)()
            For Each diagnostic As Diagnostic In diagnostics
                Dim context As New CodeFixContext(document, diagnostic, Sub(a As CodeAction, d As ImmutableArray(Of Diagnostic)) actions.Add(a),
                                                  Threading.CancellationToken.None)
                Await provider.RegisterCodeFixesAsync(context).ConfigureAwait(True)
            Next diagnostic
            Return actions.Select(Function(a As CodeAction) a.EquivalenceKey).Where(Function(k As String) k IsNot Nothing).
                Distinct(StringComparer.Ordinal).ToList()
        End Function

        ''' <summary>
        ''' Gets the diagnostics that an analyzer reports in a document, with the options of the <c>.editorconfig</c> file of the solution.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document.
        ''' </param>
        '''
        ''' <param name="analyzer">
        ''' The analyzer.
        ''' </param>
        '''
        ''' <returns>
        ''' The diagnostics located in the document.
        ''' </returns>
        Private Shared Async Function GetDocumentDiagnosticsAsync(document As Document, analyzer As DiagnosticAnalyzer) As Task(Of ImmutableArray(Of Diagnostic))

            Dim compilation As Compilation = Await document.Project.GetCompilationAsync().ConfigureAwait(True)
            Dim tree As SyntaxTree = Await document.GetSyntaxTreeAsync().ConfigureAwait(True)
            Dim options As New AnalyzerOptions(ImmutableArray(Of AdditionalText).Empty,
                                               New EditorConfigOptionsProvider(RuleTestingProjectTests.ReadEditorConfigOptions()))
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await compilation.WithAnalyzers(ImmutableArray.Create(analyzer), options).
                GetAnalyzerDiagnosticsAsync().ConfigureAwait(True)
            Return diagnostics.Where(Function(d As Diagnostic) d.Location.SourceTree Is tree).ToImmutableArray()
        End Function

        ''' <summary>
        ''' Creates the analyzer of a rule.
        ''' </summary>
        '''
        ''' <param name="ruleId">
        ''' The rule identifier.
        ''' </param>
        '''
        ''' <returns>
        ''' The analyzer.
        ''' </returns>
        Private Shared Function CreateAnalyzer(ruleId As String) As DiagnosticAnalyzer

            Return GetType(DiagnosticIds).Assembly.GetTypes().
                Where(Function(t As Type) Not t.IsAbstract AndAlso GetType(DiagnosticAnalyzer).IsAssignableFrom(t)).
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), DiagnosticAnalyzer)).
                Single(Function(a As DiagnosticAnalyzer) a.SupportedDiagnostics(0).Id.Equals(ruleId, StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' Creates a workspace project with the documents, options and references of a compilation.
        ''' </summary>
        '''
        ''' <param name="compilation">
        ''' The compilation of a project of the rule testing solution.
        ''' </param>
        '''
        ''' <returns>
        ''' The project.
        ''' </returns>
        Private Shared Function CreateProject(compilation As Compilation) As Project

            Dim workspace As New AdhocWorkspace()
            Dim info As ProjectInfo = ProjectInfo.Create(ProjectId.CreateNewId(), VersionStamp.Create(), "RuleTesting", "RuleTesting", LanguageNames.VisualBasic,
                                                         compilationOptions:=compilation.Options, parseOptions:=compilation.SyntaxTrees(0).Options,
                                                         metadataReferences:=compilation.References)
            Dim project As Project = workspace.AddProject(info)
            For Each tree As SyntaxTree In compilation.SyntaxTrees
                project = project.AddDocument(Path.GetFileName(tree.FilePath), tree.GetText(), filePath:=tree.FilePath).Project
            Next tree
            Return project
        End Function

        ''' <summary>
        ''' Runs every rule on a project of the rule testing solution and verifies its diagnostics.
        ''' </summary>
        '''
        ''' <param name="compilation">
        ''' The compilation of the project.
        ''' </param>
        '''
        ''' <param name="projectName">
        ''' The name of the project, used in the assertion messages.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous verification.
        ''' </returns>
        Private Shared Async Function VerifyAsync(compilation As Compilation, projectName As String) As Task

            Dim compilerDiagnostics As List(Of Diagnostic) = compilation.GetDiagnostics().
                Where(Function(d As Diagnostic) d.Severity >= DiagnosticSeverity.Warning AndAlso Not d.IsSuppressed).ToList()
            Assert.True(compilerDiagnostics.Count = 0, $"{projectName}: {String.Join(Environment.NewLine, compilerDiagnostics)}")

            Dim analyzers As ImmutableArray(Of DiagnosticAnalyzer) = GetType(DiagnosticIds).Assembly.GetTypes().
                Where(Function(t As Type) Not t.IsAbstract AndAlso GetType(DiagnosticAnalyzer).IsAssignableFrom(t)).
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), DiagnosticAnalyzer)).
                ToImmutableArray()
            Dim options As New AnalyzerOptions(ImmutableArray(Of AdditionalText).Empty,
                                               New EditorConfigOptionsProvider(RuleTestingProjectTests.ReadEditorConfigOptions()))
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await compilation.WithAnalyzers(analyzers, options).
                GetAnalyzerDiagnosticsAsync().ConfigureAwait(True)

            Dim problems As New List(Of String)()
            For Each tree As SyntaxTree In compilation.SyntaxTrees
                Dim fileName As String = Path.GetFileNameWithoutExtension(tree.FilePath)
                Dim ids As List(Of String) = diagnostics.Where(Function(d As Diagnostic) d.Location.SourceTree Is tree).
                    Select(Function(d As Diagnostic) d.Id).Distinct(StringComparer.Ordinal).OrderBy(Function(id As String) id, StringComparer.Ordinal).ToList()
                Dim expected As List(Of String) = If(RuleTestingProjectTests.IsRuleFileName(fileName), New List(Of String) From {fileName}, New List(Of String)())
                If Not ids.SequenceEqual(expected, StringComparer.Ordinal) Then
                    problems.Add($"{Path.GetFileName(tree.FilePath)}: expected [{String.Join(", ", expected)}], reported [{String.Join(", ", ids)}]")
                End If
            Next tree

            Dim outsideFiles As List(Of Diagnostic) = diagnostics.Where(Function(d As Diagnostic) d.Location.SourceTree Is Nothing).ToList()
            problems.AddRange(outsideFiles.Select(Function(d As Diagnostic) $"(no file): {d}"))
            Assert.True(problems.Count = 0, $"{projectName}:{Environment.NewLine}{String.Join(Environment.NewLine, problems)}")
        End Function

        ''' <summary>
        ''' Creates the compilation of the .NET Framework 4.8 project from its project file.
        ''' </summary>
        '''
        ''' <returns>
        ''' The compilation.
        ''' </returns>
        Private Shared Function CreateNetFrameworkCompilation() As Compilation

            Dim projectDirectory As String = Path.Combine(RuleTestingProjectTests.FindRuleTestingDirectory(), RuleTestingProjectTests.NetFrameworkProjectName)
            Dim project As XDocument = XDocument.Load(Directory.EnumerateFiles(projectDirectory, "*.vbproj").Single())
            Dim ns As XNamespace = project.Root.Name.Namespace
            Dim referenceDirectory As String = RuleTestingProjectTests.GetMetadataPath("Net48ReferenceAssembliesPath")

            Dim files As IEnumerable(Of String) = project.Descendants(ns + "Compile").
                Select(Function(e As XElement) Path.Combine(projectDirectory, e.Attribute("Include").Value.Replace("\"c, Path.DirectorySeparatorChar)))
            Dim references As IEnumerable(Of String) = {"mscorlib", "Microsoft.VisualBasic"}.
                Concat(project.Descendants(ns + "Reference").Select(Function(e As XElement) e.Attribute("Include").Value.Split(","c)(0))).
                Select(Function(name As String) Path.Combine(referenceDirectory, $"{name}.dll"))
            Dim globalImports As IEnumerable(Of String) = project.Descendants(ns + "Import").
                Where(Function(e As XElement) e.Parent.Name.LocalName.Equals("ItemGroup", StringComparison.Ordinal)).
                Select(Function(e As XElement) e.Attribute("Include").Value)

            Dim symbols As New List(Of KeyValuePair(Of String, Object)) From {
                New KeyValuePair(Of String, Object)("CONFIG", "Debug"), New KeyValuePair(Of String, Object)("DEBUG", -1),
                New KeyValuePair(Of String, Object)("TRACE", -1), New KeyValuePair(Of String, Object)("PLATFORM", "AnyCPU"),
                New KeyValuePair(Of String, Object)("_MyType", RuleTestingProjectTests.GetProperty(project, "MyType"))}
            Return RuleTestingProjectTests.CreateCompilation(files, references, globalImports, symbols,
                                                             RuleTestingProjectTests.GetProperty(project, "OptionStrict"),
                                                             RuleTestingProjectTests.GetProperty(project, "OptionInfer"),
                                                             RuleTestingProjectTests.GetProperty(project, "StartupObject"))
        End Function

        ''' <summary>
        ''' Creates the compilation of the .NET 10 project from its project file and the defaults of the SDK.
        ''' </summary>
        '''
        ''' <returns>
        ''' The compilation.
        ''' </returns>
        Private Shared Function CreateModernCompilation() As Compilation

            Dim projectDirectory As String = Path.Combine(RuleTestingProjectTests.FindRuleTestingDirectory(), RuleTestingProjectTests.ModernProjectName)
            Dim project As XDocument = XDocument.Load(Directory.EnumerateFiles(projectDirectory, "*.vbproj").Single())

            ' SDK projects compile every .vb file of the directory, except the build output.
            Dim files As IEnumerable(Of String) = Directory.EnumerateFiles(projectDirectory, "*.vb", SearchOption.AllDirectories).
                Where(Function(p As String) Not p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal) AndAlso
                                            Not p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))

            ' The framework assemblies of the test runtime, and the Windows Forms assemblies of the Windows Desktop reference pack.
            Dim runtime As List(Of String) = CStr(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator).
                Where(Function(p As String) Path.GetFileName(p).StartsWith("System.", StringComparison.OrdinalIgnoreCase) OrElse
                                            Path.GetFileName(p).StartsWith("Microsoft.VisualBasic", StringComparison.OrdinalIgnoreCase) OrElse
                                            Path.GetFileName(p).Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase) OrElse
                                            Path.GetFileName(p).Equals("netstandard.dll", StringComparison.OrdinalIgnoreCase)).ToList()
            Dim runtimeNames As HashSet(Of String) = runtime.Select(Function(p As String) Path.GetFileName(p)).ToHashSet(StringComparer.OrdinalIgnoreCase)
            Dim desktop As IEnumerable(Of String) = Directory.EnumerateFiles(RuleTestingProjectTests.GetMetadataPath("WindowsDesktopReferenceAssembliesPath"), "*.dll").
                Where(Function(p As String) Not runtimeNames.Contains(Path.GetFileName(p)))

            Dim symbols As List(Of KeyValuePair(Of String, Object)) = RuleTestingProjectTests.ModernSymbols.
                Select(Function(s As String) New KeyValuePair(Of String, Object)(s, -1)).ToList()
            symbols.Add(New KeyValuePair(Of String, Object)("CONFIG", "Debug"))
            symbols.Add(New KeyValuePair(Of String, Object)("DEBUG", -1))
            symbols.Add(New KeyValuePair(Of String, Object)("TRACE", -1))
            symbols.Add(New KeyValuePair(Of String, Object)("PLATFORM", "AnyCPU"))
            symbols.Add(New KeyValuePair(Of String, Object)("_MyType", RuleTestingProjectTests.GetProperty(project, "MyType")))

            ' The defaults of the SDK: Option Strict Off and Option Infer On, unless the project file sets them.
            Return RuleTestingProjectTests.CreateCompilation(files, runtime.Concat(desktop), RuleTestingProjectTests.SdkImports, symbols,
                                                             If(RuleTestingProjectTests.GetProperty(project, "OptionStrict"), "Off"),
                                                             If(RuleTestingProjectTests.GetProperty(project, "OptionInfer"), "On"),
                                                             RuleTestingProjectTests.GetProperty(project, "StartupObject"))
        End Function

        ''' <summary>
        ''' Creates the compilation of a console application.
        ''' </summary>
        '''
        ''' <param name="files">
        ''' The source files.
        ''' </param>
        '''
        ''' <param name="references">
        ''' The paths of the referenced assemblies.
        ''' </param>
        '''
        ''' <param name="globalImports">
        ''' The project-level imports.
        ''' </param>
        '''
        ''' <param name="symbols">
        ''' The conditional compilation symbols.
        ''' </param>
        '''
        ''' <param name="strictValue">
        ''' The value of <c>OptionStrict</c> (<c>On</c> or <c>Off</c>).
        ''' </param>
        '''
        ''' <param name="inferValue">
        ''' The value of <c>OptionInfer</c> (<c>On</c> or <c>Off</c>).
        ''' </param>
        '''
        ''' <param name="startupObject">
        ''' The type that declares the entry point.
        ''' </param>
        '''
        ''' <returns>
        ''' The compilation.
        ''' </returns>
        Private Shared Function CreateCompilation(files As IEnumerable(Of String), references As IEnumerable(Of String), globalImports As IEnumerable(Of String),
                                                  symbols As IEnumerable(Of KeyValuePair(Of String, Object)), strictValue As String, inferValue As String,
                                                  startupObject As String) As Compilation

            Dim parseOptions As New VisualBasicParseOptions(LanguageVersion.Latest, DocumentationMode.Diagnose, preprocessorSymbols:=symbols)
            Dim trees As List(Of SyntaxTree) = files.Select(
                Function(f As String) VisualBasicSyntaxTree.ParseText(File.ReadAllText(f, Encoding.UTF8), parseOptions, f, Encoding.UTF8)).ToList()
            Dim compilationOptions As New VisualBasicCompilationOptions(
                OutputKind.ConsoleApplication, mainTypeName:=startupObject, rootNamespace:=String.Empty,
                globalImports:=globalImports.Select(Function(i As String) GlobalImport.Parse(i)),
                optionStrict:=If("On".Equals(strictValue, StringComparison.OrdinalIgnoreCase), OptionStrict.On, OptionStrict.Off),
                optionInfer:=Not "Off".Equals(inferValue, StringComparison.OrdinalIgnoreCase), optionExplicit:=True)
            Return VisualBasicCompilation.Create("RuleTesting", trees,
                                                 references.Select(Function(r As String) DirectCast(MetadataReference.CreateFromFile(r), MetadataReference)),
                                                 compilationOptions)
        End Function

        ''' <summary>
        ''' Reads the options of the <c>[*.vb]</c> section of the <c>.editorconfig</c> file of the rule testing solution.
        ''' </summary>
        '''
        ''' <returns>
        ''' The options, by key.
        ''' </returns>
        Private Shared Function ReadEditorConfigOptions() As ImmutableDictionary(Of String, String)

            Dim options As ImmutableDictionary(Of String, String).Builder = ImmutableDictionary.CreateBuilder(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim inSection As Boolean = False
            For Each rawLine As String In File.ReadAllLines(Path.Combine(RuleTestingProjectTests.FindRuleTestingDirectory(), ".editorconfig"), Encoding.UTF8)
                Dim line As String = rawLine.Trim()
                If line.StartsWith("[", StringComparison.Ordinal) Then
                    inSection = line.Equals("[*.vb]", StringComparison.Ordinal)
                ElseIf inSection AndAlso line.Length <> 0 AndAlso Not line.StartsWith("#", StringComparison.Ordinal) AndAlso line.Contains("="c, StringComparison.Ordinal) Then
                    Dim separator As Integer = line.IndexOf("="c, StringComparison.Ordinal)
                    options(line.Substring(0, separator).Trim()) = line.Substring(separator + 1).Trim()
                End If
            Next rawLine
            Return options.ToImmutable()
        End Function

        ''' <summary>
        ''' Gets the rule identifiers of the <c>DCAnnn.vb</c> files of a project directory.
        ''' </summary>
        '''
        ''' <param name="projectDirectory">
        ''' The project directory.
        ''' </param>
        '''
        ''' <returns>
        ''' The sorted rule identifiers.
        ''' </returns>
        Private Shared Function GetRuleFileIds(projectDirectory As String) As List(Of String)

            Return Directory.EnumerateFiles(projectDirectory, "DCA*.vb").
                Select(Function(p As String) Path.GetFileNameWithoutExtension(p)).
                Where(Function(name As String) RuleTestingProjectTests.IsRuleFileName(name)).
                OrderBy(Function(name As String) name, StringComparer.Ordinal).ToList()
        End Function

        ''' <summary>
        ''' Determines whether a file name (without extension) is a rule identifier (<c>DCA</c> followed by three digits).
        ''' </summary>
        '''
        ''' <param name="fileName">
        ''' The file name.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the name is a rule identifier; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsRuleFileName(fileName As String) As Boolean

            Return fileName.Length = 6 AndAlso fileName.StartsWith("DCA", StringComparison.Ordinal) AndAlso
                   fileName.Skip(3).All(Function(c As Char) Char.IsDigit(c))
        End Function

        ''' <summary>
        ''' Gets the value of a property of a project file.
        ''' </summary>
        '''
        ''' <param name="project">
        ''' The project file.
        ''' </param>
        '''
        ''' <param name="name">
        ''' The property name.
        ''' </param>
        '''
        ''' <returns>
        ''' The value of the first property with that name, or <see langword="Nothing"/> if the project does not set it.
        ''' </returns>
        Private Shared Function GetProperty(project As XDocument, name As String) As String

            Return project.Descendants().FirstOrDefault(Function(e As XElement) e.Name.LocalName.Equals(name, StringComparison.Ordinal))?.Value.Trim()
        End Function

        ''' <summary>
        ''' Gets a directory path stored as assembly metadata of the test project.
        ''' </summary>
        '''
        ''' <param name="key">
        ''' The metadata key.
        ''' </param>
        '''
        ''' <returns>
        ''' The directory path.
        ''' </returns>
        Private Shared Function GetMetadataPath(key As String) As String

            Dim directory As String = GetType(RuleTestingProjectTests).Assembly.GetCustomAttributes(Of AssemblyMetadataAttribute)().
                Single(Function(a As AssemblyMetadataAttribute) a.Key.Equals(key, StringComparison.Ordinal)).Value
            Assert.True(IO.Directory.Exists(directory), $"The reference assemblies were not found: {directory}")
            Return directory
        End Function

        ''' <summary>
        ''' Finds the <c>RuleTesting</c> directory of the repository by walking up from the test output directory.
        ''' </summary>
        '''
        ''' <returns>
        ''' The full path of the <c>RuleTesting</c> directory.
        ''' </returns>
        '''
        ''' <exception cref="DirectoryNotFoundException">
        ''' Thrown when no ancestor of the test output directory contains the <c>RuleTesting</c> directory.
        ''' </exception>
        Private Shared Function FindRuleTestingDirectory() As String

            Dim directory As New DirectoryInfo(AppContext.BaseDirectory)
            While directory IsNot Nothing
                Dim candidate As String = Path.Combine(directory.FullName, "RuleTesting")
                If IO.Directory.Exists(Path.Combine(candidate, RuleTestingProjectTests.NetFrameworkProjectName)) Then
                    Return candidate
                End If
                directory = directory.Parent
            End While
            Throw New DirectoryNotFoundException("The 'RuleTesting' directory of the repository was not found.")
        End Function

#End Region

#Region " EditorConfigOptionsProvider "

        ''' <summary>
        ''' Provides the same options of an <c>.editorconfig</c> file to every source file.
        ''' </summary>
        Private NotInheritable Class EditorConfigOptionsProvider : Inherits AnalyzerConfigOptionsProvider

            ''' <summary>
            ''' The options.
            ''' </summary>
            Private ReadOnly options As EditorConfigOptions

            ''' <summary>
            ''' Initializes a new instance of the <see cref="EditorConfigOptionsProvider"/> class.
            ''' </summary>
            '''
            ''' <param name="values">
            ''' The options, by key.
            ''' </param>
            Public Sub New(values As ImmutableDictionary(Of String, String))
                Me.options = New EditorConfigOptions(values)
            End Sub

            ''' <inheritdoc/>
            Public Overrides ReadOnly Property GlobalOptions As AnalyzerConfigOptions
                Get
                    Return Me.options
                End Get
            End Property

            ''' <inheritdoc/>
            Public Overrides Function GetOptions(tree As SyntaxTree) As AnalyzerConfigOptions
                Return Me.options
            End Function

            ''' <inheritdoc/>
            Public Overrides Function GetOptions(textFile As AdditionalText) As AnalyzerConfigOptions
                Return Me.options
            End Function

        End Class

#End Region

#Region " EditorConfigOptions "

        ''' <summary>
        ''' The options of an <c>.editorconfig</c> file.
        ''' </summary>
        Private NotInheritable Class EditorConfigOptions : Inherits AnalyzerConfigOptions

            ''' <summary>
            ''' The options, by key.
            ''' </summary>
            Private ReadOnly values As ImmutableDictionary(Of String, String)

            ''' <summary>
            ''' Initializes a new instance of the <see cref="EditorConfigOptions"/> class.
            ''' </summary>
            '''
            ''' <param name="values">
            ''' The options, by key.
            ''' </param>
            Public Sub New(values As ImmutableDictionary(Of String, String))
                Me.values = values
            End Sub

            ''' <inheritdoc/>
            Public Overrides Function TryGetValue(key As String, ByRef value As String) As Boolean
                Return Me.values.TryGetValue(key, value)
            End Function

        End Class

#End Region

    End Class

End Namespace

#End Region
