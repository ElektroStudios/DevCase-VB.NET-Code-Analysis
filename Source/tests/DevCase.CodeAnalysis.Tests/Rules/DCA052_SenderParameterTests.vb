#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

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
"


        ''' <summary>
        ''' The names expected for the parameters of <see cref="Reports_SenderOutsideEventHandlers"/>, in source order.
        ''' </summary>
        Private Shared ReadOnly ExpectedNames As String() = {"brush", "address", "points", "value", "document", "brush2"}

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
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, $"{Declarations}
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
    Public Sub Merge([|sender|] As SolidBrush, brush As SolidBrush)
    End Sub
End Class
").ConfigureAwait(True)

            Dim project As Microsoft.CodeAnalysis.Project = AnalyzerTestHost.CreateProject($"{Declarations}
Public Class C
    Public Function ToPen(sender As SolidBrush) As Pen
        Return New Pen(sender.Color)
    End Function
    Public Sub Send(sender As IMailAddress)
    End Sub
    Public Sub Draw(sender As Point())
    End Sub
    Public Sub Show(sender As String)
    End Sub
    Public Sub Parse(sender As HTMLDocument)
    End Sub
    Public Sub Merge(sender As SolidBrush, brush As SolidBrush)
    End Sub
End Class
")
            Dim names As IEnumerable(Of String) = (Await AnalyzerTestHost.GetAnalyzerDiagnosticsAsync(project, Analyzer).ConfigureAwait(True)).
                OrderBy(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.Location.SourceSpan.Start).
                Select(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.Properties(SenderParameterAnalyzer.NewNamePropertyKey))
            Assert.Equal(ExpectedNames, names, StringComparer.Ordinal)
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
    ''' <param name=""brush"">The brush.</param>
    ''' <returns>A pen with the color of <paramref name=""brush""/>.</returns>
    Public Function ToPen(brush As SolidBrush) As Pen
        If brush Is Nothing Then
            Throw New ArgumentNullException(NameOf(brush))
        End If
        Return New Pen(brush.Color)
    End Function
    Public Function Use() As Pen
        Return Me.ToPen(brush:=New SolidBrush())
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
