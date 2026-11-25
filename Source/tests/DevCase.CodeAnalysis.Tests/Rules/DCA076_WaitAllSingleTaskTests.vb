#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA076_WaitAllSingleTaskTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA076 (WaitAll with a single task, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA076_WaitAllSingleTaskTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New WaitAllSingleTaskAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA076_WaitAllSingleTaskTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA076 reports <c>WaitAll</c> with one task, and ignores several tasks, explicit arrays and the
        ''' overloads with a timeout.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SingleTask() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(work As Task, other As Task, result As Task(Of Integer))
        [|Task.WaitAll(work)|]
        [|Task.WaitAll(result)|]
        Task.WaitAll(work, other)
        Task.WaitAll(New Task() {work})
        Task.WaitAll({work}, 100)
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA076 does not run in a modern .NET project, where the built-in rule CA1843 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(work As Task, other As Task, result As Task(Of Integer))
        Task.WaitAll(work)
        Task.WaitAll(result)
        Task.WaitAll(work, other)
        Task.WaitAll(New Task() {work})
        Task.WaitAll({work}, 100)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA076 writes <c>task.Wait()</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallsWait() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run(work As Task, other As Task, result As Task(Of Integer))
        [|Task.WaitAll(work)|]
        [|Task.WaitAll(result)|]
        Task.WaitAll(work, other)
        Task.WaitAll(New Task() {work})
        Task.WaitAll({work}, 100)
    End Sub
End Class
", "
Public Class C
    Public Sub Run(work As Task, other As Task, result As Task(Of Integer))
        work.Wait()
        result.Wait()
        Task.WaitAll(work, other)
        Task.WaitAll(New Task() {work})
        Task.WaitAll({work}, 100)
    End Sub
End Class
",
                                                      equivalenceKey:="DCA076.Wait", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
