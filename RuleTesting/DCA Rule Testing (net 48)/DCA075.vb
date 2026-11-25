#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA075 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA075: Task.WhenAll with a single task.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA075 (Info) on <c>Task.WhenAll(work)</c>, which allocates an array and a combined task to wait for one
    ''' task. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1842 reports the same
    ''' code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Await work.ConfigureAwait(False)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA075

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA075"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Waits for a task.
        ''' </summary>
        '''
        ''' <param name="work">
        ''' The task.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous operation.
        ''' </returns>
        Friend Shared Async Function CompleteAsync(work As Task) As Task

            Await Task.WhenAll(work).ConfigureAwait(False) ' DCA075
        End Function

#End Region

    End Class

End Namespace

#End Region
