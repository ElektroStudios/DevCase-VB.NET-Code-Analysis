#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA002 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA002: Blocking .Result on a task.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA002 (Warning) on <c>.Result</c>, which blocks the calling thread until the task completes.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>.GetAwaiter().GetResult()</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA002

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA002"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets a value.
        ''' </summary>
        '''
        ''' <returns>
        ''' The value.
        ''' </returns>
        Public Shared Function GetValue() As Integer

            ' DCA002: blocking access to the result of a task.
            Return Task.FromResult(42).Result
        End Function

#End Region

    End Class

End Namespace

#End Region
