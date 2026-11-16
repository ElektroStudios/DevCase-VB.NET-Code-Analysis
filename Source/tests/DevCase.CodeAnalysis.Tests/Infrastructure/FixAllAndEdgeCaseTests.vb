#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " FixAllAndEdgeCaseTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' "Fix All" scenarios (batch merge of independent fixes) and cross-rule edge cases.
    ''' </summary>
    Public NotInheritable Class FixAllAndEdgeCaseTests

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FixAllAndEdgeCaseTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that Fix All of rule DCA007 inserts one documentation comment per member, without duplicates.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_Documentation_InsertsEveryCommentOnce() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(
                New PublicMemberDocumentationAnalyzer(), New PublicMemberDocumentationCodeFixProvider(), "Public Class C
    Public Sub A()
    End Sub
    Public Sub B()
    End Sub
End Class
", NameOf(PublicMemberDocumentationCodeFixProvider)).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("''' <summary></summary>
Public Class C
    ''' <summary></summary>
    Public Sub A()
    End Sub
    ''' <summary></summary>
    Public Sub B()
    End Sub
End Class
", fixedText)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA006 wraps all the types of a file in a single <c>Namespace</c> block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_Namespace_WrapsAllTypesOnlyOnce() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(
                New NamespaceDeclarationAnalyzer(), New NamespaceDeclarationCodeFixProvider(), "Public Class A
End Class

Public Class B
End Class
", NameOf(NamespaceDeclarationCodeFixProvider)).ConfigureAwait(True)

            Assert.Equal(1, fixedText.Split("Namespace TestProject", StringSplitOptions.None).Length - 1)
            Await AnalyzerTestHost.AssertCompilesAsync(AnalyzerTestHost.CreateProject(fixedText), "fixed source").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA005 inserts a constructor in each class of a document.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_Constructors_InTwoClasses() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(
                New ExplicitConstructorAnalyzer(), New ExplicitConstructorCodeFixProvider(), "Public Class A
End Class
Public Class B
End Class
", NameOf(ExplicitConstructorCodeFixProvider)).ConfigureAwait(True)

            Assert.Equal(2, fixedText.Split("Public Sub New()", StringSplitOptions.None).Length - 1)
            Dim project As Project = AnalyzerTestHost.CreateProject(fixedText)
            Await AnalyzerTestHost.AssertCompilesAsync(project, "fixed source").ConfigureAwait(True)
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, New ExplicitConstructorAnalyzer()).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA009 applies the <c>OrdinalIgnoreCase</c> action to every call of a document.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_StringComparison_OrdinalIgnoreCase() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(
                New StringComparisonAnalyzer(), New StringComparisonCodeFixProvider(), "Public Class C
    Public Function Run(s As String) As Boolean
        Return s.StartsWith(""a"") AndAlso s.EndsWith(""b"") AndAlso s.Contains(""c"")
    End Function
End Class
", $"{NameOf(StringComparisonCodeFixProvider)}.OrdinalIgnoreCase").ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("Public Class C
    Public Function Run(s As String) As Boolean
        Return s.StartsWith(""a"", StringComparison.OrdinalIgnoreCase) AndAlso s.EndsWith(""b"", StringComparison.OrdinalIgnoreCase) AndAlso s.Contains(""c"", StringComparison.OrdinalIgnoreCase)
    End Function
End Class
", fixedText)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA001 reports awaiting a generic type parameter constrained to <see cref="Task"/>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function ConfigureAwait_ReportsGenericTypeParameterConstrainedToTask() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(New ConfigureAwaitAnalyzer(), "
Imports System.Threading.Tasks
Public Class C
    Public Async Function RunAsync(Of T As Task)(work As T) As Task
        [|Await work|]
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the text-based code fixes keep CRLF line endings.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function TextBasedFixes_PreserveCrLf() As Task
            Dim source As String = "''' <summary>S</summary>" & vbCrLf & "Public Class C" & vbCrLf & "    Public Sub Run()" & vbCrLf & "    End Sub" & vbCrLf & "End Class" & vbCrLf
            Dim fixedDocs As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(
                New PublicMemberDocumentationAnalyzer(), New PublicMemberDocumentationCodeFixProvider(), source, NameOf(PublicMemberDocumentationCodeFixProvider)).ConfigureAwait(True)
            Dim fixedCtor As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(
                New ExplicitConstructorAnalyzer(), New ExplicitConstructorCodeFixProvider(), source, NameOf(ExplicitConstructorCodeFixProvider)).ConfigureAwait(True)

            For Each text As String In {fixedDocs, fixedCtor}
                Assert.DoesNotContain(vbLf, text.Replace(vbCrLf, String.Empty, StringComparison.Ordinal), StringComparison.Ordinal)
            Next text
        End Function

        ''' <summary>
        ''' Verifies that applying every code fix in sequence produces a file that no rule reports and that still compiles.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function ChainedFixes_ProduceFullyCompliantFile() As Task
            ' Applies every structural fix in sequence on the same file, as a developer would do from the lightbulb.
            Dim text As String = "Public Class Sample
    Public Function Run(value As String) As Boolean
        If value Is Nothing Then Return False
        Return value.StartsWith(""x"")
    End Function
End Class
"
            Dim pipeline As (Analyzer As DiagnosticAnalyzer, Provider As CodeFixProvider, Key As String)() = {
                (New OptionStatementsAnalyzer(), New OptionStatementsCodeFixProvider(), NameOf(OptionStatementsCodeFixProvider)),
                (New NamespaceDeclarationAnalyzer(), New NamespaceDeclarationCodeFixProvider(), NameOf(NamespaceDeclarationCodeFixProvider)),
                (New ExplicitConstructorAnalyzer(), New ExplicitConstructorCodeFixProvider(), NameOf(ExplicitConstructorCodeFixProvider)),
                (New PublicMemberDocumentationAnalyzer(), New PublicMemberDocumentationCodeFixProvider(), NameOf(PublicMemberDocumentationCodeFixProvider)),
                (New StringComparisonAnalyzer(), New StringComparisonCodeFixProvider(), $"{NameOf(StringComparisonCodeFixProvider)}.Ordinal")}

            For Each stage As (Analyzer As DiagnosticAnalyzer, Provider As CodeFixProvider, Key As String) In pipeline
                text = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(stage.Analyzer, stage.Provider, text, stage.Key).ConfigureAwait(True)
            Next stage

            Dim project As Project = AnalyzerTestHost.CreateProject(text)
            Await AnalyzerTestHost.AssertCompilesAsync(project, "fully fixed source").ConfigureAwait(True)

            Dim structural As DiagnosticAnalyzer() = pipeline.Select(Function(s As (Analyzer As DiagnosticAnalyzer, Provider As CodeFixProvider, Key As String)) s.Analyzer).ToArray()
            Dim remaining As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, structural).ConfigureAwait(True)

            ' Only the expected DCA008 empty-tag follow-up is not part of this pipeline; nothing else may remain.
            Assert.True(remaining.IsEmpty, String.Join(vbLf, remaining.Select(Function(d As Diagnostic) d.ToString())) & vbLf & text)
            Assert.StartsWith("#Region "" Option Statements """, text, StringComparison.Ordinal)
            Assert.Contains("Namespace TestProject", text, StringComparison.Ordinal)
            Assert.Contains("Public Sub New()", text, StringComparison.Ordinal)
            Assert.Contains("value.StartsWith(""x"", StringComparison.Ordinal)", text, StringComparison.Ordinal)
        End Function

#End Region

    End Class

End Namespace

#End Region
