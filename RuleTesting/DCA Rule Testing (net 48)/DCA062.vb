#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

#End Region

#Region " DCA062 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA062: TaskContinuationOptions passed to a TaskCompletionSource constructor.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA062 (Warning) on <c>TaskContinuationOptions.RunContinuationsAsynchronously</c>, which binds to the
    ''' <c>state</c> parameter of <c>TaskCompletionSource(Of T)</c> instead of an option. This rule runs only in .NET
    ''' Framework projects (on modern .NET, the built-in rule CA2247 reports the same code), so this file is only in the
    ''' .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>TaskCreationOptions.RunContinuationsAsynchronously</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA062

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA062"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Creates a task source whose continuations run asynchronously.
        ''' </summary>
        '''
        ''' <returns>
        ''' The task source.
        ''' </returns>
        Friend Shared Function CreateSource() As TaskCompletionSource(Of Integer)

            Return New TaskCompletionSource(Of Integer)(TaskContinuationOptions.RunContinuationsAsynchronously) ' DCA062
        End Function

#End Region

    End Class

End Namespace

#End Region
