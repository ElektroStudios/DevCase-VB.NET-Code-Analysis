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
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " NamedArgumentTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests that every rule that inspects the arguments of a call treats named arguments (<c>paramName:=x</c>, in any order)
    ''' like positional ones, and that the code fixes produce code that compiles with them.
    ''' </summary>
    Public NotInheritable Class NamedArgumentTests

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="NamedArgumentTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that a rule reports the same number of diagnostics (at least one) for a call written with positional
        ''' arguments and for the same call written with named arguments.
        ''' </summary>
        '''
        ''' <param name="ruleId">
        ''' The rule identifier.
        ''' </param>
        '''
        ''' <param name="positional">
        ''' The statements with positional arguments.
        ''' </param>
        '''
        ''' <param name="named">
        ''' The same statements with named arguments.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("DCA003", "If String.IsNullOrWhiteSpace(text) Then Exit Sub", "If String.IsNullOrWhiteSpace(value:=text) Then Exit Sub", False)>
        <InlineData("DCA009", "Dim b As Boolean = text.StartsWith(""x"")", "Dim b As Boolean = text.StartsWith(value:=""x"")", False)>
        <InlineData("DCA011", "If String.IsNullOrEmpty(text) Then Throw New ArgumentNullException(NameOf(text))",
                              "If String.IsNullOrEmpty(value:=text) Then Throw New ArgumentNullException(paramName:=NameOf(text))", True)>
        <InlineData("DCA011", "If value Is Nothing Then Throw New ArgumentNullException(NameOf(value), ""Missing."")",
                              "If value Is Nothing Then Throw New ArgumentNullException(message:=""Missing."", paramName:=NameOf(value))", True)>
        <InlineData("DCA013", "Dim h As String = BitConverter.ToString(bytes).Replace(""-"", """")",
                              "Dim h As String = BitConverter.ToString(value:=bytes).Replace(newValue:="""", oldValue:=""-"")", True)>
        <InlineData("DCA013", "If Not map.ContainsKey(text) Then map.Add(text, 1)", "If Not map.ContainsKey(key:=text) Then map.Add(value:=1, key:=text)", True)>
        <InlineData("DCA013", "Dim o As StringSplitOptions = DirectCast([Enum].Parse(GetType(StringSplitOptions), text), StringSplitOptions)",
                              "Dim o As StringSplitOptions = DirectCast([Enum].Parse(value:=text, enumType:=GetType(StringSplitOptions)), StringSplitOptions)", True)>
        <InlineData("DCA013", "Dim splitParts As String() = text.Split({""ab""}, StringSplitOptions.None)",
                              "Dim splitParts As String() = text.Split(options:=StringSplitOptions.None, separator:={""ab""})", True)>
        <InlineData("DCA013", "Dim b As Boolean = text.Contains(""x"") OrElse text.StartsWith(""y"", StringComparison.Ordinal)",
                              "Dim b As Boolean = text.Contains(value:=""x"") OrElse text.StartsWith(comparisonType:=StringComparison.Ordinal, value:=""y"")", True)>
        <InlineData("DCA017", "Dim b As Boolean = System.Text.RegularExpressions.Regex.IsMatch(text, ""x"")",
                              "Dim b As Boolean = System.Text.RegularExpressions.Regex.IsMatch(pattern:=""x"", input:=text)", False)>
        <InlineData("DCA018", "Dim d As New DateTime(2020, 1, 2)", "Dim d As New DateTime(day:=2, month:=1, year:=2020)", False)>
        <InlineData("DCA019", "Dim d As DateTime = DateTime.Parse(text)", "Dim d As DateTime = DateTime.Parse(s:=text)", False)>
        <InlineData("DCA021", "Dim b As Boolean = items.Any(Function(x As Integer) x > 1)", "Dim b As Boolean = items.Any(predicate:=Function(x As Integer) x > 1)", False)>
        <InlineData("DCA022", "Dim n As Integer = items.ElementAt(2)", "Dim n As Integer = items.ElementAt(index:=2)", False)>
        <InlineData("DCA025", "Dim b As Boolean = Not IsNothing(value) AndAlso TypeOf value Is String",
                              "Dim b As Boolean = Not IsNothing(Expression:=value) AndAlso TypeOf value Is String", False)>
        <InlineData("DCA026", "Dim w As New IO.StreamWriter(text)", "Dim w As New IO.StreamWriter(path:=text)", False)>
        <InlineData("DCA049", "Dim j As String = String.Join("","", parts)", "Dim j As String = String.Join(separator:="","", value:=parts)", False)>
        <InlineData("DCA049", "Dim j As String = String.Join("","", parts)", "Dim j As String = String.Join(separator:="","", values:=parts)", True)>
        Public Async Function NamedArguments_AreReportedLikePositional(ruleId As String, positional As String, named As String, targetNetFramework As Boolean) As Task
            Dim analyzer As DiagnosticAnalyzer = NamedArgumentTests.GetAnalyzer(ruleId)
            Dim positionalCount As Integer = Await NamedArgumentTests.CountAsync(analyzer, ruleId, positional, targetNetFramework).ConfigureAwait(True)
            Dim namedCount As Integer = Await NamedArgumentTests.CountAsync(analyzer, ruleId, named, targetNetFramework).ConfigureAwait(True)

            Assert.True(positionalCount > 0, $"{ruleId}: the positional form is not reported.")
            Assert.Equal(positionalCount, namedCount)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of a rule, applied to calls with named arguments, produces code that compiles and
        ''' that the rule no longer reports.
        ''' </summary>
        '''
        ''' <param name="ruleId">
        ''' The rule identifier.
        ''' </param>
        '''
        ''' <param name="statements">
        ''' The statements with named arguments.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' A fragment that the fixed code must contain.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("DCA009", "Dim b As Boolean = text.StartsWith(value:=""x"")", "comparisonType:=StringComparison.")>
        <InlineData("DCA017", "Dim b As Boolean = System.Text.RegularExpressions.Regex.IsMatch(pattern:=""x"", input:=text)", "matchTimeout:=")>
        <InlineData("DCA018", "Dim d As New DateTime(day:=2, month:=1, year:=2020)", "kind:=DateTimeKind.")>
        <InlineData("DCA019", "Dim d As DateTime = DateTime.Parse(s:=text)", "provider:=")>
        <InlineData("DCA021", "Dim b As Boolean = items.Any(predicate:=Function(x As Integer) x > 1)", "items.Exists(Function(x As Integer) x > 1)")>
        <InlineData("DCA022", "Dim n As Integer = items.ElementAt(index:=2)", "items(2)")>
        <InlineData("DCA026", "Dim w As New IO.StreamWriter(path:=text)", "encoding:=")>
        <InlineData("DCA049", "Dim j As String = String.Join(separator:="","", value:=parts)", "String.Join(separator:="",""c, value:=parts)")>
        Public Async Function NamedArguments_FixCompiles(ruleId As String, statements As String, expected As String) As Task
            Dim analyzer As DiagnosticAnalyzer = NamedArgumentTests.GetAnalyzer(ruleId)
            Dim provider As CodeFixProvider = NamedArgumentTests.GetProvider(ruleId)
            Dim source As String = NamedArgumentTests.Wrap(statements)

            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(analyzer, provider, source).ConfigureAwait(True)
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(analyzer, provider, source, actions(0).EquivalenceKey).ConfigureAwait(True)

            Assert.True(fixedText.Contains(expected, StringComparison.Ordinal), fixedText)
            Dim project As Project = AnalyzerTestHost.CreateProject(fixedText)
            Await AnalyzerTestHost.AssertCompilesAsync(project, ruleId).ConfigureAwait(True)
            Assert.DoesNotContain(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(True),
                                  Function(d As Diagnostic) d.Id.Equals(ruleId, StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' Verifies that the migrations of rule DCA013 read named arguments by parameter (in any order) and write the new
        ''' call positionally where the parameter names differ, so that the <c>NETCOREAPP</c> branch compiles on modern .NET.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NamedArguments_Dca013MigrationsCompileOnModernDotNet() As Task
            Dim source As String = NamedArgumentTests.Wrap("
        Dim h As String = BitConverter.ToString(value:=bytes).Replace(newValue:="""", oldValue:=""-"")
        If Not map.ContainsKey(key:=text) Then map.Add(value:=1, key:=text)
        Dim o As StringSplitOptions = DirectCast([Enum].Parse(value:=text, enumType:=GetType(StringSplitOptions)), StringSplitOptions)
        Dim parts2 As String() = text.Split(options:=StringSplitOptions.None, separator:={""ab""})
        Dim b As Boolean = text.Contains(value:=""x"") OrElse text.StartsWith(comparisonType:=StringComparison.Ordinal, value:=""y"")")
            Dim analyzer As DiagnosticAnalyzer = NamedArgumentTests.GetAnalyzer("DCA013")
            Dim provider As CodeFixProvider = NamedArgumentTests.GetProvider("DCA013")
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(analyzer, provider, source, NameOf(NetFrameworkApiMigrationCodeFixProvider),
                                                                                         targetNetFramework:=True).ConfigureAwait(True)

            Assert.Contains("Convert.ToHexString(bytes)", fixedText, StringComparison.Ordinal)
            Assert.Contains("map.TryAdd(text, 1)", fixedText, StringComparison.Ordinal)
            Assert.Contains("[Enum].Parse(Of StringSplitOptions)(text)", fixedText, StringComparison.Ordinal)

            Dim project As Project = AnalyzerTestHost.CreateProject(fixedText)
            Dim options As VisualBasic.VisualBasicParseOptions = DirectCast(project.ParseOptions, VisualBasic.VisualBasicParseOptions).
                WithPreprocessorSymbols(New KeyValuePair(Of String, Object)("NETCOREAPP", True), New KeyValuePair(Of String, Object)("NET5_0_OR_GREATER", True))
            Await AnalyzerTestHost.AssertCompilesAsync(project.WithParseOptions(options), "NETCOREAPP branch").ConfigureAwait(True)
            Await AnalyzerTestHost.AssertCompilesAsync(AnalyzerTestHost.CreateProject(fixedText, targetNetFramework:=True), ".NET Framework branch").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA050 follows a named argument to the method that validates it.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NamedArguments_Dca050FollowsValidatingHelper() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(New UnvalidatedParameterAnalyzer(), "
Public Class C
    Public Function Run(provider As IServiceProvider, key As String) As Object
        Return C.GetService(key:=key, provider:=provider)
    End Function
    Private Shared Function GetService(provider As IServiceProvider, key As String) As Object
        If provider Is Nothing Then
            Throw New ArgumentNullException(NameOf(provider))
        End If
        If String.IsNullOrEmpty(key) Then
            Throw New ArgumentNullException(NameOf(key))
        End If
        Return provider.GetService(Type.GetType(key))
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Wraps statements in a method whose parameters are used by the test sources.
        ''' </summary>
        '''
        ''' <param name="statements">
        ''' The statements.
        ''' </param>
        '''
        ''' <returns>
        ''' The source.
        ''' </returns>
        Private Shared Function Wrap(statements As String) As String

            Return $"
Imports System.Linq
Imports System.Threading
Public Class C
    Public Sub Run(text As String, value As Object, bytes As Byte(), parts As String(), items As List(Of Integer), map As Dictionary(Of String, Integer))
        {statements}
    End Sub
End Class
"
        End Function

        ''' <summary>
        ''' Counts the diagnostics of a rule in a source.
        ''' </summary>
        '''
        ''' <param name="analyzer">
        ''' The analyzer.
        ''' </param>
        '''
        ''' <param name="ruleId">
        ''' The rule identifier.
        ''' </param>
        '''
        ''' <param name="statements">
        ''' The statements, wrapped by <see cref="Wrap"/>.
        ''' </param>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' The number of diagnostics of the rule.
        ''' </returns>
        Private Shared Async Function CountAsync(analyzer As DiagnosticAnalyzer, ruleId As String, statements As String, targetNetFramework As Boolean) As Task(Of Integer)

            Dim project As Project = AnalyzerTestHost.CreateProject(NamedArgumentTests.Wrap(statements), targetNetFramework:=targetNetFramework)
            Await AnalyzerTestHost.AssertCompilesAsync(project, ruleId).ConfigureAwait(True)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, analyzer).ConfigureAwait(True)
            Return diagnostics.Count(Function(d As Diagnostic) d.Id.Equals(ruleId, StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' Gets the analyzer of a rule.
        ''' </summary>
        '''
        ''' <param name="ruleId">
        ''' The rule identifier.
        ''' </param>
        '''
        ''' <returns>
        ''' A new instance of the analyzer.
        ''' </returns>
        Private Shared Function GetAnalyzer(ruleId As String) As DiagnosticAnalyzer

            Return GetType(DiagnosticIds).Assembly.GetTypes().
                Where(Function(t As Type) Not t.IsAbstract AndAlso GetType(DiagnosticAnalyzer).IsAssignableFrom(t)).
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), DiagnosticAnalyzer)).
                Single(Function(a As DiagnosticAnalyzer) a.SupportedDiagnostics.Any(Function(d As DiagnosticDescriptor) d.Id.Equals(ruleId, StringComparison.Ordinal)))
        End Function

        ''' <summary>
        ''' Gets the code fix provider of a rule.
        ''' </summary>
        '''
        ''' <param name="ruleId">
        ''' The rule identifier.
        ''' </param>
        '''
        ''' <returns>
        ''' A new instance of the provider.
        ''' </returns>
        Private Shared Function GetProvider(ruleId As String) As CodeFixProvider

            Return GetType(NetFrameworkApiMigrationCodeFixProvider).Assembly.GetTypes().
                Where(Function(t As Type) Not t.IsAbstract AndAlso GetType(CodeFixProvider).IsAssignableFrom(t)).
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), CodeFixProvider)).
                Single(Function(p As CodeFixProvider) p.FixableDiagnosticIds.Contains(ruleId, StringComparer.Ordinal))
        End Function

#End Region

    End Class

End Namespace

#End Region
