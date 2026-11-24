#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA046 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA046: Exception thrown in a Finally block.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA046 (Warning) on the <c>Throw</c> in the <c>Finally</c> block, which replaces the exception that is being
    ''' propagated. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA2219 reports the same
    ''' statements), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' No code fix: the right change depends on the intent of the code.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA046

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA046"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Processes the data.
        ''' </summary>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the cleanup fails.
        ''' </exception>
        Public Shared Sub Process()

            Try
                Console.WriteLine("Processing...")
            Finally
                Throw New InvalidOperationException("The cleanup failed.") ' DCA046
            End Try
        End Sub

#End Region

    End Class

End Namespace

#End Region
