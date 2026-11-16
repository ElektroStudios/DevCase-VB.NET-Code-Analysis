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

#Region " DCA021_CollectionMethodTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA021 (LINQ method instead of Find/Exists/TrueForAll) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA021_CollectionMethodTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New CollectionMethodAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New CollectionMethodCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA021_CollectionMethodTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA021 reports LINQ calls with a lambda or <c>AddressOf</c> predicate on lists and arrays.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ListsAndArraysWithLambdaPredicates() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(items As List(Of Integer), numbers As Integer(), sequence As IEnumerable(Of Integer), test As Func(Of Integer, Boolean))
        Dim a As Integer = [|items.FirstOrDefault(Function(x As Integer) x > 1)|]
        Dim b As Boolean = [|items.Any(Function(x As Integer) x > 1)|]
        Dim c As Boolean = [|items.All(AddressOf IsValid)|]
        Dim d As Boolean = [|numbers.Any(Function(x As Integer) x > 1)|]
        Dim e As Boolean = items.Any()
        Dim f As Boolean = sequence.Any(Function(x As Integer) x > 1)
        Dim g As Boolean = items.Any(test)
        Dim h As Integer = items.FirstOrDefault()
    End Sub
    Private Shared Function IsValid(value As Integer) As Boolean
        Return value > 0
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA021 replaces the LINQ method with the list method.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_List_RenamesTheMethod() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(items As List(Of String)) As String
        If items.All(Function(s As String) s.Length > 0) Then
            Return items.FirstOrDefault(Function(s As String) s.Length > 3)
        End If
        Return Nothing
    End Function
End Class
", "
Public Class C
    Public Function M(items As List(Of String)) As String
        If items.TrueForAll(Function(s As String) s.Length > 0) Then
            Return items.Find(Function(s As String) s.Length > 3)
        End If
        Return Nothing
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA021 uses the shared <see cref="Array"/> method for arrays.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Array_UsesTheStaticArrayMethod_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(numbers As Integer()) As Boolean
        Return numbers.Any(Function(n As Integer) n > 1)
    End Function
End Class
", "
Public Class C
    Public Function M(numbers As Integer()) As Boolean
        Return Array.Exists(numbers, Function(n As Integer) n > 1)
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
