#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading

#End Region

#Region " DCA044 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA044: DateTime.Now used to measure elapsed time.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA044 (Info) on <c>DateTime.Now - start</c>: the system clock can change while measuring.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA044

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA044"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Measures a short wait.
        ''' </summary>
        '''
        ''' <returns>
        ''' The elapsed milliseconds.
        ''' </returns>
        Public Shared Function Measure() As Double

            Dim start As Date = Date.Now
            Thread.Sleep(10)
            Dim elapsed As TimeSpan = Date.Now - start ' DCA044
            Return elapsed.TotalMilliseconds
        End Function

#End Region

    End Class

End Namespace

#End Region
