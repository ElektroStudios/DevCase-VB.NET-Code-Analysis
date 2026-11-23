#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA016 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA016: Async Sub that is not an event handler.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA016 (Warning) on the method <c>Refresh</c>, an <c>Async Sub</c> whose exceptions cannot be caught by the
    ''' caller.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): converts it to <c>Async Function Refresh() As Task</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA016

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA016"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Refreshes the data.
        ''' </summary>
        Public Shared Async Sub Refresh() ' DCA016

            Await Task.Delay(1).ConfigureAwait(False)
        End Sub

#End Region

    End Class

End Namespace

#End Region
