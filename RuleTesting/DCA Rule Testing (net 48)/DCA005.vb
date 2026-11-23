#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA005 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA005: Missing explicit constructor.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA005 (Warning) on the class, which does not declare an instance constructor.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds a documented <c>Public Sub New()</c> inside a <c>Constructors</c> region.
    ''' </para>
    ''' </remarks>
    Public Class DCA005

#Region " Members "

        ''' <summary>
        ''' Gets or sets the name.
        ''' </summary>
        Public Property Name As String

#End Region

    End Class

End Namespace

#End Region
