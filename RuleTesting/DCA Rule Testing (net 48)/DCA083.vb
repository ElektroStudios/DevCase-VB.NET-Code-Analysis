#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA083 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA083: Redundant length argument of Substring.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA083 (Info) on <c>text.Length - start</c>, the length argument of <c>Substring</c> that already reads to
    ''' the end of the string. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1514
    ''' reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>text.Substring(start)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA083

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA083"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the end of a text.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text.
        ''' </param>
        '''
        ''' <param name="start">
        ''' The index of the first character.
        ''' </param>
        '''
        ''' <returns>
        ''' The text from <paramref name="start"/> to the end.
        ''' </returns>
        Friend Shared Function GetTail(text As String, start As Integer) As String

            Return text.Substring(start, text.Length - start) ' DCA083
        End Function

#End Region

    End Class

End Namespace

#End Region
