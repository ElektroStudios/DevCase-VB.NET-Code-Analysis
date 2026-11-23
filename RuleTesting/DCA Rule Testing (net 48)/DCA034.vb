#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA034 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA034: GoTo statement.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA034 (Info) on <c>GoTo Again</c>.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA034

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA034"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Counts down to zero.
        ''' </summary>
        '''
        ''' <param name="start">
        ''' The initial value.
        ''' </param>
        '''
        ''' <returns>
        ''' The final value.
        ''' </returns>
        Public Shared Function CountDown(start As Integer) As Integer

            Dim current As Integer = start
Again:
            current -= 1
            If current > 0 Then
                GoTo Again ' DCA034
            End If
            Return current
        End Function

#End Region

    End Class

End Namespace

#End Region
