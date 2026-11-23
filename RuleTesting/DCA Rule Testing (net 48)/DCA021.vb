#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

#End Region

#Region " DCA021 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA021: LINQ method instead of Find / Exists / TrueForAll.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA021 (Warning) on <c>numbers.Any(predicate)</c> on a <c>List(Of Integer)</c>, which has the faster
    ''' <c>Exists</c> method.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>numbers.Exists(Function(number As Integer) number &lt; 0)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA021

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA021"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a list contains a negative number.
        ''' </summary>
        '''
        ''' <param name="numbers">
        ''' The numbers.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if a number is negative; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasNegative(numbers As List(Of Integer)) As Boolean

            ' DCA021: List(Of T) has Exists.
            Return numbers.Any(Function(number As Integer) number < 0)
        End Function

#End Region

    End Class

End Namespace

#End Region
