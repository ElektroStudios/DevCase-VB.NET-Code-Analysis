#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Linq

#End Region

#Region " DCA073 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA073: Enumerable.Count() instead of the Length or Count property.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA073 (Info) on <c>text.Count()</c>, which enumerates every character of the string. This rule runs only
    ''' in .NET Framework projects (on modern .NET, the built-in rule CA1829 reports the same code), so this file is only in
    ''' the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>text.Length</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA073

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA073"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Counts the characters of a text.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text.
        ''' </param>
        '''
        ''' <returns>
        ''' The number of characters.
        ''' </returns>
        Friend Shared Function CountCharacters(text As String) As Integer

            Return text.Count() ' DCA073
        End Function

#End Region

    End Class

End Namespace

#End Region
