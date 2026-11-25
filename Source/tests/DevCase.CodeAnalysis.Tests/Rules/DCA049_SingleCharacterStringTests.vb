#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA049_SingleCharacterStringTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA049 (single-character string where a Char can be used) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA049_SingleCharacterStringTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New SingleCharacterStringAnalyzer()

        ''' <summary>
        ''' The expected messages of <see cref="Message_NamesTheCharOverload"/>.
        ''' </summary>
        Private Shared ReadOnly ExpectedMessages As String() = {
            "Use 'StringBuilder.Append(Char)' instead of 'StringBuilder.Append(String)' when the input is a constant unit string",
            "Use 'String.Replace(Char)' instead of 'String.Replace(String)' when the input is a constant unit string"}

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New SingleCharacterStringCodeFixProvider()

        ''' <summary>
        ''' A source with every reported form.
        ''' </summary>
        Private Const ReportedSource As String = "
Imports System.IO
Imports System.Text
Public Class C
    Public Function Run(parts As String(), list As List(Of String), a As String, b As String, s As String, writer As StreamWriter) As String
        Dim sb As New StringBuilder()
        sb.Append([|""x""|])
        sb.Insert(0, [|""-""|])
        sb.Replace([|""a""|], ""b"")
        Console.Write([|"" ""|])
        writer.WriteLine([|"";""|])
        Dim t As String = s.Replace([|""\""|], ""/"")
        Return String.Join([|""&""|], parts) & String.Join([|"";""|], list) & String.Join([|""-""|], a, b) & String.Join([|""""""""|], parts) & sb.ToString() & t
    End Function
End Class
"

        ''' <summary>
        ''' The fixed version of <see cref="ReportedSource"/>, without markup.
        ''' </summary>
        Private Const FixedSource As String = "
Imports System.IO
Imports System.Text
Public Class C
    Public Function Run(parts As String(), list As List(Of String), a As String, b As String, s As String, writer As StreamWriter) As String
        Dim sb As New StringBuilder()
        sb.Append(""x""c)
        sb.Insert(0, ""-""c)
        sb.Replace(""a""c, ""b""c)
        Console.Write("" ""c)
        writer.WriteLine("";""c)
        Dim t As String = s.Replace(""\""c, ""/""c)
        Return String.Join(""&""c, parts) & String.Join("";""c, list) & String.Join(""-""c, a, b) & String.Join(""""""""c, parts) & sb.ToString() & t
    End Function
End Class
"

        ''' <summary>
        ''' A source with the forms that only modern .NET reports (their Char overloads do not exist on .NET Framework). 
        ''' <c>String.Contains</c> is not reported there: the built-in rule CA1847 already reports it.
        ''' </summary>
        Private Const ModernSource As String = "
Imports System.Text
Public Class C
    Public Function Run(parts As String(), s As String) As Boolean
        Dim fields As String() = s.Split([|"",""|])
        Dim sb As New StringBuilder()
        sb.AppendJoin([|"";""|], parts)
        Return s.Contains(""x"") OrElse s.Contains(""y"", StringComparison.OrdinalIgnoreCase) OrElse fields.Length > sb.Length
    End Function
End Class
"

        ''' <summary>
        ''' <see cref="ReportedSource"/> as reported on modern .NET: <c>StringBuilder.Append</c> is left to the built-in rule CA1834.
        ''' </summary>
        Private Shared ReadOnly ModernReportedSource As String = ReportedSource.Replace("sb.Append([|""x""|])", "sb.Append(""x"")", StringComparison.Ordinal)

        ''' <summary>
        ''' <see cref="FixedSource"/> as fixed on modern .NET, where <c>StringBuilder.Append</c> is not reported.
        ''' </summary>
        Private Shared ReadOnly ModernFixedSource As String = FixedSource.Replace("sb.Append(""x""c)", "sb.Append(""x"")", StringComparison.Ordinal)

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA049_SingleCharacterStringTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA049 reports single-character separators of String.Join and StringBuilder.Append(String), on modern .NET and on .NET Framework.
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Async Function Reports_SingleCharacterStrings(targetNetFramework As Boolean) As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, If(targetNetFramework, ReportedSource, ModernReportedSource), targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA049 does not report longer or empty strings, non-literal arguments, overloads without 
        ''' an equivalent Char overload (AppendLine, format strings, Replace with a longer value), methods whose Char overload 
        ''' compares differently (IndexOf, StartsWith), other methods, and, on .NET Framework, the Char overloads that only exist 
        ''' on modern .NET (Contains).
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Async Function DoesNotReport_OtherArguments(targetNetFramework As Boolean) As Task
            Dim netFrameworkOnly As String = If(targetNetFramework, "Dim contained As Boolean = s.Contains(""x"")", String.Empty)
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"
Imports System.IO
Imports System.Text
Public Class C
    Private Const Separator As String = "",""
    Public Function Run(parts As String(), s As String, path As String) As String
        Dim sb As New StringBuilder()
        sb.Append(""ab"")
        sb.Append("""")
        sb.Append(""x"", 0, 1)
        sb.Append(""x""c)
        sb.AppendLine(""x"")
        sb.AppendFormat(""x"", 1)
        Console.WriteLine(""x"", 1)
        File.WriteAllText(path, ""x"")
        Dim index As Integer = s.IndexOf(""x"")
        Dim starts As Boolean = s.StartsWith(""x"")
        Dim replaced As String = s.Replace(""a"", ""bc"")
        {netFrameworkOnly}
        Return String.Join("", "", parts) & String.Join("""", parts) & String.Join(Separator, parts) & String.Concat(""x"", s) & index.ToString() & starts.ToString() & replaced
    End Function
End Class
", targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA049 reports, on modern .NET, the calls whose Char overload only exists there 
        ''' (String.Split, StringBuilder.AppendJoin), and that the fix compiles.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ModernOverloads() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ModernSource).ConfigureAwait(True)
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, ModernSource, ModernSource.
                Replace("[|"",""|]", """,""c", StringComparison.Ordinal).Replace("[|"";""|]", """;""c", StringComparison.Ordinal)).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA049 writes character literals, and that the result compiles on modern .NET and on .NET Framework.
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Async Function Fix_WritesCharacterLiterals(targetNetFramework As Boolean) As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, If(targetNetFramework, ReportedSource, ModernReportedSource),
                                                      If(targetNetFramework, FixedSource, ModernFixedSource), targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA049 fixes every literal of the document.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_FixesEveryLiteral() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, ReportedSource.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal),
                                                                                         NameOf(SingleCharacterStringCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual(FixedSource, fixedText)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA049 leaves <c>StringBuilder.Append</c> and <c>String.Contains</c> to the built-in rules CA1834 and 
        ''' CA1847 on modern .NET, so that the same literal is never reported twice, and still reports <c>StringBuilder.Append</c> 
        ''' on .NET Framework (which has no <c>String.Contains(Char)</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function CallsCoveredByMicrosoftRules_ReportedOnlyOnNetFramework() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text
Public Class C
    Public Function Run(s As String) As Boolean
        Dim sb As New StringBuilder()
        sb.Append(""x"")
        Return s.Contains(""x"") OrElse sb.Length = 0
    End Function
End Class
").ConfigureAwait(True)
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text
Public Class C
    Public Function Run(s As String) As Boolean
        Dim sb As New StringBuilder()
        sb.Append([|""x""|])
        Return s.Contains(""x"") OrElse sb.Length = 0
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies on modern .NET that the fixed calls bind to the Char overloads, and on .NET Framework that they bind to the
        ''' same String overloads with a constant string argument (the conversion is done at compile time).
        ''' </summary>
        '''
        ''' <param name="targetNetFramework">
        ''' <see langword="True"/> to compile against .NET Framework 4.8.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData(False)>
        <InlineData(True)>
        Public Async Function FixedCalls_BindToExpectedOverloads(targetNetFramework As Boolean) As Task
            Dim project As Project = AnalyzerTestHost.CreateProject(FixedSource, targetNetFramework:=targetNetFramework)
            Dim compilation As Compilation = Await project.GetCompilationAsync(CancellationToken.None).ConfigureAwait(True)
            Dim tree As SyntaxTree = compilation.SyntaxTrees(0)
            Dim model As SemanticModel = compilation.GetSemanticModel(tree)

            Dim checkedJoins As Integer = 0
            For Each invocation As InvocationExpressionSyntax In (Await tree.GetRootAsync(CancellationToken.None).ConfigureAwait(True)).DescendantNodes().OfType(Of InvocationExpressionSyntax)()
                Dim operation As IInvocationOperation = TryCast(model.GetOperation(invocation), IInvocationOperation)
                If operation Is Nothing OrElse Not operation.TargetMethod.Name.Equals("Join", StringComparison.Ordinal) Then
                    Continue For
                End If

                Dim separator As IOperation = operation.Arguments.First(Function(a As IArgumentOperation) a.Parameter.Ordinal = 0).Value
                If targetNetFramework Then
                    Assert.Equal(SpecialType.System_String, operation.TargetMethod.Parameters(0).Type.SpecialType)
                    Assert.True(separator.ConstantValue.HasValue)
                    Assert.IsType(Of String)(separator.ConstantValue.Value)
                Else
                    Assert.Equal(SpecialType.System_Char, operation.TargetMethod.Parameters(0).Type.SpecialType)
                End If
                checkedJoins += 1
            Next invocation
            Assert.Equal(4, checkedJoins)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA049, which follows CA1834 with the name of the called method, in a .NET Framework project.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_NamesTheCharOverload() As Task
            Dim diagnostics As Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text

Public Class C
    Public Function Run(sb As StringBuilder, text As String) As String
        sb.Append([|"",""|])
        Return text.Replace([|""a""|], ""b"")
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA049_SingleCharacterStringTests.ExpectedMessages,
                         diagnostics.Select(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
