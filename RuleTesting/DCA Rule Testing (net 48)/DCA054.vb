#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Runtime.InteropServices

#End Region

#Region " DCA054 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA054: Code in a ComImport class never runs.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA054 (Warning) on the constructor: Visual Basic compiles the code of a <c>&lt;ComImport&gt;</c> class
    ''' without a body.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): removes the constructor, whose code never runs.
    ''' </para>
    ''' </remarks>
    <ComImport>
    <Guid("F8383852-FCD3-11D1-A6B9-006097DF5BD4")>
    <ClassInterface(ClassInterfaceType.None)>
    Public Class DCA054

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA054"/> class.
        ''' </summary>
        Public Sub New() ' DCA054

            Console.WriteLine("This code never runs.")
        End Sub

#End Region

    End Class

End Namespace

#End Region
