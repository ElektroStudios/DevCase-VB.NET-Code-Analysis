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

#Region " DCA023_InvertedComparisonTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA023 (negated comparison) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA023_InvertedComparisonTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New InvertedComparisonAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New InvertedComparisonCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA023_InvertedComparisonTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA023 reports negated comparisons and skips the unsafe ones.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_NegatedComparisons() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(a As Integer, b As Integer, s As String, x As Double, y As Double, o As Object, flag As Boolean, n As Integer?)
        Dim r1 As Boolean = [|Not (a = b)|]
        Dim r2 As Boolean = [|Not a < b|]
        Dim r3 As Boolean = [|Not (s <> ""x"")|]
        Dim r4 As Boolean = [|Not (x = y)|]
        Dim r5 As Boolean? = [|Not (n > 3)|]
        Dim r6 As Boolean = Not (x < y)
        Dim r7 As Boolean = Not o Is Nothing
        Dim r8 As Boolean = Not flag
        Dim r9 As Boolean = Not TypeOf o Is String
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA023 replaces the negation with the inverse operator.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesTheInverseOperator() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(a As Integer, b As Integer) As Boolean
        If Not (a = b) Then
            Return Not a < b
        End If
        Return Not (a >= b) AndAlso Not (a <= 0)
    End Function
End Class
", "
Public Class C
    Public Function M(a As Integer, b As Integer) As Boolean
        If a <> b Then
            Return a >= b
        End If
        Return a < b AndAlso a > 0
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
