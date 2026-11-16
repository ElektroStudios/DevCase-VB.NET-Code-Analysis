#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA024_RethrowOnlyCatchTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA024 (Catch block that only rethrows) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA024_RethrowOnlyCatchTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New RethrowOnlyCatchAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New RethrowOnlyCatchCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA024_RethrowOnlyCatchTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA024 reports the trailing <c>Catch</c> blocks that only rethrow.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_TrailingRethrowOnlyBlocks() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        [|Catch ex As Exception|]
            Throw
        End Try

        Try
            Console.WriteLine(2)
        Catch ex As ArgumentException
            Throw
        Catch ex As Exception
            Console.WriteLine(ex.Message)
        End Try

        Try
            Console.WriteLine(3)
        Catch ex As Exception When ex.HResult = 5
            Throw
        End Try

        Try
            Console.WriteLine(4)
        [|Catch ex As Exception|]
            Throw ex
        End Try

        Try
            Console.WriteLine(5)
        Catch ex As Exception
            Throw New InvalidOperationException(""Wrapped."", ex)
        End Try
    End Sub
End Class
", allowCompilerErrors:=False).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA024 replaces the <c>Try</c> statement with its body.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UnwrapsTheTryStatement() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub M()
        Dim before As Integer = 0
        Try
            Dim value As Integer = 1
            Console.WriteLine(value)
        Catch ex As Exception
            Throw
        End Try
        Console.WriteLine(before)
    End Sub
End Class
", "
Public Class C
    Public Sub M()
        Dim before As Integer = 0
        Dim value As Integer = 1
        Console.WriteLine(value)
        Console.WriteLine(before)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA024 keeps the <c>Finally</c> block and the meaningful <c>Catch</c> blocks.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsFinallyAndMeaningfulCatches() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As IO.IOException
            Console.WriteLine(ex.Message)
        Catch
            Throw
        End Try
        Try
            Console.WriteLine(2)
        Catch ex As Exception
            Throw
        Finally
            Console.WriteLine(3)
        End Try
    End Sub
End Class
", "
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As IO.IOException
            Console.WriteLine(ex.Message)
        End Try
        Try
            Console.WriteLine(2)
        Finally
            Console.WriteLine(3)
        End Try
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA024 offers no fix when unwrapping the <c>Try</c> statement would produce invalid code.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NoFix_WhenUnwrappingWouldConflict() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Sub M(flag As Boolean)
        Try
            Dim value As Integer = 1
            If flag Then
                Exit Try
            End If
            Console.WriteLine(value)
        Catch ex As Exception
            Throw
        End Try
    End Sub
End Class
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA024 removes a block that rethrows the caught exception with <c>Throw ex</c>, 
        ''' which also restores the original stack trace.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesThrowExBlock() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As IO.IOException
            Console.WriteLine(ex.Message)
        Catch ex As Exception
            Throw ex
        End Try
    End Sub
End Class
", "
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As IO.IOException
            Console.WriteLine(ex.Message)
        End Try
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
