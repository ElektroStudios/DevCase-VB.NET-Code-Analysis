#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA048 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA048: Exception thrown in a property getter.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA048 (Info) on the <c>Throw</c> in the getter of <c>Current</c> (a getter should only throw
    ''' <c>InvalidOperationException</c>, <c>NotSupportedException</c> or <c>NotImplementedException</c>).
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA048

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA048"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the current value.
        ''' </summary>
        '''
        ''' <exception cref="FormatException">
        ''' Always.
        ''' </exception>
        Public Shared ReadOnly Property Current As Integer
            Get
                Throw New FormatException("The value is not available.") ' DCA048
            End Get
        End Property

#End Region

    End Class

End Namespace

#End Region
