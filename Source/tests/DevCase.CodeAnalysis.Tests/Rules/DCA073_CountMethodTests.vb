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

#Region " DCA073_CountMethodTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA073 (Count method instead of property, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA073_CountMethodTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New CountMethodAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA073_CountMethodTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA073 reports <c>Enumerable.Count()</c> on arrays, strings and collections reached through the module, and
        ''' ignores the property, sequences without a size property and calls with a predicate.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CountOnTypesWithASizeProperty() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Sizes(numbers As Integer(), text As String, list As List(Of Integer), query As IEnumerable(Of Integer)) As Integer
        Dim a As Integer = [|numbers.Count()|]
        Dim b As Integer = [|text.Count()|]
        Dim c As Integer = [|Enumerable.Count(list)|]
        Dim d As Integer = list.Count()
        Dim e As Integer = query.Count()
        Dim f As Integer = numbers.Count(Function(n As Integer) n > 0)
        Dim g As Integer = [|Me.GetNumbers().Count()|]
        Return a + b + c + d + e + f + g
    End Function

    Private Function GetNumbers() As Integer()
        Return Array.Empty(Of Integer)()
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA073 does not run in a modern .NET project, where the built-in rule CA1829 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Sizes(numbers As Integer(), text As String, list As List(Of Integer), query As IEnumerable(Of Integer)) As Integer
        Dim a As Integer = numbers.Count()
        Dim b As Integer = text.Count()
        Dim c As Integer = Enumerable.Count(list)
        Dim d As Integer = list.Count()
        Dim e As Integer = query.Count()
        Dim f As Integer = numbers.Count(Function(n As Integer) n > 0)
        Dim g As Integer = Me.GetNumbers().Count()
        Return a + b + c + d + e + f + g
    End Function

    Private Function GetNumbers() As Integer()
        Return Array.Empty(Of Integer)()
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA073 writes <c>Length</c> or <c>Count</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesLengthOrCount() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Sizes(numbers As Integer(), text As String, list As List(Of Integer), query As IEnumerable(Of Integer)) As Integer
        Dim a As Integer = [|numbers.Count()|]
        Dim b As Integer = [|text.Count()|]
        Dim c As Integer = [|Enumerable.Count(list)|]
        Dim d As Integer = list.Count()
        Dim e As Integer = query.Count()
        Dim f As Integer = numbers.Count(Function(n As Integer) n > 0)
        Dim g As Integer = [|Me.GetNumbers().Count()|]
        Return a + b + c + d + e + f + g
    End Function

    Private Function GetNumbers() As Integer()
        Return Array.Empty(Of Integer)()
    End Function
End Class
", "
Public Class C
    Public Function Sizes(numbers As Integer(), text As String, list As List(Of Integer), query As IEnumerable(Of Integer)) As Integer
        Dim a As Integer = numbers.Length
        Dim b As Integer = text.Length
        Dim c As Integer = list.Count
        Dim d As Integer = list.Count()
        Dim e As Integer = query.Count()
        Dim f As Integer = numbers.Count(Function(n As Integer) n > 0)
        Dim g As Integer = Me.GetNumbers().Length
        Return a + b + c + d + e + f + g
    End Function

    Private Function GetNumbers() As Integer()
        Return Array.Empty(Of Integer)()
    End Function
End Class
",
                                                      equivalenceKey:="DCA073.Property", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA073 replaces a call without parentheses.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallWithoutParentheses() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Size(numbers As Integer()) As Integer
        Return [|numbers.Count|]
    End Function
End Class
", "
Public Class C
    Public Function Size(numbers As Integer()) As Integer
        Return numbers.Length
    End Function
End Class
",
                                                      equivalenceKey:="DCA073.Property", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
