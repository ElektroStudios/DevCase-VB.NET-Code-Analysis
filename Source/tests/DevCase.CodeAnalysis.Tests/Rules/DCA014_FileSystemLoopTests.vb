#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA014 (file-system loops without Thread.CurrentThread.Join(0)) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA014_FileSystemLoopTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New FileSystemLoopAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New FileSystemLoopCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA014_FileSystemLoopTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA014 reports loops over file-system enumeration methods.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_LoopsOverEnumerationMethods() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Imports System.Linq
Public Class C
    Public Sub Run(root As String)
        [|For Each file As String In Directory.GetFiles(root)|]
            Console.WriteLine(file)
        Next file
        [|For Each file As String In Directory.EnumerateFiles(root, ""*.vb"", SearchOption.AllDirectories).Where(Function(f As String) f.Length > 3)|]
        Next file
        [|For Each info As DirectoryInfo In New DirectoryInfo(root).EnumerateDirectories()|]
        Next info
        Dim entries As String() = Directory.GetFileSystemEntries(root)
        [|For Each entry As String In entries|]
        Next entry
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA014 does not report loops that already pump messages, or loops over other sources.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_LoopsWithJoinOrOtherSources() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Imports System.Threading
Public Class C
    Public Sub Run(root As String, names As String())
        For Each file As String In Directory.GetFiles(root)
            Thread.CurrentThread.Join(0)
        Next file
        For Each file As String In Directory.EnumerateFiles(root)
            If file.Length > 0 Then
                System.Threading.Thread.CurrentThread.Join(0)
            End If
        Next file
        For Each name As String In names
        Next name
        For Each line As String In File.ReadAllLines(root)
        Next line
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA014 inserts <c>Thread.CurrentThread.Join(0)</c> with its explanatory comment.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InsertsJoinWithComment() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Imports System.Threading
Public Class C
    Public Sub Run(root As String)
        For Each file As String In Directory.GetFiles(root)
            Console.WriteLine(file)
        Next file
    End Sub
End Class
", "
Imports System.IO
Imports System.Threading
Public Class C
    Public Sub Run(root As String)
        For Each file As String In Directory.GetFiles(root)
#If DEBUG Then
            Thread.CurrentThread.Join(0) ' Pumps pending messages to prevent ContextSwitchDeadlock during long-running file system enumerations.
#End If
            Console.WriteLine(file)
        Next file
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA014 qualifies <c>Thread</c> when its namespace is not imported, and handles empty loops.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_QualifiesThreadWhenNotImported_AndHandlesEmptyLoops() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run(root As String)
        For Each file As String In IO.Directory.EnumerateFiles(root)
        Next file
    End Sub
End Class
", "
Public Class C
    Public Sub Run(root As String)
        For Each file As String In IO.Directory.EnumerateFiles(root)
#If DEBUG Then
            Threading.Thread.CurrentThread.Join(0) ' Pumps pending messages to prevent ContextSwitchDeadlock during long-running file system enumerations.
#End If
        Next file
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA014 does not report a loop whose <c>Join(0)</c> call is inside an inactive <c>#If DEBUG Then</c> block
        ''' (a Release build, where the call is only disabled text).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_JoinInsideInactiveDebugBlock() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Imports System.Threading
Public Class C
    Public Sub Run(root As String)
        For Each file As String In Directory.GetFiles(root)
#If DEBUG Then
            Thread.CurrentThread.Join(0)
#End If
            Console.WriteLine(file)
        Next file
        For Each folder As String In Directory.GetDirectories(root)
#If DEBUG Then
            System.Threading.Thread.CurrentThread.Join(0) ' Qualified.
#End If
        Next folder
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA014 does not report a loop whose <c>Join(0)</c> call is inside an active <c>#If DEBUG Then</c> block
        ''' (a Debug build).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_JoinInsideActiveDebugBlock() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject("
Imports System.IO
Imports System.Threading
Public Class C
    Public Sub Run(root As String)
        For Each file As String In Directory.GetFiles(root)
#If DEBUG Then
            Thread.CurrentThread.Join(0)
#End If
        Next file
    End Sub
End Class
")
            project = project.WithParseOptions(
                DirectCast(project.ParseOptions, Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions).
                WithPreprocessorSymbols(New KeyValuePair(Of String, Object)("DEBUG", True)))
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA014 still reports a loop whose inactive conditional text does not contain the call.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_WhenInactiveTextHasNoJoinCall() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Public Class C
    Public Sub Run(root As String)
        [|For Each file As String In Directory.GetFiles(root)|]
#If DEBUG Then
            Console.WriteLine(file)
#End If
        Next file
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace
