#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA007_PublicMemberDocumentationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA007 (missing XML documentation on public members) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA007_PublicMemberDocumentationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New PublicMemberDocumentationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New PublicMemberDocumentationCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA007_PublicMemberDocumentationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA007 reports public members without documentation.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_UndocumentedPublicMembers() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class [|C|]
    Public [|Field|] As Integer
    Public Const [|Limit|] As Integer = 1
    Public Sub [|New|]()
    End Sub
    Public Property [|Value|] As Integer
    Public Event [|Changed|] As System.EventHandler
    <System.Diagnostics.DebuggerStepThrough>
    Public Function [|Run|](x As Integer) As Integer
        Return x
    End Function
    Public Shared Operator [|+|](a As C, b As C) As C
        Return a
    End Operator
    Public Delegate Sub [|Callback|]()
    Public Enum [|Kind|]
        A
    End Enum
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA007 does not report documented, <c>inheritdoc</c> or non-public members.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_DocumentedInheritdocOrNonPublic() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
''' <summary>Documented.</summary>
Public Class C
    ''' <inheritdoc/>
    Public Overrides Function ToString() As String
        Return String.Empty
    End Function
    Private Sub Hidden()
    End Sub
    Friend Sub Internal()
    End Sub
    Protected Sub Inherited()
    End Sub
    Sub NoModifier()
    End Sub
End Class
''' <summary>Documented.</summary>
Public Interface I
    Sub InterfaceMember()
End Interface
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA007 recognizes documentation when the compilation parses it as plain comments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_DocumentationParsedAsPlainComments() As Task
            ' Command-line builds without GenerateDocumentationFile parse ''' comments as plain comment trivia.
            Dim project As Project = AnalyzerTestHost.CreateProject(
                {New TestSource("Test0.vb", "''' <summary>Documented.</summary>" & vbLf & "Public Class C" & vbLf & "End Class" & vbLf)},
                documentationMode:=DocumentationMode.None)
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA007 does not report the declarations of a partial type when another declaration is documented
        ''' (documenting them would produce the compiler warning BC42314).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_PartialTypeDocumentedInAnotherDeclaration() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", $"''' <summary>Documented.</summary>{vbLf}Public NotInheritable Class P{vbLf}End Class{vbLf}"),
                New TestSource("B.vb", $"Partial Public NotInheritable Class P{vbLf}End Class{vbLf}"),
                New TestSource("C.vb", $"Partial Public NotInheritable Class P{vbLf}End Class{vbLf}")})
            Assert.Empty(Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True))
            Assert.Empty(Await AnalyzerTestHost.GetCompilerErrorsAsync(project).ConfigureAwait(True))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA007 reports an undocumented partial type once, on its main declaration (the one without
        ''' the <see langword="Partial"/> modifier), even when that declaration is not the first one.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_PartialTypeOnce_OnMainDeclaration() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", $"Partial Public Class P{vbLf}End Class{vbLf}"),
                New TestSource("B.vb", $"Public Class P{vbLf}End Class{vbLf}")})
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Assert.Single(diagnostics)
            Assert.EndsWith("B.vb", diagnostics(0).Location.SourceTree.FilePath, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA007 reports an undocumented partial type once, on its first declaration, when every declaration
        ''' has the <see langword="Partial"/> modifier; members of every declaration are still reported.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_PartialTypeOnce_OnFirstDeclaration() As Task
            Dim project As Project = AnalyzerTestHost.CreateProject({
                New TestSource("A.vb", $"Partial Public Class P{vbLf}End Class{vbLf}"),
                New TestSource("B.vb", $"Partial Public Class P{vbLf}    Public Sub Run(){vbLf}    End Sub{vbLf}End Class{vbLf}")})
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)
            Assert.Equal(2, diagnostics.Length)
            Assert.Single(diagnostics, Function(d As Diagnostic) d.GetMessage(CultureInfo.InvariantCulture).Contains("'P'", StringComparison.Ordinal) AndAlso
                                                                d.Location.SourceTree.FilePath.EndsWith("A.vb", StringComparison.Ordinal))
            Assert.Single(diagnostics, Function(d As Diagnostic) d.GetMessage(CultureInfo.InvariantCulture).Contains("'Run'", StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA007 generates the <c>summary</c>, <c>param</c> and <c>returns</c> sections.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_GeneratesSummaryParamsAndReturns() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
''' <summary>Documented.</summary>
Public Class C

    <System.Diagnostics.DebuggerStepThrough>
    Public Function Run(first As Integer, second As String) As Integer
        Return first
    End Function

    Public Sub Clear()
    End Sub

End Class
", "
''' <summary>Documented.</summary>
Public Class C

    ''' <summary></summary>
    '''
    ''' <param name=""first"">
    ''' The first.
    ''' </param>
    '''
    ''' <param name=""second"">
    ''' The second.
    ''' </param>
    '''
    ''' <returns></returns>
    <System.Diagnostics.DebuggerStepThrough>
    Public Function Run(first As Integer, second As String) As Integer
        Return first
    End Function

    ''' <summary></summary>
    Public Sub Clear()
    End Sub

End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA007 writes the conventional constructor summary and type-based parameter descriptions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ConstructorsGetConventionalSummary_AndTypedParameterTexts() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Text
Imports System.Threading
''' <summary>Documented.</summary>
Public Class LogConsoleBox
    Public Sub New()
    End Sub
    Public Sub New(encoding As Encoding, isEnabled As Boolean, overwrite As Boolean, recursive As Boolean,
                   mode As System.IO.FileMode, cancellationToken As CancellationToken, maxURLLength As Integer)
    End Sub
End Class
''' <summary>Documented.</summary>
Public Structure Point(Of T)
    Public Sub New(x As T)
    End Sub
End Structure
", "
Imports System.Text
Imports System.Threading
''' <summary>Documented.</summary>
Public Class LogConsoleBox
    ''' <summary>
    ''' Initializes a new instance of the <see cref=""LogConsoleBox""/> class.
    ''' </summary>
    Public Sub New()
    End Sub
    ''' <summary>
    ''' Initializes a new instance of the <see cref=""LogConsoleBox""/> class.
    ''' </summary>
    '''
    ''' <param name=""encoding"">
    ''' The <see cref=""Encoding""/> to use.
    ''' </param>
    '''
    ''' <param name=""isEnabled"">
    ''' <see langword=""True""/> if enabled; otherwise, <see langword=""False""/>.
    ''' </param>
    '''
    ''' <param name=""overwrite"">
    ''' <see langword=""True""/> to overwrite; otherwise, <see langword=""False""/>.
    ''' </param>
    '''
    ''' <param name=""recursive"">
    ''' <see langword=""True""/> to enable the recursive option; otherwise, <see langword=""False""/>.
    ''' </param>
    '''
    ''' <param name=""mode"">
    ''' One of the enumeration values that specifies the mode.
    ''' </param>
    '''
    ''' <param name=""cancellationToken"">
    ''' The token to monitor for cancellation requests.
    ''' </param>
    '''
    ''' <param name=""maxURLLength"">
    ''' The max URL length.
    ''' </param>
    Public Sub New(encoding As Encoding, isEnabled As Boolean, overwrite As Boolean, recursive As Boolean,
                   mode As System.IO.FileMode, cancellationToken As CancellationToken, maxURLLength As Integer)
    End Sub
End Class
''' <summary>Documented.</summary>
Public Structure Point(Of T)
    ''' <summary>
    ''' Initializes a new instance of the <see cref=""Point(Of T)""/> structure.
    ''' </summary>
    '''
    ''' <param name=""x"">
    ''' The x.
    ''' </param>
    Public Sub New(x As T)
    End Sub
End Structure
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
