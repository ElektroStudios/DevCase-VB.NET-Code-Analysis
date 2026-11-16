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
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Imports Xunit

#End Region

#Region " SelfAnalysisTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Dogfooding: the analyzers and code fixes must satisfy their own rules.
    ''' </summary>
    Public NotInheritable Class SelfAnalysisTests

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SelfAnalysisTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that the source code of the analyzers and code fixes satisfies every rule (dogfooding).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function OwnSourceCode_SatisfiesEveryRule() As Task
            Dim sourceRoot As String = SelfAnalysisTests.FindSourceDirectory()
            Dim sources As List(Of TestSource) = Directory.EnumerateFiles(sourceRoot, "*.vb", SearchOption.AllDirectories).
                Where(Function(p As String) Not p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal) AndAlso
                                            Not p.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)).
                Select(Function(p As String) New TestSource(Path.GetRelativePath(sourceRoot, p).Replace(Path.DirectorySeparatorChar, "_"c), File.ReadAllText(p, System.Text.Encoding.UTF8))).
                ToList()
            Assert.True(sources.Count > 20, $"Only {sources.Count} source files found under {sourceRoot}.")

            ' Roslyn and MEF assemblies (the compiled sources themselves are excluded to avoid duplicated types).
            Dim references As IEnumerable(Of MetadataReference) = Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll").
                Where(Function(p As String)
                          Dim name As String = Path.GetFileName(p)
                          Return name.StartsWith("Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase) OrElse
                                 name.StartsWith("System.Composition", StringComparison.OrdinalIgnoreCase)
                      End Function).
                Select(Function(p As String) DirectCast(MetadataReference.CreateFromFile(p), MetadataReference))

            Dim project As Project = AnalyzerTestHost.CreateProject(sources, editorConfig:="devcase_analyzers.dca010_enabled = true",
                                                                    additionalReferences:=references)
            Await AnalyzerTestHost.AssertCompilesAsync(project, "analyzer source code").ConfigureAwait(True)

            Dim analyzers As DiagnosticAnalyzer() = DescriptorAndRegistrationTests.GetAnalyzerTypes().
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), DiagnosticAnalyzer)).ToArray()
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzers).ConfigureAwait(True)

            Assert.True(diagnostics.IsEmpty, String.Join(vbLf, diagnostics.Select(Function(d As Diagnostic) d.ToString())))
        End Function

        ''' <summary>
        ''' Verifies that the source code of the analyzers and code fixes contains no recursive method.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function OwnSourceCode_ContainsNoRecursion() As Task
            ' Coding standard: no method may invoke itself; tree walks must be iterative.
            Dim sourceRoot As String = SelfAnalysisTests.FindSourceDirectory()
            Dim sources As List(Of TestSource) = Directory.EnumerateFiles(sourceRoot, "*.vb", SearchOption.AllDirectories).
                Where(Function(p As String) Not p.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal)).
                Select(Function(p As String) New TestSource(Path.GetRelativePath(sourceRoot, p).Replace(Path.DirectorySeparatorChar, "_"c), File.ReadAllText(p, System.Text.Encoding.UTF8))).
                ToList()
            Dim references As IEnumerable(Of MetadataReference) = Directory.EnumerateFiles(AppContext.BaseDirectory, "Microsoft.CodeAnalysis*.dll").
                Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "System.Composition*.dll")).
                Select(Function(p As String) DirectCast(MetadataReference.CreateFromFile(p), MetadataReference))

            Dim project As Project = AnalyzerTestHost.CreateProject(sources, additionalReferences:=references)
            Dim compilation As Compilation = Await project.GetCompilationAsync().ConfigureAwait(True)
            Dim offenders As New List(Of String)()

            For Each tree As SyntaxTree In compilation.SyntaxTrees
                Dim model As SemanticModel = compilation.GetSemanticModel(tree)
                For Each invocation As VisualBasic.Syntax.InvocationExpressionSyntax In (Await tree.GetRootAsync().ConfigureAwait(True)).DescendantNodes().OfType(Of VisualBasic.Syntax.InvocationExpressionSyntax)()
                    Dim target As ISymbol = model.GetSymbolInfo(invocation).Symbol
                    Dim container As ISymbol = model.GetEnclosingSymbol(invocation.SpanStart)
                    If target IsNot Nothing AndAlso SymbolEqualityComparer.Default.Equals(target.OriginalDefinition, container?.OriginalDefinition) Then
                        offenders.Add($"{tree.FilePath}: {container.ToDisplayString()}")
                    End If
                Next invocation
            Next tree

            Assert.True(offenders.Count = 0, $"Recursive methods found:{vbLf}{String.Join(vbLf, offenders)}")
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Finds the <c>src</c> directory of the repository by walking up from the test output directory.
        ''' </summary>
        '''
        ''' <returns>
        ''' The full path of the <c>src</c> directory.
        ''' </returns>
        '''
        ''' <exception cref="DirectoryNotFoundException">
        ''' Thrown when no ancestor of the test output directory contains the <c>src</c> directory of the solution.
        ''' </exception>
        Private Shared Function FindSourceDirectory() As String

            Dim directory As New DirectoryInfo(AppContext.BaseDirectory)
            While directory IsNot Nothing
                Dim candidate As String = Path.Combine(directory.FullName, "src")
                If IO.Directory.Exists(Path.Combine(candidate, "DevCase.CodeAnalysis.Analyzers")) Then
                    Return candidate
                End If
                directory = directory.Parent
            End While
            Throw New DirectoryNotFoundException("The 'src' directory of the solution was not found.")
        End Function

#End Region

    End Class

End Namespace

#End Region
