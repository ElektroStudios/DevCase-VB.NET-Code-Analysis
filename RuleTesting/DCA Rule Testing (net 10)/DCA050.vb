#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA050 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA050: Parameter of an externally visible method is not validated.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA050 (Info) on the parameter <c>text</c>, which is dereferenced without being validated.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds <c>If text Is Nothing Then Throw New ArgumentNullException(NameOf(text))</c>
    ''' (<c>ArgumentNullException.ThrowIfNull(text)</c> on .NET 6 or later).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA050

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA050"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the length of a text.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text.
        ''' </param>
        '''
        ''' <returns>
        ''' The length.
        ''' </returns>
        Public Shared Function GetLength(text As String) As Integer

            ' DCA050: 'text' is not validated.
            Return text.Length
        End Function

#End Region

    End Class

End Namespace

#End Region
