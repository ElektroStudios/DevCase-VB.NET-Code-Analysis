#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA010 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA010: &lt;Flags&gt; enum member without bit-shift syntax (disabled by default).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA010 (Warning) on the members <c>Read</c> and <c>Write</c>, whose values are written as numbers. The rule is
    ''' disabled by default: the <c>.editorconfig</c> file of this solution enables it
    ''' (<c>devcase_analyzers.dca010_enabled = true</c>).
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Read = 1 &lt;&lt; 0</c>, <c>Write = 1 &lt;&lt; 1</c>.
    ''' </para>
    ''' </remarks>
    <Flags>
    Public Enum DCA010

        ''' <summary>
        ''' No access.
        ''' </summary>
        None = 0

        ''' <summary>
        ''' Read access.
        ''' </summary>
        Read = 1 ' DCA010

        ''' <summary>
        ''' Write access.
        ''' </summary>
        Write = 2 ' DCA010

    End Enum

End Namespace

#End Region
