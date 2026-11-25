#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO
Imports System.Threading.Tasks

#End Region

#Region " DCA063 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA063: StreamReader.EndOfStream in an Async method.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA063 (Warning) on <c>reader.EndOfStream</c> in an <see langword="Async"/> function: when no data is
    ''' buffered, it reads the stream synchronously and blocks the thread. This rule runs only in .NET Framework projects
    ''' (on modern .NET, the built-in rule CA2024 reports the same code), so this file is only in the .NET Framework
    ''' project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): none (the loop has to test the result of <c>ReadLineAsync</c> for <see langword="Nothing"/>).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA063

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA063"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Counts the lines of a reader.
        ''' </summary>
        '''
        ''' <param name="reader">
        ''' The reader.
        ''' </param>
        '''
        ''' <returns>
        ''' The number of lines.
        ''' </returns>
        Friend Shared Async Function CountLinesAsync(reader As StreamReader) As Task(Of Integer)

            Dim count As Integer = 0
            While Not reader.EndOfStream ' DCA063
                Await reader.ReadLineAsync().ConfigureAwait(False)
                count += 1
            End While
            Return count
        End Function

#End Region

    End Class

End Namespace

#End Region
