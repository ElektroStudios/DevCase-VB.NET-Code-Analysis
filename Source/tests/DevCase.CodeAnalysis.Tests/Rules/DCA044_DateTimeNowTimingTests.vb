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

#Region " DCA044_DateTimeNowTimingTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA044 (DateTime.Now used to measure elapsed time).
    ''' </summary>
    Public NotInheritable Class DCA044_DateTimeNowTimingTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New DateTimeNowTimingAnalyzer()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA044_DateTimeNowTimingTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA044 reports Now - start and Now.Subtract(start) when start was assigned the current time, and skips other dates.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ElapsedTimeWithTheClock() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function M(deadline As Date) As Double
        Dim start As Date = DateTime.Now
        Dim elapsed As TimeSpan = [|DateTime.Now - start|]
        Dim utcStart As Date
        utcStart = DateTime.UtcNow
        Dim utcElapsed As TimeSpan = [|DateTime.UtcNow.Subtract(utcStart)|]
        Dim vbStart As Date = Now
        Dim vbElapsed As TimeSpan = [|Now - vbStart|]
        Dim remaining As TimeSpan = deadline - DateTime.Now
        Dim fixedDate As Date = deadline
        Dim other As TimeSpan = DateTime.Now - fixedDate
        Return elapsed.TotalSeconds + utcElapsed.TotalSeconds + vbElapsed.TotalSeconds + remaining.TotalSeconds + other.TotalSeconds
    End Function
End Class
").ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
