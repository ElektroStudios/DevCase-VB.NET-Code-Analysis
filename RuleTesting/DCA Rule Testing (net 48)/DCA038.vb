#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading

#End Region

#Region " DCA038 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA038: Thread.Suspend or Thread.Resume.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA038 (Warning) on <c>worker.Suspend()</c>. The compiler also reports these methods as obsolete (BC40000):
    ''' that warning is disabled around the call, so that the Error List shows only the DCA rule.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA038

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA038"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Pauses a thread.
        ''' </summary>
        '''
        ''' <param name="worker">
        ''' The thread.
        ''' </param>
        Friend Shared Sub Pause(worker As Thread)

#Disable Warning BC40000 ' Thread.Suspend is obsolete: this file only demonstrates rule DCA038.
            worker.Suspend() ' DCA038
#Enable Warning BC40000
        End Sub

#End Region

    End Class

End Namespace

#End Region
