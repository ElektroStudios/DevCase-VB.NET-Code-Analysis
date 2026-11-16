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

#Region " DCA001_ConfigureAwaitTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA001 (missing ConfigureAwait(False)) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA001_ConfigureAwaitTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ConfigureAwaitAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ConfigureAwaitCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA001_ConfigureAwaitTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA001 reports awaiting a <see cref="Task"/> or <see cref="ValueTask"/> without <c>ConfigureAwait</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_UnconfiguredAwait_OnTaskAndValueTask() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task
        [|Await Task.Delay(1)|]
        Dim value As Integer = [|Await GetValueAsync()|]
        Dim other As Integer = [|Await New ValueTask(Of Integer)(5)|]
    End Function
    Private Function GetValueAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA001 does not report configured awaitables, or awaitables without <c>ConfigureAwait</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ConfiguredOrNonConfigurableAwaitables() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Runtime.CompilerServices
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync(source As C) As Task
        Await Task.Delay(1).ConfigureAwait(False)
        Await Task.Delay(1).ConfigureAwait(False)
        Await (Task.Delay(1).ConfigureAwait(False))
        Await Task.Delay(1).configureawait(False)
        Await Task.Yield()
        Dim configured As ConfiguredTaskAwaitable = Task.Delay(1).ConfigureAwait(False)
        Await configured
        Await (source?.GetTaskAsync()).ConfigureAwait(False)
    End Function
    Private Function GetTaskAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA001 appends <c>.ConfigureAwait(False)</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InjectsConfigureAwaitFalse() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task(Of Integer)
        [|Await Task.Delay(1)|] ' keep this comment
        Return [|Await GetValueAsync()|] + 1
    End Function
    Private Function GetValueAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class
", "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task(Of Integer)
        Await Task.Delay(1).ConfigureAwait(False) ' keep this comment
        Return Await GetValueAsync().ConfigureAwait(False) + 1
    End Function
    Private Function GetValueAsync() As Task(Of Integer)
        Return Task.FromResult(1)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA001 writes a named argument when the option is enabled.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesNamedArgument_WhenConfigured() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task
        Await Task.Delay(1)
    End Function
End Class
", "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task
        Await Task.Delay(1).ConfigureAwait(continueOnCapturedContext:=False)
    End Function
End Class
", editorConfig:="devcase_analyzers.configure_await_named_argument = true").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA001 parenthesizes null-conditional and nested <c>Await</c> expressions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ParenthesizesNullConditionalAndNestedAwait() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync(source As C) As Task
        Await source?.GetTaskAsync()
        Await Await GetNestedAsync()
        Dim task As Task = Task.CompletedTask
        Await If(task, Task.CompletedTask)
    End Function
    Private Function GetTaskAsync() As Task
        Return Task.CompletedTask
    End Function
    Private Function GetNestedAsync() As Task(Of Task)
        Return Task.FromResult(Task.CompletedTask)
    End Function
End Class
", "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync(source As C) As Task
        Await (source?.GetTaskAsync()).ConfigureAwait(False)
        Await (Await GetNestedAsync().ConfigureAwait(False)).ConfigureAwait(False)
        Dim task As Task = Task.CompletedTask
        Await If(task, Task.CompletedTask).ConfigureAwait(False)
    End Function
    Private Function GetTaskAsync() As Task
        Return Task.CompletedTask
    End Function
    Private Function GetNestedAsync() As Task(Of Task)
        Return Task.FromResult(Task.CompletedTask)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA001 configures every <c>Await</c> of a document.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_FixesEveryAwaitInDocument() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task
        Await Task.Delay(1)
        Await Task.Delay(2)
        Await Task.Delay(3)
    End Function
End Class
", NameOf(ConfigureAwaitCodeFixProvider)).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync() As Task
        Await Task.Delay(1).ConfigureAwait(False)
        Await Task.Delay(2).ConfigureAwait(False)
        Await Task.Delay(3).ConfigureAwait(False)
    End Function
End Class
", fixedText)
        End Function

#End Region

    End Class

End Namespace

#End Region
