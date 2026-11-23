#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Windows.Forms

#End Region

#Region " DCA035 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA035: Application.DoEvents inside a loop.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA035 (Warning) on <c>Application.DoEvents()</c> inside the <c>For</c> loop.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA035

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA035"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Keeps the user interface responsive while waiting.
        ''' </summary>
        '''
        ''' <param name="iterations">
        ''' The number of iterations.
        ''' </param>
        Public Shared Sub Wait(iterations As Integer)

            For index As Integer = 1 To iterations
                Application.DoEvents() ' DCA035
            Next index
        End Sub

#End Region

    End Class

End Namespace

#End Region
