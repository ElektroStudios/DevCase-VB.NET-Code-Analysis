#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA039 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA039: Delegate BeginInvoke without EndInvoke.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA039 (Warning) on <c>Work.BeginInvoke(Nothing, Nothing)</c>, whose result and exceptions are never
    ''' collected.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA039

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA039"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' The work to run.
        ''' </summary>
        Private Shared ReadOnly Work As Func(Of Integer) = Function() 42

        ''' <summary>
        ''' Starts the work.
        ''' </summary>
        Public Shared Sub Start()

            Work.BeginInvoke(Nothing, Nothing) ' DCA039
        End Sub

#End Region

    End Class

End Namespace

#End Region
