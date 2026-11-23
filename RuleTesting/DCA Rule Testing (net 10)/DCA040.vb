#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA040 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA040: Duplicated condition in If/ElseIf or Select Case.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA040 (Warning) on the second <c>value = 1</c> condition, which can never be reached.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA040

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA040"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Describes a number.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The number.
        ''' </param>
        '''
        ''' <returns>
        ''' The description.
        ''' </returns>
        Public Shared Function Describe(value As Integer) As String

            If value = 1 Then
                Return "one"
            ElseIf value = 2 Then
                Return "two"
            ElseIf value = 1 Then ' DCA040
                Return "one"
            End If
            Return "many"
        End Function

#End Region

    End Class

End Namespace

#End Region
