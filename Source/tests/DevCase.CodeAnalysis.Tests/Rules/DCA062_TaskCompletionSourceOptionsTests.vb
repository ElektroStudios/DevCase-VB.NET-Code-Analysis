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
Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA062_TaskCompletionSourceOptionsTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA062 (TaskContinuationOptions passed as state, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA062_TaskCompletionSourceOptionsTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New TaskCompletionSourceOptionsAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA062_TaskCompletionSourceOptionsTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA062 reports <c>TaskContinuationOptions</c> values passed to <c>TaskCompletionSource(Of T)</c>, and
        ''' ignores <c>TaskCreationOptions</c> and other states.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ContinuationOptionsPassedAsState() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading.Tasks

Public Class C
    Public Function Create() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)([|TaskContinuationOptions.RunContinuationsAsynchronously|])
    End Function

    Public Function Combined() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)([|(TaskContinuationOptions.RunContinuationsAsynchronously Or TaskContinuationOptions.LongRunning)|])
    End Function

    Public Function Correct() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskCreationOptions.RunContinuationsAsynchronously)
    End Function

    Public Function State() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(""state"")
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA062 does not run in a modern .NET project, where the built-in rule CA2247 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading.Tasks

Public Class C
    Public Function Create() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskContinuationOptions.RunContinuationsAsynchronously)
    End Function

    Public Function Combined() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)((TaskContinuationOptions.RunContinuationsAsynchronously Or TaskContinuationOptions.LongRunning))
    End Function

    Public Function Correct() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskCreationOptions.RunContinuationsAsynchronously)
    End Function

    Public Function State() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(""state"")
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA062 replaces every member with the <c>TaskCreationOptions</c> member of the same name.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesTaskCreationOptions() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading.Tasks

Public Class C
    Public Function Create() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)([|TaskContinuationOptions.RunContinuationsAsynchronously|])
    End Function

    Public Function Combined() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)([|(TaskContinuationOptions.RunContinuationsAsynchronously Or TaskContinuationOptions.LongRunning)|])
    End Function

    Public Function Correct() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskCreationOptions.RunContinuationsAsynchronously)
    End Function

    Public Function State() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(""state"")
    End Function
End Class
", "
Imports System.Threading.Tasks

Public Class C
    Public Function Create() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskCreationOptions.RunContinuationsAsynchronously)
    End Function

    Public Function Combined() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)((TaskCreationOptions.RunContinuationsAsynchronously Or TaskCreationOptions.LongRunning))
    End Function

    Public Function Correct() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskCreationOptions.RunContinuationsAsynchronously)
    End Function

    Public Function State() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(""state"")
    End Function
End Class
",
                                                      equivalenceKey:="DCA062.TaskCreationOptions", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA062 is not offered for <c>ExecuteSynchronously</c>, which does not exist in
        ''' <c>TaskCreationOptions</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_NoneForAMemberWithoutEquivalent() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Imports System.Threading.Tasks

Public Class C
    Public Function Create() As TaskCompletionSource(Of Integer)
        Return New TaskCompletionSource(Of Integer)(TaskContinuationOptions.ExecuteSynchronously)
    End Function
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(Array.Empty(Of String)(), actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
