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
        ''' Verifies that rule DCA022 reports indexable <c>IList(Of T)</c> collections in a modern .NET project, where the 
        ''' <c>IReadOnlyList(Of T)</c> types that do not implement <c>IList(Of T)</c> are left to the built-in rule CA1826.
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

        ''' <summary>
        ''' Verifies that rule DCA022 reports <c>IReadOnlyList(Of T)</c> types and strings in a .NET Framework project, where 
        ''' CA1826 does not exist.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ReadOnlyListsAndStrings_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(readOnlyList As IReadOnlyList(Of Integer), text As String, sequence As IEnumerable(Of Integer), custom As Window)
        Dim a As Integer = [|readOnlyList.First()|]
        Dim b As Integer = [|readOnlyList.Last()|]
        Dim c As Integer = [|readOnlyList.ElementAt(1)|]
        Dim d As Char = [|text.First()|]
        Dim e As Char = [|text.Last()|]
        Dim f As Integer = [|custom.First()|]
        Dim g As Integer = readOnlyList.FirstOrDefault()
        Dim h As Integer = sequence.Last()
    End Sub
End Class

Public Class Window
    Implements IReadOnlyList(Of Integer)

    Default Public ReadOnly Property Item(index As Integer) As Integer Implements IReadOnlyList(Of Integer).Item
        Get
            Return index
        End Get
    End Property

    Public ReadOnly Property Count As Integer Implements IReadOnlyCollection(Of Integer).Count
        Get
            Return 1
        End Get
    End Property

    Public Function GetEnumerator() As IEnumerator(Of Integer) Implements IEnumerable(Of Integer).GetEnumerator
        Return Enumerable.Range(0, 1).GetEnumerator()
    End Function

    Private Function GetEnumeratorCore() As IEnumerator Implements IEnumerable.GetEnumerator
        Return Me.GetEnumerator()
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA022 reports strings in a modern .NET project (CA1826 does not cover them), and leaves 
        ''' <c>IReadOnlyList(Of T)</c> to CA1826.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_Strings_ButNotReadOnlyLists_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(readOnlyList As IReadOnlyList(Of Integer), text As String)
        Dim a As Integer = readOnlyList.First()
        Dim d As Char = [|text.Last()|]
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA022 indexes <c>IReadOnlyList(Of T)</c> types with <c>Count</c> and strings with 
        ''' <c>Length</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReadOnlyListsAndStrings() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(readOnlyList As IReadOnlyList(Of Integer), text As String) As String
        Return $""{readOnlyList.First()} {readOnlyList.Last()} {text.First()} {text.Last()} {readOnlyList.ElementAt(2)}""
    End Function
End Class
", "
Public Class C
    Public Function M(readOnlyList As IReadOnlyList(Of Integer), text As String) As String
        Return $""{readOnlyList(0)} {readOnlyList(readOnlyList.Count - 1)} {text(0)} {text(text.Length - 1)} {readOnlyList(2)}""
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA022, which matches CA1826.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_MatchesCA1826() As Task
            Dim diagnostics As Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(items As List(Of Integer)) As Integer
        Return [|items.First()|]
    End Function
End Class
").ConfigureAwait(True)
            Assert.Equal("Do not use Enumerable methods on indexable collections; instead use the collection directly",
                         diagnostics.Single().GetMessage(Globalization.CultureInfo.InvariantCulture))
        End Function

#End Region

    End Class

End Namespace

#End Region
