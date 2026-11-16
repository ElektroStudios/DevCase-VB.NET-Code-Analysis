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

#Region " DCA019_DateTimeParseTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA019 (date and time parsed without a format provider) and its two code fixes.
    ''' </summary>
    Public NotInheritable Class DCA019_DateTimeParseTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DateTimeParseAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New DateTimeParseCodeFixProvider()

        ''' <summary>
        ''' The equivalence key of the <c>CultureInfo.CurrentCulture</c> action.
        ''' </summary>
        Private Const CurrentKey As String = "DateTimeParseCodeFixProvider.CurrentCulture"

        ''' <summary>
        ''' The equivalence key of the <c>CultureInfo.InvariantCulture</c> action.
        ''' </summary>
        Private Const InvariantKey As String = "DateTimeParseCodeFixProvider.InvariantCulture"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA019_DateTimeParseTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA019 reports date and time parsing without a format provider.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ParsingWithoutProvider() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Globalization
Public Class C
    Public Sub M(s As String)
        Dim d As DateTime
        Dim t As TimeSpan
        Dim a As DateTime = [|DateTime.Parse(s)|]
        Dim b As Boolean = [|DateTime.TryParse(s, d)|]
        Dim c As TimeSpan = [|TimeSpan.Parse(s)|]
        Dim e As DateTimeOffset = [|DateTimeOffset.Parse(s)|]
        Dim f As Boolean = [|TimeSpan.TryParse(s, t)|]
        Dim g As DateTime = DateTime.Parse(s, CultureInfo.InvariantCulture)
        Dim h As DateTime = DateTime.ParseExact(s, ""yyyy-MM-dd"", CultureInfo.InvariantCulture)
        Dim i As Integer = Integer.Parse(s)
    End Sub
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the <c>CurrentCulture</c> action of rule DCA019 appends the format provider.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_CurrentCulture_Appended() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Globalization
Public Class C
    Public Function M(s As String) As DateTime
        Return DateTime.Parse(s)
    End Function
End Class
", "
Imports System.Globalization
Public Class C
    Public Function M(s As String) As DateTime
        Return DateTime.Parse(s, CultureInfo.CurrentCulture)
    End Function
End Class
", equivalenceKey:=CurrentKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA019 inserts the provider before the result argument of <c>TryParse</c> on .NET.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_TryParse_InsertsBeforeTheResult_Modern() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Public Class C
    Public Function M(s As String, ByRef result As DateTime) As Boolean
        Return DateTime.TryParse(s, result)
    End Function
End Class
", "
Public Class C
    Public Function M(s As String, ByRef result As DateTime) As Boolean
        Return DateTime.TryParse(s, Globalization.CultureInfo.InvariantCulture, result)
    End Function
End Class
", equivalenceKey:=InvariantKey).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA019 also adds <c>DateTimeStyles.None</c> on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_TryParse_AddsStyles_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Globalization
Public Class C
    Public Function M(s As String, ByRef result As DateTime) As Boolean
        Return DateTime.TryParse(s, result)
    End Function
End Class
", "
Imports System.Globalization
Public Class C
    Public Function M(s As String, ByRef result As DateTime) As Boolean
        Return DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, result)
    End Function
End Class
", equivalenceKey:=InvariantKey, targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA019 appends a named argument when the call uses named arguments.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_NamedArguments_AreAppended() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Globalization
Public Class C
    Public Function M(value As String) As DateTime
        Return DateTime.Parse(s:=value)
    End Function
End Class
", "
Imports System.Globalization
Public Class C
    Public Function M(value As String) As DateTime
        Return DateTime.Parse(s:=value, provider:=CultureInfo.InvariantCulture)
    End Function
End Class
", equivalenceKey:=InvariantKey).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
