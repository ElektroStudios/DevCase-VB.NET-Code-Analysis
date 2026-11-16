#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " DCA036_LockOnWeakIdentityTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA036 (SyncLock on a publicly accessible or weak-identity object).
    ''' </summary>
    Public NotInheritable Class DCA036_LockOnWeakIdentityTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New LockOnWeakIdentityAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA036_LockOnWeakIdentityTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA036 reports SyncLock on Me, strings, types, threads and MarshalByRefObject instances, and skips private objects.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_WeakIdentityLocks() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly gate As New Object()
    Private ReadOnly name As String = ""lock""
    Public Sub M(stream As IO.Stream)
        SyncLock [|Me|]
        End SyncLock
        SyncLock [|name|]
        End SyncLock
        SyncLock [|GetType(C)|]
        End SyncLock
        SyncLock [|Threading.Thread.CurrentThread|]
        End SyncLock
        SyncLock [|stream|]
        End SyncLock
        SyncLock gate
        End SyncLock
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
