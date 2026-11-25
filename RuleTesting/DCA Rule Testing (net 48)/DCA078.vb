#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic

#End Region

#Region " DCA078 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA078: Set Add or Remove guarded by Contains.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA078 (Info) on <c>numbers.Contains(number)</c>, which guards <c>numbers.Add(number)</c>: <c>Add</c>
    ''' already checks whether the item exists. This rule runs only in .NET Framework projects (on modern .NET, the built-in
    ''' rule CA1868 reports the same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>numbers.Add(number)</c>, without the <see langword="If"/> statement.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA078

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA078"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Remembers a number.
        ''' </summary>
        '''
        ''' <param name="numbers">
        ''' The remembered numbers.
        ''' </param>
        '''
        ''' <param name="number">
        ''' The number to remember.
        ''' </param>
        Friend Shared Sub Remember(numbers As HashSet(Of Integer), number As Integer)

            If Not numbers.Contains(number) Then ' DCA078
                numbers.Add(number)
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
