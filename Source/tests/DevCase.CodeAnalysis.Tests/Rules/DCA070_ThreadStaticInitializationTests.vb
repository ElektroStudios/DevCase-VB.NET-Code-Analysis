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

Imports Xunit

#End Region

#Region " DCA070_ThreadStaticInitializationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA070 (ThreadStatic field initialization, .NET Framework).
    ''' </summary>
    Public NotInheritable Class DCA070_ThreadStaticInitializationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThreadStaticInitializationAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA070_ThreadStaticInitializationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA070 reports thread-static fields with an initializer (also in modules) and their assignments in
        ''' the <see langword="Shared"/> constructor, and ignores lazy initialization, other fields and instance fields.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_InitializersAndSharedConstructorAssignments() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text

Public Class C
    <ThreadStatic>
    Private Shared [|buffer|] As New StringBuilder()

    <ThreadStatic>
    Private Shared [|counter|] As Integer = 5

    <ThreadStatic>
    Private Shared lazy As StringBuilder

    <ThreadStatic>
    Private Shared fromConstructor As StringBuilder

    Private Shared normal As New StringBuilder()

    <ThreadStatic>
    Private instance As Integer = 1

    Shared Sub New()
        [|fromConstructor = New StringBuilder()|]
        normal = New StringBuilder()
    End Sub

    Public Sub Reset()
        lazy = New StringBuilder()
    End Sub
End Class

Public Module M
    <ThreadStatic>
    Private [|cache|] As New StringBuilder()
End Module
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA070 does not run in a modern .NET project, where the built-in rule CA2019 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text

Public Class C
    <ThreadStatic>
    Private Shared buffer As New StringBuilder()

    <ThreadStatic>
    Private Shared counter As Integer = 5

    <ThreadStatic>
    Private Shared lazy As StringBuilder

    <ThreadStatic>
    Private Shared fromConstructor As StringBuilder

    Private Shared normal As New StringBuilder()

    <ThreadStatic>
    Private instance As Integer = 1

    Shared Sub New()
        fromConstructor = New StringBuilder()
        normal = New StringBuilder()
    End Sub

    Public Sub Reset()
        lazy = New StringBuilder()
    End Sub
End Class

Public Module M
    <ThreadStatic>
    Private cache As New StringBuilder()
End Module
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
