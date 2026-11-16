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

#Region " DCA047_ThrowInFinalizeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA047 (Exception thrown in Finalize).
    ''' </summary>
    Public NotInheritable Class DCA047_ThrowInFinalizeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThrowInFinalizeAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA047_ThrowInFinalizeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA047 reports exceptions that leave Finalize, and skips handled exceptions and other methods.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ThrowInFinalize() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private handle As IntPtr
    Protected Overrides Sub Finalize()
        Try
            Throw New InvalidOperationException()
        Catch ex As InvalidOperationException
            Console.WriteLine(ex.Message)
        End Try
        If Me.handle = IntPtr.Zero Then
            [|Throw New InvalidOperationException()|]
        End If
        MyBase.Finalize()
    End Sub
    Public Sub Close()
        Throw New InvalidOperationException()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
