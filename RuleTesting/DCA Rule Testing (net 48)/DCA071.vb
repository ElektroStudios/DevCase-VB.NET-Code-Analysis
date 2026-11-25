#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics.CodeAnalysis
Imports System.Threading
Imports System.Threading.Tasks

#End Region

#Region " DCA071 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA071: CancellationToken parameter not forwarded.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA071 (Info) on <c>Task.Delay(100)</c> in a function that receives a <c>cancellationToken</c>:
    ''' <c>Task.Delay</c> has an overload that takes it. This rule runs only in .NET Framework projects (on modern .NET, the
    ''' built-in rule CA2016 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Task.Delay(100, cancellationToken)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA071

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA071"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Waits a moment.
        ''' </summary>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous operation.
        ''' </returns>
        <SuppressMessage("Style", "IDE0060:Remove unused parameter",
                         Justification:="The rule is about not forwarding the parameter, so it is used in the code fix.")>
        Friend Shared Async Function PauseAsync(cancellationToken As CancellationToken) As Task

            Await Task.Delay(100).ConfigureAwait(False) ' DCA071
        End Function

#End Region

    End Class

End Namespace

#End Region
