#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic

#End Region

#Region " DCA077 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA077: Dictionary.Remove guarded by ContainsKey.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA077 (Info) on <c>map.ContainsKey(key)</c>, which guards <c>map.Remove(key)</c>: <c>Remove</c> already
    ''' checks whether the key exists. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule
    ''' CA1853 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>map.Remove(key)</c>, without the <see langword="If"/> statement.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA077

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA077"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Forgets a key.
        ''' </summary>
        '''
        ''' <param name="map">
        ''' The dictionary.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The key to forget.
        ''' </param>
        Friend Shared Sub Forget(map As Dictionary(Of Integer, String), key As Integer)

            If map.ContainsKey(key) Then ' DCA077
                map.Remove(key)
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
