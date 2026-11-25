#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

#End Region

#Region " DCA072 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA072: Count() compared with zero instead of Any().
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA072 (Info) on <c>numbers.Count(...) &gt; 0</c>, which counts every matching element to know whether
    ''' there is one. This rule runs only in .NET Framework projects (on modern .NET, the built-in rule CA1827 reports the
    ''' same code), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>numbers.Any(...)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA072

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA072"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Determines whether a sequence has a positive number.
        ''' </summary>
        '''
        ''' <param name="numbers">
        ''' The numbers.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if a number is positive; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasPositive(numbers As IEnumerable(Of Integer)) As Boolean

            Return numbers.Count(Function(n As Integer) n > 0) > 0 ' DCA072
        End Function

#End Region

    End Class

End Namespace

#End Region
