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

#Region " DCA020_FlagsEnumNoneTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA020 (Flags enumeration without a 'None' zero value) and its code fixes.
    ''' </summary>
    Public NotInheritable Class DCA020_FlagsEnumNoneTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New FlagsEnumNoneAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New FlagsEnumNoneCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA020_FlagsEnumNoneTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA020 reports <c>Flags</c> enumerations without a zero member and with a misnamed zero member.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_MissingAndMisnamedZero() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
<Flags>
Public Enum [|Missing|]
    A = 1
    B = 2
End Enum

<Flags>
Public Enum Misnamed
    [|Empty|] = 0
    A = 1
End Enum

<Flags>
Public Enum Valid
    None = 0
    A = 1
End Enum

Public Enum NotFlags
    A = 1
End Enum
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA020 adds a documented <c>None</c> member and keeps the blank-line layout.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsNoneWithDocumentationAndBlankLines() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
''' <summary>Options.</summary>
<Flags>
Public Enum Options

    ''' <summary>
    ''' Reads.
    ''' </summary>
    Read = 1 << 0

    ''' <summary>
    ''' Writes.
    ''' </summary>
    Write = 1 << 1

End Enum
", "
''' <summary>Options.</summary>
<Flags>
Public Enum Options

    ''' <summary>
    ''' No flags are set.
    ''' </summary>
    None = 0

    ''' <summary>
    ''' Reads.
    ''' </summary>
    Read = 1 << 0

    ''' <summary>
    ''' Writes.
    ''' </summary>
    Write = 1 << 1

End Enum
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA020 adds <c>None = 0</c> to an enumeration without blank lines.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsNone_CompactLayout() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
<Flags>
Public Enum Options
    Read = 1
    Write = 2
End Enum
", "
<Flags>
Public Enum Options
    None = 0
    Read = 1
    Write = 2
End Enum
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA020 renames the zero member and every reference to it.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RenamesZeroMemberAndItsReferences() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
<Flags>
Public Enum Options
    Empty = 0
    Read = 1
End Enum

Public Class C
    Private ReadOnly Value As Options = Options.Empty
End Class
", "
<Flags>
Public Enum Options
    None = 0
    Read = 1
End Enum

Public Class C
    Private ReadOnly Value As Options = Options.None
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA020 offers no fix when a member named <c>None</c> already has a non-zero value.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Conflict_IsReportedWithoutFix() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
<Flags>
Public Enum Options
    None = 4
    Read = 1
End Enum
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

#End Region

    End Class

End Namespace

#End Region
