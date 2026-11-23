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

#Region " DCA055_ZeroLengthArrayTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA055 (zero-length array allocation, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA055_ZeroLengthArrayTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ZeroLengthArrayAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ZeroLengthArrayCodeFixProvider()

        ''' <summary>
        ''' A source with zero-length arrays in every reported form.
        ''' </summary>
        Private Const ReportedSource As String = "
Public Class C
    Private ReadOnly empty As String() = [|New String() {}|]
    Public Function Run(Of T)() As Object()
        Dim parameters As Object() = New Object() {[|New String() {}|]}
        Dim numbers As Integer() = [|New Integer(-1) {}|]
        Dim folded As Integer() = [|New Integer(0 - 1) {}|]
        Dim literal As Long() = [|{}|]
        Dim jagged As String()() = [|New String()() {}|]
        Dim generic As T() = [|New T() {}|]
        Return {parameters, numbers, folded, literal, jagged, generic, Me.empty}
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA055_ZeroLengthArrayTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA055 reports zero-length arrays in a .NET Framework project: <c>New T() {}</c>, 
        ''' <c>New T(-1) {}</c>, the empty array literal, jagged and generic arrays.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ZeroLengthArrays_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ReportedSource, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA055 does not run in a modern .NET project, where the built-in rule CA1825 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ReportedSource.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal)).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA055 does not report arrays with elements or with a size that is not a constant zero, 
        ''' multidimensional arrays, attribute arguments, ParamArray arrays and arrays declared with bounds on the variable.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_NonEmptyOrRequiredArrays() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
<AttributeUsage(AttributeTargets.Class)>
Public NotInheritable Class TagsAttribute : Inherits Attribute
    Public Sub New(tags As String())
    End Sub
End Class
<Tags(New String() {})>
Public Class C
    Public Function Run(count As Integer) As Object()
        Dim values As String() = New String() {""a""}
        Dim sized As String() = New String(9) {}
        Dim variable As String() = New String(count - 1) {}
        Dim grid As Integer(,) = New Integer(,) {}
        Dim text As String = String.Format(""x"")
        Dim declared(-1) As String
        Return {values, sized, variable, grid, text, declared}
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA055 replaces the array with <c>Array.Empty(Of T)()</c> (the reported example).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesArrayEmpty() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run()
        Dim parameters As Object()
        parameters = New Object() {[|New String() {}|]}
    End Sub
End Class
", "
Public Class C
    Public Sub Run()
        Dim parameters As Object()
        parameters = New Object() {Array.Empty(Of String)()}
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA055 replaces every form, and qualifies <c>Array</c> when a type with that name is in scope.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_ReplacesEveryForm() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider,
                ReportedSource.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal),
                NameOf(ZeroLengthArrayCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Public Class C
    Private ReadOnly empty As String() = Array.Empty(Of String)()
    Public Function Run(Of T)() As Object()
        Dim parameters As Object() = New Object() {Array.Empty(Of String)()}
        Dim numbers As Integer() = Array.Empty(Of Integer)()
        Dim folded As Integer() = Array.Empty(Of Integer)()
        Dim literal As Long() = Array.Empty(Of Long)()
        Dim jagged As String()() = Array.Empty(Of String())()
        Dim generic As T() = Array.Empty(Of T)()
        Return {parameters, numbers, folded, literal, jagged, generic, Me.empty}
    End Function
End Class
", fixedText)

            Dim qualified As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public Class Array
End Class
Public Class C
    Public Function Run() As String()
        Return New String() {}
    End Function
End Class
", NameOf(ZeroLengthArrayCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Public Class Array
End Class
Public Class C
    Public Function Run() As String()
        Return System.Array.Empty(Of String)()
    End Function
End Class
", qualified)
        End Function

#End Region

    End Class

End Namespace

#End Region
