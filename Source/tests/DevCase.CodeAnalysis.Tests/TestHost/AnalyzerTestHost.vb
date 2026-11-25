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
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic

Imports Xunit

#End Region

#Region " TestSource "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Represents one in-memory source file of a test project.
    ''' </summary>
    Friend NotInheritable Class TestSource

        ''' <summary>
        ''' Gets the file name (e.g. <c>Test0.vb</c>).
        ''' </summary>
        Friend ReadOnly Property FileName As String

        ''' <summary>
        ''' Gets the source text, without markup.
        ''' </summary>
        Friend ReadOnly Property Text As String

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TestSource"/> class.
        ''' </summary>
        '''
        ''' <param name="fileName">
        ''' The file name.
        ''' </param>
        '''
        ''' <param name="text">
        ''' The source text.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="fileName"/> or <paramref name="text"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="fileName"/> is empty or consists only of white-space characters.
        ''' </exception>
        Friend Sub New(fileName As String, text As String)

            ArgumentException.ThrowIfNullOrWhiteSpace(fileName)
            ArgumentNullException.ThrowIfNull(text)

            Me.FileName = fileName
            Me.Text = text
        End Sub

    End Class

End Namespace

#End Region

#Region " AnalyzerTestHost "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Provides a self-contained Roslyn test host: builds in-memory Visual Basic projects (with optional <c>.editorconfig</c>),
    ''' runs analyzers, applies code fixes, and verifies that the fixed code still compiles.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Markup: expected diagnostic spans are delimited with <c>[|</c> and <c>|]</c> in the test source.
    ''' </remarks>
    Friend NotInheritable Class AnalyzerTestHost

#Region " Constants "

        ''' <summary>
        ''' The directory used for the in-memory document paths (so that <c>.editorconfig</c> sections apply).
        ''' </summary>
        Friend Const ProjectDirectory As String = "/0/"

        ''' <summary>
        ''' The opening markup token.
        ''' </summary>
        Private Const SpanStartMarkup As String = "[|"

        ''' <summary>
        ''' The closing markup token.
        ''' </summary>
        Private Const SpanEndMarkup As String = "|]"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The metadata references of the test runtime (all trusted platform assemblies plus the Roslyn assemblies).
        ''' </summary>
        Private Shared ReadOnly References As New Lazy(Of ImmutableArray(Of MetadataReference))(AddressOf AnalyzerTestHost.LoadReferences)

        ''' <summary>
        ''' The .NET Framework 4.8 metadata references (mscorlib, System, System.Core, Microsoft.VisualBasic).
        ''' </summary>
        Private Shared ReadOnly NetFrameworkReferences As New Lazy(Of ImmutableArray(Of MetadataReference))(AddressOf AnalyzerTestHost.LoadNetFrameworkReferences)

        ''' <summary>
        ''' The project-level imports used by default by Visual Basic SDK projects.
        ''' </summary>
        Private Shared ReadOnly DefaultGlobalImports As String() = {
            "Microsoft.VisualBasic", "System", "System.Collections", "System.Collections.Generic",
            "System.Diagnostics", "System.Linq", "System.Threading.Tasks"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="AnalyzerTestHost"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Project Creation "

        ''' <summary>
        ''' Creates an in-memory Visual Basic project.
        ''' </summary>
        '''
        ''' <param name="sources">
        ''' The source files.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines applied to <c>[*.vb]</c>.
        ''' </param>
        '''
        ''' <param name="documentationMode">
        ''' The documentation mode of the parse options.
        ''' </param>
        '''
        ''' <param name="additionalReferences">
        ''' Optional additional metadata references.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies instead of the test runtime.
        ''' </param>
        '''
        ''' <param name="rootNamespace">
        ''' Optional project root namespace (empty by default, as in a project whose root namespace was removed).
        ''' </param>
        '''
        ''' <param name="linkedNetFrameworkProject">
        ''' <see langword="True"/> to add a second project to the solution, compiled against .NET Framework 4.8, with the same 
        ''' documents and file paths (as a multi-targeted project or linked files).
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The project.
        ''' </returns>
        Friend Shared Function CreateProject(sources As IEnumerable(Of TestSource), Optional editorConfig As String = Nothing,
                                             Optional documentationMode As DocumentationMode = DocumentationMode.Diagnose,
                                             Optional additionalReferences As IEnumerable(Of MetadataReference) = Nothing,
                                             Optional targetNetFramework As Boolean = False,
                                             Optional rootNamespace As String = Nothing,
                                             Optional linkedNetFrameworkProject As Boolean = False,
                                             Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Project

            Dim workspace As New AdhocWorkspace()
            Dim projectId As ProjectId = ProjectId.CreateNewId()

            Dim parseOptions As New VisualBasicParseOptions(LanguageVersion.Latest, documentationMode)
            Dim compilationOptions As VisualBasicCompilationOptions =
                New VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                                                  optionStrict:=If(optionStatements.HasValue, optionStatements.Value.Strict, OptionStrict.On),
                                                  optionInfer:=optionStatements.HasValue AndAlso optionStatements.Value.Infer,
                                                  optionExplicit:=Not optionStatements.HasValue OrElse optionStatements.Value.Explicit).
                    WithGlobalImports(GlobalImport.Parse(AnalyzerTestHost.DefaultGlobalImports)).
                    WithRootNamespace(If(rootNamespace, String.Empty))

            Dim projectInfo As ProjectInfo = ProjectInfo.Create(
                id:=projectId, version:=VersionStamp.Create(), name:="TestProject", assemblyName:="TestProject",
                language:=LanguageNames.VisualBasic, filePath:=$"{AnalyzerTestHost.ProjectDirectory}TestProject.vbproj",
                compilationOptions:=compilationOptions, parseOptions:=parseOptions,
                metadataReferences:=If(targetNetFramework, AnalyzerTestHost.NetFrameworkReferences.Value, AnalyzerTestHost.References.Value).
                                    AddRange(If(additionalReferences, Enumerable.Empty(Of MetadataReference)())))

            Dim solution As Solution = workspace.CurrentSolution.AddProject(projectInfo)
            For Each source As TestSource In sources
                solution = solution.AddDocument(DocumentId.CreateNewId(projectId), source.FileName, SourceText.From(source.Text),
                                                filePath:=$"{AnalyzerTestHost.ProjectDirectory}{source.FileName}")
            Next source

            If editorConfig IsNot Nothing Then
                Dim configText As String = $"root = true{vbLf}{vbLf}[*.vb]{vbLf}{editorConfig}{vbLf}"
                solution = solution.AddAnalyzerConfigDocument(DocumentId.CreateNewId(projectId), ".editorconfig", SourceText.From(configText),
                                                              filePath:=$"{AnalyzerTestHost.ProjectDirectory}.editorconfig")
            End If

            If linkedNetFrameworkProject Then
                Dim linkedId As ProjectId = ProjectId.CreateNewId()
                solution = solution.AddProject(ProjectInfo.Create(
                    id:=linkedId, version:=VersionStamp.Create(), name:="TestProject.Net48", assemblyName:="TestProject.Net48",
                    language:=LanguageNames.VisualBasic, filePath:=$"{AnalyzerTestHost.ProjectDirectory}TestProject.Net48.vbproj",
                    compilationOptions:=compilationOptions, parseOptions:=parseOptions,
                    metadataReferences:=AnalyzerTestHost.NetFrameworkReferences.Value))
                For Each source As TestSource In sources
                    solution = solution.AddDocument(DocumentId.CreateNewId(linkedId), source.FileName, SourceText.From(source.Text),
                                                    filePath:=$"{AnalyzerTestHost.ProjectDirectory}{source.FileName}")
                Next source
            End If

            Return solution.GetProject(projectId)
        End Function

        ''' <summary>
        ''' Creates a project with a single <c>Test0.vb</c> document.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source text (without markup).
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies instead of the test runtime.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The project.
        ''' </returns>
        Friend Shared Function CreateProject(source As String, Optional editorConfig As String = Nothing,
                                             Optional targetNetFramework As Boolean = False,
                                             Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Project

            Return AnalyzerTestHost.CreateProject({New TestSource("Test0.vb", source)}, editorConfig, targetNetFramework:=targetNetFramework,
                                                  optionStatements:=optionStatements)
        End Function

#End Region

#Region " Analysis "

        ''' <summary>
        ''' Runs analyzers on a project and returns their diagnostics, sorted by file and position.
        ''' Fails the test if any analyzer throws.
        ''' </summary>
        '''
        ''' <param name="project">
        ''' The project to analyze.
        ''' </param>
        '''
        ''' <param name="analyzers">
        ''' The analyzers to run.
        ''' </param>
        '''
        ''' <returns>
        ''' The analyzer diagnostics.
        ''' </returns>
        Friend Shared Async Function GetAnalyzerDiagnosticsAsync(project As Project, ParamArray analyzers As DiagnosticAnalyzer()) As Task(Of ImmutableArray(Of Diagnostic))

            Dim compilation As Compilation = Await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(False)
            Dim exceptions As New List(Of String)()
            Dim options As New CompilationWithAnalyzersOptions(
                project.AnalyzerOptions,
                Sub(ex As Exception, analyzer As DiagnosticAnalyzer, diagnostic As Diagnostic)
                    SyncLock exceptions
                        exceptions.Add($"{analyzer.GetType().Name}: {ex}")
                    End SyncLock
                End Sub,
                concurrentAnalysis:=True, logAnalyzerExecutionTime:=False)

            Dim withAnalyzers As CompilationWithAnalyzers = compilation.WithAnalyzers(analyzers.ToImmutableArray(), options)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None).ConfigureAwait(False)

            Assert.True(exceptions.Count = 0, $"Analyzer exception(s):{vbLf}{String.Join(vbLf, exceptions)}")
            Assert.DoesNotContain(diagnostics, Function(d As Diagnostic) d.Id.Equals("AD0001", StringComparison.Ordinal))

            Return diagnostics.
                OrderBy(Function(d As Diagnostic) d.Location.SourceTree?.FilePath, StringComparer.Ordinal).
                ThenBy(Function(d As Diagnostic) d.Location.SourceSpan.Start).
                ToImmutableArray()
        End Function

        ''' <summary>
        ''' Gets the compiler errors of a project.
        ''' </summary>
        '''
        ''' <param name="project">
        ''' The project to compile.
        ''' </param>
        '''
        ''' <returns>
        ''' The compiler diagnostics with <see cref="DiagnosticSeverity.Error"/> severity.
        ''' </returns>
        Friend Shared Async Function GetCompilerErrorsAsync(project As Project) As Task(Of ImmutableArray(Of Diagnostic))

            Dim compilation As Compilation = Await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(False)
            Return compilation.GetDiagnostics().Where(Function(d As Diagnostic) d.Severity = DiagnosticSeverity.Error).ToImmutableArray()
        End Function

#End Region

#Region " Verification "

        ''' <summary>
        ''' Verifies that an analyzer reports exactly the diagnostics marked with <c>[| |]</c> in the source.
        ''' </summary>
        '''
        ''' <remarks>
        ''' A source written with LF line breaks is verified twice: as written and converted to CRLF, so that line-ending bugs
        ''' (e.g. a formatter that writes the line ending of the operating system) are detected on every platform.
        ''' </remarks>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="markupSource">
        ''' The source with markup.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="allowCompilerErrors">
        ''' <see langword="True"/> to skip the check that the test source compiles.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The reported diagnostics, for additional assertions.
        ''' </returns>
        Friend Shared Async Function VerifyAnalyzerAsync(analyzer As DiagnosticAnalyzer, markupSource As String,
                                                         Optional editorConfig As String = Nothing,
                                                         Optional allowCompilerErrors As Boolean = False,
                                                         Optional targetNetFramework As Boolean = False,
                                                         Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Task(Of ImmutableArray(Of Diagnostic))

            Dim diagnostics As ImmutableArray(Of Diagnostic) =
                Await AnalyzerTestHost.VerifyAnalyzerCoreAsync(analyzer, markupSource, editorConfig, allowCompilerErrors, targetNetFramework, optionStatements).ConfigureAwait(False)
            If AnalyzerTestHost.IsLineFeedOnly(markupSource) Then
                Await AnalyzerTestHost.VerifyAnalyzerCoreAsync(analyzer, AnalyzerTestHost.ToCrLf(markupSource), editorConfig, allowCompilerErrors, targetNetFramework, optionStatements).ConfigureAwait(False)
            End If
            If Not allowCompilerErrors Then
                Await AnalyzerTestHost.VerifyAnalyzerVariationsAsync(analyzer, markupSource, editorConfig, targetNetFramework, optionStatements).ConfigureAwait(False)
            End If
            Return diagnostics
        End Function

        ''' <summary>
        ''' Verifies that an analyzer reports as many diagnostics for every <see cref="SyntaxVariations">syntax variation</see> of 
        ''' a source as for the source itself.
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="markupSource">
        ''' The source with markup.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project.
        ''' </param>
        '''
        ''' <returns>
        ''' A task that represents the asynchronous verification.
        ''' </returns>
        Private Shared Async Function VerifyAnalyzerVariationsAsync(analyzer As DiagnosticAnalyzer, markupSource As String, editorConfig As String,
                                                                     targetNetFramework As Boolean,
                                                                     optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)?) As Task

            Dim spans As List(Of TextSpan) = Nothing
            Dim source As String = AnalyzerTestHost.ParseMarkup(markupSource, spans)
            Dim original As ImmutableArray(Of Diagnostic) =
                Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(AnalyzerTestHost.CreateProject(source, editorConfig, targetNetFramework, optionStatements), analyzer).ConfigureAwait(False)
            For Each variation As SyntaxVariation In SyntaxVariations.All
                Dim rewritten As String = variation.Rewrite(source)
                If rewritten Is Nothing Then
                    Continue For
                End If

                Dim project As Project = AnalyzerTestHost.CreateProject(rewritten, editorConfig, targetNetFramework, optionStatements)
                Await AnalyzerTestHost.AssertVariationCompilesAsync(project, variation, rewritten).ConfigureAwait(False)
                Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(False)
                AnalyzerTestHost.AssertSameDiagnostics(original, diagnostics, variation, rewritten)
            Next variation
        End Function

        ''' <summary>
        ''' Verifies that a code fix still applies, and produces code that compiles and fixes the diagnostics, for every 
        ''' <see cref="SyntaxVariations">syntax variation</see> of a source.
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider under test.
        ''' </param>
        '''
        ''' <param name="markupSource">
        ''' The source with markup.
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' Optional equivalence key of the code action to apply.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="allowRemainingDiagnostics">
        ''' <see langword="True"/> when some diagnostics are expected to remain unfixable.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project.
        ''' </param>
        '''
        ''' <returns>
        ''' A task that represents the asynchronous verification.
        ''' </returns>
        '''
        ''' <exception cref="Xunit.Sdk.XunitException">
        ''' Thrown when the code fix fails for a variation; the message names the variation and shows its source.
        ''' </exception>
        Private Shared Async Function VerifyCodeFixVariationsAsync(analyzer As DiagnosticAnalyzer, provider As CodeFixProvider, markupSource As String,
                                                                    equivalenceKey As String, editorConfig As String, allowRemainingDiagnostics As Boolean,
                                                                    targetNetFramework As Boolean,
                                                                    optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)?) As Task

            Dim spans As List(Of TextSpan) = Nothing
            Dim source As String = AnalyzerTestHost.ParseMarkup(markupSource, spans)
            Dim original As ImmutableArray(Of Diagnostic) =
                Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(AnalyzerTestHost.CreateProject(source, editorConfig, targetNetFramework, optionStatements), analyzer).ConfigureAwait(False)
            For Each variation As SyntaxVariation In SyntaxVariations.All
                Dim rewritten As String = variation.Rewrite(source)
                If rewritten Is Nothing Then
                    Continue For
                End If

                Dim project As Project = AnalyzerTestHost.CreateProject(rewritten, editorConfig, targetNetFramework, optionStatements)
                Await AnalyzerTestHost.AssertVariationCompilesAsync(project, variation, rewritten).ConfigureAwait(False)
                Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(False)
                AnalyzerTestHost.AssertSameDiagnostics(original, diagnostics, variation, rewritten)
                If SyntaxVariations.IsExempt(original, variation) Then
                    Continue For
                End If

                Try
                    Await AnalyzerTestHost.VerifyCodeFixCoreAsync(analyzer, provider, rewritten, Nothing, equivalenceKey, editorConfig, allowRemainingDiagnostics,
                                                                  False, targetNetFramework, optionStatements).ConfigureAwait(False)
                Catch ex As Xunit.Sdk.XunitException
                    Throw New Xunit.Sdk.XunitException($"[{variation.Name} variation] {ex.Message}{vbLf}Source:{vbLf}{rewritten}", ex)
                End Try
            Next variation
        End Function

        ''' <summary>
        ''' Asserts that a syntax variation of a test source compiles (a variation that does not compile is a defect of the 
        ''' variation, not of the rule).
        ''' </summary>
        '''
        ''' <param name="project">
        ''' The project of the variation.
        ''' </param>
        '''
        ''' <param name="variation">
        ''' The variation.
        ''' </param>
        '''
        ''' <param name="rewritten">
        ''' The rewritten source.
        ''' </param>
        '''
        ''' <returns>
        ''' A task that represents the asynchronous assertion.
        ''' </returns>
        Private Shared Async Function AssertVariationCompilesAsync(project As Project, variation As SyntaxVariation, rewritten As String) As Task

            Dim errors As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetCompilerErrorsAsync(project).ConfigureAwait(False)
            Assert.True(errors.IsEmpty, $"The {variation.Name} variation of the test source does not compile: {String.Join(" | ", errors.Select(Function(e As Diagnostic) e.ToString()))}{vbLf}Source:{vbLf}{rewritten}")
        End Function

        ''' <summary>
        ''' Asserts that the diagnostics of a syntax variation have the same identifiers and counts as those of the original source, 
        ''' except for the exempted rules (see <see cref="SyntaxVariations.IsExempt"/>).
        ''' </summary>
        '''
        ''' <param name="original">
        ''' The diagnostics of the original source.
        ''' </param>
        '''
        ''' <param name="actual">
        ''' The diagnostics of the variation.
        ''' </param>
        '''
        ''' <param name="variation">
        ''' The variation.
        ''' </param>
        '''
        ''' <param name="rewritten">
        ''' The rewritten source.
        ''' </param>
        Private Shared Sub AssertSameDiagnostics(original As ImmutableArray(Of Diagnostic), actual As ImmutableArray(Of Diagnostic), variation As SyntaxVariation, rewritten As String)

            If SyntaxVariations.IsExempt(original, variation) OrElse SyntaxVariations.IsExempt(actual, variation) Then
                Return
            End If

            Dim summarize As Func(Of ImmutableArray(Of Diagnostic), String) =
                Function(diagnostics As ImmutableArray(Of Diagnostic)) String.Join(", ", diagnostics.GroupBy(Function(d As Diagnostic) d.Id, StringComparer.Ordinal).
                                                                                                    OrderBy(Function(g As IGrouping(Of String, Diagnostic)) g.Key, StringComparer.Ordinal).
                                                                                                    Select(Function(g As IGrouping(Of String, Diagnostic)) $"{g.Key} x{g.Count()}"))
            Dim expected As String = summarize(original)
            Dim found As String = summarize(actual)
            Assert.True(expected.Equals(found, StringComparison.Ordinal),
                        $"The {variation.Name} variation changes the diagnostics: expected [{expected}], found [{found}].{vbLf}Messages: {String.Join(" | ", actual.Select(Function(d As Diagnostic) $"{d.GetMessage()} at {d.Location.GetLineSpan().StartLinePosition}"))}{vbLf}Source:{vbLf}{rewritten}")
        End Sub

        ''' <summary>
        ''' Determines whether a source contains line breaks and all of them are LF.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source text.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the source has LF line breaks and no CR character; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsLineFeedOnly(source As String) As Boolean

            Return source.Contains(vbLf, StringComparison.Ordinal) AndAlso Not source.Contains(vbCr, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Converts the LF line breaks of a source to CRLF.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source text, with LF line breaks.
        ''' </param>
        '''
        ''' <returns>
        ''' The source text, with CRLF line breaks.
        ''' </returns>
        Private Shared Function ToCrLf(source As String) As String

            Return source.Replace(vbLf, vbCrLf, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies the diagnostics of an analyzer for one line-ending style of the source (see <see cref="VerifyAnalyzerAsync"/>).
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="markupSource">
        ''' The source with markup.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="allowCompilerErrors">
        ''' <see langword="True"/> to skip the check that the test source compiles.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The reported diagnostics, for additional assertions.
        ''' </returns>
        Private Shared Async Function VerifyAnalyzerCoreAsync(analyzer As DiagnosticAnalyzer, markupSource As String,
                                                         Optional editorConfig As String = Nothing,
                                                         Optional allowCompilerErrors As Boolean = False,
                                                         Optional targetNetFramework As Boolean = False,
                                                         Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Task(Of ImmutableArray(Of Diagnostic))

            Dim expectedSpans As List(Of TextSpan) = Nothing
            Dim source As String = AnalyzerTestHost.ParseMarkup(markupSource, expectedSpans)
            Dim project As Project = AnalyzerTestHost.CreateProject(source, editorConfig, targetNetFramework, optionStatements)

            If Not allowCompilerErrors Then
                Await AnalyzerTestHost.AssertCompilesAsync(project, "test source").ConfigureAwait(False)
            End If

            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(False)
            Dim actualSpans As List(Of TextSpan) = diagnostics.Select(Function(d As Diagnostic) d.Location.SourceSpan).ToList()

            Dim describe As Func(Of IEnumerable(Of TextSpan), String) =
                Function(spans As IEnumerable(Of TextSpan)) String.Join(", ", spans.Select(Function(s As TextSpan) $"{s} '{source.Substring(s.Start, s.Length)}'"))

            Assert.True(expectedSpans.SequenceEqual(actualSpans),
                        $"Expected: [{describe(expectedSpans)}]{vbLf}Actual:   [{describe(actualSpans)}]{vbLf}Messages: {String.Join(" | ", diagnostics.Select(Function(d As Diagnostic) d.GetMessage()))}")
            Return diagnostics
        End Function

        ''' <summary>
        ''' Verifies a code fix: applies it repeatedly (always to the first remaining diagnostic, re-parsing the text
        ''' between passes as the IDE does) until no fixable diagnostic remains, then compares the result.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Sources written with LF line breaks are verified twice: as written and converted to CRLF, so that line-ending bugs
        ''' (e.g. a formatter that writes the line ending of the operating system) are detected on every platform.
        ''' </remarks>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider under test.
        ''' </param>
        '''
        ''' <param name="markupSource">
        ''' The source; markup, if present, is validated against the diagnostics before fixing.
        ''' </param>
        '''
        ''' <param name="expectedSource">
        ''' The expected fixed source.
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' Optional equivalence key of the code action to apply (the first action is applied otherwise).
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="allowRemainingDiagnostics">
        ''' <see langword="True"/> when some diagnostics are expected to remain unfixable (e.g. empty <c>&lt;param&gt;</c> tags).
        ''' </param>
        '''
        ''' <param name="allowCompilerErrors">
        ''' <see langword="True"/> to skip the check that the fixed source compiles.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' A task that represents the asynchronous verification.
        ''' </returns>
        Friend Shared Async Function VerifyCodeFixAsync(analyzer As DiagnosticAnalyzer, provider As CodeFixProvider,
                                                        markupSource As String, expectedSource As String,
                                                        Optional equivalenceKey As String = Nothing,
                                                        Optional editorConfig As String = Nothing,
                                                        Optional allowRemainingDiagnostics As Boolean = False,
                                                        Optional allowCompilerErrors As Boolean = False,
                                                        Optional targetNetFramework As Boolean = False,
                                                        Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Task

            Await AnalyzerTestHost.VerifyCodeFixCoreAsync(analyzer, provider, markupSource, expectedSource, equivalenceKey, editorConfig,
                                                          allowRemainingDiagnostics, allowCompilerErrors, targetNetFramework, optionStatements).ConfigureAwait(False)
            If AnalyzerTestHost.IsLineFeedOnly(markupSource) AndAlso AnalyzerTestHost.IsLineFeedOnly(expectedSource) Then
                Await AnalyzerTestHost.VerifyCodeFixCoreAsync(analyzer, provider, AnalyzerTestHost.ToCrLf(markupSource), AnalyzerTestHost.ToCrLf(expectedSource),
                                                              equivalenceKey, editorConfig, allowRemainingDiagnostics, allowCompilerErrors,
                                                              targetNetFramework, optionStatements).ConfigureAwait(False)
            End If
            If Not allowCompilerErrors Then
                Await AnalyzerTestHost.VerifyCodeFixVariationsAsync(analyzer, provider, markupSource, equivalenceKey, editorConfig, allowRemainingDiagnostics,
                                                                    targetNetFramework, optionStatements).ConfigureAwait(False)
            End If
        End Function

        ''' <summary>
        ''' Verifies a code fix for one line-ending style of the source (see <see cref="VerifyCodeFixAsync"/>).
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider under test.
        ''' </param>
        '''
        ''' <param name="markupSource">
        ''' The source; markup, if present, is validated against the diagnostics before fixing.
        ''' </param>
        '''
        ''' <param name="expectedSource">
        ''' The expected fixed source; <see langword="Nothing"/> to only check that the fixed source compiles and has no 
        ''' diagnostics left (for the syntax variations).
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' Optional equivalence key of the code action to apply (the first action is applied otherwise).
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="allowRemainingDiagnostics">
        ''' <see langword="True"/> when some diagnostics are expected to remain unfixable (e.g. empty <c>&lt;param&gt;</c> tags).
        ''' </param>
        '''
        ''' <param name="allowCompilerErrors">
        ''' <see langword="True"/> to skip the check that the fixed source compiles.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' A task that represents the asynchronous verification.
        ''' </returns>
        Private Shared Async Function VerifyCodeFixCoreAsync(analyzer As DiagnosticAnalyzer, provider As CodeFixProvider,
                                                        markupSource As String, expectedSource As String,
                                                        Optional equivalenceKey As String = Nothing,
                                                        Optional editorConfig As String = Nothing,
                                                        Optional allowRemainingDiagnostics As Boolean = False,
                                                        Optional allowCompilerErrors As Boolean = False,
                                                        Optional targetNetFramework As Boolean = False,
                                                        Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Task

            Dim expectedSpans As List(Of TextSpan) = Nothing
            Dim currentText As String = AnalyzerTestHost.ParseMarkup(markupSource, expectedSpans)
            Dim hasMarkup As Boolean = expectedSpans.Count > 0
            Dim fixedOnce As Boolean = False
            Dim skipped As Integer = 0

            For iteration As Integer = 1 To 50
                Dim project As Project = AnalyzerTestHost.CreateProject(currentText, editorConfig, targetNetFramework, optionStatements)
                Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(False)

                If iteration = 1 AndAlso hasMarkup Then
                    Assert.Equal(expectedSpans, diagnostics.Select(Function(d As Diagnostic) d.Location.SourceSpan).ToList())
                End If

                Dim fixable As List(Of Diagnostic) = diagnostics.Where(Function(d As Diagnostic) provider.FixableDiagnosticIds.Contains(d.Id, StringComparer.Ordinal)).ToList()
                If fixable.Count <= skipped Then
                    Exit For
                End If

                Dim document As Document = project.GetDocument(fixable(skipped).Location.SourceTree)
                Dim action As CodeAction = Await AnalyzerTestHost.GetCodeActionAsync(provider, document, fixable(skipped), equivalenceKey).ConfigureAwait(False)
                If action Is Nothing Then
                    Assert.True(allowRemainingDiagnostics, $"No code action registered for: {fixable(skipped).GetMessage()}")
                    skipped += 1
                    Continue For
                End If

                Dim newSolution As Solution = Await AnalyzerTestHost.ApplyActionAsync(action).ConfigureAwait(False)
                Dim newText As String = (Await newSolution.GetDocument(document.Id).GetTextAsync(CancellationToken.None).ConfigureAwait(False)).ToString()
                Assert.False(newText.Equals(currentText, StringComparison.Ordinal), $"The code fix did not change the document: {fixable(skipped).GetMessage()}")
                currentText = newText
                fixedOnce = True

                Assert.True(iteration < 50, "The code fix does not converge (possible fix loop).")
            Next iteration

            Assert.True(fixedOnce, "No code fix was applied.")
            If expectedSource IsNot Nothing Then
                AnalyzerTestHost.AssertTextEqual(expectedSource, currentText)
            End If

            Dim finalProject As Project = AnalyzerTestHost.CreateProject(currentText, editorConfig, targetNetFramework, optionStatements)
            If Not allowCompilerErrors Then
                Await AnalyzerTestHost.AssertCompilesAsync(finalProject, "fixed source").ConfigureAwait(False)
            End If
            If Not allowRemainingDiagnostics Then
                Dim remaining As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(finalProject, analyzer).ConfigureAwait(False)
                Assert.True(remaining.IsEmpty, $"Diagnostics remain after fixing: {String.Join(" | ", remaining.Select(Function(d As Diagnostic) d.GetMessage()))}")
            End If
        End Function

        ''' <summary>
        ''' Applies the "Fix All in Document" action of a provider (using its <see cref="FixAllProvider"/>) and returns the resulting text.
        ''' </summary>
        '''
        ''' <remarks>
        ''' A source written with LF line breaks is also fixed after converting it to CRLF, and both results must be identical
        ''' except for the line endings.
        ''' </remarks>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider under test.
        ''' </param>
        '''
        ''' <param name="source">
        ''' The source (without markup).
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' The equivalence key of the code action.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies instead of the test runtime.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed text.
        ''' </returns>
        Friend Shared Async Function ApplyFixAllInDocumentAsync(analyzer As DiagnosticAnalyzer, provider As CodeFixProvider, source As String,
                                                                equivalenceKey As String, Optional editorConfig As String = Nothing,
                                                                Optional targetNetFramework As Boolean = False,
                                                                Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Task(Of String)

            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentCoreAsync(analyzer, provider, source, equivalenceKey, editorConfig,
                                                                                             targetNetFramework, optionStatements).ConfigureAwait(False)
            If AnalyzerTestHost.IsLineFeedOnly(source) Then
                Assert.DoesNotContain(vbCr, fixedText, StringComparison.Ordinal)
                Dim crLfText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentCoreAsync(analyzer, provider, AnalyzerTestHost.ToCrLf(source), equivalenceKey,
                                                                                               editorConfig, targetNetFramework, optionStatements).ConfigureAwait(False)
                AnalyzerTestHost.AssertTextEqual(AnalyzerTestHost.ToCrLf(fixedText), crLfText)
            End If
            Return fixedText
        End Function

        ''' <summary>
        ''' Applies the "Fix All in Document" action for one line-ending style of the source (see <see cref="ApplyFixAllInDocumentAsync"/>).
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider under test.
        ''' </param>
        '''
        ''' <param name="source">
        ''' The source (without markup).
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' The equivalence key of the code action.
        ''' </param>
        '''
        ''' <param name="editorConfig">
        ''' Optional <c>.editorconfig</c> key/value lines.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies instead of the test runtime.
        ''' </param>
        '''
        ''' <param name="optionStatements">
        ''' The <c>Option Strict</c>, <c>Option Explicit</c> and <c>Option Infer</c> settings of the project; 
        ''' <see langword="Nothing"/> for <c>Option Strict On</c>, <c>Option Explicit On</c> and <c>Option Infer Off</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed text.
        ''' </returns>
        Private Shared Async Function ApplyFixAllInDocumentCoreAsync(analyzer As DiagnosticAnalyzer, provider As CodeFixProvider, source As String,
                                                                equivalenceKey As String, Optional editorConfig As String = Nothing,
                                                                Optional targetNetFramework As Boolean = False,
                                                                Optional optionStatements As (Strict As OptionStrict, Explicit As Boolean, Infer As Boolean)? = Nothing) As Task(Of String)

            Dim project As Project = AnalyzerTestHost.CreateProject(source, editorConfig, targetNetFramework, optionStatements)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(False)
            Assert.NotEmpty(diagnostics)

            Dim document As Document = project.Documents.Single()
            Dim diagnosticProvider As New TestFixAllDiagnosticProvider(diagnostics)
            Dim fixAllContext As New FixAllContext(document, provider, FixAllScope.Document, equivalenceKey,
                                                   provider.FixableDiagnosticIds, diagnosticProvider, CancellationToken.None)

            Dim action As CodeAction = Await provider.GetFixAllProvider().GetFixAsync(fixAllContext).ConfigureAwait(False)
            Assert.NotNull(action)

            Dim newSolution As Solution = Await AnalyzerTestHost.ApplyActionAsync(action).ConfigureAwait(False)
            Return (Await newSolution.GetDocument(document.Id).GetTextAsync(CancellationToken.None).ConfigureAwait(False)).ToString()
        End Function

        ''' <summary>
        ''' Gets the titles of the code actions registered by a provider for the first diagnostic of a source.
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer under test.
        ''' </param>
        '''
        ''' <param name="provider">
        ''' The code fix provider under test.
        ''' </param>
        '''
        ''' <param name="source">
        ''' The source (without markup).
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against the .NET Framework 4.8 reference assemblies instead of the test runtime.
        ''' </param>
        '''
        ''' <returns>
        ''' The registered code actions.
        ''' </returns>
        Friend Shared Async Function GetRegisteredActionsAsync(analyzer As DiagnosticAnalyzer, provider As CodeFixProvider, source As String,
                                                               Optional targetNetFramework As Boolean = False) As Task(Of List(Of CodeAction))

            Dim project As Project = AnalyzerTestHost.CreateProject(source, targetNetFramework:=targetNetFramework)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(False)
            Assert.NotEmpty(diagnostics)

            Dim actions As New List(Of CodeAction)()
            Dim document As Document = project.GetDocument(diagnostics(0).Location.SourceTree)
            Dim context As New CodeFixContext(document, diagnostics(0),
                                              Sub(a As CodeAction, d As ImmutableArray(Of Diagnostic)) actions.Add(a),
                                              CancellationToken.None)
            Await provider.RegisterCodeFixesAsync(context).ConfigureAwait(False)
            Return actions
        End Function

#End Region

#Region " Helpers "

        ''' <summary>
        ''' Removes <c>[| |]</c> markup from a source and returns the marked spans.
        ''' </summary>
        '''
        ''' <param name="markup">
        ''' The source with markup.
        ''' </param>
        '''
        ''' <param name="spans">
        ''' Receives the marked spans, in source order.
        ''' </param>
        '''
        ''' <returns>
        ''' The source without markup.
        ''' </returns>
        Friend Shared Function ParseMarkup(markup As String, ByRef spans As List(Of TextSpan)) As String

            spans = New List(Of TextSpan)()
            Dim builder As New System.Text.StringBuilder()
            Dim openPositions As New Stack(Of Integer)()
            Dim index As Integer = 0

            While index < markup.Length
                If String.CompareOrdinal(markup, index, AnalyzerTestHost.SpanStartMarkup, 0, 2) = 0 Then
                    openPositions.Push(builder.Length)
                    index += 2
                ElseIf String.CompareOrdinal(markup, index, AnalyzerTestHost.SpanEndMarkup, 0, 2) = 0 AndAlso openPositions.Count > 0 Then
                    Dim start As Integer = openPositions.Pop()
                    spans.Add(TextSpan.FromBounds(start, builder.Length))
                    index += 2
                Else
                    builder.Append(markup(index))
                    index += 1
                End If
            End While

            Assert.True(openPositions.Count = 0, "Unbalanced markup.")
            spans.Sort(Function(a As TextSpan, b As TextSpan) a.Start.CompareTo(b.Start))
            Return builder.ToString()
        End Function

        ''' <summary>
        ''' Asserts that a project compiles without errors.
        ''' </summary>
        '''
        ''' <param name="project">
        ''' The project.
        ''' </param>
        '''
        ''' <param name="description">
        ''' A description used in the failure message.
        ''' </param>
        '''
        ''' <returns>
        ''' A task that represents the asynchronous assertion.
        ''' </returns>
        Friend Shared Async Function AssertCompilesAsync(project As Project, description As String) As Task

            Dim errors As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetCompilerErrorsAsync(project).ConfigureAwait(False)
            Dim text As String = String.Join(vbLf & "-----" & vbLf, project.Documents.Select(Function(d As Document) d.GetTextAsync().GetAwaiter().GetResult().ToString()))
            Assert.True(errors.IsEmpty, $"The {description} does not compile:{vbLf}{String.Join(vbLf, errors.Select(Function(e As Diagnostic) e.ToString()))}{vbLf}{text}")
        End Function

        ''' <summary>
        ''' Asserts that two texts are equal, reporting the first differing line.
        ''' </summary>
        '''
        ''' <param name="expected">
        ''' The expected text.
        ''' </param>
        '''
        ''' <param name="actual">
        ''' The actual text.
        ''' </param>
        Friend Shared Sub AssertTextEqual(expected As String, actual As String)

            If expected.Equals(actual, StringComparison.Ordinal) Then
                Return
            End If

            Dim expectedLines As String() = expected.Split(ChrW(10))
            Dim actualLines As String() = actual.Split(ChrW(10))
            Dim lineCount As Integer = Math.Max(expectedLines.Length, actualLines.Length)
            For index As Integer = 0 To lineCount - 1
                Dim e As String = If(index < expectedLines.Length, expectedLines(index), "<EOF>")
                Dim a As String = If(index < actualLines.Length, actualLines(index), "<EOF>")
                If Not e.Equals(a, StringComparison.Ordinal) Then
                    Assert.Fail($"Texts differ at line {index + 1}:{vbLf}Expected: «{e.Replace(vbCr, "\r", StringComparison.Ordinal)}»{vbLf}Actual:   «{a.Replace(vbCr, "\r", StringComparison.Ordinal)}»{vbLf}{vbLf}=== Full actual text ==={vbLf}{actual}")
                End If
            Next index
        End Sub

        ''' <summary>
        ''' Gets the code action registered by a provider for a diagnostic.
        ''' </summary>
        '''
        ''' <param name="provider">
        ''' The code fix provider.
        ''' </param>
        '''
        ''' <param name="document">
        ''' The document.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' Optional equivalence key; the first action is returned when <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The code action, or <see langword="Nothing"/> if none was registered.
        ''' </returns>
        Private Shared Async Function GetCodeActionAsync(provider As CodeFixProvider, document As Document, diagnostic As Diagnostic,
                                                         equivalenceKey As String) As Task(Of CodeAction)

            Dim actions As New List(Of CodeAction)()
            Dim context As New CodeFixContext(document, diagnostic,
                                              Sub(a As CodeAction, d As ImmutableArray(Of Diagnostic)) actions.Add(a),
                                              CancellationToken.None)
            Await provider.RegisterCodeFixesAsync(context).ConfigureAwait(False)

            Return If(equivalenceKey Is Nothing,
                      actions.FirstOrDefault(),
                      actions.Find(Function(a As CodeAction) equivalenceKey.Equals(a.EquivalenceKey, StringComparison.Ordinal)))
        End Function

        ''' <summary>
        ''' Applies a code action and returns the changed solution.
        ''' </summary>
        '''
        ''' <param name="action">
        ''' The code action.
        ''' </param>
        '''
        ''' <returns>
        ''' The changed solution.
        ''' </returns>
        Private Shared Async Function ApplyActionAsync(action As CodeAction) As Task(Of Solution)

            Dim operations As ImmutableArray(Of CodeActionOperation) = Await action.GetOperationsAsync(CancellationToken.None).ConfigureAwait(False)
            Dim apply As ApplyChangesOperation = operations.OfType(Of ApplyChangesOperation)().Single()
            Return apply.ChangedSolution
        End Function

        ''' <summary>
        ''' Loads the metadata references of the test runtime.
        ''' </summary>
        '''
        ''' <returns>
        ''' The metadata references.
        ''' </returns>
        Private Shared Function LoadNetFrameworkReferences() As ImmutableArray(Of MetadataReference)

            Dim directory As String = GetType(AnalyzerTestHost).Assembly.GetCustomAttributes(GetType(Reflection.AssemblyMetadataAttribute), False).Cast(Of Reflection.AssemblyMetadataAttribute)().
                Single(Function(a As Reflection.AssemblyMetadataAttribute) a.Key.Equals("Net48ReferenceAssembliesPath", StringComparison.Ordinal)).Value
            Assert.True(IO.Directory.Exists(directory), $"The .NET Framework 4.8 reference assemblies were not found: {directory}")

            Return {"mscorlib.dll", "System.dll", "System.Core.dll", "Microsoft.VisualBasic.dll"}.
                Select(Function(f As String) DirectCast(MetadataReference.CreateFromFile(Path.Combine(directory, f)), MetadataReference)).
                ToImmutableArray()
        End Function

        ''' <summary>
        ''' Loads the metadata references of the test runtime.
        ''' </summary>
        '''
        ''' <returns>
        ''' The metadata references.
        ''' </returns>
        Private Shared Function LoadReferences() As ImmutableArray(Of MetadataReference)

            Dim paths As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim trusted As String = TryCast(AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"), String)
            If trusted IsNot Nothing Then
                For Each path As String In trusted.Split(IO.Path.PathSeparator)
                    Dim fileName As String = IO.Path.GetFileName(path)
                    ' Only framework assemblies; test-only packages would leak into the analyzed compilations.
                    If fileName.StartsWith("System.", StringComparison.OrdinalIgnoreCase) OrElse
                       fileName.StartsWith("Microsoft.VisualBasic", StringComparison.OrdinalIgnoreCase) OrElse
                       fileName.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase) OrElse
                       fileName.Equals("netstandard.dll", StringComparison.OrdinalIgnoreCase) Then
                        paths.Add(path)
                    End If
                Next path
            End If

            Return paths.Select(Function(p As String) DirectCast(MetadataReference.CreateFromFile(p), MetadataReference)).ToImmutableArray()
        End Function

#End Region

    End Class

End Namespace

#End Region

#Region " TestFixAllDiagnosticProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Provides a fixed set of diagnostics to a <see cref="FixAllContext"/>.
    ''' </summary>
    Friend NotInheritable Class TestFixAllDiagnosticProvider : Inherits FixAllContext.DiagnosticProvider

        ''' <summary>
        ''' The diagnostics to provide.
        ''' </summary>
        Private ReadOnly diagnostics As ImmutableArray(Of Diagnostic)

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TestFixAllDiagnosticProvider"/> class.
        ''' </summary>
        '''
        ''' <param name="diagnostics">
        ''' The diagnostics to provide.
        ''' </param>
        Friend Sub New(diagnostics As ImmutableArray(Of Diagnostic))

            Me.diagnostics = diagnostics
        End Sub

        ''' <inheritdoc/>
        Public Overrides Function GetAllDiagnosticsAsync(project As Project, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of Diagnostic))

            Return Task.FromResult(Of IEnumerable(Of Diagnostic))(Me.diagnostics)
        End Function

        ''' <inheritdoc/>
        Public Overrides Function GetDocumentDiagnosticsAsync(document As Document, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of Diagnostic))

            Return Task.FromResult(Of IEnumerable(Of Diagnostic))(Me.diagnostics.Where(Function(d As Diagnostic) d.Location.SourceTree Is document.GetSyntaxTreeAsync(cancellationToken).GetAwaiter().GetResult()))
        End Function

        ''' <inheritdoc/>
        Public Overrides Function GetProjectDiagnosticsAsync(project As Project, cancellationToken As CancellationToken) As Task(Of IEnumerable(Of Diagnostic))

            Return Task.FromResult(Of IEnumerable(Of Diagnostic))(Me.diagnostics.Where(Function(d As Diagnostic) Not d.Location.IsInSource))
        End Function

    End Class

End Namespace

#End Region
