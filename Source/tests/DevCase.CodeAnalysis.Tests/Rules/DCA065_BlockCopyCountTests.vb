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

#Region " DCA065_BlockCopyCountTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA065 (BlockCopy count in elements, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA065_BlockCopyCountTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New BlockCopyCountAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA065_BlockCopyCountTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA065 reports the <c>Length</c> of the source or destination array as the byte count, and ignores
        ''' arrays of one-byte elements, other arrays and computed counts.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_LengthOfACopiedArrayOfWideElements() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Copy(source As Integer(), target As Integer(), bytes As Byte(), other As Integer(), flags As Boolean())
        Buffer.BlockCopy(source, 0, target, 0, [|source.Length|])
        Buffer.BlockCopy(source, 0, target, 0, [|target.Length|])
        Buffer.BlockCopy(bytes, 0, target, 0, bytes.Length)
        Buffer.BlockCopy(flags, 0, bytes, 0, flags.Length)
        Buffer.BlockCopy(source, 0, target, 0, other.Length)
        Buffer.BlockCopy(source, 0, target, 0, source.Length * 4)
        Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA065 does not run in a modern .NET project, where the built-in rule CA2018 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Copy(source As Integer(), target As Integer(), bytes As Byte(), other As Integer(), flags As Boolean())
        Buffer.BlockCopy(source, 0, target, 0, source.Length)
        Buffer.BlockCopy(source, 0, target, 0, target.Length)
        Buffer.BlockCopy(bytes, 0, target, 0, bytes.Length)
        Buffer.BlockCopy(flags, 0, bytes, 0, flags.Length)
        Buffer.BlockCopy(source, 0, target, 0, other.Length)
        Buffer.BlockCopy(source, 0, target, 0, source.Length * 4)
        Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA065 writes <c>Buffer.ByteLength(array)</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesByteLength() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Copy(source As Integer(), target As Integer(), bytes As Byte(), other As Integer(), flags As Boolean())
        Buffer.BlockCopy(source, 0, target, 0, [|source.Length|])
        Buffer.BlockCopy(source, 0, target, 0, [|target.Length|])
        Buffer.BlockCopy(bytes, 0, target, 0, bytes.Length)
        Buffer.BlockCopy(flags, 0, bytes, 0, flags.Length)
        Buffer.BlockCopy(source, 0, target, 0, other.Length)
        Buffer.BlockCopy(source, 0, target, 0, source.Length * 4)
        Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))
    End Sub
End Class
", "
Public Class C
    Public Sub Copy(source As Integer(), target As Integer(), bytes As Byte(), other As Integer(), flags As Boolean())
        Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))
        Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(target))
        Buffer.BlockCopy(bytes, 0, target, 0, bytes.Length)
        Buffer.BlockCopy(flags, 0, bytes, 0, flags.Length)
        Buffer.BlockCopy(source, 0, target, 0, other.Length)
        Buffer.BlockCopy(source, 0, target, 0, source.Length * 4)
        Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))
    End Sub
End Class
",
                                                      equivalenceKey:="DCA065.ByteLength", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
