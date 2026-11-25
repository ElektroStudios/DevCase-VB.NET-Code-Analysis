#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA084 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA084: Constant array passed as an argument.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA084 (Info) on <c>{","c, ";"c}</c>, an array of constants that <c>IndexOfAny</c> receives on every call.
    ''' This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1861 reports the same code), so
    ''' this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): declares <c>Private Shared ReadOnly FindSeparatorIndexOfAny0 As Char() = {","c, ";"c}</c> before
    ''' the method, and passes it.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA084

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA084"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Finds the first separator of a text.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text.
        ''' </param>
        '''
        ''' <returns>
        ''' The index of the first comma or semicolon; -1 if there is none.
        ''' </returns>
        Friend Shared Function FindSeparator(text As String) As Integer

            Return text.IndexOfAny({","c, ";"c}) ' DCA084
        End Function

#End Region

    End Class

End Namespace

#End Region
