#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA012 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA012: Thrown exception is not documented.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA012 (Info) on the method <c>Fail</c>, which throws <c>InvalidOperationException</c> without an
    ''' <c>&lt;exception&gt;</c> tag.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds an empty <c>&lt;exception cref="InvalidOperationException"&gt;&lt;/exception&gt;</c> tag, to be
    ''' completed.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA012

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA012"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Always fails.
        ''' </summary>
        Public Shared Sub Fail()

            ' DCA012: the thrown exception is not documented.
            Throw New InvalidOperationException("The operation is not valid.")
        End Sub

#End Region

    End Class

End Namespace

#End Region
