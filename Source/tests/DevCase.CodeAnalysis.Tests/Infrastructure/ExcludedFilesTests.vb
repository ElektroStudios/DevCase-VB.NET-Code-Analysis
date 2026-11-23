#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " ExcludedFilesTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests that every analyzer ignores the excluded source files (designer, generated, assembly information and 
    ''' global suppression files).
    ''' </summary>
    Public NotInheritable Class ExcludedFilesTests

#Region " Fields "

        ''' <summary>
        ''' A source that many rules report: no Option statements, no namespace, an undocumented public class, a silent guard, 
        ''' a call without parentheses, a single-character separator and an unvalidated parameter.
        ''' </summary>
        Private Const Source As String = "
Public Class Sample
    Public Function Run(text As String, parts As String()) As String
        If text Is Nothing Then Return Nothing
        Dim trimmed As String = text.Trim
        Return String.Join("","", parts) & trimmed
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ExcludedFilesTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies which file names are excluded: a pattern must be immediately followed by the extension of the file.
        ''' </summary>
        '''
        ''' <param name="filePath">
        ''' The file path.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' <see langword="True"/> if the file must be excluded.
        ''' </param>
        <Theory>
        <InlineData("C:\Project\Form1.Designer.vb", True)>
        <InlineData("C:\Project\Form1.designer.cs", True)>
        <InlineData("C:\Project\obj\Debug\.NETFramework,Version=v4.8.AssemblyAttributes.vb", True)>
        <InlineData("C:\Project\My Project\AssemblyInfo.vb", True)>
        <InlineData("C:\Project\obj\Debug\MyApp.AssemblyInfo.vb", True)>
        <InlineData("C:\Project\GlobalSuppressions.vb", True)>
        <InlineData("C:\Project\GlobalSuppressions.cs", True)>
        <InlineData("C:\Project\obj\MainWindow.g.vb", True)>
        <InlineData("C:\Project\obj\App.g.i.vb", True)>
        <InlineData("C:\Project\Reference.generated.vb", True)>
        <InlineData("C:\Project\obj\TemporaryGeneratedFile_E7A71F73-0F8D-4B9B-B56E-8E70B10BC5D3.vb", True)>
        <InlineData("C:\Project\Form1.vb", False)>
        <InlineData("C:\Project\Designer.vb", False)>
        <InlineData("C:\Project\Debug.vb", False)>
        <InlineData("C:\Project\AssemblyInfoReader.vb", False)>
        <InlineData("C:\Project\Form1.Designer.Helpers.vb", False)>
        <InlineData("", False)>
        Public Sub IsExcludedFilePath_MatchesPatternBeforeExtension(filePath As String, expected As Boolean)
            Assert.Equal(expected, UtilAnalyzerFiles.IsExcludedFilePath(filePath))
        End Sub

        ''' <summary>
        ''' Verifies that no analyzer reports diagnostics in an excluded file, while the same source in a regular file is reported.
        ''' </summary>
        '''
        ''' <param name="fileName">
        ''' The name of the excluded file.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("AssemblyInfo.vb")>
        <InlineData("GlobalSuppressions.vb")>
        <InlineData("Form1.Designer.vb")>
        <InlineData("MyApp.AssemblyAttributes.vb")>
        <InlineData("MainWindow.g.vb")>
        <InlineData("App.g.i.vb")>
        <InlineData("Reference.generated.vb")>
        Public Async Function EveryAnalyzer_IgnoresExcludedFiles(fileName As String) As Task
            Dim analyzers As ImmutableArray(Of DiagnosticAnalyzer) = GetType(DiagnosticIds).Assembly.GetTypes().
                Where(Function(t As Type) Not t.IsAbstract AndAlso GetType(DiagnosticAnalyzer).IsAssignableFrom(t)).
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), DiagnosticAnalyzer)).
                ToImmutableArray()

            Dim regular As Project = AnalyzerTestHost.CreateProject({New TestSource("Sample.vb", Source)})
            Dim excluded As Project = AnalyzerTestHost.CreateProject({New TestSource(fileName, Source)})

            Dim regularCount As Integer = 0
            Dim excludedDiagnostics As New Collections.Generic.List(Of Diagnostic)()
            For Each analyzer As DiagnosticAnalyzer In analyzers
                regularCount += (Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(regular, analyzer).ConfigureAwait(True)).Length
                excludedDiagnostics.AddRange(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(excluded, analyzer).ConfigureAwait(True))
            Next analyzer

            Assert.True(regularCount >= 5, $"The regular file should be reported by several rules ({regularCount}).")
            Assert.Empty(excludedDiagnostics)
        End Function

#End Region

    End Class

End Namespace

#End Region
