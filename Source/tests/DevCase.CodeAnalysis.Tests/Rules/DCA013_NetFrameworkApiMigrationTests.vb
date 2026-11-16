#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Threading.Tasks

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA013 (.NET Framework APIs with a modern .NET equivalent) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA013_NetFrameworkApiMigrationTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New NetFrameworkApiMigrationAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New NetFrameworkApiMigrationCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA013_NetFrameworkApiMigrationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA013 reports every entry of the migration table on .NET Framework.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_EveryTableEntry_OnNetFramework() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Public Class C
    Public Async Function Run(s As String, d As Dictionary(Of String, Integer), bytes As Byte(), items As String()) As Task
        Dim a As Boolean = [|s.Contains(""x"")|]
        Dim b As Boolean = [|s.StartsWith(""x"", StringComparison.Ordinal)|]
        Dim c As Boolean = [|s.IndexOf(""ab"", StringComparison.OrdinalIgnoreCase) >= 0|]
        Dim e As Boolean = [|s.IndexOf(""z""c) = -1|]
        Dim f As String = [|String.Join("","", items)|]
        Dim g As String() = [|s.Split({""ab""}, StringSplitOptions.None)|]
        [|If Not d.ContainsKey(s) Then d.Add(s, 1)|]
        Dim h As StringSplitOptions = [|DirectCast([Enum].Parse(GetType(StringSplitOptions), s), StringSplitOptions)|]
        Dim i As String = [|BitConverter.ToString(bytes).Replace(""-"", """")|]
        Dim j As Integer = [|Process.GetCurrentProcess().Id|]
        Dim k As String = [|Process.GetCurrentProcess().MainModule.FileName|]
        Dim l As String = [|File.ReadAllText(s)|]
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA013 does not report usages without an exact modern equivalent, or already inside a conditional block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_NonEquivalentOrAlreadyConditional() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.IO
Public Class C
    Public Sub Run(s As String, items As String())
        Dim a As Boolean = s.StartsWith(""x"")
        Dim b As Boolean = s.Contains(""xy"")
        Dim c As Boolean = s.IndexOf(""ab"") >= 0
        Dim d As String = String.Join("", "", items)
        Dim e As String = File.ReadAllText(s)
#If NETCOREAPP Then
        Dim f As Boolean = s.Contains(""x""c)
#Else
        Dim f As Boolean = s.Contains(""x"")
#End If
    End Sub
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that rule DCA013 does not report on modern .NET, where the modern APIs can be used directly.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function DoesNotReport_OnModernDotNet() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Public Class C
    Public Function Run(s As String) As Boolean
        Return s.Contains(""x"")
    End Function
End Class
").ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that the code fix of rule DCA013 wraps the statement in a conditional compilation block.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_WrapsStatementInConditionalBlock() As Task
            Await AnalyzerTestHost.VerifyCodeFixAsync(Analyzer, Provider, "
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Public Class C
    Public Async Function Run(s As String, d As Dictionary(Of String, Integer), bytes As Byte()) As Task
        If s.IndexOf(""ab"", StringComparison.Ordinal) < 0 AndAlso s.Contains(""-"") Then ' check
            Return
        End If
        If Not d.ContainsKey(s) Then
            d.Add(s, 1)
        End If
        Dim text As String = File.ReadAllText(s).Trim() & BitConverter.ToString(bytes).Replace(""-"", String.Empty)
        Console.WriteLine(Process.GetCurrentProcess().Id)
    End Function
End Class
", "
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Public Class C
    Public Async Function Run(s As String, d As Dictionary(Of String, Integer), bytes As Byte()) As Task
#If NETCOREAPP Then
        If (Not s.Contains(""ab"", StringComparison.Ordinal)) AndAlso s.Contains(""-""c) Then ' check
#Else
        If s.IndexOf(""ab"", StringComparison.Ordinal) < 0 AndAlso s.Contains(""-"") Then ' check
#End If
            Return
        End If
#If NETCOREAPP Then
        d.TryAdd(s, 1)
#Else
        If Not d.ContainsKey(s) Then
            d.Add(s, 1)
        End If
#End If
#If NET5_0_OR_GREATER Then
        Dim text As String = (Await File.ReadAllTextAsync(s).ConfigureAwait(False)).Trim() & Convert.ToHexString(bytes)
#Else
        Dim text As String = File.ReadAllText(s).Trim() & BitConverter.ToString(bytes).Replace(""-"", String.Empty)
#End If
#If NET5_0_OR_GREATER Then
        Console.WriteLine(Environment.ProcessId)
#Else
        Console.WriteLine(Process.GetCurrentProcess().Id)
#End If
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

        ''' <summary>
        ''' Verifies that Fix All of rule DCA013 produces one conditional block for two usages in the same statement.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function FixAll_TwoUsagesInSameStatement_ProduceOneBlock() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Public Class C
    Public Function Run(s As String, items As String()) As String
        Return String.Join("","", items) & s.Contains(""x"").ToString()
    End Function
End Class
", NameOf(NetFrameworkApiMigrationCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            AnalyzerTestHost.AssertTextEqual("
Public Class C
    Public Function Run(s As String, items As String()) As String
#If NETCOREAPP Then
        Return String.Join("",""c, items) & s.Contains(""x""c).ToString()
#Else
        Return String.Join("","", items) & s.Contains(""x"").ToString()
#End If
    End Function
End Class
", fixedText)
        End Function

        ''' <summary>
        ''' Verifies that the modern branch written by the code fix of rule DCA013 compiles on .NET.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Fix_ModernBranchCompilesOnDotNet() As Task
            Dim fixedText As String = Await AnalyzerTestHost.ApplyFixAllInDocumentAsync(Analyzer, Provider, "
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.IO
Public Class C
    Public Async Function Run(s As String, d As Dictionary(Of String, Integer), bytes As Byte(), items As String()) As Task
        Dim a As Boolean = s.Contains(""x"")
        Dim b As Boolean = s.EndsWith(""x"", StringComparison.Ordinal)
        Dim c As Boolean = s.IndexOf(""ab"", StringComparison.OrdinalIgnoreCase) > -1
        Dim f As String = String.Join("","", items)
        Dim g As String() = s.Split(New String() {""ab""}, 2, StringSplitOptions.None)
        If Not d.ContainsKey(s) Then d.Add(s, 1)
        Dim h As StringSplitOptions = CType([Enum].Parse(GetType(StringSplitOptions), s, True), StringSplitOptions)
        Dim i As String = BitConverter.ToString(bytes).Replace(""-"", """")
        Dim k As String = Process.GetCurrentProcess().MainModule.FileName
        File.WriteAllText(s, s)
    End Function
End Class
", NameOf(NetFrameworkApiMigrationCodeFixProvider), targetNetFramework:=True).ConfigureAwait(True)

            ' The original branch still compiles on .NET Framework...
            Await AnalyzerTestHost.AssertCompilesAsync(AnalyzerTestHost.CreateProject(fixedText, targetNetFramework:=True), ".NET Framework branch").ConfigureAwait(True)

            ' ...and the modern branch compiles on .NET.
            Dim project As Microsoft.CodeAnalysis.Project = AnalyzerTestHost.CreateProject(fixedText)
            Dim options As Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions =
                DirectCast(project.ParseOptions, Microsoft.CodeAnalysis.VisualBasic.VisualBasicParseOptions).WithPreprocessorSymbols(
                    New KeyValuePair(Of String, Object)("NETCOREAPP", True),
                    New KeyValuePair(Of String, Object)("NET5_0_OR_GREATER", True),
                    New KeyValuePair(Of String, Object)("NET6_0_OR_GREATER", True))
            Await AnalyzerTestHost.AssertCompilesAsync(project.WithParseOptions(options), "modern .NET branch").ConfigureAwait(True)

            Assert.Contains("d.TryAdd(s, 1)", fixedText, StringComparison.Ordinal)
            Assert.Contains("[Enum].Parse(Of StringSplitOptions)(s, True)", fixedText, StringComparison.Ordinal)
            Assert.Contains("s.Split(""ab"", 2, StringSplitOptions.None)", fixedText, StringComparison.Ordinal)
            Assert.Contains("Await File.WriteAllTextAsync(s, s).ConfigureAwait(False)", fixedText, StringComparison.Ordinal)
            Assert.Contains("#If NET6_0_OR_GREATER Then", fixedText, StringComparison.Ordinal)
        End Function

#End Region

    End Class

End Namespace
