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
    ''' Tests of rule DCA020 (Flags enumeration without a zero value) and its code fix.
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
        ''' Verifies that rule DCA020 reports only the &lt;Flags&gt; enumerations without a member whose value is zero: a zero 
        ''' member is never reported, whatever its name, and whether its value is explicit or implicit (the first member).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_OnlyEnumerationsWithoutZeroValue() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
<Flags>
Public Enum [|Missing|]
    A = 1
    B = 2
End Enum

<Flags>
Public Enum ZeroWithAnotherName
    Empty = 0
    A = 1
End Enum

<Flags>
Public Enum HexadecimalZero
    Off = &H0
    A = &H1
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
        ''' Verifies that rule DCA020 does not report an enumeration whose first member has the implicit value zero under a name 
        ''' other than <c>None</c>: renaming it would break the code that uses it.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ImplicitZeroWithAnotherName() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
<Flags>
Friend Enum DesktopWallpaperStyle As Integer
    Null
    Tile
    Center
    Stretch
    Fit
    Fill
    Span
End Enum
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA020 adds <c>None = 0</c> with a documentation comment, keeping the blank lines 
        ''' between the members.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsNoneWithDocumentationAndBlankLines() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
<Flags>
Public Enum Options

    ''' <summary>
    ''' Read access.
    ''' </summary>
    Read = 1 << 0

    ''' <summary>
    ''' Write access.
    ''' </summary>
    Write = 1 << 1

End Enum
", "
<Flags>
Public Enum Options

    ''' <summary>
    ''' No flags are set.
    ''' </summary>
    None = 0

    ''' <summary>
    ''' Read access.
    ''' </summary>
    Read = 1 << 0

    ''' <summary>
    ''' Write access.
    ''' </summary>
    Write = 1 << 1

End Enum
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA020 adds <c>None = 0</c> to an enumeration without blank lines between members.
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
        ''' Verifies that the code fix of rule DCA020 uses the next free name when <c>None</c> is already used by a member with 
        ''' another value: <c>[Nothing]</c>, <c>Null</c>, <c>Zero</c>, <c>Empty</c>. Names are compared without case, as 
        ''' Visual Basic does.
        ''' </summary>
        '''
        ''' <param name="usedMembers">
        ''' The members of the enumeration, all with a non-zero value.
        ''' </param>
        '''
        ''' <param name="expectedName">
        ''' The name of the added zero member, as written in source.
        ''' </param>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Theory>
        <InlineData("None = 1", "[Nothing]")>
        <InlineData("NONE = 1", "[Nothing]")>
        <InlineData("None = 1|[Nothing] = 2", "Null")>
        <InlineData("None = 1|[Nothing] = 2|Null = 4", "Zero")>
        <InlineData("None = 1|[Nothing] = 2|Null = 4|zero = 8", "Empty")>
        Public Async Function Fix_UsesTheNextFreeName(usedMembers As String, expectedName As String) As Task
            Dim members As String = String.Join(String.Empty, usedMembers.Split("|"c).Select(Function(m As String) $"    {m}{vbLf}"))
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"
<Flags>
Public Enum Options
{members}End Enum
", $"
<Flags>
Public Enum Options
    {expectedName} = 0
{members}End Enum
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA020 is reported without a code fix when every candidate name is already used by a member with 
        ''' another value.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NoFix_WhenEveryCandidateNameIsUsed() As Task
            Const Source As String = "
<Flags>
Public Enum [|Options|]
    None = 1
    [Nothing] = 2
    Null = 4
    Zero = 8
    Empty = 16
End Enum
"
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, Source).ConfigureAwait(True)
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, Source.Replace("[|", String.Empty, StringComparison.Ordinal).
                                                                                                                    Replace("|]", String.Empty, StringComparison.Ordinal)).ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

#End Region

    End Class

End Namespace

#End Region
