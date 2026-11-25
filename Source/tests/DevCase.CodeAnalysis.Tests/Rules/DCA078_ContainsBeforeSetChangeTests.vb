#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA078_ContainsBeforeSetChangeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA078 (Contains before Add or Remove of a set, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA078_ContainsBeforeSetChangeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ContainsBeforeSetChangeAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

        ''' <summary>
        ''' The expected messages of <see cref="Message_NamesTheMethod"/>.
        ''' </summary>
        Private Shared ReadOnly Message_NamesTheMethodExpected As String() = {"Do not guard 'Remove' with 'Contains' because 'Remove' already checks whether the item exists"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA078_ContainsBeforeSetChangeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA078 reports <c>Add</c> guarded by <c>Not Contains</c> and <c>Remove</c> guarded by <c>Contains</c> on
        ''' sets, and ignores lists and mismatched pairs.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_AddOrRemoveGuardedByContains() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(items As HashSet(Of String), sorted As SortedSet(Of Integer), list As List(Of String), item As String)
        If Not [|items.Contains(item)|] Then
            items.Add(item)
        End If
        If [|items.Contains(item)|] Then items.Remove(item)
        If Not [|items.Contains(item)|] Then
            items.Add(item)
            Console.WriteLine(item)
        End If
        If Not ([|sorted.Contains(5)|]) Then sorted.Add(5)
        If Not list.Contains(item) Then list.Add(item)
        If items.Contains(item) Then items.Add(item)
        If Not items.Contains(item) Then items.Remove(item)
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA078 does not run in a modern .NET project, where the built-in rule CA1868 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(items As HashSet(Of String), sorted As SortedSet(Of Integer), list As List(Of String), item As String)
        If Not items.Contains(item) Then
            items.Add(item)
        End If
        If items.Contains(item) Then items.Remove(item)
        If Not items.Contains(item) Then
            items.Add(item)
            Console.WriteLine(item)
        End If
        If Not (sorted.Contains(5)) Then sorted.Add(5)
        If Not list.Contains(item) Then list.Add(item)
        If items.Contains(item) Then items.Add(item)
        If Not items.Contains(item) Then items.Remove(item)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA078 removes the check.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallsTheMethodDirectly() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run(items As HashSet(Of String), sorted As SortedSet(Of Integer), list As List(Of String), item As String)
        If Not [|items.Contains(item)|] Then
            items.Add(item)
        End If
        If [|items.Contains(item)|] Then items.Remove(item)
        If Not [|items.Contains(item)|] Then
            items.Add(item)
            Console.WriteLine(item)
        End If
        If Not ([|sorted.Contains(5)|]) Then sorted.Add(5)
        If Not list.Contains(item) Then list.Add(item)
        If items.Contains(item) Then items.Add(item)
        If Not items.Contains(item) Then items.Remove(item)
    End Sub
End Class
", "
Public Class C
    Public Sub Run(items As HashSet(Of String), sorted As SortedSet(Of Integer), list As List(Of String), item As String)
        items.Add(item)
        items.Remove(item)
        If items.Add(item) Then
            Console.WriteLine(item)
        End If
        sorted.Add(5)
        If Not list.Contains(item) Then list.Add(item)
        If items.Contains(item) Then items.Add(item)
        If Not items.Contains(item) Then items.Remove(item)
    End Sub
End Class
",
                                                      equivalenceKey:="DCA078.RemoveCheck", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA078.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_NamesTheMethod() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(items As HashSet(Of String), item As String)
        If [|items.Contains(item)|] Then items.Remove(item)
    End Sub
End Class
",
                                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA078_ContainsBeforeSetChangeTests.Message_NamesTheMethodExpected, diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
