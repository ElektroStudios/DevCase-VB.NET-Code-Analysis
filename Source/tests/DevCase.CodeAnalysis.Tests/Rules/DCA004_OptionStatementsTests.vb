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
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA004_OptionStatementsTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA004 (missing Option statements) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA004_OptionStatementsTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New OptionStatementsAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New OptionStatementsCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA004_OptionStatementsTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA004 reports a file without <c>Option</c> statements on its first token.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_FileWithoutOptions_OnFirstToken() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "' Header comment.
[|Imports|] System
Public Class C
End Class
").ConfigureAwait(True)
            Assert.Contains("Option Strict On, Option Explicit On, Option Infer Off", diagnostics(0).GetMessage(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA004 reports wrong or missing <c>Option</c> statements.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_WrongOrPartialOptions() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "[|Option Strict Off
Option Explicit|]
Public Class C
End Class
").ConfigureAwait(True)
            Assert.Contains("Option Strict On, Option Infer Off", diagnostics(0).GetMessage(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA004 does not report a file with the required <c>Option</c> statements.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_CompliantFile() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "Option Infer Off
Option Strict
Option Explicit On
Public Class C
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA004 does not report generated files.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_GeneratedFile() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({New TestSource("Form1.Designer.vb", "Partial Class Form1" & vbLf & "End Class" & vbLf)})
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA004 inserts the <c>Option Statements</c> region at the top of the file.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InjectsRegionAtTop() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "#Region "" Imports ""

Imports System

#End Region

Public Class C
End Class
", "#Region "" Option Statements ""

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region "" Imports ""

Imports System

#End Region

Public Class C
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA004 replaces conflicting statements and keeps <c>Option Compare</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReplacesConflictingOptions_AndKeepsOptionCompare() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "Option Compare Text
Option Strict Off
Imports System
Public Class C
End Class
", "#Region "" Option Statements ""

Option Strict On
Option Explicit On
Option Infer Off
Option Compare Text

#End Region

Imports System
Public Class C
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA004 completes an existing <c>Option Statements</c> region.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReusesExistingRegion() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "#Region "" Option Statements ""

Option Strict On
Option Infer On

#End Region

Public Class C
End Class
", "#Region "" Option Statements ""

Option Strict On
Option Explicit On
Option Infer Off

#End Region

Public Class C
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA004 keeps CRLF line endings.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_PreservesCrLfLineEndings() As Task
            Dim source As String = "Imports System" & vbCrLf & "Public Class C" & vbCrLf & "End Class" & vbCrLf
            Dim expected As String = "#Region "" Option Statements """ & vbCrLf & vbCrLf &
                                     "Option Strict On" & vbCrLf & "Option Explicit On" & vbCrLf & "Option Infer Off" & vbCrLf & vbCrLf &
                                     "#End Region" & vbCrLf & vbCrLf & source
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, source, expected).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA004 uses CRLF when the file has no line break.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_OptionsAtEndOfFileWithoutLineBreak_DefaultsToCrLf() As Task
            ' A single-line file has no line break to imitate: CRLF (the Windows default) is used.
            Dim expected As String = "#Region "" Option Statements """ & vbCrLf & vbCrLf &
                                     "Option Strict On" & vbCrLf & "Option Explicit On" & vbCrLf & "Option Infer Off" & vbCrLf & vbCrLf &
                                     "#End Region"
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "Option Strict On", expected).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
