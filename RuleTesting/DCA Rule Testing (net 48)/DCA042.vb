#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA042 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA042: Two branches of a conditional structure are identical.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA042 (Info) on <c>Case 3</c>, whose implementation is the same as <c>Case 1</c>.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA042

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA042"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the price of a category.
        ''' </summary>
        '''
        ''' <param name="category">
        ''' The category.
        ''' </param>
        '''
        ''' <returns>
        ''' The price.
        ''' </returns>
        Public Shared Function GetPrice(category As Integer) As Decimal

            Dim price As Decimal
            Select Case category
                Case 1
                    price = 10D
                Case 2
                    price = 20D
                Case 3 ' DCA042
                    price = 10D
                Case Else
                    price = 0D
            End Select
            Return price
        End Function

#End Region

    End Class

End Namespace

#End Region
