#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA031 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA031: Array designator on the variable name.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA031 (Info) on <c>digits()</c>, whose array designator is on the name.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Dim digits As Integer() = ...</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA031

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA031"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Counts digits.
        ''' </summary>
        '''
        ''' <returns>
        ''' The number of digits.
        ''' </returns>
        Public Shared Function CountDigits() As Integer

            ' DCA031: the array designator is on the variable name.
            Dim digits() As Integer = {1, 2, 3}
            Return digits.Length
        End Function

#End Region

    End Class

End Namespace

#End Region
