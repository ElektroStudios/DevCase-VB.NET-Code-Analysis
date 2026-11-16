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

#Region " DCA043_IdenticalOperandsTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA043 (Identical expressions on both sides of a binary operator).
    ''' </summary>
    Public NotInheritable Class DCA043_IdenticalOperandsTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New IdenticalOperandsAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA043_IdenticalOperandsTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA043 reports comparisons and operators with identical operands, and skips NaN tests, invocations and literals.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_IdenticalOperands() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly random As New Random()
    Public Function M(a As Integer, b As Integer, ratio As Double, item As Object) As Boolean
        Dim same As Boolean = [|a = a|] OrElse [|b <> b|] OrElse [|item Is item|]
        Dim zero As Integer = [|a - a|] + [|b Mod b|]
        Dim both As Boolean = [|same AndAlso same|]
        Dim isNaN As Boolean = ratio <> ratio
        Dim dice As Boolean = random.Next() = random.Next()
        Dim always As Boolean = 1 = 1
        Return same OrElse both OrElse isNaN OrElse dice OrElse always OrElse zero > 0 OrElse a * a > b + b
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
