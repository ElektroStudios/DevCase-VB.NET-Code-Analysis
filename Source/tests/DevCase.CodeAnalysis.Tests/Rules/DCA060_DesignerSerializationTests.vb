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
Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA060_DesignerSerializationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA060 (missing designer serialization configuration, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA060_DesignerSerializationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DesignerSerializationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

        ''' <summary>
        ''' The expected titles of the code actions of <see cref="Actions_OfferVisibleThenHidden_WithoutDefaultValueForAReferenceType"/>.
        ''' </summary>
        Private Shared ReadOnly Actions_OfferVisibleThenHidden_WithoutDefaultValueForAReferenceTypeExpected As String() = {"Add <DesignerSerializationVisibility(Visible)>", "Add <DesignerSerializationVisibility(Hidden)>"}

        ''' <summary>
        ''' The expected titles of the code actions of <see cref="Actions_OfferContent_ForClassInterfaceAndArrayTypes"/>.
        ''' </summary>
        Private Shared ReadOnly Actions_OfferContent_ForClassInterfaceAndArrayTypesExpected As String() = {"Add <DesignerSerializationVisibility(Visible)>", "Add <DesignerSerializationVisibility(Content)>", "Add <DesignerSerializationVisibility(Hidden)>"}

        ''' <summary>
        ''' The expected titles of the code actions of <see cref="Actions_OfferDefaultValue_ForAConstantInitialValue"/>.
        ''' </summary>
        Private Shared ReadOnly Actions_OfferDefaultValue_ForAConstantInitialValueExpected As String() = {"Add <DesignerSerializationVisibility(Visible)>", "Add <DefaultValue(100)>", "Add <DesignerSerializationVisibility(Hidden)>"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA060_DesignerSerializationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA060 reports the read/write properties of a component that have no
        ''' <c>DesignerSerializationVisibility</c>, no <c>DefaultValue</c> and no <c>ShouldSerialize</c> method, and ignores read-only,
        ''' private-setter, protected, shared and indexed properties and classes that are not components.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_PropertiesOfComponentsWithoutConfiguration() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property [|Title|] As String

    Public Property [|Maximum|] As Integer = 100

    Friend Property [|Scale|] As Double

    <DefaultValue(0)>
    Public Property Minimum As Integer

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Cache As Object

    Public Property Color As String

    Public ReadOnly Property Count As Integer

    Public Property Secret As String
        Get
            Return Nothing
        End Get
        Private Set(value As String)
        End Set
    End Property

    Protected Property Level As Integer

    Public Shared Property Instances As Integer

    Default Public Property Item(index As Integer) As Integer
        Get
            Return index
        End Get
        Set(value As Integer)
        End Set
    End Property

    Private Function ShouldSerializeColor() As Boolean
        Return Me.Color IsNot Nothing
    End Function
End Class

Public Class Plain
    Public Property Title As String
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA060 does not run in a modern .NET project, where the built-in rule WFO1000 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property Title As String

    Public Property Maximum As Integer = 100

    Friend Property Scale As Double

    <DefaultValue(0)>
    Public Property Minimum As Integer

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Cache As Object

    Public Property Color As String

    Public ReadOnly Property Count As Integer

    Public Property Secret As String
        Get
            Return Nothing
        End Get
        Private Set(value As String)
        End Set
    End Property

    Protected Property Level As Integer

    Public Shared Property Instances As Integer

    Default Public Property Item(index As Integer) As Integer
        Get
            Return index
        End Get
        Set(value As Integer)
        End Set
    End Property

    Private Function ShouldSerializeColor() As Boolean
        Return Me.Color IsNot Nothing
    End Function
End Class

Public Class Plain
    Public Property Title As String
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA060 offers <c>Visible</c> first and <c>Hidden</c> last, without <c>DefaultValue</c>
        ''' for a property of a reference type without an initial value.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_OfferVisibleThenHidden_WithoutDefaultValueForAReferenceType() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property Title As String
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA060_DesignerSerializationTests.Actions_OfferVisibleThenHidden_WithoutDefaultValueForAReferenceTypeExpected, actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA060 offers <c>Content</c>, after <c>Visible</c>, for properties of a class,
        ''' interface or array type, and not for <see cref="String"/> or value types.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_OfferContent_ForClassInterfaceAndArrayTypes() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property Tags As List(Of String)
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA060_DesignerSerializationTests.Actions_OfferContent_ForClassInterfaceAndArrayTypesExpected, actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

        ''' <summary>
        ''' Verifies that the <c>Content</c> code fix of rule DCA060 is applied to class, interface and array properties, and not
        ''' offered for the other ones.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Content_ForObjectsAndCollections() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property [|Tags|] As List(Of String)
    Public Property [|Items|] As IList(Of Integer)
    Public Property [|Values|] As Integer()
    Public Property [|Title|] As String
    Public Property [|Limit|] As Integer
End Class
", "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>
    Public Property Tags As List(Of String)
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>
    Public Property Items As IList(Of Integer)
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>
    Public Property Values As Integer()
    Public Property Title As String
    Public Property Limit As Integer
End Class
",
                                                      equivalenceKey:="DCA060.Content", allowRemainingDiagnostics:=True, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA060 offers <c>DefaultValue</c> for an auto-implemented property with a constant
        ''' initial value.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Actions_OfferDefaultValue_ForAConstantInitialValue() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property Maximum As Integer = 100
End Class
",
                                                                                                 targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA060_DesignerSerializationTests.Actions_OfferDefaultValue_ForAConstantInitialValueExpected, actions.Select(Function(a As CodeAction) a.Title).ToArray())
        End Function

        ''' <summary>
        ''' Verifies that the <c>Visible</c> code fix of rule DCA060 adds the attribute on its own line, before the existing
        ''' attributes and after the documentation comment.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Visible_AddsTheAttributeOnItsOwnLine() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    ''' <summary>
    ''' The title.
    ''' </summary>
    <Browsable(True)>
    Public Property [|Title|] As String
End Class
", "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    ''' <summary>
    ''' The title.
    ''' </summary>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>
    <Browsable(True)>
    Public Property Title As String
End Class
",
                                                      equivalenceKey:="DCA060.Visible", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>DefaultValue</c> code fix of rule DCA060 writes the initial value (or the default value) typed so
        ''' that it binds to the overload of the property type.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_DefaultValue_WritesTypedConstants() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    Public Property [|Maximum|] As Integer = 100
    Public Property [|Ratio|] As Double = 0.5
    Public Property [|Big|] As Long = 5
    Public Property [|Small|] As Short
    Public Property [|Enabled|] As Boolean = True
    Public Property [|Letter|] As Char = ""x""c
    Public Property [|Caption|] As String = ""A """"quoted"""" name""
    Public Property [|Day|] As DayOfWeek = DayOfWeek.Friday
    Public Property [|Data|] As Byte = 7
    Public Property [|Zoom|] As Single = 1.5F
End Class
", "
Imports System.ComponentModel

Public Class Gauge
    Inherits Component

    <DefaultValue(100)>
    Public Property Maximum As Integer = 100
    <DefaultValue(0.5R)>
    Public Property Ratio As Double = 0.5
    <DefaultValue(5L)>
    Public Property Big As Long = 5
    <DefaultValue(0S)>
    Public Property Small As Short
    <DefaultValue(True)>
    Public Property Enabled As Boolean = True
    <DefaultValue(""x""c)>
    Public Property Letter As Char = ""x""c
    <DefaultValue(""A """"quoted"""" name"")>
    Public Property Caption As String = ""A """"quoted"""" name""
    <DefaultValue(DayOfWeek.Friday)>
    Public Property Day As DayOfWeek = DayOfWeek.Friday
    <DefaultValue(CByte(7))>
    Public Property Data As Byte = 7
    <DefaultValue(1.5F)>
    Public Property Zoom As Single = 1.5F
End Class
",
                                                      equivalenceKey:="DCA060.DefaultValue", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>Hidden</c> code fix of rule DCA060 qualifies the attribute and the enumeration when
        ''' <c>System.ComponentModel</c> is not imported.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Hidden_QualifiesTheNamesWithoutImports() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class Gauge
    Inherits System.ComponentModel.Component

    Public Property [|Title|] As String
End Class
", "
Public Class Gauge
    Inherits System.ComponentModel.Component

    <ComponentModel.DesignerSerializationVisibility(ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property Title As String
End Class
",
                                                      equivalenceKey:="DCA060.Hidden", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
