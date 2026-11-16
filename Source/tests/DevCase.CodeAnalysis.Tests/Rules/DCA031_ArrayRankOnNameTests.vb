#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA031_ArrayRankOnNameTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA031 (Array designator on the variable name) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA031_ArrayRankOnNameTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ArrayRankOnNameAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ArrayRankOnNameCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA031_ArrayRankOnNameTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA031 reports array designators on names of variables, fields and parameters, and skips bounds and shared As clauses.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_DesignatorsOnNames() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private [|items()|] As String
    Public Sub M([|values()|] As Integer, ParamArray [|args()|] As Object)
        Dim [|grid(,)|] As Integer = New Integer(1, 1) {}
        Dim sized(9) As Integer
        Dim a(), b As Integer
        Dim plain As Integer() = values
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA031 moves the designator to the type, also for generic types and multidimensional arrays.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_MovesTheDesignatorToTheType() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Collections.Generic
Public Class C
    Private items() As List(Of String)
    Public Sub M(values() As Integer)
        Dim grid(,) As Integer = New Integer(1, 1) {}
        Dim copy() As Integer = values
    End Sub
End Class
", "
Imports System.Collections.Generic
Public Class C
    Private items As List(Of String)()
    Public Sub M(values As Integer())
        Dim grid As Integer(,) = New Integer(1, 1) {}
        Dim copy As Integer() = values
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
