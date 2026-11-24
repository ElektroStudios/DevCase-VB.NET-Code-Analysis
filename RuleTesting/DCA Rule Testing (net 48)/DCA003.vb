#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA003 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA003: Silent guard clause.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA003 (Info) on the <c>Exit Sub</c> of the guard clause on <c>text</c>, which hides the invalid argument.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Throw New ArgumentNullException(NameOf(text))</c> (on .NET Framework, DCA011 then offers
    ''' <c>ArgumentNullException.ThrowIfNull</c>).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA003

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA003"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Writes a text to the console.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text.
        ''' </param>
        Friend Shared Sub Print(text As String)

            ' DCA003: the guard clause exits silently.
            If text Is Nothing Then
                Exit Sub
            End If

            Console.WriteLine(text)
        End Sub

#End Region

    End Class

End Namespace

#End Region
