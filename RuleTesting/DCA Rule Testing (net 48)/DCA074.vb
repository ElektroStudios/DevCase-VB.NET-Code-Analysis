#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Concurrent

#End Region

#Region " DCA074 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA074: Count compared with zero instead of IsEmpty.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA074 (Info) on <c>queue.Count &gt; 0</c>: <c>Count</c> takes a snapshot of the concurrent queue, while
    ''' <c>IsEmpty</c> only checks whether there is an element. This rule runs only in .NET Framework projects (on modern
    ''' .NET, the built-in rule CA1836 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Not queue.IsEmpty</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA074

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA074"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether there is pending work.
        ''' </summary>
        '''
        ''' <param name="queue">
        ''' The work queue.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the queue has an item; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasWork(queue As ConcurrentQueue(Of Integer)) As Boolean

            Return queue.Count > 0 ' DCA074
        End Function

#End Region

    End Class

End Namespace

#End Region
