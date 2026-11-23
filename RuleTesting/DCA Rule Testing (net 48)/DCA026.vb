#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " DCA026 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA026: Text encoding not specified.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA026 (Warning) on <c>File.WriteAllText(filePath, contents)</c>, which uses an implicit encoding.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): three fixes: UTF-8 without BOM, UTF-8 with BOM or <c>Encoding.Default</c> (the one that keeps the
    ''' current behavior first).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA026

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA026"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Saves a text to a file.
        ''' </summary>
        '''
        ''' <param name="filePath">
        ''' The file path.
        ''' </param>
        '''
        ''' <param name="contents">
        ''' The text.
        ''' </param>
        Friend Shared Sub Save(filePath As String, contents As String)

            ' DCA026: the encoding is implicit.
            File.WriteAllText(filePath, contents)
        End Sub

#End Region

    End Class

End Namespace

#End Region
