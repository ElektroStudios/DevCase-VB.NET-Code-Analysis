#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA045_InsecureTempFileTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA045 (Path.GetTempFileName creates a predictable temporary file) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA045_InsecureTempFileTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New InsecureTempFileAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New InsecureTempFileCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA045_InsecureTempFileTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA045 reports Path.GetTempFileName and My.Computer.FileSystem.GetTempFileName (FileIO.FileSystem).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_GetTempFileName() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Public Class C
    Public Function M() As String
        Dim first As String = [|Path.GetTempFileName()|]
        Dim second As String = [|Microsoft.VisualBasic.FileIO.FileSystem.GetTempFileName()|]
        Return first & second & Path.GetTempPath()
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA045 builds a random path and creates the file with FileMode.CreateNew, for declarations and assignments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Declaration_And_Assignment() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Public Class C
    Private lastFile As String
    Public Function M() As String
        Dim tempPath As String = Path.GetTempFileName() ' Work file.
        Me.lastFile = Path.GetTempFileName()
        Return tempPath
    End Function
End Class
", "
Imports System.IO
Public Class C
    Private lastFile As String
    Public Function M() As String
        Dim tempPath As String = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ""tmp"")) ' Work file.
        Using New FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None) ' Creates the file atomically: fails instead of opening a file that already exists.
        End Using
        Me.lastFile = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ""tmp""))
        Using New FileStream(Me.lastFile, FileMode.CreateNew, FileAccess.Write, FileShare.None) ' Creates the file atomically: fails instead of opening a file that already exists.
        End Using
        Return tempPath
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA045 declares a local variable for a returned call, avoids name conflicts, qualifies Path when a local hides it, and compiles on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Return_DeclaresAVariable_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(tempFilePath As String) As String
        Dim path As String = tempFilePath
        Return IO.Path.GetTempFileName()
    End Function
End Class
", "
Public Class C
    Public Function M(tempFilePath As String) As String
        Dim path As String = tempFilePath
        Dim tempFilePath2 As String = IO.Path.Combine(IO.Path.GetTempPath(), IO.Path.ChangeExtension(IO.Path.GetRandomFileName(), ""tmp""))
        Using New IO.FileStream(tempFilePath2, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.None) ' Creates the file atomically: fails instead of opening a file that already exists.
        End Using
        Return tempFilePath2
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA045 supports a call written without parentheses (<c>Path.GetTempFileName</c>),
        ''' which is reported on a member access instead of an invocation (regression: the fix threw and was disabled by Visual Studio).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CallWithoutParentheses() As Task
            Const Source As String = "
Imports System.IO
Public Class C
    Public Shared Function LoadFromResource(resource As Byte(), Optional deleteTempFile As Boolean = False) As Integer

        Dim tmpFilepath As String = Path.GetTempFileName

        Try
            Using fs As New FileStream(tmpFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)
                fs.Write(resource, 0, resource.Length)
            End Using
            Return resource.Length

        Finally
            If deleteTempFile AndAlso File.Exists(tmpFilepath) Then
                File.Delete(tmpFilepath)
            End If

        End Try

    End Function
    Public Function Other() As String
        Return Path.GetTempFileName
    End Function
End Class
"
            Const Expected As String = "
Imports System.IO
Public Class C
    Public Shared Function LoadFromResource(resource As Byte(), Optional deleteTempFile As Boolean = False) As Integer

        Dim tmpFilepath As String = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ""tmp""))
        Using New FileStream(tmpFilepath, FileMode.CreateNew, FileAccess.Write, FileShare.None) ' Creates the file atomically: fails instead of opening a file that already exists.
        End Using

        Try
            Using fs As New FileStream(tmpFilepath, FileMode.Create, FileAccess.Write, FileShare.Read)
                fs.Write(resource, 0, resource.Length)
            End Using
            Return resource.Length

        Finally
            If deleteTempFile AndAlso File.Exists(tmpFilepath) Then
                File.Delete(tmpFilepath)
            End If

        End Try

    End Function
    Public Function Other() As String
        Dim tempFilePath As String = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), ""tmp""))
        Using New FileStream(tempFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None) ' Creates the file atomically: fails instead of opening a file that already exists.
        End Using
        Return tempFilePath
    End Function
End Class
"
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, Source, Expected, targetNetFramework:=True).ConfigureAwait(True)

            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, Source, NameOf(InsecureTempFileCodeFixProvider),
                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual(Expected, fixedText)
        End Function

        ''' <summary>
        ''' Verifies at run time that the code generated by the fix creates a new, empty .tmp file in the temporary folder, as GetTempFileName does.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CreatesAnEmptyFile_LikeGetTempFileName() As Task
            Dim tempPath As String = IO.Path.Combine(IO.Path.GetTempPath(), IO.Path.ChangeExtension(IO.Path.GetRandomFileName(), "tmp"))
            Using New IO.FileStream(tempPath, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.None)
            End Using
            Try
                Assert.True(IO.File.Exists(tempPath))
                Assert.Equal(0L, New IO.FileInfo(tempPath).Length)
                Assert.Equal(".tmp", IO.Path.GetExtension(tempPath))
                Assert.Throws(Of IO.IOException)(Sub()
                                                     Using New IO.FileStream(tempPath, IO.FileMode.CreateNew, IO.FileAccess.Write, IO.FileShare.None)
                                                     End Using
                                                 End Sub)
            Finally
                IO.File.Delete(tempPath)
            End Try
            Await Task.CompletedTask.ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA045 reports a call passed as an argument without offering a fix.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function NoFix_ForOtherUsages() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Function M() As Integer
        Return IO.Path.GetTempFileName().Length
    End Function
End Class
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

#End Region

    End Class

End Namespace

#End Region
