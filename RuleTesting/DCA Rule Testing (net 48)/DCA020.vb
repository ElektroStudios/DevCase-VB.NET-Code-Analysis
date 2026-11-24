#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DCA020 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA020: &lt;Flags&gt; enumeration without a zero value.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA020 (Warning) on the enumeration, which has no member whose value is zero. A zero member with any name
    ''' (<c>Null</c>, <c>Empty</c>...) would not be reported.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): adds <c>None = 0</c>.
    ''' </para>
    ''' </remarks>
    <Flags>
    Public Enum DCA020

        ''' <summary>
        ''' Read access.
        ''' </summary>
        Read = 1 << 0

        ''' <summary>
        ''' Write access.
        ''' </summary>
        Write = 1 << 1

    End Enum

End Namespace

#End Region
