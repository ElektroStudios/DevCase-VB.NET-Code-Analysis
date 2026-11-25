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

#Region " DCA075_WhenAllSingleTaskTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA075 (WhenAll with a single task, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA075_WhenAllSingleTaskTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New WhenAllSingleTaskAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA075_WhenAllSingleTaskTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA075 reports the non-generic <c>WhenAll</c> with one task, and ignores several tasks, explicit
        ''' arrays and the generic overload.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SingleTask() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Async Function RunAsync(work As Task, other As Task, result As Task(Of Integer)) As Task
        Await [|Task.WhenAll(work)|]
        Await Task.WhenAll(work, other)
        Await Task.WhenAll(New Task() {work})
        Dim values As Integer() = Await Task.WhenAll(result)
        Dim combined As Task = [|Task.WhenAll(If(work, other))|]
        Await combined
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA075 does not run in a modern .NET project, where the built-in rule CA1842 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Async Function RunAsync(work As Task, other As Task, result As Task(Of Integer)) As Task
        Await Task.WhenAll(work)
        Await Task.WhenAll(work, other)
        Await Task.WhenAll(New Task() {work})
        Dim values As Integer() = Await Task.WhenAll(result)
        Dim combined As Task = Task.WhenAll(If(work, other))
        Await combined
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA075 replaces the call with the task.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesTheTask() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Async Function RunAsync(work As Task, other As Task, result As Task(Of Integer)) As Task
        Await [|Task.WhenAll(work)|]
        Await Task.WhenAll(work, other)
        Await Task.WhenAll(New Task() {work})
        Dim values As Integer() = Await Task.WhenAll(result)
        Dim combined As Task = [|Task.WhenAll(If(work, other))|]
        Await combined
    End Function
End Class
", "
Public Class C
    Public Async Function RunAsync(work As Task, other As Task, result As Task(Of Integer)) As Task
        Await work
        Await Task.WhenAll(work, other)
        Await Task.WhenAll(New Task() {work})
        Dim values As Integer() = Await Task.WhenAll(result)
        Dim combined As Task = If(work, other)
        Await combined
    End Function
End Class
",
                                                      equivalenceKey:="DCA075.Task", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
