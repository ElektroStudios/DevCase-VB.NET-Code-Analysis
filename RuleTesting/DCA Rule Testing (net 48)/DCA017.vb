#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Text.RegularExpressions

#End Region

#Region " DCA017 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA017: Regular expression without match timeout.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA017 (Error) on <c>Regex.IsMatch(text, "^\d+$")</c>, which has no match timeout.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): two fixes: a finite timeout (30 seconds by default) or an explicit <c>Regex.InfiniteMatchTimeout</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA017

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA017"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a text is a number.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the text is a number; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsNumber(text As String) As Boolean

            ' DCA017: the regular expression has no match timeout.
            Return Regex.IsMatch(text, "^\d+$")
        End Function

#End Region

    End Class

End Namespace

#End Region
