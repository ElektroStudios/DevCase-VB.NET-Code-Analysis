#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " DCA045 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA045: Path.GetTempFileName creates a predictable temporary file.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA045 (Warning) on <c>Path.GetTempFileName()</c>.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): creates a file with a random name and <c>FileMode.CreateNew</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA045

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA045"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Creates a temporary file.
        ''' </summary>
        '''
        ''' <returns>
        ''' The path of the file.
        ''' </returns>
        Public Shared Function CreateTemporaryFile() As String

            ' DCA045: the name of the temporary file is predictable.
            Return Path.GetTempFileName()
        End Function

#End Region

    End Class

End Namespace

#End Region
