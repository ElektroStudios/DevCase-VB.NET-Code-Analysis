#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.ComponentModel

#End Region

#Region " DCA060 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA060: Component property without designer serialization configuration.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA060 (Warning) on the <c>Title</c> and <c>Tags</c> properties of a component, which tell the Windows
    ''' Forms designer neither whether nor how to serialize them. This rule runs only in .NET Framework projects (on modern
    ''' .NET, the built-in rule WFO1000 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds <c>&lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)&gt;</c>
    ''' (keeps the current behavior) or
    ''' <c>&lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)&gt;</c>; for <c>Tags</c>, an object,
    ''' it also offers <c>&lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)&gt;</c> (the designer
    ''' writes its items); for a property with a constant initial value, it also offers <c>&lt;DefaultValue(value)&gt;</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA060

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA060"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' A component with a property that the designer serializes.
        ''' </summary>
        Friend NotInheritable Class Gauge : Inherits Component

            ''' <summary>
            ''' Initializes a new instance of the <see cref="Gauge"/> class.
            ''' </summary>
            Friend Sub New()
            End Sub

            ''' <summary>
            ''' Gets or sets the title.
            ''' </summary>
            Public Property Title As String ' DCA060

            ''' <summary>
            ''' Gets or sets the tags.
            ''' </summary>
            Public Property Tags As List(Of String) ' DCA060

        End Class

#End Region

    End Class

End Namespace

#End Region
