#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA057_EnumerableParameterNameTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA057 (IEnumerable parameter named as a collection) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA057_EnumerableParameterNameTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New EnumerableParameterNameAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New EnumerableParameterNameCodeFixProvider()

        ''' <summary>
        ''' The names expected for the parameters of <see cref="Reports_CollectionNamesOnEnumerableParameters"/>, in source order
        ''' (an empty string when every candidate name is used and the parameter is reported without a code fix).
        ''' </summary>
        Private Shared ReadOnly ExpectedNames As String() = {"sequence", "seq", "sequence", "sequence", "source", "sequence", "seq", "seq", "sequence", "items", ""}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA057_EnumerableParameterNameTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA057 reports parameters typed IEnumerable or IEnumerable(Of T) with a collection name 
        ''' (methods, constructors, properties and lambdas), and the suggested names.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CollectionNamesOnEnumerableParameters() As Task
            Dim source As String = "
Public Class C
    Public Sub AddRange([|collection|] As IEnumerable(Of String))
    End Sub
    Public Sub AddAll([|col|] As IEnumerable(Of String))
    End Sub
    Public Sub Legacy([|List|] As IEnumerable)
    End Sub
    Public Sub Copy([|array|] As IEnumerable(Of Integer), target As Integer())
    End Sub
    Public Sub Clash([|collection|] As IEnumerable(Of Integer), sequence As Integer)
    End Sub
    Public Sub Abbreviated([|arr|] As IEnumerable(Of Integer), seq As Integer)
    End Sub
    Public Sub New([|coll|] As IEnumerable(Of String))
    End Sub
    Default Public ReadOnly Property Item([|lst|] As IEnumerable(Of String)) As Integer
        Get
            Return 0
        End Get
    End Property
    Public Function Total() As Integer
        Dim count As Func(Of IEnumerable(Of Integer), Integer) = Function([|collection|] As IEnumerable(Of Integer)) collection.Count()
        Return count({1})
    End Function
    Public Function Sum([|collection|] As IEnumerable(Of Integer)) As Integer
        Dim sequence As Integer = 0
        Dim source As Integer = 0
        For Each value As Integer In collection
            sequence += value
        Next value
        Return sequence + source
    End Function
    Public Sub Busy([|collection|] As IEnumerable(Of Integer), sequence As Integer, source As Integer, value As Integer, items As Integer)
    End Sub
End Class
"
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, source).ConfigureAwait(True)

            Dim project As Project = AnalyzerTestHost.CreateProject(source.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal))
            Dim diagnostics As List(Of Diagnostic) = (Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)).
                OrderBy(Function(d As Diagnostic) d.Location.SourceSpan.Start).ToList()
            Dim names As IEnumerable(Of String) = diagnostics.Select(
                Function(d As Diagnostic)
                    Dim name As String = Nothing
                    Return If(d.Properties.TryGetValue(EnumerableParameterNameAnalyzer.NewNamePropertyKey, name), name, String.Empty)
                End Function)
            Assert.Equal(ExpectedNames, names, StringComparer.Ordinal)

            Dim messages As List(Of String) = diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToList()
            Assert.Equal("Parameter 'collection' of 'C.AddRange' is an IEnumerable, a sequence rather than a collection; rename it to 'sequence'", messages(0))
            Assert.Equal("Parameter 'coll' of 'C.New' is an IEnumerable, a sequence rather than a collection; rename it to 'seq'", messages(6))
            Assert.Equal("Parameter 'collection' of 'C.Total' is an IEnumerable, a sequence rather than a collection; rename it to 'sequence'", messages(8))
            Assert.Equal("Parameter 'collection' of 'C.Busy' is an IEnumerable, a sequence rather than a collection; rename it", messages(10))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA057 reports the declarations that define a parameter name (interface members, MustOverride 
        ''' members, the definition of a partial method), but not collection types (even when they implement IEnumerable), 
        ''' other names, parameters whose name is defined elsewhere (overrides, interface implementations, the implementation 
        ''' of a partial method, events, delegates) and lambda parameters without an <c>As</c> clause.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function ReportsDeclarations_ButNotCollectionsOrInheritedNames() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Interface IProcessor
    Sub Process([|collection|] As IEnumerable(Of String))
End Interface
Public MustInherit Class Base
    Public MustOverride Sub Run([|collection|] As IEnumerable(Of String))
End Class
Public Delegate Sub Handler(collection As IEnumerable(Of String))
Public Partial Class C : Inherits Base : Implements IProcessor
    Public Event Changed(collection As IEnumerable(Of String))
    Public Sub Process(collection As IEnumerable(Of String)) Implements IProcessor.Process
    End Sub
    Public Overrides Sub Run(collection As IEnumerable(Of String))
    End Sub
    Public Sub Collections(collection As ICollection(Of String), list As List(Of String), array As String(), col As IList(Of String), arr As IReadOnlyCollection(Of Integer))
    End Sub
    Public Sub Names(items As IEnumerable(Of String), values As IEnumerable, collectionName As IEnumerable(Of String))
    End Sub
    Partial Private Sub Load([|collection|] As IEnumerable(Of String))
    End Sub
    Public Function Count(source As IEnumerable(Of IEnumerable(Of Integer))) As Integer
        Return source.Sum(Function(collection) collection.Count())
    End Function
End Class
Partial Public Class C
    Private Sub Load(collection As IEnumerable(Of String))
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA057 renames the parameter everywhere: body, <see langword="NameOf"/>, 
        ''' named arguments of callers and the XML documentation.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RenamesEverywhere() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    ''' <summary>Joins the values.</summary>
    ''' <param name=""collection"">The values.</param>
    ''' <returns>The values of <paramref name=""collection""/>, joined.</returns>
    Public Function JoinAll([|collection|] As IEnumerable(Of String)) As String
        If collection Is Nothing Then
            Throw New ArgumentNullException(NameOf(collection))
        End If
        Return String.Join("","", collection)
    End Function
    Public Function Use() As String
        Return Me.JoinAll(collection:={""a""})
    End Function
End Class
", "
Public Class C
    ''' <summary>Joins the values.</summary>
    ''' <param name=""sequence"">The values.</param>
    ''' <returns>The values of <paramref name=""sequence""/>, joined.</returns>
    Public Function JoinAll(sequence As IEnumerable(Of String)) As String
        If sequence Is Nothing Then
            Throw New ArgumentNullException(NameOf(sequence))
        End If
        Return String.Join("","", sequence)
    End Function
    Public Function Use() As String
        Return Me.JoinAll(sequence:={""a""})
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA057 uses the abbreviated name for an abbreviation, and the next free name 
        ''' when the preferred one is used.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_UsesTheFreeNames() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public Class C
    Public Function First(col As IEnumerable(Of Integer)) As Integer
        Return col.First()
    End Function
    Public Function Last(collection As IEnumerable(Of Integer)) As Integer
        Dim sequence As Integer = collection.Last()
        Return sequence
    End Function
End Class
", NameOf(EnumerableParameterNameCodeFixProvider)).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Public Class C
    Public Function First(seq As IEnumerable(Of Integer)) As Integer
        Return seq.First()
    End Function
    Public Function Last(source As IEnumerable(Of Integer)) As Integer
        Dim sequence As Integer = source.Last()
        Return sequence
    End Function
End Class
", fixedText)
        End Function

        ''' <summary>
        ''' Verifies that no code fix is offered when every candidate name is already used in the member.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NotOfferedWhenEveryNameIsUsed() As Task
            Dim actions As List(Of CodeActions.CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Public Sub Busy(collection As IEnumerable(Of Integer), sequence As Integer, source As Integer, value As Integer, items As Integer)
    End Sub
End Class
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

#End Region

    End Class

End Namespace

#End Region
