#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA047 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA047: Exception thrown in Finalize.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA047 (Warning) on the <c>Throw</c> in <c>Finalize</c>, which terminates the process.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public Class DCA047

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA047"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' The handle of a native resource.
        ''' </summary>
        Private ReadOnly handle As IntPtr

        ''' <summary>
        ''' Releases the resources of the instance.
        ''' </summary>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the native resource was never created.
        ''' </exception>
        Protected Overrides Sub Finalize()

            If Me.handle = IntPtr.Zero Then
                Throw New InvalidOperationException("The native resource was never created.") ' DCA047
            End If
            MyBase.Finalize()
        End Sub

#End Region

    End Class

End Namespace

#End Region
