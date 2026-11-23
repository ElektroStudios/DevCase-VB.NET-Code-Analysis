#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA029 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA029: String concatenated with +.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA029 (Warning) on the <c>+</c> operator between strings.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>&amp;</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA029

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA029"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Creates a greeting.
        ''' </summary>
        '''
        ''' <param name="name">
        ''' The name.
        ''' </param>
        '''
        ''' <returns>
        ''' The greeting.
        ''' </returns>
        Public Shared Function Greet(name As String) As String

            ' DCA029: strings concatenated with +.
            Return "Hello, " + name
        End Function

#End Region

    End Class

End Namespace

#End Region
