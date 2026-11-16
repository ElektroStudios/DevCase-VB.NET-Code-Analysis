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

#Region " DCA010_FlagsEnumBitShiftTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA010 (Flags enums without bit-shift format) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA010_FlagsEnumBitShiftTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New FlagsEnumBitShiftAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New FlagsEnumBitShiftCodeFixProvider()

        ''' <summary>
        ''' The <c>.editorconfig</c> line that enables rule DCA010, which is disabled by default.
        ''' </summary>
        Private Const EnabledConfig As String = "devcase_analyzers.dca010_enabled = true"

        ''' <summary>
        ''' The source shared by the DCA010 tests: <c>Flags</c> enumerations written with decimal and hexadecimal literals.
        ''' </summary>
        Private Const FlagsSource As String = "
<System.Flags>
Public Enum Access
    None = 0
    Read = 1
    Write = 2
    Execute = &H4
    Delete = 8
    ReadWrite = 3
    Shifted = 1 << 4
    Combined = Read Or Write
End Enum
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA010_FlagsEnumBitShiftTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA010 does not run unless it is enabled.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function IsDisabledByDefault() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, DCA010_FlagsEnumBitShiftTests.FlagsSource).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA010 does not run when it is explicitly disabled.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function CanBeExplicitlyDisabled() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, DCA010_FlagsEnumBitShiftTests.FlagsSource, editorConfig:="devcase_analyzers.dca010_enabled = false").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA010, when enabled, reports single-bit literals of <c>Flags</c> enumerations.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SingleBitLiterals_WhenEnabled() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
<System.Flags>
Public Enum Access
    None = 0
    Read = [|1|]
    Write = [|2|]
    Execute = [|&H4|]
    Delete = [|8|]
    ReadWrite = 3
    Shifted = 1 << 4
    Combined = Read Or Write
End Enum
Public Enum NotFlags
    A = 1
    B = 2
End Enum
", editorConfig:=EnabledConfig).ConfigureAwait(True)
            Assert.Contains("'1 << 3'", diagnostics(3).GetMessage(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA010 writes bit-shift expressions that respect the underlying type width.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ConvertsLiterals_RespectingUnderlyingWidth() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
<System.Flags>
Public Enum Access
    None = 0
    Read = 1 ' first bit
    Write = 2
    Execute = &H4
    ReadWrite = 3
End Enum
<FlagsAttribute>
Public Enum Wide As Long
    Low = 1
    High = 1099511627776
End Enum
<System.FlagsAttribute()>
Public Enum Unsigned As UInteger
    Top = &H80000000UI
End Enum
<System.Flags>
Public Enum Signed As Integer
    Sign = &H80000000
End Enum
<System.Flags>
Public Enum Tiny As Byte
    Last = 128
End Enum
", "
<System.Flags>
Public Enum Access
    None = 0
    Read = 1 << 0 ' first bit
    Write = 1 << 1
    Execute = 1 << 2
    ReadWrite = 3
End Enum
<FlagsAttribute>
Public Enum Wide As Long
    Low = 1L << 0
    High = 1L << 40
End Enum
<System.FlagsAttribute()>
Public Enum Unsigned As UInteger
    Top = 1UI << 31
End Enum
<System.Flags>
Public Enum Signed As Integer
    Sign = 1 << 31
End Enum
<System.Flags>
Public Enum Tiny As Byte
    Last = 1 << 7
End Enum
", editorConfig:=EnabledConfig).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
