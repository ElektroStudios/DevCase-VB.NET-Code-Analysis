#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " SuppressionTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Verifies that every suppression mechanism documented in the "How to disable" section of the rule files works.
    ''' </summary>
    Public NotInheritable Class SuppressionTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New InvertedComparisonAnalyzer()

        ''' <summary>
        ''' A class with one violation of rule DCA023, shared by every suppression test.
        ''' </summary>
        Private Const Violation As String = "
Public Class C
    Public Function M(a As Integer, b As Integer) As Boolean
        Return Not (a = b)
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SuppressionTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that the sample violation is reported when no suppression is applied.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Baseline_IsReported() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(AnalyzerTestHost.CreateProject(Violation), Analyzer).ConfigureAwait(True)
            Assert.Single(diagnostics)
        End Function

        ''' <summary>
        ''' Verifies that <c>dotnet_diagnostic.ID.severity = none</c> in an <c>.editorconfig</c> file disables a rule.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function EditorConfig_SeverityNone() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject(Violation, editorConfig:="dotnet_diagnostic.DCA023.severity = none")
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Assert.DoesNotContain(diagnostics, Function(d As Diagnostic) d.Severity <> DiagnosticSeverity.Hidden AndAlso Not d.IsSuppressed)
        End Function

        ''' <summary>
        ''' Verifies that a <c>#Disable Warning</c> directive suppresses a rule in a block of code.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DisableWarningDirective() As Task
            Dim source As String = Violation.Replace("        Return Not (a = b)", "#Disable Warning DCA023 ' Reason.
        Return Not (a = b)
#Enable Warning DCA023", StringComparison.Ordinal)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(AnalyzerTestHost.CreateProject(source), Analyzer).ConfigureAwait(True)
            Assert.DoesNotContain(diagnostics, Function(d As Diagnostic) Not d.IsSuppressed)
        End Function

        ''' <summary>
        ''' Verifies that a <c>SuppressMessage</c> attribute on a member suppresses a rule in that member.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function SuppressMessageAttribute_OnMember() As Task
            Dim source As String = Violation.Replace("    Public Function M(", "    <System.Diagnostics.CodeAnalysis.SuppressMessage(""Style"", ""DCA023:Negated comparison"", Justification:=""Reason."")>
    Public Function M(", StringComparison.Ordinal)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(AnalyzerTestHost.CreateProject(source), Analyzer).ConfigureAwait(True)
            Assert.DoesNotContain(diagnostics, Function(d As Diagnostic) Not d.IsSuppressed)
        End Function

        ''' <summary>
        ''' Verifies that an assembly-level <c>SuppressMessage</c> attribute with module scope suppresses a rule in the whole project.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function SuppressMessageAttribute_ModuleScope() As Task
            Dim source As String = $"<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage(""Style"", ""DCA023:Negated comparison"", Justification:=""Reason."", Scope:=""module"")>{Violation}"
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(AnalyzerTestHost.CreateProject(source), Analyzer).ConfigureAwait(True)
            Assert.DoesNotContain(diagnostics, Function(d As Diagnostic) Not d.IsSuppressed)
        End Function

#End Region

    End Class

End Namespace

#End Region
