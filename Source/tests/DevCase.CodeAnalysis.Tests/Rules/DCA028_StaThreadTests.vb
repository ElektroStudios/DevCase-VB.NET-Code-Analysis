#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA028_StaThreadTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA028 (Windows Forms entry point without STAThread) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA028_StaThreadTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New StaThreadAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New StaThreadCodeFixProvider()

        ''' <summary>
        ''' A minimal stand-in for the Windows Forms <c>Application</c> type, so that a test project "references" Windows Forms.
        ''' </summary>
        Friend Const WindowsFormsStub As String = "
Namespace System.Windows.Forms
    Public NotInheritable Class Application
        Private Sub New()
        End Sub
        Public Shared Sub DoEvents()
        End Sub
    End Class
End Namespace
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA028_StaThreadTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA028 reports Sub Main without STAThread when Windows Forms is referenced, and skips marked methods and other methods.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_MainWithoutApartmentAttribute() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"
Public Module Program
    Public Sub [|Main|]()
        Windows.Forms.Application.DoEvents()
    End Sub
End Module
Public NotInheritable Class Other
    Private Sub New()
    End Sub
    <MTAThread>
    Public Shared Function Main(args As String()) As Integer
        Return 0
    End Function
    Public Shared Sub Run()
    End Sub
End Class
Public NotInheritable Class Marked
    Private Sub New()
    End Sub
    <STAThread>
    Public Shared Sub Main(args As String())
    End Sub
    Public Sub Main(value As Integer)
    End Sub
End Class
{DCA028_StaThreadTests.WindowsFormsStub}").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA028 does not report Sub Main in projects that do not reference Windows Forms.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_WithoutWindowsForms() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Module Program
    Public Sub Main()
    End Sub
End Module
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA028 adds STAThread before the existing attributes and modifiers, keeping the documentation comment.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsTheAttribute() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"
Public Module Program
    ''' <summary>
    ''' The entry point.
    ''' </summary>
    <Diagnostics.DebuggerStepThrough>
    Public Sub Main()
    End Sub
End Module
{DCA028_StaThreadTests.WindowsFormsStub}", $"
Public Module Program
    ''' <summary>
    ''' The entry point.
    ''' </summary>
    <STAThread>
    <Diagnostics.DebuggerStepThrough>
    Public Sub Main()
    End Sub
End Module
{DCA028_StaThreadTests.WindowsFormsStub}").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
