#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " DCA040_DuplicateConditionTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA040 (Duplicated condition in If/ElseIf or Select Case).
    ''' </summary>
    Public NotInheritable Class DCA040_DuplicateConditionTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DuplicateConditionAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA040_DuplicateConditionTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA040 reports repeated If/ElseIf conditions and constant Case values, and skips conditions with invocations.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_DuplicatedConditions() As Task
            Dim diagnostics As Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(value As Integer, reader As IO.TextReader) As String
        If value = 1 Then
            Return ""one""
        ElseIf value = 2 Then
            Return ""two""
        ElseIf [|value = 1|] Then
            Return ""again""
        ElseIf reader.Peek() > 0 Then
            Return ""data""
        ElseIf reader.Peek() > 0 Then
            Return ""more""
        End If
        Select Case value
            Case 1, 2
                Return ""small""
            Case 3, [|1|]
                Return ""three""
            Case 4 To 9
                Return ""medium""
        End Select
        Return String.Empty
    End Function
End Class
").ConfigureAwait(True)
            Assert.Contains("line 4", diagnostics(0).GetMessage(Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
            Assert.Contains("line 16", diagnostics(1).GetMessage(Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal)
        End Function

#End Region

    End Class

End Namespace

#End Region
