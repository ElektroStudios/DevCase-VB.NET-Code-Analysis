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

#Region " DCA072_CountInsteadOfAnyTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA072 (Count instead of Any, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA072_CountInsteadOfAnyTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New CountInsteadOfAnyAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA072_CountInsteadOfAnyTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA072 reports every comparison of <c>Count</c> or <c>LongCount</c> that checks emptiness, and
        ''' ignores other comparisons and calls without a predicate on types with a size property.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_EmptinessChecksWithCount() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Check(query As IEnumerable(Of Integer), numbers As Integer(), list As List(Of Integer)) As Boolean
        Dim a As Boolean = [|query.Count() > 0|]
        Dim b As Boolean = [|query.Count() = 0|]
        Dim c As Boolean = [|0 < query.Count(Function(n As Integer) n > 1)|]
        Dim d As Boolean = [|query.LongCount() <> 0|]
        Dim e As Boolean = [|query.Count() >= 1|]
        Dim f As Boolean = [|1 > query.Count()|]
        Dim g As Boolean = [|numbers.Count(Function(n As Integer) n > 1) = 0|]
        Dim h As Boolean = numbers.Count() > 0
        Dim i As Boolean = query.Count() > 1
        Dim j As Boolean = [|query.Count() = 0|] = a
        Dim k As Boolean = [|Enumerable.Count(query) <= 0|]
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g OrElse h OrElse i OrElse j OrElse k
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA072 does not run in a modern .NET project, where the built-in rule CA1827 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Check(query As IEnumerable(Of Integer), numbers As Integer(), list As List(Of Integer)) As Boolean
        Dim a As Boolean = query.Count() > 0
        Dim b As Boolean = query.Count() = 0
        Dim c As Boolean = 0 < query.Count(Function(n As Integer) n > 1)
        Dim d As Boolean = query.LongCount() <> 0
        Dim e As Boolean = query.Count() >= 1
        Dim f As Boolean = 1 > query.Count()
        Dim g As Boolean = numbers.Count(Function(n As Integer) n > 1) = 0
        Dim h As Boolean = numbers.Count() > 0
        Dim i As Boolean = query.Count() > 1
        Dim j As Boolean = query.Count() = 0 = a
        Dim k As Boolean = Enumerable.Count(query) <= 0
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g OrElse h OrElse i OrElse j OrElse k
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA072 writes <c>Any</c>, negated (with parentheses when needed) for empty checks.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesAny_NegatedWhenEmpty() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Check(query As IEnumerable(Of Integer), numbers As Integer(), list As List(Of Integer)) As Boolean
        Dim a As Boolean = [|query.Count() > 0|]
        Dim b As Boolean = [|query.Count() = 0|]
        Dim c As Boolean = [|0 < query.Count(Function(n As Integer) n > 1)|]
        Dim d As Boolean = [|query.LongCount() <> 0|]
        Dim e As Boolean = [|query.Count() >= 1|]
        Dim f As Boolean = [|1 > query.Count()|]
        Dim g As Boolean = [|numbers.Count(Function(n As Integer) n > 1) = 0|]
        Dim h As Boolean = numbers.Count() > 0
        Dim i As Boolean = query.Count() > 1
        Dim j As Boolean = [|query.Count() = 0|] = a
        Dim k As Boolean = [|Enumerable.Count(query) <= 0|]
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g OrElse h OrElse i OrElse j OrElse k
    End Function
End Class
", "
Public Class C
    Public Function Check(query As IEnumerable(Of Integer), numbers As Integer(), list As List(Of Integer)) As Boolean
        Dim a As Boolean = query.Any()
        Dim b As Boolean = Not query.Any()
        Dim c As Boolean = query.Any(Function(n As Integer) n > 1)
        Dim d As Boolean = query.Any()
        Dim e As Boolean = query.Any()
        Dim f As Boolean = Not query.Any()
        Dim g As Boolean = Not numbers.Any(Function(n As Integer) n > 1)
        Dim h As Boolean = numbers.Count() > 0
        Dim i As Boolean = query.Count() > 1
        Dim j As Boolean = (Not query.Any()) = a
        Dim k As Boolean = Not Enumerable.Any(query)
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g OrElse h OrElse i OrElse j OrElse k
    End Function
End Class
",
                                                      equivalenceKey:="DCA072.Any", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA072 adds parentheses to <c>Any</c> when <c>Count</c> was called without them, with
        ''' <c>Option Strict Off</c> and <c>Option Infer On</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallWithoutParentheses_GetsThem() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Check(query As IEnumerable(Of Integer)) As Boolean
        Dim found = [|query.Count > 0|]
        If [|query.Count = 0|] Then Return False
        Return found
    End Function
End Class
", "
Public Class C
    Public Function Check(query As IEnumerable(Of Integer)) As Boolean
        Dim found = query.Any()
        If Not query.Any() Then Return False
        Return found
    End Function
End Class
",
                                                      equivalenceKey:="DCA072.Any",
                                                      optionStatements:=(VisualBasic.OptionStrict.Off, True, True), targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
