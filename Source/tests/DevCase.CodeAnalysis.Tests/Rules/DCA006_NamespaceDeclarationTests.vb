#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA006_NamespaceDeclarationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA006 (types outside namespaces) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA006_NamespaceDeclarationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New NamespaceDeclarationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New NamespaceDeclarationCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA006_NamespaceDeclarationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA006 reports every kind of top-level type declared in the global namespace.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_EveryTopLevelTypeKind() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class [|C|]
    Public Class Nested
    End Class
End Class
Public Module [|M|]
End Module
Public Structure [|S|]
End Structure
Public Interface [|I|]
End Interface
Public Enum [|E|]
    A
End Enum
Public Delegate Sub [|D|]()
Namespace N
    Public Class Inside
    End Class
End Namespace
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA006 does not report designer-generated forms.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_DesignerGeneratedForm() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
<Microsoft.VisualBasic.CompilerServices.DesignerGenerated>
Partial Public Class Form1
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA006 keeps region directives outside the new block and documentation inside it.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_WrapsKeepingRegionOutsideAndDocInside() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "Imports System

#Region "" Sample ""

''' <summary>
''' A sample.
''' </summary>
Public Class Sample
    Public Sub Run()
        Dim text As String = ""a
  b""
    End Sub
End Class

#End Region
", "Imports System

#Region "" Sample ""

Namespace TestProject

    ''' <summary>
    ''' A sample.
    ''' </summary>
    Public Class Sample
        Public Sub Run()
            Dim text As String = ""a
  b""
        End Sub
    End Class

End Namespace

#End Region
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA006 uses the configured namespace and wraps all the types in one block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesConfiguredNamespace_AndWrapsAllTypesOnce() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "Public Class A
End Class

Public Enum B
    X
End Enum
", "Namespace Contoso.Tools

    Public Class A
    End Class

    Public Enum B
        X
    End Enum

End Namespace
", editorConfig:="devcase_analyzers.default_namespace = Contoso.Tools").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA006 ignores an invalid configured namespace and uses the assembly name.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InvalidConfiguredNamespace_FallsBackToAssemblyName() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "Public Class A
End Class
", "Namespace TestProject

    Public Class A
    End Class

End Namespace
", editorConfig:="devcase_analyzers.default_namespace = 1 bad name").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA006 wraps every partial declaration of the type.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_WrapsEveryPartialDeclaration() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", "Partial Public Class P" & vbLf & "End Class" & vbLf),
                New TestSource("B.vb", "Partial Public Class P" & vbLf & "    Public Sub Run()" & vbLf & "    End Sub" & vbLf & "End Class" & vbLf)})

            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Assert.Single(diagnostics)

            Dim actions As New List(Of CodeAction)()
            Dim document As Document = project.GetDocument(diagnostics(0).Location.SourceTree)
            Await Provider.RegisterCodeFixesAsync(New CodeFixContext(document, diagnostics(0),
                                                                     Sub(a As CodeAction, d As ImmutableArray(Of Diagnostic)) actions.Add(a),
                                                                     CancellationToken.None)).ConfigureAwait(True)
            Dim operations As ImmutableArray(Of CodeActionOperation) = Await actions.Single().GetOperationsAsync(CancellationToken.None).ConfigureAwait(True)
            Dim newSolution As Solution = operations.OfType(Of ApplyChangesOperation)().Single().ChangedSolution
            Dim newProject As Project = newSolution.GetProject(project.Id)

            For Each newDocument As Document In newProject.Documents
                Dim text As String = (Await newDocument.GetTextAsync().ConfigureAwait(True)).ToString()
                Assert.StartsWith("Namespace TestProject", text, StringComparison.Ordinal)
            Next newDocument

            Await AnalyzerTestHost.AssertCompilesAsync(newProject, "fixed partial class").ConfigureAwait(True)
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(newProject, Analyzer).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA006 does not report types of a project whose root namespace is set.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_WhenProjectRootNamespaceIsSet() As Task
            ' The type already lives in "MyApp"; wrapping it would produce "MyApp.MyApp.C".
            Dim project As Project = AnalyzerTestHost.CreateProject({New TestSource("Test0.vb", "Public Class C" & vbLf & "End Class" & vbLf)}, rootNamespace:="MyApp")
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA006 does not report types declared in an explicit <c>Namespace Global</c> block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ExplicitGlobalNamespaceBlock() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Namespace Global
    Public Class C
    End Class
End Namespace
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA006 reuses the namespace already declared in the same file.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReusesNamespaceAlreadyDeclaredInFile() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "Namespace Contoso.Data
    Public Class Existing
    End Class
End Namespace

Public Class Orphan
End Class
", "Namespace Contoso.Data
    Public Class Existing
    End Class
End Namespace

Namespace Contoso.Data

    Public Class Orphan
    End Class

End Namespace
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies how assembly names are converted into valid namespace names.
        ''' </summary>
        '''
        ''' <param name="assemblyName">
        ''' The assembly name.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' The expected namespace name, or <see langword="Nothing"/> if none can be derived.
        ''' </param>
        <Theory>
        <InlineData("MyApp", "MyApp")>
        <InlineData("My-App.2026", "My_App._2026")>
        <InlineData("Contoso.Class", "Contoso.[Class]")>
        <InlineData("..", Nothing)>
        Public Sub GetNamespaceFromAssemblyName(assemblyName As String, expected As String)
            Assert.Equal(expected, NamespaceDeclarationCodeFixProvider.GetNamespaceFromAssemblyName(assemblyName))
        End Sub

        ''' <summary>
        ''' Verifies that converting an empty assembly name into a namespace name is rejected.
        ''' </summary>
        '''
        ''' <param name="assemblyName">
        ''' An empty or white-space assembly name.
        ''' </param>
        <Theory>
        <InlineData("")>
        <InlineData(" ")>
        <InlineData(Nothing)>
        Public Sub GetNamespaceFromAssemblyName_RejectsEmptyNames(assemblyName As String)
            Assert.Throws(Of ArgumentNullException)(Function() NamespaceDeclarationCodeFixProvider.GetNamespaceFromAssemblyName(assemblyName))
        End Sub

#End Region

    End Class

End Namespace

#End Region
