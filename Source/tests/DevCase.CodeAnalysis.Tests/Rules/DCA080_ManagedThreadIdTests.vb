#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA080_ManagedThreadIdTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA080 (Thread.CurrentThread.ManagedThreadId, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA080_ManagedThreadIdTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ManagedThreadIdAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA080_ManagedThreadIdTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA080 reports <c>Thread.CurrentThread.ManagedThreadId</c> and ignores other threads.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CurrentThreadManagedThreadId() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading

Public Class C
    Public Function Id() As Integer
        Return [|Thread.CurrentThread.ManagedThreadId|]
    End Function

    Public Function Other(worker As Thread) As Integer
        Return worker.ManagedThreadId
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA080 does not run in a modern .NET project, where the built-in rule CA1840 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading

Public Class C
    Public Function Id() As Integer
        Return Thread.CurrentThread.ManagedThreadId
    End Function

    Public Function Other(worker As Thread) As Integer
        Return worker.ManagedThreadId
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA080 writes <c>Environment.CurrentManagedThreadId</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesEnvironment() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading

Public Class C
    Public Function Id() As Integer
        Return [|Thread.CurrentThread.ManagedThreadId|]
    End Function

    Public Function Other(worker As Thread) As Integer
        Return worker.ManagedThreadId
    End Function
End Class
", "
Imports System.Threading

Public Class C
    Public Function Id() As Integer
        Return Environment.CurrentManagedThreadId
    End Function

    Public Function Other(worker As Thread) As Integer
        Return worker.ManagedThreadId
    End Function
End Class
",
                                                      equivalenceKey:="DCA080.CurrentManagedThreadId", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
