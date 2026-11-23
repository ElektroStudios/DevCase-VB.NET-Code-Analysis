#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic

#End Region

#Region " DCA057 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA057: IEnumerable parameter named as a collection.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA057 (Info) on the parameter <c>collection</c>, which is a sequence (<c>IEnumerable(Of String)</c>).
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): renames it to <c>sequence</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA057

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA057"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Joins values.
        ''' </summary>
        '''
        ''' <param name="collection">
        ''' The values.
        ''' </param>
        '''
        ''' <returns>
        ''' The joined values.
        ''' </returns>
        Friend Shared Function JoinAll(collection As IEnumerable(Of String)) As String ' DCA057

            Return String.Join(", ", collection)
        End Function

#End Region

    End Class

End Namespace

#End Region
