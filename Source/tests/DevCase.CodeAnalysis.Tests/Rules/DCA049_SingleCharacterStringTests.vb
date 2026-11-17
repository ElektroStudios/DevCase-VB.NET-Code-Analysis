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
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New SingleCharacterStringCodeFixProvider()

        ''' <summary>
        ''' A source with every reported form.
        ''' </summary>
        Private Const ReportedSource As String = "
Imports System.Text
Public Class C
    Public Function Run(parts As String(), list As List(Of String), a As String, b As String) As String
        Dim sb As New StringBuilder()
        sb.Append([|""x""|])
        Return String.Join([|""&""|], parts) & String.Join([|"";""|], list) & String.Join([|""-""|], a, b) & String.Join([|""""""""|], parts) & sb.ToString()
    End Function
End Class
"

        ''' <summary>
        ''' The fixed version of <see cref="ReportedSource"/>, without markup.
        ''' </summary>
        Private Const FixedSource As String = "
Imports System.Text
Public Class C
    Public Function Run(parts As String(), list As List(Of String), a As String, b As String) As String
        Dim sb As New StringBuilder()
        sb.Append(""x""c)
        Return String.Join(""&""c, parts) & String.Join("";""c, list) & String.Join(""-""c, a, b) & String.Join(""""""""c, parts) & sb.ToString()
    End Function
End Class
"

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
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ReportedSource, targetNetFramework:=targetNetFramework).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA049 does not report longer or empty strings, non-literal arguments, named arguments,
        ''' other overloads or other methods.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OtherArguments() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text
Public Class C
    Private Const Separator As String = "",""
    Public Function Run(parts As String(), s As String) As String
        Dim sb As New StringBuilder()
        sb.Append(""ab"")
        sb.Append("""")
        sb.Append(""x"", 0, 1)
        sb.Append(""x""c)
        sb.AppendLine(""x"")
        Dim found As Boolean = s.Contains(""x"")
        Return String.Join("", "", parts) & String.Join("""", parts) & String.Join(Separator, parts) & String.Join(separator:=""&"", value:=parts, startIndex:=0, count:=1) & String.Concat(""x"", s) & found.ToString()
    End Function
End Class
").ConfigureAwait(True)
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
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, ReportedSource, FixedSource, targetNetFramework:=targetNetFramework).ConfigureAwait(True)
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
                                                                                         NameOf(SingleCharacterStringCodeFixProvider)).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual(FixedSource, fixedText)
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

#End Region

    End Class

End Namespace

#End Region
