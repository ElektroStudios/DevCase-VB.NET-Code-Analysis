#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

#End Region

#Region " DCA022 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA022: LINQ method instead of the indexer.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA022 (Warning) on <c>numbers.First()</c> on a <c>List(Of Integer)</c>, which has an indexer.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>numbers(0)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA022

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA022"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the first number of a list.
        ''' </summary>
        '''
        ''' <param name="numbers">
        ''' The numbers.
        ''' </param>
        '''
        ''' <returns>
        ''' The first number.
        ''' </returns>
        Friend Shared Function GetFirst(numbers As List(Of Integer)) As Integer

            ' DCA022: List(Of T) has an indexer.
            Return numbers.First()
        End Function

#End Region

    End Class

End Namespace

#End Region
