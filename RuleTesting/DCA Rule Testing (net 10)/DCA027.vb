#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading
Imports System.Threading.Tasks

#End Region

#Region " DCA027 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA027: Thread.Sleep in an Async method.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA027 (Warning) on <c>Thread.Sleep(100)</c>, which blocks the thread inside an <c>Async</c> method.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): two fixes: <c>Await Task.Delay(100).ConfigureAwait(False)</c> or <c>.ConfigureAwait(True)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA027

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA027"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Waits a moment.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous operation.
        ''' </returns>
        Public Shared Async Function WaitAsync() As Task

            ' DCA027: Thread.Sleep blocks the thread.
            Thread.Sleep(100)
            Await Task.Delay(1).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
