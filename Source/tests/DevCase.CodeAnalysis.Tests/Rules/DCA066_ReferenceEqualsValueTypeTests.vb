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

#Region " DCA066_ReferenceEqualsValueTypeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA066 (value type in ReferenceEquals, .NET Framework).
    ''' </summary>
    Public NotInheritable Class DCA066_ReferenceEqualsValueTypeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ReferenceEqualsValueTypeAnalyzer()

        ''' <summary>
        ''' The expected messages of <see cref="Message_NamesTheValueType"/>.
        ''' </summary>
        Private Shared ReadOnly Message_NamesTheValueTypeExpected As String() = {"Do not pass value type 'Integer' to 'ReferenceEquals' because each call boxes it into a new object, causing the reference comparison to always return 'False'"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA066_ReferenceEqualsValueTypeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA066 reports each value-type argument of <c>ReferenceEquals</c> (nullable values included), and
        ''' ignores reference types.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_EachValueTypeArgument() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Same(first As Integer, second As Integer, text As String, other As Object, moment As Date?) As Boolean
        Dim a As Boolean = Object.ReferenceEquals([|first|], [|second|])
        Dim b As Boolean = ReferenceEquals(text, other)
        Dim c As Boolean = ReferenceEquals([|moment|], Nothing)
        Return a OrElse b OrElse c
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA066 does not run in a modern .NET project, where the built-in rule CA2013 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Same(first As Integer, second As Integer, text As String, other As Object, moment As Date?) As Boolean
        Dim a As Boolean = Object.ReferenceEquals(first, second)
        Dim b As Boolean = ReferenceEquals(text, other)
        Dim c As Boolean = ReferenceEquals(moment, Nothing)
        Return a OrElse b OrElse c
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA066.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_NamesTheValueType() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Same(first As Integer, text As String) As Boolean
        Return ReferenceEquals([|first|], text)
    End Function
End Class
",
                                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA066_ReferenceEqualsValueTypeTests.Message_NamesTheValueTypeExpected, diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
