#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA066 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA066: Value type passed to ReferenceEquals.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA066 (Warning) on each argument of <c>Object.ReferenceEquals(first, second)</c>: each
    ''' <see cref="Integer"/> is boxed into a new object, so the result is always <see langword="False"/>. This rule runs
    ''' only in .NET Framework projects (on modern .NET, the built-in rule CA2013 reports the same code), so this file is
    ''' only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): none (the intended comparison, <c>=</c> or <c>Equals</c>, depends on the type).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA066

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA066"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether two numbers are the same object.
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
        ''' Always <see langword="False"/>.
        ''' </returns>
        Friend Shared Function AreSame(first As Integer, second As Integer) As Boolean

            Return Object.ReferenceEquals(first, second) ' DCA066 (twice)
        End Function

#End Region

    End Class

End Namespace

#End Region
