#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

#End Region

#Region " DCA064 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA064: Enumerable.Cast or OfType with an incompatible type.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA064 (Warning) on <c>numbers.Cast(Of String)()</c>: an <see cref="Integer"/> is never a
    ''' <see cref="String"/>, so every element throws <see cref="InvalidCastException"/>. This rule runs only in .NET
    ''' Framework projects (on modern .NET, the built-in rule CA2021 reports the same code), so this file is only in the
    ''' .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): none (the conversion that the code needs, such as <c>Select</c> with <c>CStr</c>, depends on what
    ''' it means).
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA064

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA064"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Converts numbers to texts.
        ''' </summary>
        '''
        ''' <param name="numbers">
        ''' The numbers.
        ''' </param>
        '''
        ''' <returns>
        ''' The texts.
        ''' </returns>
        Friend Shared Function ToTexts(numbers As Integer()) As List(Of String)

            Return numbers.Cast(Of String)().ToList() ' DCA064
        End Function

#End Region

    End Class

End Namespace

#End Region
