#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

#End Region

#Region " DCA056 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA056: Enumerable.Any() instead of IsEmpty, Count or Length.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA056 (Info) on <c>queue.Any()</c> on a <c>Queue(Of Integer)</c>, which has a <c>Count</c> property. This
    ''' rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1860 reports the same calls), so this
    ''' file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>queue.Count &lt;&gt; 0</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA056

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA056"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Empties a queue.
        ''' </summary>
        '''
        ''' <param name="queue">
        ''' The queue.
        ''' </param>
        Friend Shared Sub Drain(queue As Queue(Of Integer))

            While queue.Any() ' DCA056
                queue.Dequeue()
            End While
        End Sub

#End Region

    End Class

End Namespace

#End Region
