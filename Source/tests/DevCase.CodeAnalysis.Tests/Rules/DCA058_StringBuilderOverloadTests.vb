#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DCA058_StringBuilderOverloadTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA058 (StringBuilder argument converted to a string, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA058_StringBuilderOverloadTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New StringBuilderOverloadAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New StringBuilderOverloadCodeFixProvider()

        ''' <summary>
        ''' A source with every reported form.
        ''' </summary>
        Private Const ReportedSource As String = "
Imports System.Text

Public Class C
    Public Function Run(sb As StringBuilder, c As Char, count As Integer, flag As Boolean, price As Decimal, a As Long, b As Long) As String
        sb.Append([|New String(c, 2)|])
        sb.Append([|count.ToString()|])
        sb.Append([|price.ToString|]).Append([|c.ToString()|])
        sb.Insert(0, [|flag.ToString()|])
        sb.Append([|(a + b).ToString()|])
        sb.Append(value:=[|New String(c, count:=count)|])
        sb.Append([|New String(count:=3, c:=""-""c)|])
        Return sb.ToString()
    End Function
End Class
"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA058_StringBuilderOverloadTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA058 reports <c>ToString()</c> on primitive values and <c>New String(Char, Integer)</c> 
        ''' passed to <c>Append</c> and <c>Insert</c> in a .NET Framework project.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_ConvertedArguments_OnNetFramework() As Task
            Dim diagnostics As Collections.Immutable.ImmutableArray(Of Microsoft.CodeAnalysis.Diagnostic) =
                Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, ReportedSource, targetNetFramework:=True).ConfigureAwait(True)

            ' The message matches CA1830.
            Dim messages As String() = diagnostics.Select(Function(d As Microsoft.CodeAnalysis.Diagnostic) d.GetMessage(Globalization.CultureInfo.InvariantCulture)).Distinct(StringComparer.Ordinal).ToArray()
            Assert.Equal("Prefer strongly-typed StringBuilder overload", Assert.Single(messages))
        End Function

        ''' <summary>
        ''' Verifies that rule DCA058 does not run in a modern .NET project, where the built-in rule CA1830 applies.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer,
                ReportedSource.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal)).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA058 does not report formatted conversions, types without an overload (strings, dates, enumerations, 
        ''' nullable values), <c>AppendLine</c>, and <c>New String(Char, Integer)</c> passed to <c>Insert</c>.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_ConversionsWithoutOverload() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Text

Public Class C
    Public Function Run(sb As StringBuilder, c As Char, count As Integer, text As String, moment As Date, kind As DayOfWeek, maybe As Integer?) As String
        sb.Append(count.ToString(""X""))
        sb.Append(count.ToString(Globalization.CultureInfo.InvariantCulture))
        sb.Append(text.ToString())
        sb.Append(moment.ToString())
        sb.Append(kind.ToString())
        sb.Append(maybe.ToString())
        sb.AppendLine(count.ToString())
        sb.Insert(0, New String(c, 2))
        sb.Append(New String({c, c}))
        Return sb.ToString()
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA058 passes every value to its overload, keeping named arguments 
        ''' (the reported example: <c>sb.Append(New String(c, 2))</c> → <c>sb.Append(c, 2)</c>).
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_UsesTheStronglyTypedOverloads() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider,
                ReportedSource.Replace("[|", String.Empty, StringComparison.Ordinal).Replace("|]", String.Empty, StringComparison.Ordinal),
                NameOf(StringBuilderOverloadCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Imports System.Text

Public Class C
    Public Function Run(sb As StringBuilder, c As Char, count As Integer, flag As Boolean, price As Decimal, a As Long, b As Long) As String
        sb.Append(c, 2)
        sb.Append(count)
        sb.Append(price).Append(c)
        sb.Insert(0, flag)
        sb.Append(a + b)
        sb.Append(value:=c, repeatCount:=count)
        sb.Append(""-""c, 3)
        Return sb.ToString()
    End Function
End Class
", fixedText)
        End Function

#End Region

    End Class

End Namespace

#End Region
