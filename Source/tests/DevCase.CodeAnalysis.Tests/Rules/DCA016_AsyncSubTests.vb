#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA016 (Async Sub that is not an event handler) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA016_AsyncSubTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New AsyncSubAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New AsyncSubCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA016_AsyncSubTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA016 reports <c>Async Sub</c> methods that are not event handlers.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_AsyncSubsThatAreNotEventHandlers() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading.Tasks
Public Interface IWorker
    Sub Work()
End Interface
Public MustInherit Class Base
    Public MustOverride Sub OnTick()
End Class
Public Class C : Inherits Base : Implements IWorker
    Public Event Changed As EventHandler
    Public Async Sub [|Refresh|]()
        Await Task.Delay(1)
    End Sub
    Private Async Sub [|Save|](path As String)
        Await Task.Delay(1)
    End Sub
    Private Async Sub OnChanged(sender As Object, e As EventArgs) Handles Me.Changed
        Await Task.Delay(1)
    End Sub
    Private Async Sub Button_Click(sender As Object, e As EventArgs)
        Await Task.Delay(1)
    End Sub
    Public Overrides Async Sub OnTick()
        Await Task.Delay(1)
    End Sub
    Public Async Sub Work() Implements IWorker.Work
        Await Task.Delay(1)
    End Sub
    Public Sub NotAsync()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA016 converts the method into an <c>Async Function</c> that returns a <see cref="Task"/>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ConvertsToTaskFunction() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading.Tasks
Public Class C
    Public Async Sub Refresh(force As Boolean)
        If Not force Then
            Exit Sub
        End If
        Dim action As Action = Sub()
                                   Exit Sub
                               End Sub
        Await Task.Delay(1)
    End Sub
    Private Async Sub Load
        Await Task.Delay(1)
    End Sub
End Class
", "
Imports System.Threading.Tasks
Public Class C
    Public Async Function Refresh(force As Boolean) As Task
        If Not force Then
            Exit Function
        End If
        Dim action As Action = Sub()
                                   Exit Sub
                               End Sub
        Await Task.Delay(1)
    End Function
    Private Async Function Load() As Task
        Await Task.Delay(1)
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace
