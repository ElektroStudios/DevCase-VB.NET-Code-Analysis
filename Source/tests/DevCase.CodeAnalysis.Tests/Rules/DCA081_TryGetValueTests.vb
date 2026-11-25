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
Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA081_TryGetValueTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA081 (ContainsKey and the indexer, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA081_TryGetValueTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New TryGetValueAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA081_TryGetValueTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA081 reports <c>ContainsKey</c> followed by reads of the indexer with the same key, and ignores
        ''' writes, other members, assigned keys, blocks without reads and reads inside lambdas.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_IndexerReadsAfterContainsKey() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Lookup(key As String) As Integer
        If [|map.ContainsKey(key)|] Then
            Return map(key)
        End If
        Return 0
    End Function

    Public Sub Print(key As String)
        If [|Me.map.ContainsKey(key)|] Then
            Console.WriteLine(Me.map(key))
            Console.WriteLine(map(key) + 1)
        End If
        If [|map.ContainsKey(""total"")|] Then Console.WriteLine(map(""total""))
    End Sub

    Public Sub NotReported(key As String)
        If map.ContainsKey(key) Then map(key) = 1
        If map.ContainsKey(key) Then map.Remove(key)
        If map.ContainsKey(key) Then
            key = ""other""
            Console.WriteLine(map(key))
        End If
        If map.ContainsKey(key) Then Console.WriteLine(key)
        If map.ContainsKey(key) Then
            Dim read As Func(Of Integer) = Function() map(key)
        End If
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA081 does not run in a modern .NET project, where the built-in rule CA1854 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Lookup(key As String) As Integer
        If map.ContainsKey(key) Then
            Return map(key)
        End If
        Return 0
    End Function

    Public Sub Print(key As String)
        If Me.map.ContainsKey(key) Then
            Console.WriteLine(Me.map(key))
            Console.WriteLine(map(key) + 1)
        End If
        If map.ContainsKey(""total"") Then Console.WriteLine(map(""total""))
    End Sub

    Public Sub NotReported(key As String)
        If map.ContainsKey(key) Then map(key) = 1
        If map.ContainsKey(key) Then map.Remove(key)
        If map.ContainsKey(key) Then
            key = ""other""
            Console.WriteLine(map(key))
        End If
        If map.ContainsKey(key) Then Console.WriteLine(key)
        If map.ContainsKey(key) Then
            Dim read As Func(Of Integer) = Function() map(key)
        End If
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA081 declares the value, calls <c>TryGetValue</c> and replaces every read.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_DeclaresTheValueAndUsesTryGetValue() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Lookup(key As String) As Integer
        If [|map.ContainsKey(key)|] Then
            Return map(key)
        End If
        Return 0
    End Function

    Public Sub Print(key As String)
        If [|Me.map.ContainsKey(key)|] Then
            Console.WriteLine(Me.map(key))
            Console.WriteLine(map(key) + 1)
        End If
        If [|map.ContainsKey(""total"")|] Then Console.WriteLine(map(""total""))
    End Sub

    Public Sub NotReported(key As String)
        If map.ContainsKey(key) Then map(key) = 1
        If map.ContainsKey(key) Then map.Remove(key)
        If map.ContainsKey(key) Then
            key = ""other""
            Console.WriteLine(map(key))
        End If
        If map.ContainsKey(key) Then Console.WriteLine(key)
        If map.ContainsKey(key) Then
            Dim read As Func(Of Integer) = Function() map(key)
        End If
    End Sub
End Class
", "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Lookup(key As String) As Integer
        Dim value As Integer = Nothing
        If map.TryGetValue(key, value) Then
            Return value
        End If
        Return 0
    End Function

    Public Sub Print(key As String)
        Dim value As Integer = Nothing
        If Me.map.TryGetValue(key, value) Then
            Console.WriteLine(value)
            Console.WriteLine(value + 1)
        End If
        Dim value1 As Integer = Nothing
        If map.TryGetValue(""total"", value1) Then Console.WriteLine(value1)
    End Sub

    Public Sub NotReported(key As String)
        If map.ContainsKey(key) Then map(key) = 1
        If map.ContainsKey(key) Then map.Remove(key)
        If map.ContainsKey(key) Then
            key = ""other""
            Console.WriteLine(map(key))
        End If
        If map.ContainsKey(key) Then Console.WriteLine(key)
        If map.ContainsKey(key) Then
            Dim read As Func(Of Integer) = Function() map(key)
        End If
    End Sub
End Class
",
                                                      equivalenceKey:="DCA081.TryGetValue", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA081 declares a different variable for each statement of a method.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_DeclaresADifferentValuePerStatement() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Lookup(key As String) As Integer
        If map.ContainsKey(key) Then
            Return map(key)
        End If
        Return 0
    End Function

    Public Sub Print(key As String)
        If Me.map.ContainsKey(key) Then
            Console.WriteLine(Me.map(key))
            Console.WriteLine(map(key) + 1)
        End If
        If map.ContainsKey(""total"") Then Console.WriteLine(map(""total""))
    End Sub

    Public Sub NotReported(key As String)
        If map.ContainsKey(key) Then map(key) = 1
        If map.ContainsKey(key) Then map.Remove(key)
        If map.ContainsKey(key) Then
            key = ""other""
            Console.WriteLine(map(key))
        End If
        If map.ContainsKey(key) Then Console.WriteLine(key)
        If map.ContainsKey(key) Then
            Dim read As Func(Of Integer) = Function() map(key)
        End If
    End Sub
End Class
",
                                                                                       "DCA081.TryGetValue", targetNetFramework:=True).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual("
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Lookup(key As String) As Integer
        Dim value As Integer = Nothing
        If map.TryGetValue(key, value) Then
            Return value
        End If
        Return 0
    End Function

    Public Sub Print(key As String)
        Dim value As Integer = Nothing
        If Me.map.TryGetValue(key, value) Then
            Console.WriteLine(value)
            Console.WriteLine(value + 1)
        End If
        Dim value1 As Integer = Nothing
        If map.TryGetValue(""total"", value1) Then Console.WriteLine(value1)
    End Sub

    Public Sub NotReported(key As String)
        If map.ContainsKey(key) Then map(key) = 1
        If map.ContainsKey(key) Then map.Remove(key)
        If map.ContainsKey(key) Then
            key = ""other""
            Console.WriteLine(map(key))
        End If
        If map.ContainsKey(key) Then Console.WriteLine(key)
        If map.ContainsKey(key) Then
            Dim read As Func(Of Integer) = Function() map(key)
        End If
    End Sub
End Class
", fixedText)
            Dim project As Microsoft.CodeAnalysis.Project = AnalyzerTestHost.CreateProject(fixedText, targetNetFramework:=True)
            Await AnalyzerTestHost.AssertCompilesAsync(project, "fixed source").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA081 skips a name that the method already uses.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AvoidsNamesInUse() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Lookup(map As Dictionary(Of String, String), key As String, value As String) As String
        If [|map.ContainsKey(key)|] Then
            Return map(key) & value
        End If
        Return value
    End Function
End Class
", "
Public Class C
    Public Function Lookup(map As Dictionary(Of String, String), key As String, value As String) As String
        Dim value1 As String = Nothing
        If map.TryGetValue(key, value1) Then
            Return value1 & value
        End If
        Return value
    End Function
End Class
",
                                                      equivalenceKey:="DCA081.TryGetValue", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA081 works on <c>IDictionary(Of TKey, TValue)</c> and with an <see langword="Else"/>
        ''' part, with <c>Option Strict Off</c> and <c>Option Infer On</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InterfaceTypes_ElseParts_AndLooseOptions() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Lookup(map As IDictionary(Of Integer, String), key As Integer) As String
        If [|map.ContainsKey(key)|] Then
            Return map(key).Trim()
        Else
            Return String.Empty
        End If
    End Function
End Class
", "
Public Class C
    Public Function Lookup(map As IDictionary(Of Integer, String), key As Integer) As String
        Dim value As String = Nothing
        If map.TryGetValue(key, value) Then
            Return value.Trim()
        Else
            Return String.Empty
        End If
    End Function
End Class
",
                                                      equivalenceKey:="DCA081.TryGetValue",
                                                      optionStatements:=(VisualBasic.OptionStrict.Off, True, True), targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA081 recognizes the call in every form of condition (an <c>If(...)</c> expression, <see langword="Not"/>,
        ''' <see langword="AndAlso"/>, <see langword="OrElse"/>, <see langword="ElseIf"/>, the <see langword="Else"/> part, guard clauses and
        ''' explicit line continuations), and that its code fix declares the value before the statement.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_EveryConditionForm() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Ternary(key As String) As Integer
        Return If([|map.ContainsKey(key)|], map(key), 0)
    End Function

    Public Function Negated(key As String) As Integer
        Return If(Not [|map.ContainsKey(key)|], 0, map(key))
    End Function

    Public Function Guard(key As String) As Integer
        If Not [|map.ContainsKey(key)|] Then
            Return 0
        End If
        Return map(key) * 2
    End Function

    Public Function SingleLineGuard(key As String) As Integer
        If Not [|map.ContainsKey(key)|] Then Throw New KeyNotFoundException(key)
        Return map(key)
    End Function

    Public Function Combined(key As String) As Boolean
        If [|map.ContainsKey(key)|] AndAlso map(key) > 0 Then
            Return map(key) < 10
        End If
        Return False
    End Function

    Public Function OrElseGuard(key As String) As Integer
        If Not [|map.ContainsKey(key)|] OrElse map(key) = 0 Then Return -1
        Return map(key)
    End Function

    Public Function ElseIfBranch(key As String, other As String) As Integer
        If other Is Nothing Then
            Return -1
        ElseIf [|map.ContainsKey(key)|] Then
            Return map(key)
        Else
            Return 0
        End If
    End Function

    Public Function ElsePart(key As String) As Integer
        If Not ([|map.ContainsKey(key)|]) Then
            Return 0
        Else
            Return map(key)
        End If
    End Function

    Public Function Continued(key As String) As Integer
        Return If([|map _
            .ContainsKey(key)|], _
            map(key), _
            0)
    End Function

    Public Function InLoop(keys As String()) As Integer
        Dim total As Integer = 0
        For Each key As String In keys
            If Not [|map.ContainsKey(key)|] Then Continue For
            total += map(key)
        Next key
        Return total
    End Function
End Class
", "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Ternary(key As String) As Integer
        Dim value As Integer = Nothing
        Return If(map.TryGetValue(key, value), value, 0)
    End Function

    Public Function Negated(key As String) As Integer
        Dim value As Integer = Nothing
        Return If(Not map.TryGetValue(key, value), 0, value)
    End Function

    Public Function Guard(key As String) As Integer
        Dim value As Integer = Nothing
        If Not map.TryGetValue(key, value) Then
            Return 0
        End If
        Return value * 2
    End Function

    Public Function SingleLineGuard(key As String) As Integer
        Dim value As Integer = Nothing
        If Not map.TryGetValue(key, value) Then Throw New KeyNotFoundException(key)
        Return value
    End Function

    Public Function Combined(key As String) As Boolean
        Dim value As Integer = Nothing
        If map.TryGetValue(key, value) AndAlso value > 0 Then
            Return value < 10
        End If
        Return False
    End Function

    Public Function OrElseGuard(key As String) As Integer
        Dim value As Integer = Nothing
        If Not map.TryGetValue(key, value) OrElse value = 0 Then Return -1
        Return value
    End Function

    Public Function ElseIfBranch(key As String, other As String) As Integer
        Dim value As Integer = Nothing
        If other Is Nothing Then
            Return -1
        ElseIf map.TryGetValue(key, value) Then
            Return value
        Else
            Return 0
        End If
    End Function

    Public Function ElsePart(key As String) As Integer
        Dim value As Integer = Nothing
        If Not (map.TryGetValue(key, value)) Then
            Return 0
        Else
            Return value
        End If
    End Function

    Public Function Continued(key As String) As Integer
        Dim value As Integer = Nothing
        Return If(map.TryGetValue(key, value), _
            value, _
            0)
    End Function

    Public Function InLoop(keys As String()) As Integer
        Dim total As Integer = 0
        For Each key As String In keys
            Dim value As Integer = Nothing
            If Not map.TryGetValue(key, value) Then Continue For
            total += value
        Next key
        Return total
    End Function
End Class
",
                                                      equivalenceKey:="DCA081.TryGetValue", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA081 does not report reads that also run when the key does not exist: the other branch of the
        ''' condition, the right operand of <see langword="OrElse"/> after a positive call, a statement after a condition that does not
        ''' leave the block, and conditions whose meaning is unknown.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ReadsWhereTheKeyMayNotExist() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Function Run(key As String, flag As Boolean) As Integer
        Dim a As Integer = If(map.ContainsKey(key), 1, map(key))
        If map.ContainsKey(key) OrElse map(key) > 0 Then Return 1
        If Not map.ContainsKey(key) Then Console.WriteLine(key)
        Dim b As Integer = map(key)
        If flag OrElse map.ContainsKey(key) Then Return map(key)
        If map.ContainsKey(key) = flag Then Return map(key)
        Return a + b
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA081 reports an <c>If(...)</c> expression in a single-line lambda without a code fix, because no
        ''' declaration can be inserted there.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_NoneInASingleLineLambda() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Function Create(map As Dictionary(Of String, Integer)) As Func(Of String, Integer)
        Return Function(key As String) If(map.ContainsKey(key), map(key), 0)
    End Function
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(Array.Empty(Of String)(), actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
