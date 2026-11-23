#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA015 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA015: Method calls itself (recursion).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA015 (Warning) on the call to <c>Factorial</c> inside <c>Factorial</c>.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA015

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA015"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Calculates the factorial of a number.
        ''' </summary>
        '''
        ''' <param name="number">
        ''' The number.
        ''' </param>
        '''
        ''' <returns>
        ''' The factorial.
        ''' </returns>
        Public Shared Function Factorial(number As Integer) As Long

            ' DCA015: the method calls itself.
            Return If(number <= 1, 1L, number * Factorial(number - 1))
        End Function

#End Region

    End Class

End Namespace

#End Region
