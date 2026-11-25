#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA061 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA061: ArgumentNullException for a value-type parameter.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA061 (Warning) on <c>New ArgumentNullException(NameOf(count))</c>: <c>count</c> is an
    ''' <see cref="Integer"/>, which can never be <see langword="Nothing"/>. This rule runs only in .NET Framework projects
    ''' (it is the counterpart of CA2264, which analyzes <c>ArgumentNullException.ThrowIfNull</c> on modern .NET), so this
    ''' file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): none (the right exception, <c>ArgumentOutOfRangeException</c> or <c>ArgumentException</c>,
    ''' depends on what the code means).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA061

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA061"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Validates a number of items.
        ''' </summary>
        '''
        ''' <param name="count">
        ''' The number of items.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="count"/> is zero.
        ''' </exception>
        Friend Shared Sub Validate(count As Integer)

            If count = 0 Then
                Throw New ArgumentNullException(NameOf(count)) ' DCA061
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
