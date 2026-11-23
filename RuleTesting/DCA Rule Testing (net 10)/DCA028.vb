#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Windows.Forms

#End Region

#Region " DCA028 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA028: Windows Forms entry point without &lt;STAThread&gt;.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA028 (Warning) on <c>Sub Main</c>, which has no <c>&lt;STAThread&gt;</c> attribute in a project that
    ''' references Windows Forms.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds <c>&lt;STAThread&gt;</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA028

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA028"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' The entry point of a Windows Forms application.
        ''' </summary>
        Public Shared Sub Main() ' DCA028

            Application.EnableVisualStyles()
        End Sub

#End Region

    End Class

End Namespace

#End Region
