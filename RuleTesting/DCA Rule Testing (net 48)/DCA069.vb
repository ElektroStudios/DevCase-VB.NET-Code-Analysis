#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.IO

#End Region

#Region " DCA069 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA069: Enum.HasFlag with a different enum type.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA069 (Warning) on <c>ConsoleModifiers.Alt</c> passed to <c>HasFlag</c> of a <c>FileAttributes</c> value,
    ''' which throws <see cref="ArgumentException"/>. This rule runs only in .NET Framework projects (on modern .NET, the
    ''' built-in rule CA2248 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): none (the right flag depends on what the code means).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA069

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA069"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether attributes have a flag.
        ''' </summary>
        '''
        ''' <param name="attributes">
        ''' The attributes of a file.
        ''' </param>
        '''
        ''' <returns>
        ''' Never: the call throws.
        ''' </returns>
        Friend Shared Function HasAltFlag(attributes As FileAttributes) As Boolean

            Return attributes.HasFlag(ConsoleModifiers.Alt) ' DCA069
        End Function

#End Region

    End Class

End Namespace

#End Region
