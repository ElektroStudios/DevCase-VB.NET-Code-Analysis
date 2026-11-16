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

#Region " DCA002_TaskResultTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA002 (naked .Result on tasks) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA002_TaskResultTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New TaskResultAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New TaskResultCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA002_TaskResultTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA002 reports reading <c>Result</c> on a <see cref="Task"/> or <see cref="ValueTask"/>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ResultOnTaskAndValueTask() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading.Tasks
Public Class C
    Public Sub Run(work As Task(Of Integer), valueWork As ValueTask(Of String))
        Dim a As Integer = [|work.Result|]
        Dim b As Integer = [|work.Result()|]
        Dim c As String = [|valueWork.Result|]
        Dim d As Integer = [|Task.FromResult(3).Result|]
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA002 does not report <c>Result</c> members of types that are not tasks.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_LookalikesThatAreNotTasks() As Task
            ' The original text heuristic matched "Task"/"Async" in any expression text.
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading.Tasks
Public Class AsyncTaskSettings
    Public Property Result As Integer
End Class
Public Class C
    Public Sub Run(myAsyncTask As AsyncTaskSettings, work As Task(Of Integer))
        Dim a As Integer = myAsyncTask.Result
        Dim name As String = NameOf(work.Result)
        Dim b As Integer = work.GetAwaiter().GetResult()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA002 replaces every form of <c>Result</c> access.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReplacesAllForms() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading.Tasks
Public Class C
    Public Sub Run(work As Task(Of Integer), maybe As Task(Of Integer))
        Dim a As Integer = work.Result ' comment
        Dim b As Integer = work.Result()
        Dim c As Integer? = maybe?.Result
        With work
            Dim d As Integer = .Result
        End With
        Dim e As String = Task.FromResult(1).Result.ToString()
    End Sub
End Class
", "
Imports System.Threading.Tasks
Public Class C
    Public Sub Run(work As Task(Of Integer), maybe As Task(Of Integer))
        Dim a As Integer = work.GetAwaiter().GetResult() ' comment
        Dim b As Integer = work.GetAwaiter().GetResult()
        Dim c As Integer? = maybe?.GetAwaiter().GetResult()
        With work
            Dim d As Integer = .GetAwaiter().GetResult()
        End With
        Dim e As String = Task.FromResult(1).GetAwaiter().GetResult().ToString()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
