#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA067_NameOfTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA067 (string literal instead of NameOf, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA067_NameOfTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New NameOfAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA067_NameOfTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA067 reports literals passed to <c>paramName</c> and <c>propertyName</c> that name a parameter or a
        ''' property, and ignores other cases, unknown names and other parameters.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_LiteralNamesOfParametersAndProperties() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.ComponentModel

Public Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private titleValue As String

    Public Property Title As String
        Get
            Return Me.titleValue
        End Get
        Set(value As String)
            Me.titleValue = value
            Me.OnPropertyChanged([|""Title""|])
        End Set
    End Property

    Public Sub Run(path As String, [error] As String)
        If path Is Nothing Then Throw New ArgumentNullException([|""path""|])
        If [error] Is Nothing Then Throw New ArgumentNullException([|""error""|])
        If path.Length = 0 Then Throw New ArgumentException(""The path is empty."", [|""path""|])
    End Sub

    Public Sub NotReported(path As String)
        If path Is Nothing Then Throw New ArgumentNullException(""Path"")
        If path Is Nothing Then Throw New ArgumentNullException(""missing"")
        If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))
        Me.OnPropertyChanged(""Unknown"")
        Console.WriteLine(""path"")
    End Sub

    Protected Sub OnPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA067 does not run in a modern .NET project, where the built-in rule CA1507 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.ComponentModel

Public Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private titleValue As String

    Public Property Title As String
        Get
            Return Me.titleValue
        End Get
        Set(value As String)
            Me.titleValue = value
            Me.OnPropertyChanged(""Title"")
        End Set
    End Property

    Public Sub Run(path As String, [error] As String)
        If path Is Nothing Then Throw New ArgumentNullException(""path"")
        If [error] Is Nothing Then Throw New ArgumentNullException(""error"")
        If path.Length = 0 Then Throw New ArgumentException(""The path is empty."", ""path"")
    End Sub

    Public Sub NotReported(path As String)
        If path Is Nothing Then Throw New ArgumentNullException(""Path"")
        If path Is Nothing Then Throw New ArgumentNullException(""missing"")
        If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))
        Me.OnPropertyChanged(""Unknown"")
        Console.WriteLine(""path"")
    End Sub

    Protected Sub OnPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA067 writes <c>NameOf</c>, escaping keywords.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_UsesNameOf_WithBracketsForKeywords() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.ComponentModel

Public Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private titleValue As String

    Public Property Title As String
        Get
            Return Me.titleValue
        End Get
        Set(value As String)
            Me.titleValue = value
            Me.OnPropertyChanged([|""Title""|])
        End Set
    End Property

    Public Sub Run(path As String, [error] As String)
        If path Is Nothing Then Throw New ArgumentNullException([|""path""|])
        If [error] Is Nothing Then Throw New ArgumentNullException([|""error""|])
        If path.Length = 0 Then Throw New ArgumentException(""The path is empty."", [|""path""|])
    End Sub

    Public Sub NotReported(path As String)
        If path Is Nothing Then Throw New ArgumentNullException(""Path"")
        If path Is Nothing Then Throw New ArgumentNullException(""missing"")
        If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))
        Me.OnPropertyChanged(""Unknown"")
        Console.WriteLine(""path"")
    End Sub

    Protected Sub OnPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
End Class
", "
Imports System.ComponentModel

Public Class C
    Implements INotifyPropertyChanged

    Public Event PropertyChanged As PropertyChangedEventHandler Implements INotifyPropertyChanged.PropertyChanged

    Private titleValue As String

    Public Property Title As String
        Get
            Return Me.titleValue
        End Get
        Set(value As String)
            Me.titleValue = value
            Me.OnPropertyChanged(NameOf(Title))
        End Set
    End Property

    Public Sub Run(path As String, [error] As String)
        If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))
        If [error] Is Nothing Then Throw New ArgumentNullException(NameOf([error]))
        If path.Length = 0 Then Throw New ArgumentException(""The path is empty."", NameOf(path))
    End Sub

    Public Sub NotReported(path As String)
        If path Is Nothing Then Throw New ArgumentNullException(""Path"")
        If path Is Nothing Then Throw New ArgumentNullException(""missing"")
        If path Is Nothing Then Throw New ArgumentNullException(NameOf(path))
        Me.OnPropertyChanged(""Unknown"")
        Console.WriteLine(""path"")
    End Sub

    Protected Sub OnPropertyChanged(propertyName As String)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub
End Class
",
                                                      equivalenceKey:="DCA067.NameOf", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
