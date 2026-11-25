#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA065 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA065: Buffer.BlockCopy count is an array length.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA065 (Warning) on <c>source.Length</c> passed as the byte count of <c>Buffer.BlockCopy</c>: the elements
    ''' are <see cref="Integer"/>, so only a quarter of the array is copied. This rule runs only in .NET Framework projects
    ''' (on modern .NET, the built-in rule CA2018 reports the same code), so this file is only in the .NET Framework
    ''' project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Buffer.ByteLength(source)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA065

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA065"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Copies an array.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The array to copy.
        ''' </param>
        '''
        ''' <returns>
        ''' The copy.
        ''' </returns>
        Friend Shared Function Copy(source As Integer()) As Integer()

            Dim target As Integer() = New Integer(source.Length - 1) {}
            Buffer.BlockCopy(source, 0, target, 0, source.Length) ' DCA065
            Return target
        End Function

#End Region

    End Class

End Namespace

#End Region
