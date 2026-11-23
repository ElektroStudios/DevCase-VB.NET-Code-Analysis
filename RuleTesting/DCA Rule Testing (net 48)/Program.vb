#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Program "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' The entry point of the project.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Every DCA<i>nnn</i>.vb file of this project triggers only the rule of its name. This file triggers no rule.
    ''' </remarks>
    Public NotInheritable Class Program

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="Program"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Entry Point "

        ''' <summary>
        ''' The entry point of the application.
        ''' </summary>
        <STAThread>
        Public Shared Sub Main()
        End Sub

#End Region

    End Class

End Namespace

#End Region
