#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA022_IndexerTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA022 (LINQ First/Last/ElementAt instead of the indexer) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA022_IndexerTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New IndexerAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New IndexerCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA022_IndexerTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA022 reports indexable <c>IList(Of T)</c> collections only.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_IListTypesOnly() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(items As List(Of Integer), numbers As Integer(), list As IList(Of Integer), sequence As IEnumerable(Of Integer), readOnlyList As IReadOnlyList(Of Integer))
        Dim a As Integer = [|items.First()|]
        Dim b As Integer = [|items.Last()|]
        Dim c As Integer = [|numbers.ElementAt(2)|]
        Dim d As Integer = [|list.Last()|]
        Dim e As Integer = items.First(Function(x As Integer) x > 1)
        Dim f As Integer = items.FirstOrDefault()
        Dim g As Integer = sequence.First()
        Dim h As Integer = readOnlyList.First()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA022 uses the indexer with <c>Count</c> or <c>Length</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesIndexerCountAndLength() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly Items As New List(Of Integer)
    Public Function M(numbers As Integer(), list As IList(Of Integer)) As Integer
        Return Items.First() + Me.Items.Last() + numbers.Last() + numbers.ElementAt(2) + list.Last()
    End Function
End Class
", "
Public Class C
    Private ReadOnly Items As New List(Of Integer)
    Public Function M(numbers As Integer(), list As IList(Of Integer)) As Integer
        Return Items(0) + Me.Items(Me.Items.Count - 1) + numbers(numbers.Length - 1) + numbers(2) + list(list.Count - 1)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA022 adds parentheses to a method receiver, and offers no fix for <c>Last</c> on a call.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_MethodReceiver_AddsParentheses_AndLastOnCallHasNoFix() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private Function GetItems() As List(Of Integer)
        Return New List(Of Integer) From {1, 2}
    End Function
    Public Function M() As Integer
        Return GetItems.First() + GetItems().Last()
    End Function
End Class
", "
Public Class C
    Private Function GetItems() As List(Of Integer)
        Return New List(Of Integer) From {1, 2}
    End Function
    Public Function M() As Integer
        Return GetItems()(0) + GetItems().Last()
    End Function
End Class
", allowRemainingDiagnostics:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
