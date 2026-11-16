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

#Region " DCA035_DoEventsInLoopTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA035 (Application.DoEvents inside a loop).
    ''' </summary>
    Public NotInheritable Class DCA035_DoEventsInLoopTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DoEventsInLoopAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA035_DoEventsInLoopTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA035 reports DoEvents inside For, For Each, While and Do loops of the same method, and skips calls outside loops or in lambdas declared in a loop.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_DoEventsInsideLoops() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"
Imports System.Windows.Forms
Public Class C
    Public Sub M(items As String())
        Application.DoEvents()
        For index As Integer = 0 To 9
            [|Application.DoEvents()|]
        Next index
        For Each item As String In items
            If item.Length > 0 Then
                [|Application.DoEvents()|]
            End If
        Next item
        While items.Length > 100
            [|Application.DoEvents()|]
        End While
        Do
            [|Application.DoEvents()|]
        Loop Until items.Length < 100
        For Each item As String In items
            Dim refresh As Action = Sub() Application.DoEvents()
            refresh()
        Next item
    End Sub
End Class
{DCA028_StaThreadTests.WindowsFormsStub}").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
