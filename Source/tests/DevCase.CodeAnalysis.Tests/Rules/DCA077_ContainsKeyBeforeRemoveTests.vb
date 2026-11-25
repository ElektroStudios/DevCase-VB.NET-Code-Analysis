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

#Region " DCA077_ContainsKeyBeforeRemoveTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA077 (ContainsKey before Remove, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA077_ContainsKeyBeforeRemoveTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ContainsKeyBeforeRemoveAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA077_ContainsKeyBeforeRemoveTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA077 reports <c>Remove</c> guarded by <c>ContainsKey</c> on the same dictionary and key, and ignores
        ''' other receivers, other first statements, <see langword="Else"/> parts, keys with side effects and negated checks.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_RemoveGuardedByContainsKey() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Sub Remove(key As String)
        If [|map.ContainsKey(key)|] Then
            map.Remove(key)
        End If
    End Sub

    Public Sub RemoveAndLog(key As String)
        If [|Me.map.ContainsKey(key)|] Then
            Me.map.Remove(key)
            Console.WriteLine(key)
        End If
    End Sub

    Public Sub SingleLine(key As String)
        If [|map.ContainsKey(key)|] Then map.Remove(key)
    End Sub

    Public Sub NotReported(key As String, other As Dictionary(Of String, Integer))
        If map.ContainsKey(key) Then other.Remove(key)
        If map.ContainsKey(key) Then
            Console.WriteLine(key)
            map.Remove(key)
        End If
        If map.ContainsKey(key) Then
            map.Remove(key)
        Else
            Console.WriteLine(key)
        End If
        If map.ContainsKey(key.Trim()) Then map.Remove(key.Trim())
        If Not map.ContainsKey(key) Then map.Remove(key)
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA077 does not run in a modern .NET project, where the built-in rule CA1853 applies.
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

    Public Sub Remove(key As String)
        If map.ContainsKey(key) Then
            map.Remove(key)
        End If
    End Sub

    Public Sub RemoveAndLog(key As String)
        If Me.map.ContainsKey(key) Then
            Me.map.Remove(key)
            Console.WriteLine(key)
        End If
    End Sub

    Public Sub SingleLine(key As String)
        If map.ContainsKey(key) Then map.Remove(key)
    End Sub

    Public Sub NotReported(key As String, other As Dictionary(Of String, Integer))
        If map.ContainsKey(key) Then other.Remove(key)
        If map.ContainsKey(key) Then
            Console.WriteLine(key)
            map.Remove(key)
        End If
        If map.ContainsKey(key) Then
            map.Remove(key)
        Else
            Console.WriteLine(key)
        End If
        If map.ContainsKey(key.Trim()) Then map.Remove(key.Trim())
        If Not map.ContainsKey(key) Then map.Remove(key)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA077 keeps only the call when it is the only statement, and uses its result as the
        ''' condition otherwise.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallsRemoveDirectly() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Sub Remove(key As String)
        If [|map.ContainsKey(key)|] Then
            map.Remove(key)
        End If
    End Sub

    Public Sub RemoveAndLog(key As String)
        If [|Me.map.ContainsKey(key)|] Then
            Me.map.Remove(key)
            Console.WriteLine(key)
        End If
    End Sub

    Public Sub SingleLine(key As String)
        If [|map.ContainsKey(key)|] Then map.Remove(key)
    End Sub

    Public Sub NotReported(key As String, other As Dictionary(Of String, Integer))
        If map.ContainsKey(key) Then other.Remove(key)
        If map.ContainsKey(key) Then
            Console.WriteLine(key)
            map.Remove(key)
        End If
        If map.ContainsKey(key) Then
            map.Remove(key)
        Else
            Console.WriteLine(key)
        End If
        If map.ContainsKey(key.Trim()) Then map.Remove(key.Trim())
        If Not map.ContainsKey(key) Then map.Remove(key)
    End Sub
End Class
", "
Public Class C
    Private ReadOnly map As New Dictionary(Of String, Integer)()

    Public Sub Remove(key As String)
        map.Remove(key)
    End Sub

    Public Sub RemoveAndLog(key As String)
        If Me.map.Remove(key) Then
            Console.WriteLine(key)
        End If
    End Sub

    Public Sub SingleLine(key As String)
        map.Remove(key)
    End Sub

    Public Sub NotReported(key As String, other As Dictionary(Of String, Integer))
        If map.ContainsKey(key) Then other.Remove(key)
        If map.ContainsKey(key) Then
            Console.WriteLine(key)
            map.Remove(key)
        End If
        If map.ContainsKey(key) Then
            map.Remove(key)
        Else
            Console.WriteLine(key)
        End If
        If map.ContainsKey(key.Trim()) Then map.Remove(key.Trim())
        If Not map.ContainsKey(key) Then map.Remove(key)
    End Sub
End Class
",
                                                      equivalenceKey:="DCA077.RemoveCheck", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA077 keeps the comment lines of the removed block before the call, and the comment 
        ''' lines above the guarded call of a block that keeps other statements.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsTheCommentLines() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), key As String)
        If [|map.ContainsKey(key)|] Then
            ' Forget the key.
            map.Remove(key)
        End If
        If [|map.ContainsKey(key)|] Then
            ' Forget it again.
            map.Remove(key)
            Console.WriteLine(key)
        End If
    End Sub
End Class
", "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), key As String)
        ' Forget the key.
        map.Remove(key)
        If map.Remove(key) Then
            ' Forget it again.
            Console.WriteLine(key)
        End If
    End Sub
End Class
",
                                                      equivalenceKey:="DCA077.RemoveCheck", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA077 is not offered when it would remove a comment that follows code on its line.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_NoneWhenACommentWouldBeLost() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), key As String)
        If map.ContainsKey( _ ' The key to forget.
                key) Then map.Remove(key)
    End Sub
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(Array.Empty(Of String)(), actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA077 keeps the <see langword="Call"/> keyword and fixes a guard nested in another
        ''' block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallStatement_AndNestedBlocks() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), keys As String())
        For Each key As String In keys
            If [|map.ContainsKey(key)|] Then
                Call map.Remove(key)
            End If
        Next key
    End Sub
End Class
", "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), keys As String())
        For Each key As String In keys
            Call map.Remove(key)
        Next key
    End Sub
End Class
",
                                                      equivalenceKey:="DCA077.RemoveCheck", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA077 keeps the comment at the end of the <see langword="If"/> line.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsTheCommentAtTheEndOfTheIfLine() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), key As String)
        If [|map.ContainsKey(key)|] Then ' Known key.
            map.Remove(key)
        End If
    End Sub
End Class
", "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), key As String)
        map.Remove(key) ' Known key.
    End Sub
End Class
",
                                                      equivalenceKey:="DCA077.RemoveCheck", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA077 does not report a statement with preprocessor directives inside, because another configuration
        ''' may compile other statements.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_StatementsWithDirectives() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Remove(map As Dictionary(Of String, Integer), key As String)
        If map.ContainsKey(key) Then
#If DEBUG Then
            map.Remove(key)
#Else
            Console.WriteLine(key)
#End If
        End If
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
