#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis.CodeActions

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA018_DateTimeKindTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA018 (DateTime created without DateTimeKind) and its three code fixes.
    ''' </summary>
    Public NotInheritable Class DCA018_DateTimeKindTests

#Region " Fields "

        ''' <summary>
        ''' The equivalence keys of the three code actions, in registration order.
        ''' </summary>
        Private Shared ReadOnly ExpectedEquivalenceKeys As String() = {
            "DateTimeKindCodeFixProvider.Unspecified", "DateTimeKindCodeFixProvider.Utc", "DateTimeKindCodeFixProvider.Local"}

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DateTimeKindAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New DateTimeKindCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA018_DateTimeKindTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA018 reports <see cref="DateTime"/> constructors that have an overload with a <see cref="DateTimeKind"/>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ConstructorsWithAKindOverload() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Sub M(ticks As Long)
        Dim a As DateTime = [|New DateTime(2020, 1, 2)|]
        Dim b As DateTime = [|New DateTime(ticks)|]
        Dim c As Date = [|New Date(2020, 1, 2, 3, 4, 5)|]
        Dim d As DateTime = [|New DateTime(2020, 1, 2, 3, 4, 5, 6)|]
        Dim e As DateTime = New DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        Dim f As DateTime = New DateTime()
        Dim g As DateTime = New DateTime(2020, 1, 2, New Globalization.GregorianCalendar())
        Dim h As DateTime = DateTime.UtcNow
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>Unspecified</c> action of rule DCA018 adds the time components needed by the overload.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Unspecified_KeepsTheValueAndAddsTimeComponents() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly Start As New DateTime(2020, 1, 2)
End Class
", "
Public Class C
    Private ReadOnly Start As New DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)
End Class
", equivalenceKey:="DateTimeKindCodeFixProvider.Unspecified").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>Utc</c> action of rule DCA018 fixes the constructor that takes ticks.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Utc_OnTicks() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(ticks As Long) As DateTime
        Return New DateTime(ticks)
    End Function
End Class
", "
Public Class C
    Public Function M(ticks As Long) As DateTime
        Return New DateTime(ticks, DateTimeKind.Utc)
    End Function
End Class
", equivalenceKey:="DateTimeKindCodeFixProvider.Utc").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>Local</c> action of rule DCA018 writes named arguments and compiles on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_Local_WithNamedArguments_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M() As DateTime
        Return New DateTime(year:=2020, month:=1, day:=2)
    End Function
End Class
", "
Public Class C
    Public Function M() As DateTime
        Return New DateTime(year:=2020, month:=1, day:=2, hour:=0, minute:=0, second:=0, kind:=DateTimeKind.Local)
    End Function
End Class
", equivalenceKey:="DateTimeKindCodeFixProvider.Local", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA018 offers <c>Unspecified</c> first, because it keeps the current behavior.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_OffersUnspecifiedFirst() As Task
            Dim actions As List(Of CodeAction) = Await AnalyzerTestHost.GetRegisteredActionsAsync(Analyzer, Provider, "
Public Class C
    Private ReadOnly Start As New DateTime(2020, 1, 2)
End Class
").ConfigureAwait(True)
            Assert.Equal(DCA018_DateTimeKindTests.ExpectedEquivalenceKeys,
                         actions.Select(Function(a As CodeAction) a.EquivalenceKey).ToArray())
            Assert.Contains("keeps the current behavior", actions(0).Title, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Verifies that the constructor written by the <c>Unspecified</c> action creates the same value and kind as a constructor without a kind.
        ''' </summary>
        <Fact>
        Public Sub FixedConstructor_CreatesTheSameValue()
            ' Behavior check: the Unspecified fix is a no-op at run time.
            ' The constructor without a kind is invoked through reflection, so this file does not contain the code that the rule reports.
            Dim withoutKind As Reflection.ConstructorInfo = GetType(DateTime).GetConstructor({GetType(Integer), GetType(Integer), GetType(Integer)})
            Assert.NotNull(withoutKind)
            Dim original As DateTime = DirectCast(withoutKind.Invoke({2020, 1, 2}), DateTime)
            Dim fixedValue As New DateTime(2020, 1, 2, 0, 0, 0, DateTimeKind.Unspecified)

            Assert.Equal(original, fixedValue)
            Assert.Equal(original.Kind, fixedValue.Kind)
        End Sub

#End Region

    End Class

End Namespace

#End Region
