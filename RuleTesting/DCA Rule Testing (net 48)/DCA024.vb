#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Globalization

#End Region

#Region " DCA024 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA024: Catch block that only rethrows.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA024 (Warning) on the <c>Catch</c> block, whose only statement is <c>Throw</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): removes the <c>Catch</c> block (and the <c>Try</c> statement, when nothing else is left).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA024

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA024"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Parses a number.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text to parse.
        ''' </param>
        '''
        ''' <returns>
        ''' The number.
        ''' </returns>
        Friend Shared Function ParseNumber(text As String) As Integer

            Try
                Return Integer.Parse(text, CultureInfo.InvariantCulture)
            Catch ex As FormatException ' DCA024: the Catch block only rethrows.
                Throw
            End Try
        End Function

#End Region

    End Class

End Namespace

#End Region
