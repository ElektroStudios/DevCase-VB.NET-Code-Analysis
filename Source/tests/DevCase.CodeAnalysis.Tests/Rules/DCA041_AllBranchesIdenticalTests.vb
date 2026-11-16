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

#Region " DCA041_AllBranchesIdenticalTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA041 (All branches of a conditional structure are identical).
    ''' </summary>
    Public NotInheritable Class DCA041_AllBranchesIdenticalTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New AllBranchesIdenticalAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA041_AllBranchesIdenticalTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA041 reports If blocks, single-line If statements, Select Case statements and If() expressions whose branches are all identical.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_IdenticalBranches() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(value As Integer) As Integer
        Dim result As Integer
        [|If value > 0 Then|]
            result = 1
        ElseIf value < 0 Then
            result = 1
        Else
            result = 1
        End If
        [|If value = 3 Then result = 2 Else result = 2|]
        [|Select Case value|]
            Case 1
                result += 1
            Case Else
                result += 1
        End Select
        Return [|If(value > 5, result, result)|]
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA041 skips structures without an Else branch, empty branches and different branches.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_DifferentOrWithoutElse() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(value As Integer) As Integer
        Dim result As Integer
        If value > 0 Then
            result = 1
        ElseIf value < 0 Then
            result = 1
        End If
        If value > 10 Then
        Else
        End If
        Select Case value
            Case 1
                result += 1
            Case Else
                result -= 1
        End Select
        Return If(value > 5, result, -result)
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
