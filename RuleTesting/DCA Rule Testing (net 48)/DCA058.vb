#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Text

#End Region

#Region " DCA058 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA058: StringBuilder argument converted to a string.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA058 (Info) on <c>New String(character, count)</c> passed to <c>StringBuilder.Append</c>. This rule runs
    ''' only in .NET Framework projects (on modern .NET, the built-in rule CA1830 reports the same arguments), so this file is
    ''' only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>builder.Append(character, count)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA058

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA058"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Repeats a character.
        ''' </summary>
        '''
        ''' <param name="character">
        ''' The character.
        ''' </param>
        '''
        ''' <param name="count">
        ''' The number of repetitions.
        ''' </param>
        '''
        ''' <returns>
        ''' The text.
        ''' </returns>
        Public Shared Function Repeat(character As Char, count As Integer) As String

            Dim builder As New StringBuilder()
            builder.Append(New String(character, count)) ' DCA058
            Return builder.ToString()
        End Function

#End Region

    End Class

End Namespace

#End Region
