#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA033 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA033: On Error statement.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA033 (Warning) on <c>On Error Resume Next</c>.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA033

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA033"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Runs the work.
        ''' </summary>
        Public Shared Sub Run()

            ' DCA033: unstructured error handling.
            On Error Resume Next
            Console.WriteLine("Running...")
        End Sub

#End Region

    End Class

End Namespace

#End Region
