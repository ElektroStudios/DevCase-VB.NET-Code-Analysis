#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA053 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA053: Method call without parentheses.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA053 (Info) on <c>text.Trim</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>text.Trim()</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA053

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA053"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets a trimmed text.
        ''' </summary>
        '''
        ''' <returns>
        ''' The trimmed text.
        ''' </returns>
        Public Shared Function GetTrimmed() As String

            Dim text As String = "  value  "
            Return text.Trim ' DCA053
        End Function

#End Region

    End Class

End Namespace

#End Region
