#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Globalization
Imports System.Threading

#End Region

#Region " DCA011 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA011: Argument guard with a modern throw-helper equivalent.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA011 (Info) on the guard clause on <c>cultureName</c>, which has a modern equivalent
    ''' (<c>ArgumentException.ThrowIfNullOrEmpty</c>). This rule runs only in .NET Framework projects (on modern .NET, the
    ''' built-in rules CA1510 to CA1513 suggest the throw helpers directly), so this file is only in the .NET Framework project.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): <c>#If NETCOREAPP Then ArgumentException.ThrowIfNullOrEmpty(cultureName) #Else</c> (the original
    ''' guard, with its message) <c>#End If</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA011

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA011"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Sets the culture of the current thread.
        ''' </summary>
        '''
        ''' <param name="cultureName">
        ''' The name of the culture.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="cultureName"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        Public Shared Sub SetCulture(cultureName As String)

            ' DCA011: this guard clause has a throw helper on modern .NET.
            If String.IsNullOrEmpty(cultureName) Then
                Throw New ArgumentNullException(NameOf(cultureName), $"The {NameOf(cultureName)} cannot be null or empty.")
            End If

            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(cultureName)
        End Sub

#End Region

    End Class

End Namespace

#End Region
