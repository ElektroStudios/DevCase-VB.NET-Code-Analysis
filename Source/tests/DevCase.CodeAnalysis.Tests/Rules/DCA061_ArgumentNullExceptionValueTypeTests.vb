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

#Region " DCA061_ArgumentNullExceptionValueTypeTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA061 (ArgumentNullException for a value type, .NET Framework).
    ''' </summary>
    Public NotInheritable Class DCA061_ArgumentNullExceptionValueTypeTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ArgumentNullExceptionValueTypeAnalyzer()

        ''' <summary>
        ''' The expected messages of <see cref="Message_NamesTheParameterAndItsType"/>.
        ''' </summary>
        Private Shared ReadOnly Message_NamesTheParameterAndItsTypeExpected As String() = {"Do not throw 'ArgumentNullException' for 'count' because a 'Integer' value can never be Nothing"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA061_ArgumentNullExceptionValueTypeTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA061 reports <c>ArgumentNullException</c> for parameters of non-nullable value types (by
        ''' <c>NameOf</c> or by a literal, also of a lambda), and ignores reference types, nullable values, unconstrained type parameters
        ''' and names that are not parameters.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ValueTypeParameters() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(Of T As Structure, U)(count As Integer, name As String, maybe As Integer?, item As T, other As U, moment As Date)
        If count = 0 Then Throw [|New ArgumentNullException(NameOf(count))|]
        If moment = Nothing Then Throw [|New ArgumentNullException(""moment"", ""The date is required."")|]
        If item.Equals(Nothing) Then Throw [|New ArgumentNullException(paramName:=NameOf(item))|]
        If name Is Nothing Then Throw New ArgumentNullException(NameOf(name))
        If maybe Is Nothing Then Throw New ArgumentNullException(NameOf(maybe))
        If other Is Nothing Then Throw New ArgumentNullException(NameOf(other))
        If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count))
        If count = 1 Then Throw New ArgumentNullException(""Count"")
        If count = 2 Then Throw New ArgumentNullException(""unknown"")
        Dim check As Action(Of Integer) = Sub(level As Integer)
                                              If level = 0 Then Throw [|New ArgumentNullException(""level"")|]
                                          End Sub
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA061 does not run in a modern .NET project, where the built-in rule CA2264 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(Of T As Structure, U)(count As Integer, name As String, maybe As Integer?, item As T, other As U, moment As Date)
        If count = 0 Then Throw New ArgumentNullException(NameOf(count))
        If moment = Nothing Then Throw New ArgumentNullException(""moment"", ""The date is required."")
        If item.Equals(Nothing) Then Throw New ArgumentNullException(paramName:=NameOf(item))
        If name Is Nothing Then Throw New ArgumentNullException(NameOf(name))
        If maybe Is Nothing Then Throw New ArgumentNullException(NameOf(maybe))
        If other Is Nothing Then Throw New ArgumentNullException(NameOf(other))
        If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count))
        If count = 1 Then Throw New ArgumentNullException(""Count"")
        If count = 2 Then Throw New ArgumentNullException(""unknown"")
        Dim check As Action(Of Integer) = Sub(level As Integer)
                                              If level = 0 Then Throw New ArgumentNullException(""level"")
                                          End Sub
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies the message of rule DCA061.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Message_NamesTheParameterAndItsType() As Task
            Dim diagnostics As ImmutableArray(Of Diagnostic) = Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub Run(count As Integer)
        If count = 0 Then Throw [|New ArgumentNullException(NameOf(count))|]
    End Sub
End Class
",
                                                                                                         targetNetFramework:=True).ConfigureAwait(True)
            Assert.Equal(DCA061_ArgumentNullExceptionValueTypeTests.Message_NamesTheParameterAndItsTypeExpected, diagnostics.Select(Function(d As Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).ToArray())
        End Function

#End Region

    End Class

End Namespace

#End Region
