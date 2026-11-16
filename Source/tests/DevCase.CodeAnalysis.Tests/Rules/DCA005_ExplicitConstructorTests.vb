#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA005_ExplicitConstructorTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA005 (missing explicit constructors) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA005_ExplicitConstructorTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ExplicitConstructorAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ExplicitConstructorCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA005_ExplicitConstructorTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA005 reports classes without an explicit instance constructor.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ClassesWithoutInstanceConstructor() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class [|A|]
End Class
Public Class [|B|]
    Shared Sub New()
    End Sub
    Public Class [|Nested|]
    End Class
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA005 does not report modules, generated classes and the other excluded types.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ExcludedTypes() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class WithConstructor
    Private Sub New()
    End Sub
End Class
Public Module M
End Module
Public Structure S
End Structure
<Microsoft.VisualBasic.CompilerServices.DesignerGenerated>
Partial Public Class Form1
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA005 does not report a partial class whose constructor is declared in another file.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_PartialClassWithConstructorInAnotherFile() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", "Partial Public Class P" & vbLf & "    Public Property X As Integer" & vbLf & "End Class" & vbLf),
                New TestSource("B.vb", "Partial Public Class P" & vbLf & "    Public Sub New()" & vbLf & "    End Sub" & vbLf & "End Class" & vbLf)})
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA005 reports a partial class once, on its primary declaration.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_PartialClassOnlyOnce() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", "Partial Public Class P" & vbLf & "End Class" & vbLf),
                New TestSource("B.vb", "Partial Public Class P" & vbLf & "End Class" & vbLf)})
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Assert.Single(diagnostics)
            Assert.EndsWith("A.vb", diagnostics(0).Location.SourceTree.FilePath, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA005 inserts the <c>Constructors</c> region after the <c>Properties</c> region.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InsertsRegionAfterPropertiesRegion() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class Sample

#Region "" Properties ""

    ''' <summary>
    ''' Gets the value.
    ''' </summary>
    Public ReadOnly Property Value As Integer

#End Region

#Region "" Public Methods ""

    ''' <summary>
    ''' Runs.
    ''' </summary>
    Public Sub Run()
    End Sub

#End Region

End Class
", "
Public Class Sample

#Region "" Properties ""

    ''' <summary>
    ''' Gets the value.
    ''' </summary>
    Public ReadOnly Property Value As Integer

#End Region

#Region "" Constructors ""

    ''' <summary>
    ''' Initializes a new instance of the <see cref=""Sample""/> class.
    ''' </summary>
    Public Sub New()
    End Sub

#End Region

#Region "" Public Methods ""

    ''' <summary>
    ''' Runs.
    ''' </summary>
    Public Sub Run()
    End Sub

#End Region

End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA005 chooses the constructor accessibility from the shape of the class.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ChoosesAccessibilityFromClassShape() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Namespace N
    Public NotInheritable Class Util
        Public Shared Function Twice(value As Integer) As Integer
            Return value * 2
        End Function
    End Class
    Public MustInherit Class Base(Of T)
        Public MustOverride Sub Run()
    End Class
    Public Class Empty
    End Class
End Namespace
", "
Namespace N
    Public NotInheritable Class Util

#Region "" Constructors ""

        ''' <summary>
        ''' Prevents a default instance of the <see cref=""Util""/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

        Public Shared Function Twice(value As Integer) As Integer
            Return value * 2
        End Function
    End Class
    Public MustInherit Class Base(Of T)

#Region "" Constructors ""

        ''' <summary>
        ''' Initializes a new instance of the <see cref=""Base(Of T)""/> class.
        ''' </summary>
        Protected Sub New()
        End Sub

#End Region

        Public MustOverride Sub Run()
    End Class
    Public Class Empty

#Region "" Constructors ""

        ''' <summary>
        ''' Initializes a new instance of the <see cref=""Empty""/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

    End Class
End Namespace
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
