#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA027_ThreadSleepTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA027 (Thread.Sleep in an Async method) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA027_ThreadSleepTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThreadSleepAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ThreadSleepCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA027_ThreadSleepTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA027 reports Thread.Sleep in Async methods and lambdas, and skips synchronous code and Thread.Sleep(0).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SleepInAsyncMethodsAndLambdas() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading
Public Class C
    Public Async Function RunAsync() As Task
        [|Thread.Sleep(100)|]
        Thread.Sleep(0)
        Dim action As Action = Sub() Thread.Sleep(5)
        Dim work As Func(Of Task) = Async Function()
                                        [|Thread.Sleep(5)|]
                                        Await Task.Delay(1).ConfigureAwait(False)
                                    End Function
        Await work().ConfigureAwait(False)
    End Function
    Public Sub Run()
        Thread.Sleep(100)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the first action of rule DCA027 writes Await Task.Delay with ConfigureAwait(False), also for Call statements and TimeSpan arguments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ConfigureAwaitFalse_ReplacesStatements() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading
Public Class C
    Public Async Function RunAsync() As Task
        Thread.Sleep(100)
        Call Thread.Sleep(TimeSpan.FromSeconds(1))
        Await Task.Yield()
    End Function
End Class
", "
Imports System.Threading
Public Class C
    Public Async Function RunAsync() As Task
        Await Task.Delay(100).ConfigureAwait(False)
        Await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(False)
        Await Task.Yield()
    End Function
End Class
", equivalenceKey:="ThreadSleepCodeFixProvider.False").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the second action of rule DCA027 writes ConfigureAwait(True), with the shortest Task name, and that the code compiles on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ConfigureAwaitTrue_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Async Sub OnClick(sender As Object, e As EventArgs)
        Threading.Thread.Sleep(millisecondsTimeout:=250)
        Await Threading.Tasks.Task.Yield()
    End Sub
End Class
", "
Public Class C
    Public Async Sub OnClick(sender As Object, e As EventArgs)
        Await Task.Delay(250).ConfigureAwait(True)
        Await Threading.Tasks.Task.Yield()
    End Sub
End Class
", equivalenceKey:="ThreadSleepCodeFixProvider.True", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA027 reports Thread.Sleep inside Catch, Finally and SyncLock blocks, where Await is not allowed, without offering a fix.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NoFix_WhereAwaitIsNotAllowed() As Task
            Dim source As String = "
Imports System.Threading
Public Class C
    Private ReadOnly gate As New Object()
    Public Async Function RunAsync() As Task
        Try
            Await Task.Yield()
        Catch ex As InvalidOperationException
            [|Thread.Sleep(10)|]
        Finally
            [|Thread.Sleep(10)|]
        End Try
        SyncLock gate
            [|Thread.Sleep(10)|]
        End SyncLock
    End Function
End Class
"
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, source).ConfigureAwait(True)
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, source.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal)).ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

#End Region

    End Class

End Namespace

#End Region
