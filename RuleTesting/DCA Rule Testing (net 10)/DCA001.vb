#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA001 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA001: Missing ConfigureAwait(False).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA001 (Error) on the <c>Await</c> of <c>Task.Delay(1)</c>, which is not configured.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Await Task.Delay(1).ConfigureAwait(False)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA001

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA001"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Waits one millisecond.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous operation.
        ''' </returns>
        Public Shared Async Function WaitAsync() As Task

            ' DCA001: the awaited task is not configured.
            Await Task.Delay(1)
        End Function

#End Region

    End Class

End Namespace

#End Region
