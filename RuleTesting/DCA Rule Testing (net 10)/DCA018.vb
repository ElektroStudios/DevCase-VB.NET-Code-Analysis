#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA018 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA018: DateTime created without DateTimeKind.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA018 (Warning) on <c>New DateTime(1970, 1, 1)</c>, whose kind is Unspecified.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): three fixes: <c>DateTimeKind.Unspecified</c> (keeps the current behavior), <c>DateTimeKind.Utc</c> or
    ''' <c>DateTimeKind.Local</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA018

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA018"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the Unix epoch.
        ''' </summary>
        '''
        ''' <returns>
        ''' The Unix epoch.
        ''' </returns>
        Public Shared Function GetEpoch() As Date

            ' DCA018: the date has no DateTimeKind.
            Return New Date(1970, 1, 1)
        End Function

#End Region

    End Class

End Namespace

#End Region
