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

#Region " DCA017_RegexTimeoutTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA017 (regular expression without match timeout) and its two code fixes.
    ''' </summary>
    Public NotInheritable Class DCA017_RegexTimeoutTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New RegexTimeoutAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New RegexTimeoutCodeFixProvider()

        ''' <summary>
        ''' The equivalence key of the finite timeout action.
        ''' </summary>
        Private Const FiniteKey As String = "RegexTimeoutCodeFixProvider.Finite"

        ''' <summary>
        ''' The equivalence key of the <c>Regex.InfiniteMatchTimeout</c> action.
        ''' </summary>
        Private Const InfiniteKey As String = "RegexTimeoutCodeFixProvider.Infinite"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA017_RegexTimeoutTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA017 reports static <c>Regex</c> methods and constructors without a match timeout.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_StaticMethodsAndConstructors() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text.RegularExpressions
Public Class C
    Public Sub M(input As String)
        Dim a As Boolean = [|Regex.IsMatch(input, ""a+"")|]
        Dim b As Match = [|Regex.Match(input, ""a+"", RegexOptions.IgnoreCase)|]
        Dim c As MatchCollection = [|Regex.Matches(input, ""a+"")|]
        Dim d As String = [|Regex.Replace(input, ""a+"", ""b"")|]
        Dim e As String = [|Regex.Replace(input, ""a+"", Function(x As Match) x.Value)|]
        Dim f As String() = [|Regex.Split(input, ""a+"")|]
        Dim g As New Regex(""a+"")
        Dim h As Regex = [|New Regex(""a+"", RegexOptions.Compiled)|]
    End Sub
End Class
".Replace("Dim g As New Regex(""a+"")", "Dim g As Regex = [|New Regex(""a+"")|]", StringComparison.Ordinal)).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA017 does not report calls with a timeout, instance methods, <c>NonBacktracking</c> options or methods without a timeout overload.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_TimeoutsInstanceMethodsNonBacktrackingAndOthers() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text.RegularExpressions
Public Class C
    Private Shared ReadOnly Pattern As New Regex(""a+"", RegexOptions.None, TimeSpan.FromSeconds(1))
    Public Sub M(input As String)
        Dim a As Boolean = Regex.IsMatch(input, ""a+"", RegexOptions.None, TimeSpan.FromSeconds(1))
        Dim b As Boolean = Regex.IsMatch(input, ""a+"", RegexOptions.None, Regex.InfiniteMatchTimeout)
        Dim c As Match = Pattern.Match(input)
        Dim d As String = Pattern.Replace(input, ""b"")
        Dim e As String = Regex.Escape(input)
        Dim f As Boolean = Regex.IsMatch(input, ""(a+)+"", RegexOptions.NonBacktracking Or RegexOptions.IgnoreCase)
        Dim g As New Regex(""(a+)+"", RegexOptions.NonBacktracking)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA017 adds <c>RegexOptions.None</c> and the default timeout.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsOptionsAndTimeout() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As Boolean
        Return Regex.IsMatch(input, ""a+"")
    End Function
End Class
", "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As Boolean
        Return Regex.IsMatch(input, ""a+"", RegexOptions.None, TimeSpan.FromSeconds(1))
    End Function
End Class
", equivalenceKey:=FiniteKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA017 keeps existing options and also fixes <c>Regex</c> constructors.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsOnlyTimeout_WhenOptionsArePresent_AndHandlesConstructors() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Private ReadOnly Pattern As New Regex(""a+"", RegexOptions.Compiled)
    Public Function M(input As String) As String
        Return Regex.Replace(input, ""a+"", Function(x As Match) x.Value.ToUpperInvariant(), RegexOptions.IgnoreCase)
    End Function
End Class
", "
Imports System.Text.RegularExpressions
Public Class C
    Private ReadOnly Pattern As New Regex(""a+"", RegexOptions.Compiled, TimeSpan.FromSeconds(1))
    Public Function M(input As String) As String
        Return Regex.Replace(input, ""a+"", Function(x As Match) x.Value.ToUpperInvariant(), RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))
    End Function
End Class
", equivalenceKey:=FiniteKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the second action of rule DCA017 adds <c>Regex.InfiniteMatchTimeout</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InfiniteTimeout_IsAnExplicitOptOut() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Private ReadOnly Pattern As New Regex(""a+"")
End Class
", "
Imports System.Text.RegularExpressions
Public Class C
    Private ReadOnly Pattern As New Regex(""a+"", RegexOptions.None, Regex.InfiniteMatchTimeout)
End Class
", equivalenceKey:=InfiniteKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA017 uses the configured timeout, and the default one for invalid values.
        ''' </summary>
        '''
        ''' <param name="configured">
        ''' The configured timeout, in milliseconds.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' The expected <see cref="TimeSpan"/> expression.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("2000", "TimeSpan.FromSeconds(2)")>
        <InlineData("250", "TimeSpan.FromMilliseconds(250)")>
        <InlineData("-5", "TimeSpan.FromSeconds(1)")>
        <InlineData("abc", "TimeSpan.FromSeconds(1)")>
        Public Async Function Fix_UsesConfiguredTimeout(configured As String, expected As String) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As String()
        Return Regex.Split(input, "","", RegexOptions.None)
    End Function
End Class
", $"
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As String()
        Return Regex.Split(input, "","", RegexOptions.None, {expected})
    End Function
End Class
", equivalenceKey:=FiniteKey, editorConfig:=$"devcase_analyzers.dca017_timeout_ms = {configured}").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA017 handles named arguments, multi-line calls and qualified type names.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NamedArguments_MultiLine_AndQualifiedNames() As Task
            ' No Imports of System.Text.RegularExpressions: type names are written in their shortest valid form.
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(input As String) As Boolean
        Return System.Text.RegularExpressions.Regex.IsMatch(input:=input,
                                                            pattern:=""a+"")
    End Function
End Class
", "
Public Class C
    Public Function M(input As String) As Boolean
        Return System.Text.RegularExpressions.Regex.IsMatch(input:=input,
                                                            pattern:=""a+"", options:=Text.RegularExpressions.RegexOptions.None, matchTimeout:=TimeSpan.FromSeconds(1))
    End Function
End Class
", equivalenceKey:=FiniteKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA017 offers the finite and infinite timeout actions, in that order.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_OffersBothActions() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As Boolean
        Return Regex.IsMatch(input, ""a+"")
    End Function
End Class
").ConfigureAwait(True)
            Assert.Equal({FiniteKey, InfiniteKey}, actions.Select(Function(a As CodeAction) a.EquivalenceKey).ToArray())
            Assert.Contains("TimeSpan.FromSeconds(1)", actions(0).Title, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA017 fixes nested calls without conflicts.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_NestedCalls_DoNotConflict() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As String
        Return Regex.Replace(Regex.Replace(input, ""a"", ""b""), ""c"", ""d"")
    End Function
End Class
", FiniteKey).ConfigureAwait(True)
            Assert.Contains("Regex.Replace(Regex.Replace(input, ""a"", ""b"", RegexOptions.None, TimeSpan.FromSeconds(1)), ""c"", ""d"", RegexOptions.None, TimeSpan.FromSeconds(1))", fixedText, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that the code written by the fix of rule DCA017 compiles on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CompilesOnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As MatchCollection
        Return Regex.Matches(input, ""\d+"")
    End Function
End Class
", "
Imports System.Text.RegularExpressions
Public Class C
    Public Function M(input As String) As MatchCollection
        Return Regex.Matches(input, ""\d+"", RegexOptions.None, TimeSpan.FromSeconds(1))
    End Function
End Class
", equivalenceKey:=FiniteKey, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that formatting a timeout rejects values that are not positive.
        ''' </summary>
        <Fact>
        Public Sub FormatTimeout_RejectsNonPositiveValues()
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() RegexTimeoutCodeFixProvider.FormatTimeout(0))
        End Sub

#End Region

    End Class

End Namespace

#End Region
