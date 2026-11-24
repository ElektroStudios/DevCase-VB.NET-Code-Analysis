#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA007 "

Namespace DCA_Rule_Testing

    ' DCA007: this class has no XML documentation comment on purpose.
    ' Expected: DCA007 (Info) on the class, which has no XML documentation comment.
    ' Code fix (Ctrl+.): adds a ''' <summary> comment.
    Public NotInheritable Class DCA007

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA007"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

End Namespace

#End Region
