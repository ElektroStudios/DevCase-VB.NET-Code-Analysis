#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA025 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA025: Redundant null check before TypeOf.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA025 (Info) on <c>value IsNot Nothing</c>, because <c>TypeOf value Is String</c> is already <c>False</c>
    ''' for <c>Nothing</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>TypeOf value Is String</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA025

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA025"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a value is a string.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The value.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the value is a string; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsText(value As Object) As Boolean

            ' DCA025: TypeOf already returns False for Nothing.
            Return value IsNot Nothing AndAlso TypeOf value Is String
        End Function

#End Region

    End Class

End Namespace

#End Region
