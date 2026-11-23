#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA036 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA036: SyncLock on a publicly accessible or weak-identity object.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA036 (Warning) on <c>SyncLock Me</c>: any code that has a reference to the instance can lock it too.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA036

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA036"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' The number of increments.
        ''' </summary>
        Private counter As Integer

        ''' <summary>
        ''' Increments the counter.
        ''' </summary>
        '''
        ''' <returns>
        ''' The new value of the counter.
        ''' </returns>
        Public Function Increment() As Integer

            SyncLock Me ' DCA036
                Me.counter += 1
                Return Me.counter
            End SyncLock
        End Function

#End Region

    End Class

End Namespace

#End Region
