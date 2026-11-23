#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Diagnostics

#End Region

#Region " DCA013 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA013: .NET Framework API with a modern .NET equivalent.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA013 (Info) on <c>Process.GetCurrentProcess().Id</c>, which is <c>Environment.ProcessId</c> on .NET 5 or
    ''' later. This rule runs only in .NET Framework projects (on modern .NET, the modern API is used directly), so this file is
    ''' only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>#If NET5_0_OR_GREATER Then</c> (with <c>Environment.ProcessId</c>) <c>#Else</c> (the original
    ''' statement) <c>#End If</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA013

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA013"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the identifier of the current process.
        ''' </summary>
        '''
        ''' <returns>
        ''' The process identifier.
        ''' </returns>
        Public Shared Function GetProcessId() As Integer

            ' DCA013: modern .NET has Environment.ProcessId.
            Return Process.GetCurrentProcess().Id
        End Function

#End Region

    End Class

End Namespace

#End Region
