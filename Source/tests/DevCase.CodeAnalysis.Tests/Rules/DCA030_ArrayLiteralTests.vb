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

#Region " DCA030_ArrayLiteralTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA030 (Array creation expression can be an array literal) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA030_ArrayLiteralTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ArrayLiteralAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ArrayLiteralCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA030_ArrayLiteralTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA030 reports array creations whose target type is the same array type, and skips arguments, conversions, bounds and lambdas.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_TargetTypedArrayCreations() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly names As String() = [|New String()|] {""a"", ""b""}
    Public Property Values As Integer() = [|New Integer()|] {1, 2}
    Public Function M() As Integer()
        Dim empty As Integer() = [|New Integer()|] {}
        empty = [|New Integer()|] {3}
        Dim objects As Object() = New String() {""x""}
        Dim sized As Integer() = New Integer(9) {}
        Dim grid As Integer()() = New Integer()() {New Integer() {1}}
        Dim count As Integer = String.Concat(New String() {""a"", ""b""}).Length
        Dim factory As Func(Of Long()) = Function()
                                             Return New Long() {1}
                                         End Function
        Return [|New Integer()|] {count, objects.Length, sized.Length, grid.Length, factory().Length, names.Length}
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA030 keeps only the array literal, and that the array keeps its type (Long elements written as Integer literals).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesTheCreationExpression() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M() As Long()
        Dim values As Long() = New Long() {1, 2}
        Return New Long() {values(0) + values(1)}
    End Function
End Class
", "
Public Class C
    Public Function M() As Long()
        Dim values As Long() = {1, 2}
        Return {values(0) + values(1)}
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
