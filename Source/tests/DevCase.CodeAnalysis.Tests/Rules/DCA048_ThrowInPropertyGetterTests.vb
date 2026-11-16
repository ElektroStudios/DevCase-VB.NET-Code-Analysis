#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

Imports Xunit

#End Region

#Region " DCA048_ThrowInPropertyGetterTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA048 (Exception thrown in a property getter).
    ''' </summary>
    Public NotInheritable Class DCA048_ThrowInPropertyGetterTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ThrowInPropertyGetterAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA048_ThrowInPropertyGetterTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA048 reports getters that throw unexpected exception types, and skips allowed types, setters, indexers and handled exceptions.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_UnexpectedExceptions() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Private value As Integer
    Public ReadOnly Property Parsed As Integer
        Get
            If Me.value < 0 Then
                [|Throw New ArgumentOutOfRangeException(NameOf(Me.value))|]
            End If
            Return Me.value
        End Get
    End Property
    Public ReadOnly Property State As Integer
        Get
            If Me.value = 0 Then
                Throw New ObjectDisposedException(NameOf(C))
            End If
            Throw New NotImplementedException()
        End Get
    End Property
    Public Property Item(index As Integer) As Integer
        Get
            Throw New ArgumentOutOfRangeException(NameOf(index))
        End Get
        Set(newValue As Integer)
            Throw New ArgumentException(NameOf(newValue))
        End Set
    End Property
    Public ReadOnly Property Safe As Integer
        Get
            Try
                Throw New FormatException()
            Catch ex As FormatException
                Return 0
            End Try
        End Get
    End Property
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
