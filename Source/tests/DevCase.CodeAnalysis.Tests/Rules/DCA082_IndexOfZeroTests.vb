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

#Region " DCA082_IndexOfZeroTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA082 (IndexOf compared with zero, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA082_IndexOfZeroTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New IndexOfZeroAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA082_IndexOfZeroTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA082 reports <c>IndexOf(String)</c> compared for equality with zero, and ignores the <see cref="Char"/>
        ''' and start-index overloads and other comparisons.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_IndexOfEqualityWithZero() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Check(path As String, prefix As String) As Boolean
        Dim a As Boolean = [|path.IndexOf(prefix) = 0|]
        Dim b As Boolean = [|path.IndexOf(prefix, StringComparison.Ordinal) <> 0|]
        Dim c As Boolean = [|0 = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase)|]
        Dim d As Boolean = path.IndexOf(""x""c) = 0
        Dim e As Boolean = path.IndexOf(prefix) > 0
        Dim f As Boolean = path.IndexOf(prefix, 2) = 0
        Dim g As Boolean = [|path.IndexOf(prefix) <> 0|] = a
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA082 does not run in a modern .NET project, where the built-in rule CA1858 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Check(path As String, prefix As String) As Boolean
        Dim a As Boolean = path.IndexOf(prefix) = 0
        Dim b As Boolean = path.IndexOf(prefix, StringComparison.Ordinal) <> 0
        Dim c As Boolean = 0 = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase)
        Dim d As Boolean = path.IndexOf(""x""c) = 0
        Dim e As Boolean = path.IndexOf(prefix) > 0
        Dim f As Boolean = path.IndexOf(prefix, 2) = 0
        Dim g As Boolean = path.IndexOf(prefix) <> 0 = a
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA082 writes <c>StartsWith</c> with the same arguments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesStartsWith() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Check(path As String, prefix As String) As Boolean
        Dim a As Boolean = [|path.IndexOf(prefix) = 0|]
        Dim b As Boolean = [|path.IndexOf(prefix, StringComparison.Ordinal) <> 0|]
        Dim c As Boolean = [|0 = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase)|]
        Dim d As Boolean = path.IndexOf(""x""c) = 0
        Dim e As Boolean = path.IndexOf(prefix) > 0
        Dim f As Boolean = path.IndexOf(prefix, 2) = 0
        Dim g As Boolean = [|path.IndexOf(prefix) <> 0|] = a
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g
    End Function
End Class
", "
Public Class C
    Public Function Check(path As String, prefix As String) As Boolean
        Dim a As Boolean = path.StartsWith(prefix)
        Dim b As Boolean = Not path.StartsWith(prefix, StringComparison.Ordinal)
        Dim c As Boolean = path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        Dim d As Boolean = path.IndexOf(""x""c) = 0
        Dim e As Boolean = path.IndexOf(prefix) > 0
        Dim f As Boolean = path.IndexOf(prefix, 2) = 0
        Dim g As Boolean = (Not path.StartsWith(prefix)) = a
        Return a OrElse b OrElse c OrElse d OrElse e OrElse f OrElse g
    End Function
End Class
",
                                                      equivalenceKey:="DCA082.StartsWith", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
