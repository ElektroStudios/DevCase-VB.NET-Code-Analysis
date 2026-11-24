#Region " DCA004 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA004: Required Option settings not in effect.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA004 (Warning) on the file, which declares no <c>Option</c> statement: it inherits <c>Option Strict Off</c>
    ''' and <c>Option Infer On</c> from the project (the defaults of a new project), instead of <c>Option Strict On</c> and
    ''' <c>Option Infer Off</c>. In a project that already uses the required settings, the file would not be reported.
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
