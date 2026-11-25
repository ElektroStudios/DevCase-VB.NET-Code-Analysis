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

#Region " DCA071_CancellationTokenForwardingTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA071 (cancellation token not forwarded, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA071_CancellationTokenForwardingTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New CancellationTokenForwardingAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA071_CancellationTokenForwardingTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA071 reports calls with an omitted optional token or an overload that takes one, and ignores calls
        ''' that pass a token, methods without a token and lambdas without a token.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CallsThatCanReceiveTheToken() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Imports System.Threading

Public Class C
    Public Async Function RunAsync(stream As Stream, buffer As Byte(), cancellationToken As CancellationToken) As Task
        Await Task.[|Delay|](100)
        Dim read As Integer = Await stream.[|ReadAsync|](buffer, 0, buffer.Length)
        Await Me.[|SaveAsync|](""a"")
        Await Me.[|SaveAsync|](""a"", retries:=2)
        Await Task.Delay(100, cancellationToken)
        Await Me.ProcessAsync()
        Dim work As Func(Of Task) = Function() Task.Delay(5)
        Await work()
    End Function

    Public Async Function NoTokenAsync() As Task
        Await Task.Delay(100)
    End Function

    Public Sub Wait(gate As SemaphoreSlim, token As CancellationToken)
        gate.[|Wait|]()
    End Sub

    Public Function SaveAsync(name As String, Optional retries As Integer = 1, Optional token As CancellationToken = Nothing) As Task
        Return Task.CompletedTask
    End Function

    Public Function ProcessAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA071 does not run in a modern .NET project, where the built-in rule CA2016 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Imports System.Threading

Public Class C
    Public Async Function RunAsync(stream As Stream, buffer As Byte(), cancellationToken As CancellationToken) As Task
        Await Task.Delay(100)
        Dim read As Integer = Await stream.ReadAsync(buffer, 0, buffer.Length)
        Await Me.SaveAsync(""a"")
        Await Me.SaveAsync(""a"", retries:=2)
        Await Task.Delay(100, cancellationToken)
        Await Me.ProcessAsync()
        Dim work As Func(Of Task) = Function() Task.Delay(5)
        Await work()
    End Function

    Public Async Function NoTokenAsync() As Task
        Await Task.Delay(100)
    End Function

    Public Sub Wait(gate As SemaphoreSlim, token As CancellationToken)
        gate.Wait()
    End Sub

    Public Function SaveAsync(name As String, Optional retries As Integer = 1, Optional token As CancellationToken = Nothing) As Task
        Return Task.CompletedTask
    End Function

    Public Function ProcessAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA071 appends the token, as a named argument when a positional one would not reach the
        ''' parameter or when the call uses named arguments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AppendsTheToken_NamedWhenNeeded() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Imports System.Threading

Public Class C
    Public Async Function RunAsync(stream As Stream, buffer As Byte(), cancellationToken As CancellationToken) As Task
        Await Task.[|Delay|](100)
        Dim read As Integer = Await stream.[|ReadAsync|](buffer, 0, buffer.Length)
        Await Me.[|SaveAsync|](""a"")
        Await Me.[|SaveAsync|](""a"", retries:=2)
        Await Task.Delay(100, cancellationToken)
        Await Me.ProcessAsync()
        Dim work As Func(Of Task) = Function() Task.Delay(5)
        Await work()
    End Function

    Public Async Function NoTokenAsync() As Task
        Await Task.Delay(100)
    End Function

    Public Sub Wait(gate As SemaphoreSlim, token As CancellationToken)
        gate.[|Wait|]()
    End Sub

    Public Function SaveAsync(name As String, Optional retries As Integer = 1, Optional token As CancellationToken = Nothing) As Task
        Return Task.CompletedTask
    End Function

    Public Function ProcessAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
", "
Imports System.IO
Imports System.Threading

Public Class C
    Public Async Function RunAsync(stream As Stream, buffer As Byte(), cancellationToken As CancellationToken) As Task
        Await Task.Delay(100, cancellationToken)
        Dim read As Integer = Await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
        Await Me.SaveAsync(""a"", token:=cancellationToken)
        Await Me.SaveAsync(""a"", retries:=2, token:=cancellationToken)
        Await Task.Delay(100, cancellationToken)
        Await Me.ProcessAsync()
        Dim work As Func(Of Task) = Function() Task.Delay(5)
        Await work()
    End Function

    Public Async Function NoTokenAsync() As Task
        Await Task.Delay(100)
    End Function

    Public Sub Wait(gate As SemaphoreSlim, token As CancellationToken)
        gate.Wait(token)
    End Sub

    Public Function SaveAsync(name As String, Optional retries As Integer = 1, Optional token As CancellationToken = Nothing) As Task
        Return Task.CompletedTask
    End Function

    Public Function ProcessAsync() As Task
        Return Task.CompletedTask
    End Function
End Class
",
                                                      equivalenceKey:="DCA071.Forward", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA071 adds the argument list to a call without parentheses, forwards the token of an
        ''' <see langword="Async"/> lambda, and ignores implicit calls (the <c>Add</c> calls of a collection initializer).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallWithoutParentheses_AndLambdaToken() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading

Public Class C
    Public Sub Run(gate As SemaphoreSlim, token As CancellationToken)
        gate.[|Wait|]
        Dim list As New List(Of Integer) From {1, 2}
        Dim work As Func(Of CancellationToken, Task) = Async Function(inner As CancellationToken)
                                                           Await Task.[|Delay|](10)
                                                       End Function
    End Sub
End Class
", "
Imports System.Threading

Public Class C
    Public Sub Run(gate As SemaphoreSlim, token As CancellationToken)
        gate.Wait(token)
        Dim list As New List(Of Integer) From {1, 2}
        Dim work As Func(Of CancellationToken, Task) = Async Function(inner As CancellationToken)
                                                           Await Task.Delay(10, inner)
                                                       End Function
    End Sub
End Class
",
                                                      equivalenceKey:="DCA071.Forward", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
