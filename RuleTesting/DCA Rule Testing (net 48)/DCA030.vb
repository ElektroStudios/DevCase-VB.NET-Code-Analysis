#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA030 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA030: Array creation expression can be an array literal.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA030 (Info) on <c>New Integer()</c>, which the type of the variable already gives.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>{2, 3, 5, 7}</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA030

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA030"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the first prime numbers.
        ''' </summary>
        '''
        ''' <returns>
        ''' The prime numbers.
        ''' </returns>
        Public Shared Function GetPrimes() As Integer()

            ' DCA030: New Integer() is redundant.
            Dim primes As Integer() = New Integer() {2, 3, 5, 7}
            Return primes
        End Function

#End Region

    End Class

End Namespace

#End Region
