#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " DCA038_ThreadSuspendResumeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA038 (Thread.Suspend or Thread.Resume).
    ''' </summary>
    Public NotInheritable Class DCA038_ThreadSuspendResumeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThreadSuspendResumeAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA038_ThreadSuspendResumeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA038 reports Thread.Suspend and Thread.Resume (on .NET Framework, where they still compile) and skips other Thread methods.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SuspendAndResume() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading
Public Class C
#Disable Warning BC40000 ' Obsolete members: the rule is under test.
    Public Sub M(worker As Thread)
        [|worker.Suspend()|]
        [|worker.Resume()|]
        worker.Join()
    End Sub
#Enable Warning BC40000
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
