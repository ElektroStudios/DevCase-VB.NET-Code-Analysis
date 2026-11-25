#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Diagnostics.CodeAnalysis

#End Region

#Region " DCA081 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA081: ContainsKey followed by the indexer instead of TryGetValue.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA081 (Info) on <c>ages.ContainsKey(id)</c> followed by <c>ages(id)</c>, which looks the key up twice.
    ''' The simplified form <c>Return If(ages.ContainsKey(id), ages(id), 0)</c> is reported too. This rule runs only in .NET
    ''' Framework projects (on modern .NET, the built-in rule CA1854 reports the same code), so this file is only in the
    ''' .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): declares <c>Dim value As Integer = Nothing</c>, calls <c>ages.TryGetValue(id, value)</c> and
    ''' returns <c>value</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA081

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA081"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the age of a person.
        ''' </summary>
        '''
        ''' <param name="ages">
        ''' The ages by identifier.
        ''' </param>
        '''
        ''' <param name="id">
        ''' The identifier of the person.
        ''' </param>
        '''
        ''' <returns>
        ''' The age, or 0 if the person is unknown.
        ''' </returns>
        <SuppressMessage("Style", "IDE0046:Convert to conditional expression",
                         Justification:="The rule is about using TryGetValue instead of ContainsKey followed by the indexer, so it is used in the code fix.")>
        Friend Shared Function GetAge(ages As Dictionary(Of Integer, Integer), id As Integer) As Integer

            If ages.ContainsKey(id) Then ' DCA081
                Return ages(id)
            End If
            Return 0
        End Function

#End Region

    End Class

End Namespace

#End Region
