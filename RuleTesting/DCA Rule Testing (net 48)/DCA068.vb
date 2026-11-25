#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading

#End Region

#Region " DCA068 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA068: IsCancellationRequested check that throws OperationCanceledException.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA068 (Info) on <c>cancellationToken.IsCancellationRequested</c>, checked by an <see langword="If"/>
    ''' statement that only throws <c>OperationCanceledException</c>. This rule runs only in .NET Framework projects (on
    ''' modern .NET, the built-in rule CA2250 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>cancellationToken.ThrowIfCancellationRequested()</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA068

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA068"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Stops the operation when cancellation is requested.
        ''' </summary>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <exception cref="OperationCanceledException">
        ''' Thrown when cancellation is requested.
        ''' </exception>
        Friend Shared Sub Check(cancellationToken As CancellationToken)

            If cancellationToken.IsCancellationRequested Then ' DCA068
                Throw New OperationCanceledException()
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
