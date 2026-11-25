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

#Region " DCA074_IsEmptyTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA074 (Count instead of IsEmpty, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA074_IsEmptyTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New IsEmptyAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA074_IsEmptyTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA074 reports emptiness checks with <c>Count</c> on types that have <c>IsEmpty</c>, and ignores other
        ''' types and other comparisons.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CountChecksOnTypesWithIsEmpty() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Collections.Concurrent

Public Class C
    Public Function Check(queue As ConcurrentQueue(Of Integer), map As ConcurrentDictionary(Of String, Integer), list As List(Of Integer)) As Boolean
        Dim a As Boolean = [|queue.Count = 0|]
        Dim b As Boolean = [|map.Count > 0|]
        Dim c As Boolean = [|0 <> queue.Count|]
        Dim d As Boolean = list.Count = 0
        Dim e As Boolean = queue.Count = 2
        Return a OrElse b OrElse c OrElse d OrElse e
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA074 does not run in a modern .NET project, where the built-in rule CA1836 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Collections.Concurrent

Public Class C
    Public Function Check(queue As ConcurrentQueue(Of Integer), map As ConcurrentDictionary(Of String, Integer), list As List(Of Integer)) As Boolean
        Dim a As Boolean = queue.Count = 0
        Dim b As Boolean = map.Count > 0
        Dim c As Boolean = 0 <> queue.Count
        Dim d As Boolean = list.Count = 0
        Dim e As Boolean = queue.Count = 2
        Return a OrElse b OrElse c OrElse d OrElse e
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA074 writes <c>IsEmpty</c> or <c>Not IsEmpty</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesIsEmpty() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Collections.Concurrent

Public Class C
    Public Function Check(queue As ConcurrentQueue(Of Integer), map As ConcurrentDictionary(Of String, Integer), list As List(Of Integer)) As Boolean
        Dim a As Boolean = [|queue.Count = 0|]
        Dim b As Boolean = [|map.Count > 0|]
        Dim c As Boolean = [|0 <> queue.Count|]
        Dim d As Boolean = list.Count = 0
        Dim e As Boolean = queue.Count = 2
        Return a OrElse b OrElse c OrElse d OrElse e
    End Function
End Class
", "
Imports System.Collections.Concurrent

Public Class C
    Public Function Check(queue As ConcurrentQueue(Of Integer), map As ConcurrentDictionary(Of String, Integer), list As List(Of Integer)) As Boolean
        Dim a As Boolean = queue.IsEmpty
        Dim b As Boolean = Not map.IsEmpty
        Dim c As Boolean = Not queue.IsEmpty
        Dim d As Boolean = list.Count = 0
        Dim e As Boolean = queue.Count = 2
        Return a OrElse b OrElse c OrElse d OrElse e
    End Function
End Class
",
                                                      equivalenceKey:="DCA074.IsEmpty", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
