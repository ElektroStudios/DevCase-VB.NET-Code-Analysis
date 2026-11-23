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

        ''' <summary>
        ''' Verifies that the code fix of rule DCA024 keeps the conditional code of the unwrapped <c>Try</c> body: the 
        ''' <c>#If</c> blocks at its start and at its end (whose <c>#End If</c> line precedes the <c>Catch</c> keyword) stay 
        ''' around the same statements, re-indented, whether the condition is true (statements) or false (disabled code).
        ''' </summary>
        '''
        ''' <param name="debug">
        ''' The value of the <c>DEBUG</c> constant of the source.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        Public Async Function Fix_KeepsTheConditionalCodeOfTheBody(debug As Boolean) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Dim before As Integer = 0
        Try
#If DEBUG Then
            Console.WriteLine(""Start"")
#End If
            Console.WriteLine(before)
#If DEBUG Then
            Console.WriteLine(""End"")
#End If
        Catch ex As Exception
            Throw
        End Try
    End Sub
End Class
", $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Dim before As Integer = 0
#If DEBUG Then
        Console.WriteLine(""Start"")
#End If
        Console.WriteLine(before)
#If DEBUG Then
        Console.WriteLine(""End"")
#End If
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA024 keeps the conditional code of a <c>Try</c> body that only has an 
        ''' <c>#If</c> block (no statement when the condition is false).
        ''' </summary>
        '''
        ''' <param name="debug">
        ''' The value of the <c>DEBUG</c> constant of the source.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        Public Async Function Fix_KeepsTheConditionalCodeOfAnEmptyBody(debug As Boolean) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Dim before As Integer = 0
        Try
#If DEBUG Then
            Console.WriteLine(before)
#End If
        Catch ex As Exception
            Throw
        End Try
        Console.WriteLine(before)
    End Sub
End Class
", $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Dim before As Integer = 0
#If DEBUG Then
        Console.WriteLine(before)
#End If
        Console.WriteLine(before)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA024 keeps the conditional code at the end of the previous <c>Catch</c> block 
        ''' (whose <c>#End If</c> line precedes the removed <c>Catch</c> keyword).
        ''' </summary>
        '''
        ''' <param name="debug">
        ''' The value of the <c>DEBUG</c> constant of the source.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(True)>
        <InlineData(False)>
        Public Async Function Fix_KeepsTheConditionalCodeOfThePreviousCatch(debug As Boolean) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As IO.IOException
#If DEBUG Then
            Console.WriteLine(ex.Message)
#End If
        Catch ex As Exception
            Throw
        End Try
    End Sub
End Class
", $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As IO.IOException
#If DEBUG Then
            Console.WriteLine(ex.Message)
#End If
        End Try
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA024 does not report a <c>Catch</c> block with conditional code: in another configuration 
        ''' (<c>#If DEBUG Then</c>) the block does more than rethrow the exception.
        ''' </summary>
        '''
        ''' <param name="body">
        ''' The body of the <c>Catch</c> block.
        ''' </param>
        '''
        ''' <param name="debug">
        ''' The value of the <c>DEBUG</c> constant of the source.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("#If DEBUG Then" & vbLf & "            Console.WriteLine(ex.Message)" & vbLf & "#End If" & vbLf & "            Throw", True)>
        <InlineData("#If DEBUG Then" & vbLf & "            Console.WriteLine(ex.Message)" & vbLf & "#End If" & vbLf & "            Throw", False)>
        <InlineData("            Throw" & vbLf & "#If DEBUG Then" & vbLf & "            Console.WriteLine(ex.Message)" & vbLf & "#End If", True)>
        <InlineData("            Throw" & vbLf & "#If DEBUG Then" & vbLf & "            Console.WriteLine(ex.Message)" & vbLf & "#End If", False)>
        <InlineData("#If DEBUG Then" & vbLf & "            Throw" & vbLf & "#Else" & vbLf & "            Console.WriteLine(ex.Message)" & vbLf & "#End If", True)>
        <InlineData("#If DEBUG Then" & vbLf & "            Throw" & vbLf & "#Else" & vbLf & "            Console.WriteLine(ex.Message)" & vbLf & "#End If", False)>
        Public Async Function DoesNotReport_CatchBlockWithConditionalCode(body As String, debug As Boolean) As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"#Const DEBUG = {debug}
Public Class C
    Public Sub M()
        Try
            Console.WriteLine(1)
        Catch ex As Exception
{body}
        End Try
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
