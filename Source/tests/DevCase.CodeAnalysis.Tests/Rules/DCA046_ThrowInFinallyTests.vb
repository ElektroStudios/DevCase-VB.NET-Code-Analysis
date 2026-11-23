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

#Region " DCA046_ThrowInFinallyTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA046 (Exception thrown in a Finally block).
    ''' </summary>
    Public NotInheritable Class DCA046_ThrowInFinallyTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThrowInFinallyAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA046_ThrowInFinallyTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA046 reports exceptions that leave a Finally block, and skips exceptions handled inside it and lambdas.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ThrowInFinally() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(value As Integer)
        Try
            Console.WriteLine(value)
        Finally
            If value < 0 Then
                [|Throw New InvalidOperationException()|]
            End If
            Try
                Throw New ArgumentException()
            Catch ex As ArgumentException
                [|Throw|]
            End Try
            Dim fail As Action = Sub() Throw New InvalidOperationException()
        End Try
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA046 does not run in a modern .NET project, where the built-in rule CA2219 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M()
        Try
        Finally
            Throw New InvalidOperationException()
        End Try
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
