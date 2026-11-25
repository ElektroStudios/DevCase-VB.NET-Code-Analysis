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

#Region " DCA068_CancellationCheckTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA068 (manual cancellation check, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA068_CancellationCheckTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New CancellationCheckAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA068_CancellationCheckTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA068 reports an <see langword="If"/> statement that only throws <c>OperationCanceledException</c>
        ''' (without arguments or with the token), and ignores other statements, messages, <see langword="Else"/> parts and derived exceptions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_IfThatOnlyThrowsOperationCanceledException() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If [|cancellationToken.IsCancellationRequested|] Then
            Throw New OperationCanceledException()
        End If
        If [|cancellationToken.IsCancellationRequested|] Then Throw New OperationCanceledException(cancellationToken)
        If cancellationToken.IsCancellationRequested Then
            Console.WriteLine(""Canceled"")
            Throw New OperationCanceledException()
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException(""Stopped"")
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException()
        Else
            Console.WriteLine(""Running"")
        End If
        If cancellationToken.IsCancellationRequested Then Throw New TaskCanceledException()
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA068 does not run in a modern .NET project, where the built-in rule CA2250 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException()
        End If
        If cancellationToken.IsCancellationRequested Then Throw New OperationCanceledException(cancellationToken)
        If cancellationToken.IsCancellationRequested Then
            Console.WriteLine(""Canceled"")
            Throw New OperationCanceledException()
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException(""Stopped"")
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException()
        Else
            Console.WriteLine(""Running"")
        End If
        If cancellationToken.IsCancellationRequested Then Throw New TaskCanceledException()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA068 replaces the statement with <c>ThrowIfCancellationRequested()</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallsThrowIfCancellationRequested() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If [|cancellationToken.IsCancellationRequested|] Then
            Throw New OperationCanceledException()
        End If
        If [|cancellationToken.IsCancellationRequested|] Then Throw New OperationCanceledException(cancellationToken)
        If cancellationToken.IsCancellationRequested Then
            Console.WriteLine(""Canceled"")
            Throw New OperationCanceledException()
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException(""Stopped"")
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException()
        Else
            Console.WriteLine(""Running"")
        End If
        If cancellationToken.IsCancellationRequested Then Throw New TaskCanceledException()
    End Sub
End Class
", "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        cancellationToken.ThrowIfCancellationRequested()
        cancellationToken.ThrowIfCancellationRequested()
        If cancellationToken.IsCancellationRequested Then
            Console.WriteLine(""Canceled"")
            Throw New OperationCanceledException()
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException(""Stopped"")
        End If
        If cancellationToken.IsCancellationRequested Then
            Throw New OperationCanceledException()
        Else
            Console.WriteLine(""Running"")
        End If
        If cancellationToken.IsCancellationRequested Then Throw New TaskCanceledException()
    End Sub
End Class
",
                                                      equivalenceKey:="DCA068.ThrowIfCancellationRequested", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA068 keeps the comment lines of the removed block before the call.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsTheCommentLines() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If [|cancellationToken.IsCancellationRequested|] Then
            ' Stop here.
            Throw New OperationCanceledException()
        End If
    End Sub
End Class
", "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        ' Stop here.
        cancellationToken.ThrowIfCancellationRequested()
    End Sub
End Class
",
                                                      equivalenceKey:="DCA068.ThrowIfCancellationRequested", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA068 is not offered when it would remove a comment that follows code on its line.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_NoneWhenACommentWouldBeLost() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If cancellationToken.IsCancellationRequested Then Throw New OperationCanceledException( _ ' No token.
                )
    End Sub
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(Array.Empty(Of String)(), actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA068 keeps the comments at the end of the removed lines after the call.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsTheCommentsAtTheEndOfTheLines() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If [|cancellationToken.IsCancellationRequested|] Then ' Canceled?
            Throw New OperationCanceledException() ' Stop.
        End If ' Done.
    End Sub
End Class
", "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        cancellationToken.ThrowIfCancellationRequested() ' Canceled? ' Stop. ' Done.
    End Sub
End Class
",
                                                      equivalenceKey:="DCA068.ThrowIfCancellationRequested", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA068 does not report a statement with preprocessor directives inside, because another configuration
        ''' may compile other statements.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_StatementsWithDirectives() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading

Public Class C
    Public Sub Run(cancellationToken As CancellationToken)
        If cancellationToken.IsCancellationRequested Then
#If DEBUG Then
            Console.WriteLine(""Canceled"")
#End If
            Throw New OperationCanceledException()
        End If
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
