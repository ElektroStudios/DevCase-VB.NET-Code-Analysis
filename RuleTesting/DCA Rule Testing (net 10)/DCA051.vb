#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA051 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA051: Awaiter or ConfigureAwait result discarded.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA051 (Warning) on <c>Task.Delay(100).ConfigureAwait(False)</c> as a statement: nothing waits for the task.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA051

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA051"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Starts a delay.
        ''' </summary>
        Public Shared Sub Start()

            Task.Delay(100).ConfigureAwait(False) ' DCA051
        End Sub

#End Region

    End Class

End Namespace

#End Region
