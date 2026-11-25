#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA082 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA082: IndexOf compared with zero instead of StartsWith.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA082 (Info) on <c>path.IndexOf(prefix, StringComparison.Ordinal) = 0</c>, which searches the whole
    ''' string when the prefix is not at the start. This rule runs only in .NET Framework projects (on modern .NET, the
    ''' built-in rule CA1858 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>path.StartsWith(prefix, StringComparison.Ordinal)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA082

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA082"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a path starts with a prefix.
        ''' </summary>
        '''
        ''' <param name="path">
        ''' The path.
        ''' </param>
        '''
        ''' <param name="prefix">
        ''' The prefix.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the path starts with the prefix; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasPrefix(path As String, prefix As String) As Boolean

            Return path.IndexOf(prefix, StringComparison.Ordinal) = 0 ' DCA082
        End Function

#End Region

    End Class

End Namespace

#End Region
