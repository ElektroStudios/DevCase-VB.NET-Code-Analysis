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

#Region " DCA084_ConstantArrayArgumentTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA084 (constant array argument, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA084_ConstantArrayArgumentTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ConstantArrayArgumentAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

        ''' <summary>
        ''' The expected field names of <see cref="FieldNames_AreUniqueAcrossTheFilesOfAPartialType"/>.
        ''' </summary>
        Private Shared ReadOnly PartialTypeNames As String() = {"ParseSplit0", "ParseSplit1"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA084_ConstantArrayArgumentTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA084 reports explicit arrays of constants passed as arguments (array creations and literals).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ArraysOfConstants() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function FindSeparator(text As String) As Integer
        Return text.IndexOfAny([|New Char() {"",""c, "";""c}|])
    End Function

    Public Function Split(text As String) As String()
        Dim first As String() = text.Split([|New Char() {"",""c}|])
        Return text.Split([|{"";""c}|], StringSplitOptions.None)
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA084 does not run in a modern .NET project, where the built-in rule CA1861 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function FindSeparator(text As String) As Integer
        Return text.IndexOfAny(New Char() {"",""c, "";""c})
    End Function

    Public Function Split(text As String) As String()
        Dim first As String() = text.Split(New Char() {"",""c})
        Return text.Split({"";""c}, StringSplitOptions.None)
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA084 does not report arrays in the <see langword="Shared"/> constructor and <see langword="Shared"/>
        ''' initializers, <see langword="ParamArray"/> arguments, empty arrays, arrays with variables, arrays passed to the methods of
        ''' <see cref="Array"/>, nor <see langword="ByRef"/> arguments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_CodeThatRunsOnceAndOtherArrays() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private Shared ReadOnly Separators As Char() = ""a,b"".ToCharArray()
    Private Shared ReadOnly Parts As String() = ""a,b"".Split(New Char() {"",""c})

    Shared Sub New()
        Console.WriteLine(String.Join("","", New String() {""a"", ""b""}))
    End Sub

    Public Sub Run(text As String, separator As Char)
        Dim a As String() = text.Split("",""c, "";""c)
        Dim b As String() = text.Split(New Char() {})
        Dim c As String() = text.Split(New Char() {separator})
        Dim d As Integer = Array.IndexOf(New Integer() {1, 2, 3}, 2)
        Dim values As Integer() = {1, 2}
        Me.Change(values)
    End Sub

    Private Sub Change(ByRef values As Integer())
        values = Nothing
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA084 declares a <c>Private Shared ReadOnly</c> field before the member, named after the
        ''' member, the called method and a number (the reported example: <c>FindSeparatorIndexOfAny0</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ExtractsSharedReadOnlyFields_NamedAfterMemberAndMethod() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function FindSeparator(text As String) As Integer
        Return text.IndexOfAny([|New Char() {"",""c, "";""c}|])
    End Function

    Public Function Split(text As String) As String()
        Dim first As String() = text.Split([|New Char() {"",""c}|])
        Return text.Split([|{"";""c}|], StringSplitOptions.None)
    End Function
End Class
", "
Public Class C
    Private Shared ReadOnly FindSeparatorIndexOfAny0 As Char() = {"",""c, "";""c}

    Public Function FindSeparator(text As String) As Integer
        Return text.IndexOfAny(FindSeparatorIndexOfAny0)
    End Function

    Private Shared ReadOnly SplitSplit0 As Char() = {"",""c}

    Private Shared ReadOnly SplitSplit1 As Char() = {"";""c}

    Public Function Split(text As String) As String()
        Dim first As String() = text.Split(SplitSplit0)
        Return text.Split(SplitSplit1, StringSplitOptions.None)
    End Function
End Class
",
                                                      equivalenceKey:="DCA084.Extract", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA084 declares one field per array with different names.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_DeclaresEveryFieldOnce() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public Class C
    Public Function FindSeparator(text As String) As Integer
        Return text.IndexOfAny(New Char() {"",""c, "";""c})
    End Function

    Public Function Split(text As String) As String()
        Dim first As String() = text.Split(New Char() {"",""c})
        Return text.Split({"";""c}, StringSplitOptions.None)
    End Function
End Class
",
                                                                                       "DCA084.Extract", targetNetFramework:=True).ConfigureAwait(True)
            AnalyzerTestHost.AssertTextEqual("
Public Class C
    Private Shared ReadOnly FindSeparatorIndexOfAny0 As Char() = {"",""c, "";""c}

    Public Function FindSeparator(text As String) As Integer
        Return text.IndexOfAny(FindSeparatorIndexOfAny0)
    End Function

    Private Shared ReadOnly SplitSplit0 As Char() = {"",""c}

    Private Shared ReadOnly SplitSplit1 As Char() = {"";""c}

    Public Function Split(text As String) As String()
        Dim first As String() = text.Split(SplitSplit0)
        Return text.Split(SplitSplit1, StringSplitOptions.None)
    End Function
End Class
", fixedText)
            Dim project As Microsoft.CodeAnalysis.Project = AnalyzerTestHost.CreateProject(fixedText, targetNetFramework:=True)
            Await AnalyzerTestHost.AssertCompilesAsync(project, "fixed source").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA084 inserts the field after the <c>#Region</c> directive and before the documentation
        ''' comment of the member, and skips names in use.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_KeepsTheFieldInsideTheRegionOfTheMember() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private FindIndexOfAny0 As Integer

#Region "" Methods ""

    ''' <summary>
    ''' Finds a separator.
    ''' </summary>
    Public Function Find(text As String) As Integer
        Return text.IndexOfAny([|New Char() {""x""c}|])
    End Function

#End Region

End Class
", "
Public Class C
    Private FindIndexOfAny0 As Integer

#Region "" Methods ""

    Private Shared ReadOnly FindIndexOfAny1 As Char() = {""x""c}

    ''' <summary>
    ''' Finds a separator.
    ''' </summary>
    Public Function Find(text As String) As Integer
        Return text.IndexOfAny(FindIndexOfAny1)
    End Function

#End Region

End Class
",
                                                      equivalenceKey:="DCA084.Extract", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA084 declares <c>Private ReadOnly</c> fields in modules, and names the fields of
        ''' constructors and created types.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesPrivateReadOnlyInModules_AndConstructorNames() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Module M
    Public Function Describe() As String
        Return String.Join("", "", [|New String() {""a"", ""b""}|])
    End Function
End Module

Public Class C
    Private ReadOnly items As List(Of Integer)

    Public Sub New()
        Me.items = New List(Of Integer)([|New Integer() {1, 2, 3}|])
    End Sub
End Class
", "
Public Module M
    Private ReadOnly DescribeJoin0 As String() = {""a"", ""b""}

    Public Function Describe() As String
        Return String.Join("", "", DescribeJoin0)
    End Function
End Module

Public Class C
    Private ReadOnly items As List(Of Integer)

    Private Shared ReadOnly ConstructorList0 As Integer() = {1, 2, 3}

    Public Sub New()
        Me.items = New List(Of Integer)(ConstructorList0)
    End Sub
End Class
",
                                                      equivalenceKey:="DCA084.Extract", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA084 numbers the fields of a partial type across its files, so that the fixes of both files 
        ''' declare different fields.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FieldNames_AreUniqueAcrossTheFilesOfAPartialType() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", $"Partial Public Class C{vbLf}    Public Function Parse(text As String) As String(){vbLf}        Return text.Split({{"",""c}}){vbLf}    End Function{vbLf}End Class{vbLf}"),
                New TestSource("B.vb", $"Partial Public Class C{vbLf}    Public Function Parse(text As String, count As Integer) As String(){vbLf}        Return text.Split({{"";""c}}, count){vbLf}    End Function{vbLf}End Class{vbLf}")},
                targetNetFramework:=True)
            Await AnalyzerTestHost.AssertCompilesAsync(project, "partial type").ConfigureAwait(True)
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Dim names As String() = diagnostics.OrderBy(Function(d As Diagnostic) d.Location.SourceTree.FilePath, StringComparer.Ordinal).
                Select(Function(d As Diagnostic) d.Properties("Fix.0.1")).ToArray()
            Assert.Equal(DCA084_ConstantArrayArgumentTests.PartialTypeNames, names)
        End Function

#End Region

    End Class

End Namespace

#End Region
