#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

#End Region

#Region " DCA079 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA079: Keys.Contains or Values.Contains on a dictionary.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA079 (Info) on <c>map.Keys.Contains(key)</c>, which goes through the key collection instead of the
    ''' lookup of the dictionary. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1841
    ''' reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>map.ContainsKey(key)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA079

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA079"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a dictionary has a key.
        ''' </summary>
        '''
        ''' <param name="map">
        ''' The dictionary.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The key.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the dictionary has the key; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasKey(map As Dictionary(Of Integer, String), key As Integer) As Boolean

            Return map.Keys.Contains(key) ' DCA079
        End Function

#End Region

    End Class

End Namespace

#End Region
