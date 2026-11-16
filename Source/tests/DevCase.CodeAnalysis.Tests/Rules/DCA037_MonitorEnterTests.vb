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

#Region " DCA037_MonitorEnterTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA037 (Monitor.Enter without Monitor.Exit in a Finally block).
    ''' </summary>
    Public NotInheritable Class DCA037_MonitorEnterTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New MonitorEnterAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA037_MonitorEnterTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA037 reports Monitor.Enter and Monitor.TryEnter calls whose lock is not released in a Finally block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_UnprotectedLocks() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading
Public Class C
    Private ReadOnly gate As New Object()
    Private ReadOnly other As New Object()
    Public Sub Unprotected()
        [|Monitor.Enter(gate)|]
        Console.WriteLine(""work"")
        Monitor.Exit(gate)
    End Sub
    Public Sub WrongLock()
        [|Monitor.Enter(gate)|]
        Try
            Console.WriteLine(""work"")
        Finally
            Monitor.Exit(other)
        End Try
    End Sub
    Public Sub NotInFinally()
        If [|Monitor.TryEnter(gate)|] Then
            Console.WriteLine(""work"")
            Monitor.Exit(gate)
        End If
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA037 accepts the three Try/Finally patterns.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ReleasedInFinally() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading
Public Class C
    Private ReadOnly gate As New Object()
    Public Sub EnterBeforeTry()
        Monitor.Enter(gate)
        Try
            Console.WriteLine(""work"")
        Finally
            Monitor.Exit(gate)
        End Try
    End Sub
    Public Sub LockTaken()
        Dim taken As Boolean = False
        Try
            Monitor.Enter(gate, taken)
            Console.WriteLine(""work"")
        Finally
            If taken Then
                Monitor.Exit(gate)
            End If
        End Try
    End Sub
    Public Sub TryEnter()
        If Monitor.TryEnter(gate, 100) Then
            Try
                Console.WriteLine(""work"")
            Finally
                Monitor.Exit(gate)
            End Try
        End If
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
