#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " DCA014 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA014: File-system loop without Thread.CurrentThread.Join(0).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA014 (Warning) on the <c>For Each</c> loop over <c>Directory.EnumerateFiles</c>, which does not pump
    ''' messages.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): inserts <c>Thread.CurrentThread.Join(0)</c> (with an explanatory comment) inside an
    ''' <c>#If DEBUG Then</c> block in the loop.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA014

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA014"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Writes the files of a directory to the console.
        ''' </summary>
        '''
        ''' <param name="directoryPath">
        ''' The directory path.
        ''' </param>
        Friend Shared Sub ListFiles(directoryPath As String)

            ' DCA014: the loop does not call Thread.CurrentThread.Join(0).
            For Each filePath As String In Directory.EnumerateFiles(directoryPath)
                Console.WriteLine(filePath)
            Next filePath
        End Sub

#End Region

    End Class

End Namespace

#End Region
