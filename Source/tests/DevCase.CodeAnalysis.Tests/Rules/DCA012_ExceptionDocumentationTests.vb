#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA012 (undocumented thrown exceptions) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA012_ExceptionDocumentationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ExceptionDocumentationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ExceptionDocumentationCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA012_ExceptionDocumentationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA012 reports undocumented exceptions thrown explicitly or through throw helpers.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ThrowsAndThrowHelpers() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    ''' <summary>Run.</summary>
    Public Sub [|Run|](value As Object, name As String, count As Integer)
        If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
        ArgumentException.ThrowIfNullOrWhiteSpace(name)
        ArgumentOutOfRangeException.ThrowIfNegative(count)
    End Sub
    ''' <summary>Value.</summary>
    Public Property [|Value|] As Integer
        Get
            Return 0
        End Get
        Set(value As Integer)
            Throw New InvalidOperationException(""Read-only."")
        End Set
    End Property
End Class
").ConfigureAwait(True)
            Assert.Contains("ArgumentNullException, ArgumentException, ArgumentOutOfRangeException", diagnostics(0).GetMessage(), StringComparison.Ordinal)
            Assert.Contains("InvalidOperationException", diagnostics(1).GetMessage(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA012 does not report documented, caught or ignored exceptions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_DocumentedCaughtOrIgnoredThrows() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    ''' <summary>Documented.</summary>
    ''' <exception cref=""ArgumentNullException"">Null.</exception>
    ''' <exception cref=""T:System.InvalidOperationException""/>
    Public Sub A(value As Object)
        If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
        Throw New System.InvalidOperationException()
    End Sub
    ''' <summary>Caught.</summary>
    Public Sub B()
        Try
            Throw New ArgumentNullException(""x"")
        Catch ex As ArgumentException
        End Try
        Try
            Throw New FormatException()
        Catch
        End Try
    End Sub
    ''' <summary>Ignored.</summary>
    Public Sub D()
        Dim action As Action = Sub() Throw New InvalidOperationException()
        Try
            Throw New NotImplementedException()
        Catch ex As Exception When ex.Message IsNot Nothing
            Throw
        End Try
    End Sub
    ''' <inheritdoc/>
    Public Overrides Function ToString() As String
        Throw New InvalidOperationException()
    End Function
    Public Sub Undocumented()
        Throw New InvalidOperationException()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA012 reports exceptions that a filtered or unrelated <c>Catch</c> block does not handle.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ThrowNotCaughtByFilteredOrUnrelatedCatch() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    ''' <summary>Run.</summary>
    Public Sub [|Run|]()
        Try
            Throw New InvalidOperationException()
        Catch ex As ArgumentException
        Catch ex As Exception When ex.Data.Count > 0
        End Try
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA012 adds empty single-line <c>exception</c> tags before the <c>example</c> section.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AddsEmptyExceptionTags_BeforeExample() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C

    ''' <summary>
    ''' Runs.
    ''' </summary>
    '''
    ''' <param name=""value"">
    ''' The value.
    ''' </param>
    '''
    ''' <example>
    ''' <code>Run(1)</code>
    ''' </example>
    Public Sub Run(value As String)
        ArgumentException.ThrowIfNullOrEmpty(value)
    End Sub

    ''' <summary>Count.</summary>
    Public Function Count() As Integer
        Throw New System.IO.IOException()
    End Function

End Class
", "
Public Class C

    ''' <summary>
    ''' Runs.
    ''' </summary>
    '''
    ''' <param name=""value"">
    ''' The value.
    ''' </param>
    '''
    ''' <exception cref=""ArgumentNullException""></exception>
    ''' <exception cref=""ArgumentException""></exception>
    '''
    ''' <example>
    ''' <code>Run(1)</code>
    ''' </example>
    Public Sub Run(value As String)
        ArgumentException.ThrowIfNullOrEmpty(value)
    End Sub

    ''' <summary>Count.</summary>
    '''
    ''' <exception cref=""IO.IOException""></exception>
    Public Function Count() As Integer
        Throw New System.IO.IOException()
    End Function

End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA012 reuses a trailing blank line as the separator and leaves none after the tags.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReusesTrailingBlankLine_WithoutDanglingSeparator() As Task
            ' Reported scenario: a trailing blank documentation line becomes the separator and never remains after the group.
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C

    ''' <summary>
    ''' Displays the welcome banner.
    ''' </summary>
    '''
    Private Sub DisplayWelcomeBanner(arg1 As String, arg2 As Decimal)
        ArgumentException.ThrowIfNullOrEmpty(arg1)
        If arg2 < 0 Then
            Throw New Exception()
        End If
    End Sub

End Class
", "
Public Class C

    ''' <summary>
    ''' Displays the welcome banner.
    ''' </summary>
    '''
    ''' <exception cref=""ArgumentNullException""></exception>
    ''' <exception cref=""ArgumentException""></exception>
    ''' <exception cref=""Exception""></exception>
    Private Sub DisplayWelcomeBanner(arg1 As String, arg2 As Decimal)
        ArgumentException.ThrowIfNullOrEmpty(arg1)
        If arg2 < 0 Then
            Throw New Exception()
        End If
    End Sub

End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA012 removes extra blank lines between the tags and the declaration.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RemovesExtraTrailingBlankLines() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    ''' <summary>Runs.</summary>
    '''
    '''
    '''
    Public Sub Run()
        Throw New InvalidOperationException()
    End Sub
End Class
", "
Public Class C
    ''' <summary>Runs.</summary>
    '''
    ''' <exception cref=""InvalidOperationException""></exception>
    Public Sub Run()
        Throw New InvalidOperationException()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA012 appends new tags to an existing group of single-line <c>exception</c> tags.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_AppendsToExistingSingleLineExceptionGroup() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    ''' <summary>Runs.</summary>
    '''
    ''' <exception cref=""InvalidOperationException""></exception>
    Public Sub Run(value As Integer)
        If value = 0 Then
            Throw New InvalidOperationException()
        End If
        Throw New NotSupportedException()
    End Sub
End Class
", "
Public Class C
    ''' <summary>Runs.</summary>
    '''
    ''' <exception cref=""InvalidOperationException""></exception>
    ''' <exception cref=""NotSupportedException""></exception>
    Public Sub Run(value As Integer)
        If value = 0 Then
            Throw New InvalidOperationException()
        End If
        Throw New NotSupportedException()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA012 adds one blank line before the tags when none exists.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NoBlankLine_AddsSingleLeadingSeparator() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    ''' <summary>Runs.</summary>
    Public Sub Run()
        Throw New InvalidOperationException()
    End Sub
End Class
", "
Public Class C
    ''' <summary>Runs.</summary>
    '''
    ''' <exception cref=""InvalidOperationException""></exception>
    Public Sub Run()
        Throw New InvalidOperationException()
    End Sub
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace
