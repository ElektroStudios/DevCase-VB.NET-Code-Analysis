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

#Region " DCA025_RedundantNullCheckTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA025 (redundant null check before TypeOf) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA025_RedundantNullCheckTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New RedundantNullCheckAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New RedundantNullCheckCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA025_RedundantNullCheckTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA025 reports redundant null checks on simple references.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_RedundantChecksOnSimpleReferences() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private ReadOnly Field As Object
    Public Sub M(value As Object, other As Object, enabled As Boolean)
        Dim a As Boolean = [|value IsNot Nothing|] AndAlso TypeOf value Is String
        Dim b As Boolean = enabled AndAlso [|Me.Field IsNot Nothing|] AndAlso TypeOf Me.Field Is String
        Dim c As Boolean = [|value Is Nothing|] OrElse TypeOf value IsNot String
        Dim d As Boolean = other IsNot Nothing AndAlso TypeOf value Is String
        Dim e As Boolean = GetValue() IsNot Nothing AndAlso TypeOf GetValue() Is String
        Dim f As Boolean = value IsNot Nothing OrElse TypeOf value Is String
    End Sub
    Private Function GetValue() As Object
        Return Nothing
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA025 removes the null check and its logical operator.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesTheCheckAndItsOperator() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(value As Object, enabled As Boolean) As Integer
        If value IsNot Nothing AndAlso TypeOf value Is String Then
            Return 1
        End If
        If enabled AndAlso value IsNot Nothing AndAlso TypeOf value Is Integer Then
            Return 2
        End If
        If value Is Nothing OrElse TypeOf value IsNot Double Then
            Return 3
        End If
        Return 0
    End Function
End Class
", "
Public Class C
    Public Function M(value As Object, enabled As Boolean) As Integer
        If TypeOf value Is String Then
            Return 1
        End If
        If enabled AndAlso TypeOf value Is Integer Then
            Return 2
        End If
        If TypeOf value IsNot Double Then
            Return 3
        End If
        Return 0
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA025 recognizes every Visual Basic form of a null check: <c>Not x Is Nothing</c>, 
        ''' <c>Nothing IsNot x</c>, <c>Not IsNothing(x)</c> and their inverse forms before <c>TypeOf ... IsNot</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_EveryNullCheckForm() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(sender As Object)
        Dim a As Boolean = [|Not sender Is Nothing|] AndAlso TypeOf sender Is String
        Dim b As Boolean = [|Not (sender Is Nothing)|] AndAlso TypeOf sender Is String
        Dim c As Boolean = [|Nothing IsNot sender|] AndAlso TypeOf sender Is String
        Dim d As Boolean = [|Not IsNothing(sender)|] AndAlso TypeOf sender Is String
        Dim e As Boolean = [|IsNothing(sender)|] OrElse TypeOf sender IsNot String
        Dim f As Boolean = [|Not sender IsNot Nothing|] OrElse TypeOf sender IsNot String
        Dim g As Boolean = IsNothing(sender) AndAlso TypeOf sender Is String
        Dim h As Boolean = Not sender Is Nothing OrElse TypeOf sender IsNot String
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA025 removes a null check written as <c>Not x Is Nothing</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesNegatedIsNothingCheck() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(sender As Object) As Boolean
        If Not sender Is Nothing AndAlso TypeOf sender Is String Then
            Return True
        End If
        Return False
    End Function
End Class
", "
Public Class C
    Public Function M(sender As Object) As Boolean
        If TypeOf sender Is String Then
            Return True
        End If
        Return False
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
