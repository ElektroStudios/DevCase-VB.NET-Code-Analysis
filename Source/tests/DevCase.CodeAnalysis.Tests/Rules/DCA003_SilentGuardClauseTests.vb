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

#Region " DCA003_SilentGuardClauseTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA003 (silent null checks) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA003_SilentGuardClauseTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New SilentGuardClauseAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New SilentGuardClauseCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA003_SilentGuardClauseTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA003 reports every supported shape of a silent guard clause.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SupportedGuardShapes() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub A(value As Object)
        If value Is Nothing Then [|Return|]
    End Sub
    Public Sub B(path As String)
        If String.IsNullOrWhiteSpace(path) Then
            [|Exit Sub|]
        End If
    End Sub
    Public Function C1(count As Integer) As Integer
        If count < 0 Then [|Exit Function|]
        Return count
    End Function
    Public Function D(items As Integer()) As Object
        If (items.Length = 0) Then [|Return Nothing|]
        Return items
    End Function
    Public Sub E(first As Object, second As String)
        If first Is Nothing OrElse String.IsNullOrEmpty(second) Then [|Return|]
    End Sub
    Public Sub F(value As Object)
        If Nothing Is value Then [|Return|]
        If IsNothing(value) Then [|Return|]
    End Sub
    Public Sub G(count As Integer)
        If 0 > count Then [|Return|]
    End Sub
    Public WriteOnly Property P As String
        Set(value As String)
            If value Is Nothing Then [|Exit Property|]
        End Set
    End Property
    Default Public ReadOnly Property Item(index As Integer) As Integer
        Get
            If index < 0 Then [|Exit Property|]
            Return index
        End Get
    End Property
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA003 does not report conditions that are not guard clauses on parameters.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_NonGuardOrNonParameterChecks() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System
Public Class C
    Private field As Object
    Public Sub A(value As Object)
        If value Is Nothing Then
            Return
        Else
            Console.WriteLine(value)
        End If
    End Sub
    Public Sub B(value As Object)
        If value Is Nothing Then
            Console.WriteLine(""missing"")
            Return
        End If
    End Sub
    Public Sub C1()
        Dim local As Object = Nothing
        If local Is Nothing Then Return
        If Me.field Is Nothing Then Return
    End Sub
    Public Function D(value As Object) As Boolean
        If value Is Nothing Then Return False
        Return True
    End Function
    Public Sub E(value As Object)
        Dim action As Action = Sub()
                                   If value Is Nothing Then Return
                               End Sub
    End Sub
    Public Sub F(value As Object, other As Object)
        If value Is Nothing OrElse Me.field Is Nothing Then Return
        If value Is other Then Return
    End Sub
    Public Sub G(count As Integer, limit As Integer)
        If count > count + limit Then Return
    End Sub
    Public Function H(input As String, ByRef result As Object) As Object
        result = Nothing
        If result Is Nothing Then Return Nothing
        Return result
    End Function
    Public Sub I(value As Object)
        value = New Object()
        If value Is Nothing Then Return
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA003 throws the exception that matches each guard condition.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ThrowsTheRightException() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub A(value As Object)
        If value Is Nothing Then Return
    End Sub
    Public Sub B(path As String)
        If String.IsNullOrWhiteSpace(path) Then
            Exit Sub ' silent
        End If
    End Sub
    Public Function C1(count As Integer) As Integer
        If count < 0 Then Exit Function
        Return count
    End Function
    Public Function D(items As Integer()) As Object
        If items.Length = 0 Then Return Nothing
        Return items
    End Function
    Public Sub E(value As Object)
        If value IsNot Nothing Then Return
    End Sub
End Class
", "
Public Class C
    Public Sub A(value As Object)
        If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
    End Sub
    Public Sub B(path As String)
        If String.IsNullOrWhiteSpace(path) Then
            Throw New ArgumentNullException(NameOf(path)) ' silent
        End If
    End Sub
    Public Function C1(count As Integer) As Integer
        If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count))
        Return count
    End Function
    Public Function D(items As Integer()) As Object
        If items.Length = 0 Then Throw New ArgumentException(""Value cannot be empty."", NameOf(items))
        Return items
    End Function
    Public Sub E(value As Object)
        If value IsNot Nothing Then Throw New ArgumentException(""Value must be Nothing."", NameOf(value))
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA003 splits a combined guard into one check per parameter.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_SplitsCombinedGuards() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub A(first As Object, second As String)
        ' Validate arguments.
        If first Is Nothing OrElse String.IsNullOrEmpty(second) Then Return
        System.Console.WriteLine(second)
    End Sub
End Class
", "
Public Class C
    Public Sub A(first As Object, second As String)
        ' Validate arguments.
        If first Is Nothing Then
            Throw New ArgumentNullException(NameOf(first))
        End If

        If String.IsNullOrEmpty(second) Then
            Throw New ArgumentNullException(NameOf(second))
        End If
        System.Console.WriteLine(second)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA003 produces valid code for a nested single-line <c>If</c> statement.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NestedSingleLineIf_DoesNotProduceInvalidCode() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub A(flag As Boolean, first As Object, second As Object)
        If flag Then If first Is Nothing OrElse second Is Nothing Then Return
    End Sub
End Class
", "
Public Class C
    Public Sub A(flag As Boolean, first As Object, second As Object)
        If flag Then If first Is Nothing OrElse second Is Nothing Then Throw New ArgumentNullException(NameOf(first))
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
