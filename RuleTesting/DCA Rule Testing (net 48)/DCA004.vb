#Region " DCA004 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA004: Missing Option statements.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA004 (Warning) on the file, which does not declare <c>Option Strict On</c>, <c>Option Explicit On</c> and
    ''' <c>Option Infer Off</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds the <c>Option Statements</c> region at the top of the file.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA004

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA004"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

End Namespace

#End Region
