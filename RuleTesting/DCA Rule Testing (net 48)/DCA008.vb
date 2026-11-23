#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA008 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA008: &lt;param&gt; tags do not match the parameters.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA008 (Warning) on the method <c>Add</c>, whose documentation has no <c>&lt;param&gt;</c> tag for
    ''' <c>second</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds the missing <c>&lt;param&gt;</c> tag.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA008

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA008"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Adds two numbers.
        ''' </summary>
        '''
        ''' <param name="first">
        ''' The first number.
        ''' </param>
        '''
        ''' <returns>
        ''' The sum.
        ''' </returns>
        Public Shared Function Add(first As Integer, second As Integer) As Integer

            ' DCA008: there is no <param> tag for 'second'.
            Return first + second
        End Function

#End Region

    End Class

End Namespace

#End Region
