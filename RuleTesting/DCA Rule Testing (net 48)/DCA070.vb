#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Text

#End Region

#Region " DCA070 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA070: ThreadStatic field initialized inline or in a Shared constructor.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA070 (Warning) on the <c>buffer</c> field: its initializer runs only on the thread that initializes the
    ''' class, so the other threads see <see langword="Nothing"/>. This rule runs only in .NET Framework projects (on modern
    ''' .NET, the built-in rule CA2019 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): none (the field has to be initialized lazily where it is read).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA070

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA070"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' The buffer of each thread.
        ''' </summary>
        <ThreadStatic>
        Private Shared buffer As New StringBuilder() ' DCA070

        ''' <summary>
        ''' Gets the buffer of the current thread.
        ''' </summary>
        '''
        ''' <returns>
        ''' The buffer.
        ''' </returns>
        Friend Shared Function GetBuffer() As StringBuilder

            Return DCA070.buffer
        End Function

#End Region

    End Class

End Namespace

#End Region
