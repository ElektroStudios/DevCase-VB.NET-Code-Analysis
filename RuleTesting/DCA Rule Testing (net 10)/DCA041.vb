#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA041 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA041: All branches of a conditional structure are identical.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA041 (Warning) on the <c>If</c> statement, whose branches all do the same.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA041

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA041"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the maximum number of items.
        ''' </summary>
        '''
        ''' <param name="isAdministrator">
        ''' <see langword="True"/> for an administrator.
        ''' </param>
        '''
        ''' <returns>
        ''' The maximum number of items.
        ''' </returns>
        Public Shared Function GetLimit(isAdministrator As Boolean) As Integer

            Dim limit As Integer = If(isAdministrator, 100, 100)
            Return limit
        End Function

#End Region

    End Class

End Namespace

#End Region
