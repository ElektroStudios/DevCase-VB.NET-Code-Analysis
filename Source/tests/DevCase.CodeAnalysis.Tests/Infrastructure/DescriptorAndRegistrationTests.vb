#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Reflection

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " DescriptorAndRegistrationTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of the descriptors, analyzer attributes and code fix exports.
    ''' </summary>
    Public NotInheritable Class DescriptorAndRegistrationTests

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DescriptorAndRegistrationTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the concrete analyzer types exported by the analyzers assembly.
        ''' </summary>
        '''
        ''' <returns>
        ''' The analyzer types.
        ''' </returns>
        Friend Shared Function GetAnalyzerTypes() As List(Of Type)

            Return GetType(ConfigureAwaitAnalyzer).Assembly.GetTypes().
                Where(Function(t As Type) GetType(DiagnosticAnalyzer).IsAssignableFrom(t) AndAlso Not t.IsAbstract).ToList()
        End Function

        ''' <summary>
        ''' Gets the concrete code fix provider types exported by the code fixes assembly.
        ''' </summary>
        '''
        ''' <returns>
        ''' The code fix provider types.
        ''' </returns>
        Friend Shared Function GetProviderTypes() As List(Of Type)

            Return GetType(ConfigureAwaitCodeFixProvider).Assembly.GetTypes().
                Where(Function(t As Type) GetType(CodeFixProvider).IsAssignableFrom(t) AndAlso Not t.IsAbstract).ToList()
        End Function

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that the assemblies export one analyzer per rule and one code fix provider per fixable rule.
        ''' </summary>
        <Fact>
        Public Sub AnalyzerAndProviderCounts_MatchTheRuleSet()
            Assert.Equal(26, DescriptorAndRegistrationTests.GetAnalyzerTypes().Count)
            Assert.Equal(25, DescriptorAndRegistrationTests.GetProviderTypes().Count)
        End Sub

        ''' <summary>
        ''' Verifies that every analyzer targets Visual Basic only and supports exactly one descriptor with a unique, well-formed identifier.
        ''' </summary>
        <Fact>
        Public Sub EveryAnalyzer_IsVisualBasicOnly_AndSupportsOneUniqueDescriptor()
            Dim ids As New HashSet(Of String)(StringComparer.Ordinal)
            For Each analyzerType As Type In DescriptorAndRegistrationTests.GetAnalyzerTypes()
                Dim attribute As DiagnosticAnalyzerAttribute = analyzerType.GetCustomAttribute(Of DiagnosticAnalyzerAttribute)()
                Assert.NotNull(attribute)
                Assert.Equal({LanguageNames.VisualBasic}, attribute.Languages)

                Dim analyzer As DiagnosticAnalyzer = DirectCast(Activator.CreateInstance(analyzerType), DiagnosticAnalyzer)
                Dim descriptor As DiagnosticDescriptor = Assert.Single(analyzer.SupportedDiagnostics)
                Assert.True(ids.Add(descriptor.Id), $"Duplicated id {descriptor.Id}")
                Assert.Matches("^DCA0(0[1-9]|1[0-9]|2[0-6])$", descriptor.Id)
                Assert.False(String.IsNullOrWhiteSpace(descriptor.Title.ToString()))
                Assert.False(String.IsNullOrWhiteSpace(descriptor.Description.ToString()))
                Assert.True(descriptor.IsEnabledByDefault)
            Next analyzerType
        End Sub

        ''' <summary>
        ''' Verifies that the default severity of every rule matches the documented specification.
        ''' </summary>
        <Fact>
        Public Sub Severities_MatchTheSpecification()
            Dim errors As String() = {DiagnosticIds.MissingConfigureAwait, DiagnosticIds.TaskResultAccess, DiagnosticIds.SilentGuardClause, DiagnosticIds.RegexWithoutTimeout}
            Dim infos As String() = {DiagnosticIds.ThrowHelperMigration, DiagnosticIds.NetFrameworkApiMigration}
            For Each analyzerType As Type In DescriptorAndRegistrationTests.GetAnalyzerTypes()
                Dim analyzer As DiagnosticAnalyzer = DirectCast(Activator.CreateInstance(analyzerType), DiagnosticAnalyzer)
                Dim descriptor As DiagnosticDescriptor = analyzer.SupportedDiagnostics(0)
                Dim expected As DiagnosticSeverity = DiagnosticSeverity.Warning
                If errors.Contains(descriptor.Id, StringComparer.Ordinal) Then
                    expected = DiagnosticSeverity.Error
                ElseIf infos.Contains(descriptor.Id, StringComparer.Ordinal) Then
                    expected = DiagnosticSeverity.Info
                End If
                Assert.Equal(expected, descriptor.DefaultSeverity)
            Next analyzerType
        End Sub

        ''' <summary>
        ''' Verifies that every code fix provider is exported for Visual Basic, supports Fix All, and fixes exactly one known rule.
        ''' </summary>
        <Fact>
        Public Sub EveryProvider_IsExported_HasFixAll_AndFixesAKnownId()
            Dim knownIds As HashSet(Of String) = DescriptorAndRegistrationTests.GetAnalyzerTypes().
                Select(Function(t As Type) DirectCast(Activator.CreateInstance(t), DiagnosticAnalyzer).SupportedDiagnostics(0).Id).
                ToHashSet(StringComparer.Ordinal)
            Dim fixedIds As New HashSet(Of String)(StringComparer.Ordinal)

            For Each providerType As Type In DescriptorAndRegistrationTests.GetProviderTypes()
                Dim export As ExportCodeFixProviderAttribute = providerType.GetCustomAttribute(Of ExportCodeFixProviderAttribute)()
                Assert.NotNull(export)
                Assert.Equal({LanguageNames.VisualBasic}, export.Languages)
                Assert.Equal(providerType.Name, export.Name)
                Assert.NotNull(providerType.GetCustomAttribute(Of Composition.SharedAttribute)())

                Dim provider As CodeFixProvider = DirectCast(Activator.CreateInstance(providerType), CodeFixProvider)
                Assert.NotNull(provider.GetFixAllProvider())
                For Each id As String In provider.FixableDiagnosticIds
                    Assert.Contains(id, knownIds)
                    Assert.True(fixedIds.Add(id), $"Id {id} is fixed by several providers.")
                Next id
            Next providerType

            ' Every rule has a code fix except DCA015 (recursion), which only reports.
            knownIds.Remove(DiagnosticIds.RecursiveCall)
            Assert.Equal(knownIds.OrderBy(Function(s As String) s, StringComparer.Ordinal), fixedIds.OrderBy(Function(s As String) s, StringComparer.Ordinal))
        End Sub

        ''' <summary>
        ''' Verifies that every message format can be formatted with the arguments its analyzer reports.
        ''' </summary>
        <Fact>
        Public Sub MessageFormats_AcceptTheirArguments()
            Dim samples As New Dictionary(Of DiagnosticDescriptor, Object()) From {
                {DevCaseDescriptors.MissingConfigureAwait, Array.Empty(Of Object)()},
                {DevCaseDescriptors.TaskResultAccess, {"Task(Of Integer)"}},
                {DevCaseDescriptors.SilentGuardClause, {"value"}},
                {DevCaseDescriptors.MissingOptionStatements, {"Option Strict On"}},
                {DevCaseDescriptors.MissingExplicitConstructor, {"C"}},
                {DevCaseDescriptors.TypeOutsideNamespace, {"C"}},
                {DevCaseDescriptors.MissingXmlDocumentation, {"Run"}},
                {DevCaseDescriptors.XmlParameterMismatch, {"Run", "missing <param> for 'a'"}},
                {DevCaseDescriptors.MissingStringComparison, {"String.StartsWith", "StringComparison"}},
                {DevCaseDescriptors.FlagsEnumWithoutBitShift, {"Read", "1 << 0"}},
                {DevCaseDescriptors.ThrowHelperMigration, {3, "ArgumentNullException.ThrowIfNull"}},
                {DevCaseDescriptors.MissingExceptionDocumentation, {"Run", "ArgumentNullException"}},
                {DevCaseDescriptors.NetFrameworkApiMigration, {"String.Contains(String)", "String.Contains(Char)"}},
                {DevCaseDescriptors.FileSystemLoopWithoutMessagePumping, {"Directory.GetFiles"}},
                {DevCaseDescriptors.RecursiveCall, {"Walk"}},
                {DevCaseDescriptors.AsyncSubNotEventHandler, {"Refresh"}}}

            For Each pair As KeyValuePair(Of DiagnosticDescriptor, Object()) In samples
                Dim message As String = Diagnostic.Create(pair.Key, Location.None, pair.Value).GetMessage(Globalization.CultureInfo.InvariantCulture)
                Assert.DoesNotContain("{", message, StringComparison.Ordinal)
            Next pair
        End Sub

#End Region

    End Class

End Namespace

#End Region
