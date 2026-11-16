#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.IO
Imports System.Linq
Imports System.Reflection
Imports System.Text
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA026_EncodingTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA026 (text encoding not specified) and its three code fixes.
    ''' </summary>
    Public NotInheritable Class DCA026_EncodingTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New EncodingAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New EncodingCodeFixProvider()

        ''' <summary>
        ''' The equivalence key of the UTF-8 without BOM action.
        ''' </summary>
        Private Const Utf8WithoutBomKey As String = "EncodingCodeFixProvider.Utf8WithoutBom"

        ''' <summary>
        ''' The equivalence key of the UTF-8 with BOM action.
        ''' </summary>
        Private Const Utf8WithBomKey As String = "EncodingCodeFixProvider.Utf8WithBom"

        ''' <summary>
        ''' The equivalence key of the <c>Encoding.Default</c> action.
        ''' </summary>
        Private Const DefaultKey As String = "EncodingCodeFixProvider.Default"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA026_EncodingTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA026 reports text APIs that have an overload with an encoding, and skips calls that already pass one.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CallsWithAnEncodingOverload() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Imports System.Text
Public Class C
    Public Sub M(path As String, text As String, stream As Stream)
        [|File.WriteAllText(path, text)|]
        Dim a As String = [|File.ReadAllText(path)|]
        [|File.AppendAllText(path, text)|]
        Dim b As String() = [|File.ReadAllLines(path)|]
        Using writer As StreamWriter = [|New StreamWriter(path)|]
        End Using
        Using reader As StreamReader = [|New StreamReader(stream, True)|]
        End Using
        File.WriteAllText(path, text, Encoding.UTF8)
        Dim c As Byte() = Encoding.UTF8.GetBytes(text)
        Console.WriteLine(text)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the first action of rule DCA026 adds UTF-8 without BOM to the <c>System.IO</c> APIs, appending or inserting the argument.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Utf8WithoutBom_AppendsOrInserts() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Public Class C
    Public Sub M(path As String, text As String, stream As Stream)
        File.WriteAllText(path, text)
        Using reader As New StreamReader(stream, True)
        End Using
    End Sub
End Class
", "
Imports System.IO
Public Class C
    Public Sub M(path As String, text As String, stream As Stream)
        File.WriteAllText(path, text, New System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
        Using reader As New StreamReader(stream, New System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier:=False), True)
        End Using
    End Sub
End Class
", equivalenceKey:=Utf8WithoutBomKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA026 fills <c>append</c> with <see langword="False"/> when the overload requires it
        ''' (<c>New StreamWriter(path)</c> becomes <c>New StreamWriter(path, False, encoding)</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_StreamWriter_FillsAppendWithFalse() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Imports System.Text
Public Class C
    Public Sub M(path As String)
        Using writer As New StreamWriter(path)
        End Using
    End Sub
End Class
", "
Imports System.IO
Imports System.Text
Public Class C
    Public Sub M(path As String)
        Using writer As New StreamWriter(path, False, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
        End Using
    End Sub
End Class
", equivalenceKey:=Utf8WithoutBomKey, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>Encoding.UTF8</c> and <c>Encoding.Default</c> actions of rule DCA026 write the expected expression,
        ''' and that the code compiles on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_OtherChoices_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Imports System.Text
Public Class C
    Public Function M(path As String) As String
        Return File.ReadAllText(path)
    End Function
End Class
", "
Imports System.IO
Imports System.Text
Public Class C
    Public Function M(path As String) As String
        Return File.ReadAllText(path, Encoding.UTF8)
    End Function
End Class
", equivalenceKey:=Utf8WithBomKey, targetNetFramework:=True).ConfigureAwait(True)

            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.IO
Imports System.Text
Public Class C
    Public Function M(path As String) As String
        Return File.ReadAllText(path:=path)
    End Function
End Class
", "
Imports System.IO
Imports System.Text
Public Class C
    Public Function M(path As String) As String
        Return File.ReadAllText(path:=path, encoding:=Encoding.Default)
    End Function
End Class
", equivalenceKey:=DefaultKey, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the order of the actions of rule DCA026: the encoding that the call already uses comes first.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ImplicitEncodingIsOfferedFirst() As Task
            Dim systemIo As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Sub M(path As String, text As String)
        IO.File.WriteAllText(path, text)
    End Sub
End Class
").ConfigureAwait(True)
            Assert.Equal(Utf8WithoutBomKey, systemIo(0).EquivalenceKey)
            Assert.Contains("keeps the current behavior", systemIo(0).Title, StringComparison.Ordinal)
            Assert.Equal(DefaultKey, systemIo(2).EquivalenceKey)

            Dim visualBasicIo As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Sub M(path As String, text As String)
        Microsoft.VisualBasic.FileIO.FileSystem.WriteAllText(path, text, False)
    End Sub
End Class
").ConfigureAwait(True)
            Assert.Equal(Utf8WithBomKey, visualBasicIo(0).EquivalenceKey)
            Assert.Contains("keeps the current behavior", visualBasicIo(0).Title, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies, at run time, the implicit encodings that the first action of rule DCA026 writes: 
        ''' <c>File.WriteAllText</c> writes UTF-8 without BOM, and <c>Microsoft.VisualBasic.FileIO.FileSystem.WriteAllText</c> writes UTF-8 with BOM.
        ''' </summary>
        ''' 
        ''' <remarks>
        ''' The overloads without an encoding are invoked through reflection, so this file does not contain the calls that the rule reports.
        ''' </remarks>
        <Fact>
        Public Sub ImplicitEncodings_MatchTheFirstAction()
            Const Text As String = "Año ñandú €"
            Dim directory As String = Path.Combine(Path.GetTempPath(), $"DCA026_{Guid.NewGuid():N}")
            IO.Directory.CreateDirectory(directory)
            Try
                Dim implicitIo As String = Path.Combine(directory, "implicit-io.txt")
                Dim explicitIo As String = Path.Combine(directory, "explicit-io.txt")
                Dim writeAllText As MethodInfo = GetType(File).GetMethod(NameOf(File.WriteAllText), {GetType(String), GetType(String)})
                Assert.NotNull(writeAllText)
                writeAllText.Invoke(Nothing, {implicitIo, Text})
                File.WriteAllText(explicitIo, Text, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
                Assert.Equal(File.ReadAllBytes(explicitIo), File.ReadAllBytes(implicitIo))

                Dim implicitVb As String = Path.Combine(directory, "implicit-vb.txt")
                Dim explicitVb As String = Path.Combine(directory, "explicit-vb.txt")
                Dim vbWriteAllText As MethodInfo = GetType(Microsoft.VisualBasic.FileIO.FileSystem).GetMethod(
                    NameOf(Microsoft.VisualBasic.FileIO.FileSystem.WriteAllText), {GetType(String), GetType(String), GetType(Boolean)})
                Assert.NotNull(vbWriteAllText)
                vbWriteAllText.Invoke(Nothing, {implicitVb, Text, False})
                File.WriteAllText(explicitVb, Text, Encoding.UTF8)
                Assert.Equal(File.ReadAllBytes(explicitVb), File.ReadAllBytes(implicitVb))
            Finally
                IO.Directory.Delete(directory, recursive:=True)
            End Try
        End Sub

#End Region

    End Class

End Namespace

#End Region
