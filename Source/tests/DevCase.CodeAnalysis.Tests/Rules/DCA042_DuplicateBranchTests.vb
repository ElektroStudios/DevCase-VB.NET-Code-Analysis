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

#Region " DCA042_DuplicateBranchTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA042 (Two branches of a conditional structure are identical).
    ''' </summary>
    Public NotInheritable Class DCA042_DuplicateBranchTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DuplicateBranchAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA042_DuplicateBranchTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA042 reports If branches with several identical statements and identical Case branches, and skips single-statement If branches.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_DuplicatedBranches() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(value As Integer) As Integer
        Dim result As Integer
        If value > 0 Then
            result = 1
            result *= 2
        ElseIf value < -10 Then
            result = 3
        [|ElseIf value < 0 Then|]
            result = 1
            result *= 2
        End If
        If value = 1 Then
            Return 0
        ElseIf value = 2 Then
            Return 1
        Else
            Return 0
        End If
    End Function
    Public Function N(value As Integer) As String
        Select Case value
            Case 1
                Return ""one""
            Case 2
                Return ""two""
            [|Case 3|]
                Return ""one""
        End Select
        Return String.Empty
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
