#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA049 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA049: Single-character string where a Char can be used.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA049 (Info) on <c>","</c> in <c>String.Join</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>","c</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA049

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA049"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Joins words.
        ''' </summary>
        '''
        ''' <param name="words">
        ''' The words.
        ''' </param>
        '''
        ''' <returns>
        ''' The joined words.
        ''' </returns>
        Friend Shared Function JoinWords(words As String()) As String

            ' DCA049: the separator can be a Char.
            Return String.Join(",", words)
        End Function

#End Region

    End Class

End Namespace

#End Region
