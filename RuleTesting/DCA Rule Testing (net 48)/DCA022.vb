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
    ''' Expected: DCA022 (Info) on <c>numbers.First()</c> on a <c>List(Of Integer)</c>, which has an indexer, and on
    ''' <c>values.Last()</c> on an <c>IReadOnlyList(Of Integer)</c> (this second case only in .NET Framework projects: on
    ''' modern .NET, the built-in rule CA1826 reports it).
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>numbers(0)</c> and <c>values(values.Count - 1)</c>.
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

        ''' <summary>
        ''' Gets the last value of a read-only list.
        ''' </summary>
        '''
        ''' <param name="values">
        ''' The values.
        ''' </param>
        '''
        ''' <returns>
        ''' The last value.
        ''' </returns>
        Friend Shared Function GetLast(values As IReadOnlyList(Of Integer)) As Integer

            ' DCA022: IReadOnlyList(Of T) has an indexer, and Last() enumerates the whole list.
            Return values.Last()
        End Function

#End Region

    End Class

End Namespace

#End Region
