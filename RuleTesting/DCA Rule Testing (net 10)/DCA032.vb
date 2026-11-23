#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA032 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA032: End statement.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA032 (Warning) on the <c>End</c> statement, which terminates the process without running <c>Finally</c>
    ''' blocks or <c>Dispose</c>.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA032

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA032"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Terminates the application.
        ''' </summary>
        Public Shared Sub Quit()

            ' DCA032: End terminates the process.
            End
        End Sub

#End Region

    End Class

End Namespace

#End Region
