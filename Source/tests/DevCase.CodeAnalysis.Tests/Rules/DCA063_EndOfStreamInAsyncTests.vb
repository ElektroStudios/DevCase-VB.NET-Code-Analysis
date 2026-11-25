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

Imports Xunit

#End Region

#Region " DCA063_EndOfStreamInAsyncTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA063 (EndOfStream in an Async method, .NET Framework).
    ''' </summary>
    Public NotInheritable Class DCA063_EndOfStreamInAsyncTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New EndOfStreamInAsyncAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA063_EndOfStreamInAsyncTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA063 reports <c>EndOfStream</c> in <see langword="Async"/> methods and lambdas, and ignores
        ''' synchronous methods and synchronous lambdas inside <see langword="Async"/> methods.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_EndOfStreamInAsyncCode() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO

Public Class C
    Public Async Function CountAsync(reader As StreamReader) As Task(Of Integer)
        Dim count As Integer = 0
        While Not reader.[|EndOfStream|]
            Await reader.ReadLineAsync()
            count += 1
        End While
        Return count
    End Function

    Public Function Count(reader As StreamReader) As Integer
        Dim lines As Integer = 0
        While Not reader.EndOfStream
            reader.ReadLine()
            lines += 1
        End While
        Return lines
    End Function

    Public Async Function LambdasAsync(reader As StreamReader) As Task
        Dim check As Func(Of Boolean) = Function() reader.EndOfStream
        Dim asyncCheck As Func(Of Task(Of Boolean)) = Async Function()
                                                          Await Task.Yield()
                                                          Return reader.[|EndOfStream|]
                                                      End Function
        Await asyncCheck()
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA063 does not run in a modern .NET project, where the built-in rule CA2024 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO

Public Class C
    Public Async Function CountAsync(reader As StreamReader) As Task(Of Integer)
        Dim count As Integer = 0
        While Not reader.EndOfStream
            Await reader.ReadLineAsync()
            count += 1
        End While
        Return count
    End Function

    Public Function Count(reader As StreamReader) As Integer
        Dim lines As Integer = 0
        While Not reader.EndOfStream
            reader.ReadLine()
            lines += 1
        End While
        Return lines
    End Function

    Public Async Function LambdasAsync(reader As StreamReader) As Task
        Dim check As Func(Of Boolean) = Function() reader.EndOfStream
        Dim asyncCheck As Func(Of Task(Of Boolean)) = Async Function()
                                                          Await Task.Yield()
                                                          Return reader.EndOfStream
                                                      End Function
        Await asyncCheck()
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
