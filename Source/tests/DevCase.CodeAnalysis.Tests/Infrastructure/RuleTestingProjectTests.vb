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
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

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

#End Region

#Region " Private Methods "

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
