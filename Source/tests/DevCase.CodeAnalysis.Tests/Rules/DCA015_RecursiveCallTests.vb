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
    ''' Tests of rule DCA015 (recursion).
    ''' </summary>
    Public NotInheritable Class DCA015_RecursiveCallTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New RecursiveCallAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA015_RecursiveCallTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA015 reports methods and properties that call themselves.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_DirectRecursion() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class Node
    Public Children As New System.Collections.Generic.List(Of Node)
    Private valueField As Integer
    Public Function Count() As Integer
        Dim total As Integer = 1
        For Each child As Node In Me.Children
            total += child.[|Count|]()
        Next child
        Return total
    End Function
    Public Sub Walk(Of T)(item As T)
        [|Walk|](item)
        Me.[|Walk(Of T)|](item)
    End Sub
    Public Property Value As Integer
        Get
            Return Me.[|Value|]
        End Get
        Set(value As Integer)
            Me.[|Value|] = value
        End Set
    End Property
    Public Function Factorial(n As Integer) As Long
        Return If(n <= 1, 1, n * [|Factorial|](n - 1))
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA015 does not report references that are not recursive calls.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_NonRecursiveReferences() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class Base
    Public Overridable Sub Run()
    End Sub
End Class
Public Class C : Inherits Base
    Private valueField As Integer
    Public Overrides Sub Run()
        MyBase.Run()
        Dim action As Action = Sub() Me.Run()
        Dim handler As Action = AddressOf Me.Run
        Dim name As String = NameOf(Run)
    End Sub
    Public Sub Print(value As Integer)
        Print(value, 1)
    End Sub
    Public Sub Print(value As Integer, times As Integer)
    End Sub
    Public Function Compute() As Integer
        Compute = 5
        Return Compute
    End Function
    Public Property Value As Integer
        Get
            Return Me.valueField
        End Get
        Set(value As Integer)
            Me.valueField = If(Me.Value = value, value, value + 0)
        End Set
    End Property
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace
