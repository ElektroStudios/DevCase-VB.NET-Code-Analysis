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
Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA008_XmlParameterDocumentationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA008 (undocumented XML parameters) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA008_XmlParameterDocumentationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New XmlParameterDocumentationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New XmlParameterDocumentationCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA008_XmlParameterDocumentationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA008 reports missing, empty, unknown and duplicated <c>param</c> tags.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_MissingEmptyUnknownAndDuplicatedTags() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    ''' <summary>Missing.</summary>
    ''' <param name=""a"">The a.</param>
    Public Sub [|Missing|](a As Integer, b As Integer)
    End Sub
    ''' <summary>Empty.</summary>
    ''' <param name=""a""></param>
    ''' <param name=""b"">
    '''
    ''' </param>
    ''' <param name=""c""/>
    Public Sub [|Empty|](a As Integer, b As Integer, c As Integer)
    End Sub
    ''' <summary>Unknown.</summary>
    ''' <param name=""a"">The a.</param>
    ''' <param name=""old"">Stale.</param>
    Public Sub [|Unknown|](a As Integer)
    End Sub
    ''' <summary>Duplicated.</summary>
    ''' <param name=""a"">The a.</param>
    ''' <param name=""a"">Again.</param>
    Public Sub [|Duplicated|](a As Integer)
    End Sub
    ''' <summary>Indexer.</summary>
    Default Public ReadOnly Property [|Item|](index As Integer) As Integer
        Get
            Return index
        End Get
    End Property
End Class
").ConfigureAwait(True)
            Assert.Contains("missing <param> for 'b'", diagnostics(0).GetMessage(), StringComparison.Ordinal)
            Assert.Contains("empty <param> for 'a', 'b', 'c'", diagnostics(1).GetMessage(), StringComparison.Ordinal)
            Assert.Contains("unknown <param> 'old'", diagnostics(2).GetMessage(), StringComparison.Ordinal)
            Assert.Contains("duplicated <param> 'a'", diagnostics(3).GetMessage(), StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA008 does not report valid, <c>inheritdoc</c> or undocumented members.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ValidInheritdocOrUndocumented() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    ''' <summary>Valid.</summary>
    ''' <param name=""a"">The <see cref=""Integer""/>.</param>
    ''' <param name=""B"">Case-insensitive match.</param>
    Public Sub Valid(a As Integer, b As Integer)
    End Sub
    ''' <inheritdoc/>
    Public Overrides Function Equals(obj As Object) As Boolean
        Return False
    End Function
    Public Sub Undocumented(a As Integer)
    End Sub
    ''' <summary>No parameters.</summary>
    Public Property Value As Integer
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA008 inserts the missing tags in parameter order.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_InsertsMissingTagsInParameterOrder() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C

    ''' <summary>
    ''' Runs.
    ''' </summary>
    '''
    ''' <param name=""b"">
    ''' The b.
    ''' </param>
    '''
    ''' <returns>
    ''' A value.
    ''' </returns>
    Public Function Run(a As Integer, b As Integer, c As Integer) As Integer
        Return a
    End Function

    ''' <summary>
    ''' Clears.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Remarks.
    ''' </remarks>
    Public Sub Clear(x As Integer)
    End Sub

End Class
", "
Public Class C

    ''' <summary>
    ''' Runs.
    ''' </summary>
    '''
    ''' <param name=""a"">
    ''' The a.
    ''' </param>
    '''
    ''' <param name=""b"">
    ''' The b.
    ''' </param>
    '''
    ''' <param name=""c"">
    ''' The c.
    ''' </param>
    '''
    ''' <returns>
    ''' A value.
    ''' </returns>
    Public Function Run(a As Integer, b As Integer, c As Integer) As Integer
        Return a
    End Function

    ''' <summary>
    ''' Clears.
    ''' </summary>
    '''
    ''' <param name=""x"">
    ''' The x.
    ''' </param>
    '''
    ''' <remarks>
    ''' Remarks.
    ''' </remarks>
    Public Sub Clear(x As Integer)
    End Sub

End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA008 fills empty <c>param</c> tags with a description.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_FillsEmptyTags() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    ''' <summary>Empty.</summary>
    ''' <param name=""filePath""></param>
    ''' <param name=""retryCount"">
    '''
    ''' </param>
    ''' <param name=""overwrite""/>
    Public Sub Run(filePath As String, retryCount As Integer, overwrite As Boolean)
    End Sub
End Class
", "
Public Class C
    ''' <summary>Empty.</summary>
    ''' <param name=""filePath"">
    ''' The file path.
    ''' </param>
    ''' <param name=""retryCount"">
    ''' The retry count.
    ''' </param>
    ''' <param name=""overwrite"">
    ''' <see langword=""True""/> to overwrite; otherwise, <see langword=""False""/>.
    ''' </param>
    Public Sub Run(filePath As String, retryCount As Integer, overwrite As Boolean)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA008 is not offered when the only problem is an unknown tag.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_IsNotOffered_ForUnknownTagsOnly() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    ''' <summary>Unknown.</summary>
    ''' <param name=""a"">The a.</param>
    ''' <param name=""old"">Stale.</param>
    Public Sub Run(a As Integer)
    End Sub
End Class
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

#End Region

    End Class

End Namespace

#End Region
