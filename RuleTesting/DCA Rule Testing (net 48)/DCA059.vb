#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Runtime.InteropServices

#End Region

#Region " DCA059 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA059: System.Type argument instead of a generic overload.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA059 (Info) on <c>Activator.CreateInstance(GetType(T))</c> and
    ''' <c>Marshal.SizeOf(GetType(NativePoint))</c>, whose generic overloads take the type as a type argument. This rule
    ''' runs only in .NET Framework projects (on modern .NET, the built-in rule CA2263 reports the same code), so this file
    ''' is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>Activator.CreateInstance(Of T)()</c>, without the <see langword="DirectCast"/> that it makes
    ''' redundant, and <c>Marshal.SizeOf(Of NativePoint)()</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA059

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA059"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Creates an instance of a type.
        ''' </summary>
        '''
        ''' <typeparam name="T">
        ''' The type to create.
        ''' </typeparam>
        '''
        ''' <returns>
        ''' The new instance.
        ''' </returns>
        Friend Shared Function Create(Of T)() As T

            Return DirectCast(Activator.CreateInstance(GetType(T)), T) ' DCA059
        End Function

        ''' <summary>
        ''' Gets the size of a native point.
        ''' </summary>
        '''
        ''' <returns>
        ''' The size, in bytes.
        ''' </returns>
        Friend Shared Function GetPointSize() As Integer

            Return Marshal.SizeOf(GetType(NativePoint)) ' DCA059
        End Function

        ''' <summary>
        ''' A point of a native API.
        ''' </summary>
        <StructLayout(LayoutKind.Sequential)>
        Friend Structure NativePoint

            ''' <summary>
            ''' The horizontal coordinate.
            ''' </summary>
            Friend X As Integer

            ''' <summary>
            ''' The vertical coordinate.
            ''' </summary>
            Friend Y As Integer

        End Structure

#End Region

    End Class

End Namespace

#End Region
