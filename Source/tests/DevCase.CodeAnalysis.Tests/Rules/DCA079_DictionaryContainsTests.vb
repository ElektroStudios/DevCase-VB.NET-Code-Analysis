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

#Region " DCA079_DictionaryContainsTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA079 (Contains on Keys or Values, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA079_DictionaryContainsTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DictionaryContainsAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA079_DictionaryContainsTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA079 reports <c>Contains</c> on <c>Keys</c> and <c>Values</c> when the dictionary has the direct method,
        ''' and ignores calls with a comparer and <c>Values</c> of an interface without <c>ContainsValue</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ContainsOnKeysAndValues() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Check(map As Dictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Dim a As Boolean = [|map.Keys.Contains(name)|]
        Dim b As Boolean = [|map.Values.Contains(value)|]
        Dim c As Boolean = map.Keys.Contains(name, StringComparer.OrdinalIgnoreCase)
        Dim d As Boolean = map.ContainsKey(name)
        Return a OrElse b OrElse c OrElse d
    End Function

    Public Function CheckInterface(map As IDictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Return [|map.Keys.Contains(name)|] OrElse map.Values.Contains(value)
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA079 does not run in a modern .NET project, where the built-in rule CA1841 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Check(map As Dictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Dim a As Boolean = map.Keys.Contains(name)
        Dim b As Boolean = map.Values.Contains(value)
        Dim c As Boolean = map.Keys.Contains(name, StringComparer.OrdinalIgnoreCase)
        Dim d As Boolean = map.ContainsKey(name)
        Return a OrElse b OrElse c OrElse d
    End Function

    Public Function CheckInterface(map As IDictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Return map.Keys.Contains(name) OrElse map.Values.Contains(value)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA079 calls the method of the dictionary.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesContainsKeyOrContainsValue() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function Check(map As Dictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Dim a As Boolean = [|map.Keys.Contains(name)|]
        Dim b As Boolean = [|map.Values.Contains(value)|]
        Dim c As Boolean = map.Keys.Contains(name, StringComparer.OrdinalIgnoreCase)
        Dim d As Boolean = map.ContainsKey(name)
        Return a OrElse b OrElse c OrElse d
    End Function

    Public Function CheckInterface(map As IDictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Return [|map.Keys.Contains(name)|] OrElse map.Values.Contains(value)
    End Function
End Class
", "
Public Class C
    Public Function Check(map As Dictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Dim a As Boolean = map.ContainsKey(name)
        Dim b As Boolean = map.ContainsValue(value)
        Dim c As Boolean = map.Keys.Contains(name, StringComparer.OrdinalIgnoreCase)
        Dim d As Boolean = map.ContainsKey(name)
        Return a OrElse b OrElse c OrElse d
    End Function

    Public Function CheckInterface(map As IDictionary(Of String, Integer), name As String, value As Integer) As Boolean
        Return map.ContainsKey(name) OrElse map.Values.Contains(value)
    End Function
End Class
",
                                                      equivalenceKey:="DCA079.DirectLookup", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
