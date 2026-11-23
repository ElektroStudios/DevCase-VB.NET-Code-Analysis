#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading

#End Region

#Region " DCA037 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA037: Monitor.Enter without Monitor.Exit in a Finally block.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA037 (Warning) on <c>Monitor.Enter(Gate)</c>: if the work throws an exception, the lock is never released.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA037

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA037"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' The object used for locking.
        ''' </summary>
        Private Shared ReadOnly Gate As New Object()

        ''' <summary>
        ''' Does the work under the lock.
        ''' </summary>
        Public Shared Sub Work()

            Monitor.Enter(Gate) ' DCA037
            Console.WriteLine("Working...")
            Monitor.Exit(Gate)
        End Sub

#End Region

    End Class

End Namespace

#End Region
