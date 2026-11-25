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

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " DCA069_HasFlagEnumTypeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA069 (HasFlag with another enum type, .NET Framework).
    ''' </summary>
    Public NotInheritable Class DCA069_HasFlagEnumTypeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New HasFlagEnumTypeAnalyzer()

        ''' <summary>
        ''' The expected messages of <see cref="Message_NamesBothTypes"/>.
        ''' </summary>
        Private Shared ReadOnly Message_NamesBothTypesExpected As String() = {"Pass a 'FileAttributes' value to 'HasFlag' instead of 'ConsoleModifiers' because a different enum type throws 'ArgumentException'"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA069_HasFlagEnumTypeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA069 reports <c>HasFlag</c> arguments of another enumeration type, and ignores the same type and
        ''' arguments typed as <see cref="[Enum]"/>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ArgumentsOfAnotherEnumType() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO

Public Class C
    Public Function Check(attributes As FileAttributes, modifiers As ConsoleModifiers, value As [Enum]) As Boolean
        Dim a As Boolean = attributes.HasFlag([|ConsoleModifiers.Alt|])
        Dim b As Boolean = attributes.HasFlag(FileAttributes.Hidden)
        Dim c As Boolean = modifiers.HasFlag([|attributes|])
        Dim d As Boolean = attributes.HasFlag(value)
        Return a OrElse b OrElse c OrElse d
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA069 does not run in a modern .NET project, where the built-in rule CA2248 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO

Public Class C
    Public Function Check(attributes As FileAttributes, modifiers As ConsoleModifiers, value As [Enum]) As Boolean
        Dim a As Boolean = attributes.HasFlag(ConsoleModifiers.Alt)
        Dim b As Boolean = attributes.HasFlag(FileAttributes.Hidden)
        Dim c As Boolean = modifiers.HasFlag(attributes)
        Dim d As Boolean = attributes.HasFlag(value)
        Return a OrElse b OrElse c OrElse d
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA069.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_NamesBothTypes() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO

Public Class C
    Public Function Check(attributes As FileAttributes) As Boolean
        Return attributes.HasFlag([|ConsoleModifiers.Alt|])
    End Function
End Class
",
                                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA069_HasFlagEnumTypeTests.Message_NamesBothTypesExpected, diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
