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

#Region " DCA039_BeginInvokeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA039 (Delegate BeginInvoke without EndInvoke).
    ''' </summary>
    Public NotInheritable Class DCA039_BeginInvokeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New BeginInvokeAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA039_BeginInvokeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA039 reports delegate BeginInvoke calls without EndInvoke, and accepts EndInvoke in the method, in a lambda callback or in a method callback.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_UnpairedBeginInvoke() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly work As Func(Of Integer) = Function() 42
    Public Sub FireAndForget()
        [|work.BeginInvoke(Nothing, Nothing)|]
    End Sub
    Public Sub LambdaWithoutEnd()
        [|work.BeginInvoke(Sub(r As IAsyncResult) Console.WriteLine(""done""), Nothing)|]
    End Sub
    Public Function Paired() As Integer
        Dim result As IAsyncResult = work.BeginInvoke(Nothing, Nothing)
        Return work.EndInvoke(result)
    End Function
    Public Sub LambdaCallback()
        work.BeginInvoke(Sub(r As IAsyncResult) Console.WriteLine(work.EndInvoke(r)), Nothing)
    End Sub
    Public Sub MethodCallback()
        work.BeginInvoke(AddressOf Me.OnCompleted, Nothing)
    End Sub
    Public Sub UnknownCallback(callback As AsyncCallback)
        work.BeginInvoke(callback, Nothing)
    End Sub
    Private Sub OnCompleted(result As IAsyncResult)
        Console.WriteLine(work.EndInvoke(result))
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
