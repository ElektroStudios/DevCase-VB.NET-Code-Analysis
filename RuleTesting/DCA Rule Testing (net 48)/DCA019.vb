#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA019 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA019: Date or time parsed without a format provider.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA019 (Info) on <c>DateTime.Parse(text)</c>, which depends on the culture of the current thread.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): two fixes: <c>CultureInfo.CurrentCulture</c> (keeps the current behavior) or
    ''' <c>CultureInfo.InvariantCulture</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA019

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA019"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Parses a date.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text to parse.
        ''' </param>
        '''
        ''' <returns>
        ''' The date.
        ''' </returns>
        Friend Shared Function ParseDate(text As String) As Date

            ' DCA019: the parsing depends on the current culture.
            Return Date.Parse(text)
        End Function

#End Region

    End Class

End Namespace

#End Region
