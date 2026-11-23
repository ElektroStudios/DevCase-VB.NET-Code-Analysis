#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA052 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA052: Parameter named sender outside an event handler.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA052 (Info) on the parameter <c>sender</c>, which is not the sender of an event.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): renames it to <c>source</c> (or the next free name: <c>value</c>, <c>input</c>, <c>instance</c>,
    ''' <c>item</c>).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA052

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA052"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the text of a version.
        ''' </summary>
        '''
        ''' <param name="sender">
        ''' The version.
        ''' </param>
        '''
        ''' <returns>
        ''' The text.
        ''' </returns>
        Friend Shared Function GetText(sender As Version) As String ' DCA052

            Return sender.ToString()
        End Function

#End Region

    End Class

End Namespace

#End Region
