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

#Region " DCA011_ThrowHelperMigrationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA011 (.NET Framework guards paired with modern throw helpers) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA011_ThrowHelperMigrationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThrowHelperMigrationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ThrowHelperMigrationCodeFixProvider()

        ''' <summary>
        ''' The source shared by the DCA011 tests: guard clauses that have an exact throw helper equivalent.
        ''' </summary>
        Private Const GuardsSource As String = "
Public Class C
    Public Shared Function ProcessFile(filePath As String, encoding As System.Text.Encoding,
                                       ParamArray rewriters As Object()) As Boolean

        If String.IsNullOrWhiteSpace(filePath) Then
            Throw New ArgumentNullException(NameOf(filePath))
        End If
        If encoding Is Nothing Then
            Throw New ArgumentNullException(NameOf(encoding))
        End If
        If rewriters Is Nothing Then
            Throw New ArgumentNullException(NameOf(rewriters))
        End If

        Return True
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA011_ThrowHelperMigrationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA011 reports one diagnostic per run of consecutive guards on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_OneDiagnosticPerRun_OnNetFramework() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(name As String, items As Object(), count As Integer, limit As Integer)
        [|If String.IsNullOrEmpty(name) Then Throw New ArgumentNullException(NameOf(name))
        If items Is Nothing Then Throw New ArgumentNullException(NameOf(items))|]
        System.Console.WriteLine(name)
        [|If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count))
        If count > limit Then
            Throw New ArgumentOutOfRangeException(NameOf(count))
        End If|]
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
            Assert.Contains("ArgumentException.ThrowIfNullOrEmpty, ArgumentNullException.ThrowIfNull", diagnostics(0).GetMessage(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA011 does not report guards on modern .NET, where the throw helpers can be used directly.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernDotNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, DCA011_ThrowHelperMigrationTests.GuardsSource).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA011 does not report guards without an exact throw helper equivalent, or already inside a conditional block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_NonEquivalentOrAlreadyConditionalGuards() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private field As Object
    Public Sub Run(name As String, value As Object, other As Object, ByRef output As Object, count As Integer)
        If name = Nothing Then Throw New ArgumentNullException(NameOf(name))
        If value Is Nothing Then Throw New ArgumentNullException(""Custom message."", New InvalidOperationException())
        If value Is Nothing Then Throw New ArgumentNullException(""Value"")
        If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count), count \ 2, ""Negative."")
        If value Is Nothing Then Throw New ArgumentNullException(NameOf(other))
        If value Is Nothing Then Throw New ArgumentException(NameOf(value))
        If Me.field Is Nothing Then Throw New ArgumentNullException(NameOf(value))
        If output Is Nothing Then Throw New ArgumentNullException(NameOf(output))
        If count < count \ 2 Then Throw New ArgumentOutOfRangeException(NameOf(count))
        If value Is Nothing Then
            Throw New ArgumentNullException(NameOf(value))
        Else
            System.Console.WriteLine(value)
        End If
#If DEBUG Then
        If other Is Nothing Then Throw New ArgumentNullException(NameOf(other))
#End If
        Dim action As Action = Sub()
                                   If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
                               End Sub
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA011 wraps the guards in a conditional compilation block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_WrapsRunInConditionalBlock() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, DCA011_ThrowHelperMigrationTests.GuardsSource, "
Public Class C
    Public Shared Function ProcessFile(filePath As String, encoding As System.Text.Encoding,
                                       ParamArray rewriters As Object()) As Boolean

#If NETCOREAPP Then
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath)
        ArgumentNullException.ThrowIfNull(encoding)
        ArgumentNullException.ThrowIfNull(rewriters)
#Else
        If String.IsNullOrWhiteSpace(filePath) Then
            Throw New ArgumentNullException(NameOf(filePath))
        End If
        If encoding Is Nothing Then
            Throw New ArgumentNullException(NameOf(encoding))
        End If
        If rewriters Is Nothing Then
            Throw New ArgumentNullException(NameOf(rewriters))
        End If
#End If

        Return True
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA011 converts the guards whose exception uses named arguments 
        ''' (<c>paramName:=</c>, in any order), a custom message, a literal parameter name or the parameter as actual value, 
        ''' and whose condition uses a named argument (<c>String.IsNullOrEmpty(value:=p)</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NamedArgumentsAndCustomMessages() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run(icon As String, command As String, value As Object, text As String, count As Integer)
        If String.IsNullOrEmpty(icon) Then
            Throw New ArgumentNullException(NameOf(icon), $""The {NameOf(icon)} cannot be null or empty."")
        End If
        If String.IsNullOrWhiteSpace(command) Then
            Throw New ArgumentNullException(paramName:=NameOf(command))
        End If
        If value Is Nothing Then Throw New ArgumentNullException(message:=""Missing."", paramName:=NameOf(value))
        If String.IsNullOrEmpty(value:=text) Then Throw New ArgumentNullException(""text"")
        If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count), count, ""Negative."")
        Console.WriteLine(count)
    End Sub
End Class
", "
Public Class C
    Public Sub Run(icon As String, command As String, value As Object, text As String, count As Integer)
#If NETCOREAPP Then
        ArgumentException.ThrowIfNullOrEmpty(icon)
        ArgumentException.ThrowIfNullOrWhiteSpace(command)
        ArgumentNullException.ThrowIfNull(value)
        ArgumentException.ThrowIfNullOrEmpty(text)
        ArgumentOutOfRangeException.ThrowIfNegative(count)
#Else
        If String.IsNullOrEmpty(icon) Then
            Throw New ArgumentNullException(NameOf(icon), $""The {NameOf(icon)} cannot be null or empty."")
        End If
        If String.IsNullOrWhiteSpace(command) Then
            Throw New ArgumentNullException(paramName:=NameOf(command))
        End If
        If value Is Nothing Then Throw New ArgumentNullException(message:=""Missing."", paramName:=NameOf(value))
        If String.IsNullOrEmpty(value:=text) Then Throw New ArgumentNullException(""text"")
        If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count), count, ""Negative."")
#End If
        Console.WriteLine(count)
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA011 converts the two reported guards: a custom message built with 
        ''' <see langword="NameOf"/> (kept in the .NET Framework branch), and a range check against an expression of other 
        ''' variables (<c>count &gt; sourceCount - startIndex</c> → <c>ThrowIfGreaterThan(count, sourceCount - startIndex)</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ReportedGuards_CustomMessageAndRangeExpression() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Shared Sub SetCulture(cultureName As String)
        If String.IsNullOrEmpty(cultureName) Then
            Throw New ArgumentNullException(NameOf(cultureName), $""The {NameOf(cultureName)} cannot be null or empty."")
        End If
        Console.WriteLine(cultureName)
    End Sub
    Public Shared Function Take(source As String(), startIndex As Integer, count As Integer) As Integer
        Dim sourceCount As Integer = source.Length
        If count > sourceCount - startIndex Then
            Throw New ArgumentOutOfRangeException(NameOf(count))
        End If
        Return count
    End Function
End Class
", "
Public Class C
    Public Shared Sub SetCulture(cultureName As String)
#If NETCOREAPP Then
        ArgumentException.ThrowIfNullOrEmpty(cultureName)
#Else
        If String.IsNullOrEmpty(cultureName) Then
            Throw New ArgumentNullException(NameOf(cultureName), $""The {NameOf(cultureName)} cannot be null or empty."")
        End If
#End If
        Console.WriteLine(cultureName)
    End Sub
    Public Shared Function Take(source As String(), startIndex As Integer, count As Integer) As Integer
        Dim sourceCount As Integer = source.Length
#If NETCOREAPP Then
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, sourceCount - startIndex)
#Else
        If count > sourceCount - startIndex Then
            Throw New ArgumentOutOfRangeException(NameOf(count))
        End If
#End If
        Return count
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA011 uses the range helpers and the configured compilation symbol.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RangeHelpers_AndConfiguredCondition() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Sub Run(index As Integer, size As Long, ratio As Double, maximum As Integer)
        ' Validate.
        If index < 0 Then Throw New ArgumentOutOfRangeException(NameOf(index))
        If 0 >= size Then Throw New ArgumentOutOfRangeException(NameOf(size))
        If ratio = 0 Then Throw New ArgumentOutOfRangeException(NameOf(ratio))
        If index > maximum Then Throw New ArgumentOutOfRangeException(NameOf(index)) ' upper bound
        If index <= maximum - 10 Then Throw New ArgumentOutOfRangeException(NameOf(index))
    End Sub
End Class
", "
Public Class C
    Public Sub Run(index As Integer, size As Long, ratio As Double, maximum As Integer)
        ' Validate.
#If NET8_0_OR_GREATER Then
        ArgumentOutOfRangeException.ThrowIfNegative(index)
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size)
        ArgumentOutOfRangeException.ThrowIfZero(ratio)
        ArgumentOutOfRangeException.ThrowIfGreaterThan(index, maximum)
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(index, maximum - 10)
#Else
        If index < 0 Then Throw New ArgumentOutOfRangeException(NameOf(index))
        If 0 >= size Then Throw New ArgumentOutOfRangeException(NameOf(size))
        If ratio = 0 Then Throw New ArgumentOutOfRangeException(NameOf(ratio))
        If index > maximum Then Throw New ArgumentOutOfRangeException(NameOf(index)) ' upper bound
        If index <= maximum - 10 Then Throw New ArgumentOutOfRangeException(NameOf(index))
#End If
    End Sub
End Class
", editorConfig:="devcase_analyzers.dca011_condition = NET8_0_OR_GREATER", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the modern branch written by the code fix of rule DCA011 compiles on .NET.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixedCode_CompilesOnModernDotNet_WhenConditionIsDefined() As Task
            ' The NETCOREAPP branch must compile on .NET (where the throw helpers exist).
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, DCA011_ThrowHelperMigrationTests.GuardsSource,
                                                                                       NameOf(ThrowHelperMigrationCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            Dim project As Project = AnalyzerTestHost.CreateProject(fixedText)
            Dim options As VisualBasic.VisualBasicParseOptions = DirectCast(project.ParseOptions, VisualBasic.VisualBasicParseOptions).
                WithPreprocessorSymbols(New KeyValuePair(Of String, Object)("NETCOREAPP", True))
            project = project.WithParseOptions(options)

            Await AnalyzerTestHost.AssertCompilesAsync(project, "NETCOREAPP branch").ConfigureAwait(True)
            Assert.Contains("ArgumentNullException.ThrowIfNull(encoding)", fixedText, StringComparison.Ordinal)
        End Function

#End Region

    End Class

End Namespace

#End Region
