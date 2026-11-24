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

#Region " DCA056_AnyInsteadOfEmptinessCheckTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA056 (Enumerable.Any() instead of IsEmpty, Count or Length, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA056_AnyInsteadOfEmptinessCheckTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New AnyInsteadOfEmptinessCheckAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New AnyInsteadOfEmptinessCheckCodeFixProvider()

        ''' <summary>
        ''' A source with calls to <c>Any()</c> on collections with each property, in both call forms.
        ''' </summary>
        Private Const ReportedSource As String = "
Imports System.Collections.Concurrent

Public Class C
    Public Function Run(queue As Queue(Of Integer), list As List(Of String), numbers As Integer(), text As String,
                        pending As ConcurrentQueue(Of Integer), map As Dictionary(Of String, Integer), items As IList(Of String),
                        readOnlyItems As IReadOnlyCollection(Of String)) As Boolean
        While [|queue.Any()|]
            queue.Dequeue()
        End While
        If Not [|list.Any|] Then
            Return False
        End If
        Return [|numbers.Any()|] AndAlso [|text.Any()|] AndAlso [|pending.Any()|] AndAlso [|Enumerable.Any(map)|] AndAlso
               [|items.Any()|] AndAlso [|readOnlyItems.Any()|] AndAlso [|map.Keys.Any()|]
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA056_AnyInsteadOfEmptinessCheckTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA056 reports <c>Any()</c> on collections with an <c>IsEmpty</c>, <c>Length</c> or <c>Count</c> 
        ''' property in a .NET Framework project, in both call forms.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_AnyOnCollections_OnNetFramework() As Task
            Dim diagnostics As Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Diagnostic) =
                Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ReportedSource, targetNetFramework:=True).ConfigureAwait(True)

            Dim messages As String() = diagnostics.OrderBy(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.Location.SourceSpan.Start).
                Select(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).Distinct(StringComparer.Ordinal).ToArray()
            Assert.Contains("Use the 'Count' property instead of 'Enumerable.Any()' to avoid enumerator boxing and allocation", messages, StringComparer.Ordinal)
            Assert.Contains("Use the 'Length' property instead of 'Enumerable.Any()' to avoid enumerator boxing and allocation", messages, StringComparer.Ordinal)
            Assert.Contains("Use the 'Length' property instead of 'Enumerable.Any()' to avoid enumerator boxing and allocation", messages, StringComparer.Ordinal)
            Assert.Contains("Use the 'IsEmpty' property instead of 'Enumerable.Any()' to avoid enumerator boxing and allocation", messages, StringComparer.Ordinal)
            Assert.Contains("Use the 'Count' property instead of 'Enumerable.Any()' to avoid enumerator boxing and allocation", messages, StringComparer.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA056 does not run in a modern .NET project, where the built-in rule CA1860 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer,
                ReportedSource.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal)).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA056 does not report sequences without a property, calls with a predicate, null-conditional calls, 
        ''' and interfaces whose <c>Count</c> is ambiguous.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_SequencesPredicatesAndAmbiguousCounts() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Interface IBoth(Of T) : Inherits ICollection(Of T), IReadOnlyCollection(Of T)
End Interface
Public Class C
    Public Function Run(sequence As IEnumerable(Of Integer), list As List(Of Integer), both As IBoth(Of Integer)) As Boolean
        Dim conditional As Boolean? = list?.Any()
        Return sequence.Any() AndAlso list.Any(Function(value As Integer) value > 0) AndAlso both.Any() AndAlso conditional.Value
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA056 writes the right check in every context: negated calls, the <c>IsEmpty</c> 
        ''' property, the static form, and the parentheses that a member access or a comparison requires.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_WritesTheCheckInEveryContext() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Imports System.Collections.Concurrent

Public Class C
    Public Function Run(queue As Queue(Of Integer), list As List(Of String), numbers As Integer(), pending As ConcurrentQueue(Of Integer), flag As Boolean) As String
        While queue.Any()
            queue.Dequeue()
        End While
        If Not list.Any() Then
            Return String.Empty
        End If
        Dim hasNumbers As Boolean = numbers.Any()
        Dim hasPending As Boolean = pending.Any()
        Dim nothingPending As Boolean = Not pending.Any()
        Dim noList As Boolean = Not (list.Any())
        Dim both As Boolean = flag AndAlso Enumerable.Any(list)
        Dim same As Boolean = list.Any() = flag
        Dim samePending As Boolean = pending.Any() = flag
        Return list.Any().ToString()
    End Function
End Class
", NameOf(AnyInsteadOfEmptinessCheckCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Imports System.Collections.Concurrent

Public Class C
    Public Function Run(queue As Queue(Of Integer), list As List(Of String), numbers As Integer(), pending As ConcurrentQueue(Of Integer), flag As Boolean) As String
        While queue.Count <> 0
            queue.Dequeue()
        End While
        If list.Count = 0 Then
            Return String.Empty
        End If
        Dim hasNumbers As Boolean = numbers.Length <> 0
        Dim hasPending As Boolean = Not pending.IsEmpty
        Dim nothingPending As Boolean = pending.IsEmpty
        Dim noList As Boolean = list.Count = 0
        Dim both As Boolean = flag AndAlso list.Count <> 0
        Dim same As Boolean = (list.Count <> 0) = flag
        Dim samePending As Boolean = (Not pending.IsEmpty) = flag
        Return (list.Count <> 0).ToString()
    End Function
End Class
", fixedText)
        End Function

#End Region

    End Class

End Namespace

#End Region
