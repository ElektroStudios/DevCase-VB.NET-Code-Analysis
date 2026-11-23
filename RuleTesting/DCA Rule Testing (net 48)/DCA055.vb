#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA055 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA055: Zero-length array allocation.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA055 (Info) on <c>New String() {}</c>. This rule runs only in .NET Framework projects (on modern .NET, the
    ''' built-in rule CA1825 reports the same arrays), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Array.Empty(Of String)()</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA055

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA055"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Creates the parameters of a call.
        ''' </summary>
        '''
        ''' <returns>
        ''' The parameters.
        ''' </returns>
        Public Shared Function CreateParameters() As Object()

            ' DCA055: an empty array is allocated.
            Dim parameters As Object() = {New String() {}}
            Return parameters
        End Function

#End Region

    End Class

End Namespace

#End Region
