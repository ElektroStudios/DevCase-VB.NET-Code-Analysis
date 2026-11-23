#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA023 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA023: Negated comparison.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA023 (Warning) on <c>Not (first = second)</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>first &lt;&gt; second</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA023

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA023"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether two numbers are different.
        ''' </summary>
        '''
        ''' <param name="first">
        ''' The first number.
        ''' </param>
        '''
        ''' <param name="second">
        ''' The second number.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the numbers are different; otherwise, <see langword="False"/>.
        ''' </returns>
        Public Shared Function AreDifferent(first As Integer, second As Integer) As Boolean

            ' DCA023: the comparison is negated.
            Return Not (first = second)
        End Function

#End Region

    End Class

End Namespace

#End Region
