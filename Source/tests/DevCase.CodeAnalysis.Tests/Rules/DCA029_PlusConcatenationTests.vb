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

#Region " DCA029_PlusConcatenationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA029 (String concatenated with '+') and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA029_PlusConcatenationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New PlusConcatenationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New PlusConcatenationCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA029_PlusConcatenationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA029 reports '+' and '+=' between strings (and characters), and skips numeric additions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_StringAdditions() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(name As String, letter As Char, count As Integer) As String
        Dim text As String = name [|+|] "" "" [|+|] letter
        text [|+=|] ""!""
        Dim total As Integer = count + 1
        Return text & total.ToString(Globalization.CultureInfo.InvariantCulture)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA029 writes '&amp;' and '&amp;=', adding spaces when the original operator had none.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesTheConcatenationOperator() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(name As String, hello As String) As String
        Dim text As String = name + "" "" + hello
        text += ""!""
        Return name+hello
    End Function
End Class
", "
Public Class C
    Public Function M(name As String, hello As String) As String
        Dim text As String = name & "" "" & hello
        text &= ""!""
        Return name & hello
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
