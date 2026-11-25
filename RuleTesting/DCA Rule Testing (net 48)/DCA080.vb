#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading

#End Region

#Region " DCA080 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA080: Thread.CurrentThread.ManagedThreadId instead of Environment.CurrentManagedThreadId.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA080 (Info) on <c>Thread.CurrentThread.ManagedThreadId</c>, which gets the <c>Thread</c> object only to
    ''' read its identifier. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1840
    ''' reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Environment.CurrentManagedThreadId</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA080

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA080"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the identifier of the current thread.
        ''' </summary>
        '''
        ''' <returns>
        ''' The identifier.
        ''' </returns>
        Friend Shared Function GetThreadId() As Integer

            Return Thread.CurrentThread.ManagedThreadId ' DCA080
        End Function

#End Region

    End Class

End Namespace

#End Region
