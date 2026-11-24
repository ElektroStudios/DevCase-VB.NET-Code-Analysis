#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA052_SenderParameterTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA052 (parameter named 'sender' outside an event handler) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA052_SenderParameterTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New SenderParameterAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New SenderParameterCodeFixProvider()

        ''' <summary>
        ''' The types used by the sources of the tests.
        ''' </summary>
        Private Const Declarations As String = "
Public Class SolidBrush
    Public Property Color As Integer
End Class
Public Class Pen
    Public Sub New(color As Integer)
    End Sub
End Class
Public Interface IMailAddress
End Interface
Public Class HTMLDocument
End Class
Public Structure Point
End Structure
Public Class Holder
    Public Property Value As Integer
End Class
"


        ''' <summary>
        ''' The names expected for the parameters of <see cref="Reports_SenderOutsideEventHandlers"/>, in source order
        ''' (an empty string when every candidate name is used and the parameter is reported without a code fix).
        ''' </summary>
        Private Shared ReadOnly ExpectedNames As String() = {"source", "source", "source", "source", "source", "value", "value", "input", "value", ""}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA052_SenderParameterTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA052 reports typed <c>sender</c> parameters of methods that are not event handlers, 
        ''' and the suggested names.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_SenderOutsideEventHandlers() As Task
            Dim source As String = $"{Declarations}
Public Class C
    Public Function ToPen([|sender|] As SolidBrush) As Pen
        Return New Pen(sender.Color)
    End Function
    Public Sub Send([|sender|] As IMailAddress)
    End Sub
    Public Sub Draw([|sender|] As Point())
    End Sub
    Public Sub Show([|sender|] As String)
    End Sub
    Public Sub Parse([|Sender|] As HTMLDocument)
    End Sub
    Public Sub Merge([|sender|] As SolidBrush, source As SolidBrush)
    End Sub
    Public Function Source([|sender|] As Holder) As Integer
        Return sender.Value
    End Function
    Public Sub Fill([|sender|] As Holder)
        Dim source As New Holder()
        Dim value As Integer = sender.Value
        source.Value = value
    End Sub
    Public Sub Copy([|sender|] As Holder, target As Holder)
        Dim source As Holder = target
        source.Value = sender.Value
    End Sub
    Public Sub Busy([|sender|] As SolidBrush, source As Integer, value As Integer, input As Integer, instance As Integer, item As Integer)
    End Sub
End Class
"
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, source).ConfigureAwait(True)

            Dim project As Microsoft.CodeAnalysis.Project = AnalyzerTestHost.CreateProject(source.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal))
            Dim diagnostics As List(Of Microsoft.CodeAnalysis.Diagnostic) =
                (Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)).
                OrderBy(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.Location.SourceSpan.Start).ToList()
            Dim names As IEnumerable(Of String) = diagnostics.Select(
                Function(d As Microsoft.CodeAnalysis.Diagnostic)
                    Dim name As String = Nothing
                    Return If(d.Properties.TryGetValue(SenderParameterAnalyzer.NewNamePropertyKey, name), name, String.Empty)
                End Function)
            Assert.Equal(ExpectedNames, names, StringComparer.Ordinal)

            Dim messages As List(Of String) = diagnostics.Select(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToList()
            Assert.Equal("Rename parameter 'sender' to 'source' in 'C.ToPen' because it is not an event handler", messages(0))
            Assert.Equal("Rename parameter 'sender' in 'C.Busy' because it is not an event handler", messages(messages.Count - 1))
        End Function

        ''' <summary>
        ''' Verifies that no code fix is offered when every candidate name is already used in the method 
        ''' (no generated names such as <c>source2</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NotOfferedWhenEveryNameIsUsed() As Task
            Dim actions As List(Of Microsoft.CodeAnalysis.CodeActions.CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, $"{Declarations}
Public Class C
    Public Sub Busy(sender As SolidBrush, source As Integer, value As Integer, input As Integer, instance As Integer, item As Integer)
    End Sub
End Class
").ConfigureAwait(True)
            Assert.Empty(actions)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA052 does not report event handlers (Object sender, EventArgs argument, Handles clause), 
        ''' handlers of custom events attached with AddressOf, overrides, type parameters and other parameter names.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_EventHandlersAndCallbacks() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"{Declarations}
Public MustInherit Class Base
    Public MustOverride Sub Run(sender As SolidBrush)
End Class
Public Class C : Inherits Base
    Public Event Changed(sender As C, value As Integer)
    Private WithEvents Other As C
    Public Sub New()
        AddHandler Me.Changed, AddressOf Me.OnValueChanged
    End Sub
    Public Sub OnClick(sender As Object, e As EventArgs)
    End Sub
    Public Sub OnTyped(sender As C, e As EventArgs)
    End Sub
    Private Sub OnValueChanged(sender As C, value As Integer)
    End Sub
    Private Sub Other_Changed(sender As C, value As Integer) Handles Other.Changed
    End Sub
    Public Sub Callback(sender As Object, certificate As String)
    End Sub
    Public Overrides Sub Run(sender As SolidBrush)
    End Sub
    Public Sub Generic(Of T)(sender As T)
    End Sub
    Public Sub Named(senderName As String)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA052 renames the parameter everywhere: body, <see langword="NameOf"/>, 
        ''' named arguments of callers and the XML documentation.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_RenamesEverywhere() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, $"{Declarations}
Public Class C
    ''' <summary>Converts a brush.</summary>
    ''' <param name=""sender"">The brush.</param>
    ''' <returns>A pen with the color of <paramref name=""sender""/>.</returns>
    Public Function ToPen(sender As SolidBrush) As Pen
        If sender Is Nothing Then
            Throw New ArgumentNullException(NameOf(sender))
        End If
        Return New Pen(sender.Color)
    End Function
    Public Function Use() As Pen
        Return Me.ToPen(sender:=New SolidBrush())
    End Function
End Class
", $"{Declarations}
Public Class C
    ''' <summary>Converts a brush.</summary>
    ''' <param name=""source"">The brush.</param>
    ''' <returns>A pen with the color of <paramref name=""source""/>.</returns>
    Public Function ToPen(source As SolidBrush) As Pen
        If source Is Nothing Then
            Throw New ArgumentNullException(NameOf(source))
        End If
        Return New Pen(source.Color)
    End Function
    Public Function Use() As Pen
        Return Me.ToPen(source:=New SolidBrush())
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
