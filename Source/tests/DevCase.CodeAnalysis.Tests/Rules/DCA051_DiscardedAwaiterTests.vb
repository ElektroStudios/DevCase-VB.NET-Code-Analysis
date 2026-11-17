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

#Region " DCA051_DiscardedAwaiterTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA051 (awaiter or ConfigureAwait result discarded).
    ''' </summary>
    Public NotInheritable Class DCA051_DiscardedAwaiterTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DiscardedAwaiterAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA051_DiscardedAwaiterTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA051 reports statements that discard the result of GetAwaiter or ConfigureAwait, 
        ''' with and without parentheses, on Task, Task(Of T), ValueTask and configured awaitables.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_DiscardedAwaiters() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub FetchAlbums()
        [|Task.Run(Of Boolean)(AddressOf Me.FetchAlbumsAsync).GetAwaiter()|]
        [|Task.Run(AddressOf Me.FetchAlbumsAsync).GetAwaiter|]
        [|Me.FetchAlbumsAsync.ConfigureAwait(False)|]
        [|Me.WorkAsync().ConfigureAwait(True).GetAwaiter()|]
        Call [|Me.WorkAsync().GetAwaiter()|]
        [|Me.ValueAsync().ConfigureAwait(False)|]
    End Sub
    Private Async Function FetchAlbumsAsync() As Task(Of Boolean)
        Await Task.Delay(1).ConfigureAwait(False)
        Return True
    End Function
    Private Async Function WorkAsync() As Task
        Await Task.Delay(1).ConfigureAwait(False)
    End Function
    Private Function ValueAsync() As ValueTask
        Return New ValueTask(Task.Delay(1))
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA051 does not report awaited or waited tasks, used awaiters, or GetAwaiter methods of other types.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_UsedResults() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Runtime.CompilerServices
Public Class Custom
    Public Function GetAwaiter() As Integer
        Return 0
    End Function
End Class
Public Class C
    Public Async Function RunAsync(custom As Custom) As Task
        Dim awaiter As TaskAwaiter = Me.WorkAsync().GetAwaiter()
        awaiter.GetResult()
        Me.WorkAsync().GetAwaiter().GetResult()
        Await Me.WorkAsync().ConfigureAwait(False)
        Me.WorkAsync().Wait()
        custom.GetAwaiter()
    End Function
    Private Async Function WorkAsync() As Task
        Await Task.Delay(1).ConfigureAwait(False)
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
