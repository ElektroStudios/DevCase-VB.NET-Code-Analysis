#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA076 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA076: Task.WaitAll with a single task.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA076 (Info) on <c>Task.WaitAll(work)</c>, which allocates an array to wait for one task. This rule runs
    ''' only in .NET Framework projects (on modern .NET, the built-in rule CA1843 reports the same code), so this file is
    ''' only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>work.Wait()</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA076

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA076"/> class from being created.
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
        Friend Shared Sub Complete(work As Task)

            Task.WaitAll(work) ' DCA076
        End Sub

#End Region

    End Class

End Namespace

#End Region
