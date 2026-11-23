#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA043 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA043: Identical expressions on both sides of a binary operator.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA043 (Warning) on <c>value = value</c>, which is always <c>True</c>.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA043

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA043"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Compares a number with itself.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The number.
        ''' </param>
        '''
        ''' <returns>
        ''' Always <see langword="True"/>.
        ''' </returns>
        Public Shared Function IsSame(value As Integer) As Boolean

            ' DCA043: both operands are the same expression.
            Return value = value
        End Function

#End Region

    End Class

End Namespace

#End Region
