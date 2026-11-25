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

Imports Xunit

#End Region

#Region " DCA064_IncompatibleCastTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA064 (incompatible Cast or OfType, .NET Framework).
    ''' </summary>
    Public NotInheritable Class DCA064_IncompatibleCastTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New IncompatibleCastAnalyzer()

        ''' <summary>
        ''' The expected messages of <see cref="Message_ExplainsTheResult"/>.
        ''' </summary>
        Private Shared ReadOnly Message_ExplainsTheResultExpected As String() = {"Do not call 'Cast(Of String)' on a sequence of 'Integer' because every element throws InvalidCastException", "Do not call 'OfType(Of Integer)' on a sequence of 'String' because the result is always empty"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA064_IncompatibleCastTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA064 reports <c>Cast</c> and <c>OfType</c> between types that a run-time check can never convert, and
        ''' ignores base and derived classes, interfaces that the element can implement, <see cref="Object"/>, <see cref="ValueType"/>,
        ''' nullable values and enumerations with their underlying type.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ConversionsThatAlwaysFail() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(numbers As Integer(), names As List(Of String), items As IEnumerable(Of Object), shapes As List(Of Shape), days As DayOfWeek())
        Dim a As IEnumerable(Of String) = [|numbers.Cast(Of String)()|]
        Dim b As IEnumerable(Of Integer) = [|names.OfType(Of Integer)()|]
        Dim c As IEnumerable(Of String) = items.Cast(Of String)()
        Dim d As IEnumerable(Of Circle) = shapes.OfType(Of Circle)()
        Dim e As IEnumerable(Of Integer) = days.Cast(Of Integer)()
        Dim f As IEnumerable(Of IDisposable) = shapes.Cast(Of IDisposable)()
        Dim g As IEnumerable(Of IDisposable) = [|names.Cast(Of IDisposable)()|]
        Dim h As IEnumerable(Of IComparable) = names.Cast(Of IComparable)()
        Dim i As IEnumerable(Of Long) = [|numbers.Cast(Of Long)()|]
        Dim j As IEnumerable(Of Integer?) = numbers.Cast(Of Integer?)()
        Dim k As IEnumerable(Of String) = [|Enumerable.Cast(Of String)(numbers)|]
        Dim l As IEnumerable(Of ValueType) = numbers.Cast(Of ValueType)()
        Dim m As IEnumerable(Of Shape) = [|names.Cast(Of Shape)()|]
        Dim n As IEnumerable(Of IComparable) = numbers.Cast(Of IComparable)()
        Dim o As IEnumerable(Of DayOfWeek) = numbers.Cast(Of DayOfWeek)()
    End Sub
End Class

Public Class Shape
End Class

Public Class Circle
    Inherits Shape
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA064 does not run in a modern .NET project, where the built-in rule CA2021 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(numbers As Integer(), names As List(Of String), items As IEnumerable(Of Object), shapes As List(Of Shape), days As DayOfWeek())
        Dim a As IEnumerable(Of String) = numbers.Cast(Of String)()
        Dim b As IEnumerable(Of Integer) = names.OfType(Of Integer)()
        Dim c As IEnumerable(Of String) = items.Cast(Of String)()
        Dim d As IEnumerable(Of Circle) = shapes.OfType(Of Circle)()
        Dim e As IEnumerable(Of Integer) = days.Cast(Of Integer)()
        Dim f As IEnumerable(Of IDisposable) = shapes.Cast(Of IDisposable)()
        Dim g As IEnumerable(Of IDisposable) = names.Cast(Of IDisposable)()
        Dim h As IEnumerable(Of IComparable) = names.Cast(Of IComparable)()
        Dim i As IEnumerable(Of Long) = numbers.Cast(Of Long)()
        Dim j As IEnumerable(Of Integer?) = numbers.Cast(Of Integer?)()
        Dim k As IEnumerable(Of String) = Enumerable.Cast(Of String)(numbers)
        Dim l As IEnumerable(Of ValueType) = numbers.Cast(Of ValueType)()
        Dim m As IEnumerable(Of Shape) = names.Cast(Of Shape)()
        Dim n As IEnumerable(Of IComparable) = numbers.Cast(Of IComparable)()
        Dim o As IEnumerable(Of DayOfWeek) = numbers.Cast(Of DayOfWeek)()
    End Sub
End Class

Public Class Shape
End Class

Public Class Circle
    Inherits Shape
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the messages of rule DCA064 for <c>Cast</c> and <c>OfType</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_ExplainsTheResult() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(numbers As Integer(), names As List(Of String))
        Dim a As IEnumerable(Of String) = [|numbers.Cast(Of String)()|]
        Dim b As IEnumerable(Of Integer) = [|names.OfType(Of Integer)()|]
    End Sub
End Class
",
                                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA064_IncompatibleCastTests.Message_ExplainsTheResultExpected, diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
