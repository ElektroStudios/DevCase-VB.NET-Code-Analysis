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

#Region " DCA009_StringComparisonTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA009 (missing StringComparison / StringComparer) and its two code fixes.
    ''' </summary>
    Public NotInheritable Class DCA009_StringComparisonTests

#Region " Fields "

        ''' <summary>
        ''' The titles of the two code actions, in registration order.
        ''' </summary>
        Private Shared ReadOnly ExpectedActionTitles As String() = {"Add 'StringComparison.Ordinal'", "Add 'StringComparison.OrdinalIgnoreCase'"}

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New StringComparisonAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New StringComparisonCodeFixProvider()

        ''' <summary>
        ''' The equivalence key of the <c>Ordinal</c> action.
        ''' </summary>
        Private Const OrdinalKey As String = "StringComparisonCodeFixProvider.Ordinal"

        ''' <summary>
        ''' The equivalence key of the <c>OrdinalIgnoreCase</c> action.
        ''' </summary>
        Private Const IgnoreCaseKey As String = "StringComparisonCodeFixProvider.OrdinalIgnoreCase"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA009_StringComparisonTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA009 reports omitted comparisons available through an overload or an optional parameter.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_OverloadsAndOptionalParameters() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Linq
Public Class C
    Public Sub Run(s As String, t As String, names As String())
        Dim a As Boolean = [|s.StartsWith(""x"")|]
        Dim b As Boolean = [|String.Equals(s, t)|]
        Dim c As Integer = [|String.Compare(s, t)|]
        Dim d As Boolean = [|s.Contains(""x"")|]
        Dim e As Integer = [|s.IndexOf(""x"")|]
        Dim f As Boolean = [|names.Contains(""x"")|]
        Dim g As String = [|s.Replace(""a"", ""b"")|]
        [|Me.Find(s)|]
    End Sub
    Public Sub Find(value As String, Optional comparison As System.StringComparison = System.StringComparison.CurrentCulture)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA009 does not report calls that already specify a comparison, or that have none available.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_CompliantOrUnrelatedCalls() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System
Imports System.Collections.Generic
Public Class C
    Public Sub Run(s As String, o As Object, list As List(Of String))
        Dim a As Boolean = s.StartsWith(""x"", StringComparison.Ordinal)
        Dim b As Boolean = s.StartsWith(""x""c)
        Dim c As Boolean = o.Equals(s)
        Dim d As Boolean = list.Contains(""x"")
        Dim e As String = s.ToUpperInvariant()
        Dim f As String = NameOf(s.StartsWith)
        Me.Find(s, StringComparison.Ordinal)
        Dim g As Integer = s.Length
    End Sub
    Public Sub Find(value As String, Optional comparison As StringComparison = StringComparison.CurrentCulture)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA009 offers the <c>Ordinal</c> and <c>OrdinalIgnoreCase</c> actions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function RegistersTwoActions() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Function Run(s As String) As Boolean
        Return s.StartsWith(""x"")
    End Function
End Class
").ConfigureAwait(True)
            Assert.Equal(DCA009_StringComparisonTests.ExpectedActionTitles, actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA009 adds <c>StringComparison.Ordinal</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Ordinal() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Linq
Public Class C
    Public Sub Run(s As String, t As String, names As String())
        Dim a As Boolean = s.StartsWith(""x"")
        Dim b As Boolean = String.Equals(s, t)
        Dim f As Boolean = names.Contains(""x"")
        Dim h As Integer = s.IndexOf(value:=""x"")
        Me.Find(s)
    End Sub
    Public Sub Find(value As String, Optional comparison As System.StringComparison = System.StringComparison.CurrentCulture)
    End Sub
End Class
", "
Imports System.Linq
Public Class C
    Public Sub Run(s As String, t As String, names As String())
        Dim a As Boolean = s.StartsWith(""x"", StringComparison.Ordinal)
        Dim b As Boolean = String.Equals(s, t, StringComparison.Ordinal)
        Dim f As Boolean = names.Contains(""x"", StringComparer.Ordinal)
        Dim h As Integer = s.IndexOf(value:=""x"", comparisonType:=StringComparison.Ordinal)
        Me.Find(s, StringComparison.Ordinal)
    End Sub
    Public Sub Find(value As String, Optional comparison As System.StringComparison = System.StringComparison.CurrentCulture)
    End Sub
End Class
", equivalenceKey:=OrdinalKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA009 fully qualifies the comparison type when <c>System</c> is not imported.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_OrdinalIgnoreCase_FullyQualifiesWhenSystemIsNotImported() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Namespace N
    Public Class StringComparison
    End Class
    Public Class C
        Public Function Run(s As String) As Boolean
            Return s.EndsWith(""x"")
        End Function
    End Class
End Namespace
", "
Namespace N
    Public Class StringComparison
    End Class
    Public Class C
        Public Function Run(s As String) As Boolean
            Return s.EndsWith(""x"", System.StringComparison.OrdinalIgnoreCase)
        End Function
    End Class
End Namespace
", equivalenceKey:=IgnoreCaseKey).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
