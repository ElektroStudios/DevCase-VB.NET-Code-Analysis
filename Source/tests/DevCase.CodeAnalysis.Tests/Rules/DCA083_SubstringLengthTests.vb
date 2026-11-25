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

#Region " DCA083_SubstringLengthTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA083 (redundant Substring length, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA083_SubstringLengthTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New SubstringLengthAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA083_SubstringLengthTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA083 reports a length that reads to the end of the same string, and ignores other lengths, other
        ''' strings, receivers with side effects and named arguments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_LengthToTheEnd() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Property Name As String

    Public Function Tail(text As String, start As Integer) As String
        Dim a As String = text.Substring(start, [|text.Length - start|])
        Dim b As String = Me.Name.Substring(2, [|Me.Name.Length - 2|])
        Dim c As String = text.Substring(start, text.Length - start - 1)
        Dim d As String = text.Substring(start, Me.Name.Length - start)
        Dim e As String = Me.GetText().Substring(start, Me.GetText().Length - start)
        Dim f As String = text.Substring(startIndex:=start, length:=text.Length - start)
        Return a & b & c & d & e & f
    End Function

    Private Function GetText() As String
        Return Me.Name
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA083 does not run in a modern .NET project, where the built-in rule CA1514 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Property Name As String

    Public Function Tail(text As String, start As Integer) As String
        Dim a As String = text.Substring(start, text.Length - start)
        Dim b As String = Me.Name.Substring(2, Me.Name.Length - 2)
        Dim c As String = text.Substring(start, text.Length - start - 1)
        Dim d As String = text.Substring(start, Me.Name.Length - start)
        Dim e As String = Me.GetText().Substring(start, Me.GetText().Length - start)
        Dim f As String = text.Substring(startIndex:=start, length:=text.Length - start)
        Return a & b & c & d & e & f
    End Function

    Private Function GetText() As String
        Return Me.Name
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA083 removes the length argument.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesTheLength() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Property Name As String

    Public Function Tail(text As String, start As Integer) As String
        Dim a As String = text.Substring(start, [|text.Length - start|])
        Dim b As String = Me.Name.Substring(2, [|Me.Name.Length - 2|])
        Dim c As String = text.Substring(start, text.Length - start - 1)
        Dim d As String = text.Substring(start, Me.Name.Length - start)
        Dim e As String = Me.GetText().Substring(start, Me.GetText().Length - start)
        Dim f As String = text.Substring(startIndex:=start, length:=text.Length - start)
        Return a & b & c & d & e & f
    End Function

    Private Function GetText() As String
        Return Me.Name
    End Function
End Class
", "
Public Class C
    Public Property Name As String

    Public Function Tail(text As String, start As Integer) As String
        Dim a As String = text.Substring(start)
        Dim b As String = Me.Name.Substring(2)
        Dim c As String = text.Substring(start, text.Length - start - 1)
        Dim d As String = text.Substring(start, Me.Name.Length - start)
        Dim e As String = Me.GetText().Substring(start, Me.GetText().Length - start)
        Dim f As String = text.Substring(startIndex:=start, length:=text.Length - start)
        Return a & b & c & d & e & f
    End Function

    Private Function GetText() As String
        Return Me.Name
    End Function
End Class
",
                                                      equivalenceKey:="DCA083.RemoveLength", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
