#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA006 "

''' <summary>
''' DCA006: Type in the global namespace.
''' </summary>
'''
''' <remarks>
''' <para>
''' Expected: DCA006 (Info) on the class, which is declared outside a <c>Namespace</c> block (the project has no root
''' namespace). This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1050 reports the same
''' types), so this file is only in the .NET Framework project.
''' </para>
''' <para>
''' Code fix (Ctrl+.): wraps the class in a <c>Namespace</c> block.
''' </para>
''' </remarks>
Public NotInheritable Class DCA006

#Region " Constructors "

    ''' <summary>
    ''' Prevents a default instance of the <see cref="DCA006"/> class from being created.
    ''' </summary>
    Private Sub New()
    End Sub

#End Region

End Class

#End Region
