#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA009 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA009: String comparison argument omitted.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA009 (Info) on <c>filePath.EndsWith(".vb")</c>, which compares with the current culture.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): two fixes: <c>StringComparison.Ordinal</c> or <c>StringComparison.OrdinalIgnoreCase</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA009

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA009"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a file is a Visual Basic source file.
        ''' </summary>
        '''
        ''' <param name="filePath">
        ''' The file path.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the file is a Visual Basic source file; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsVisualBasicFile(filePath As String) As Boolean

            ' DCA009: the string comparison is not specified.
            Return filePath.EndsWith(".vb")
        End Function

#End Region

    End Class

End Namespace

#End Region
