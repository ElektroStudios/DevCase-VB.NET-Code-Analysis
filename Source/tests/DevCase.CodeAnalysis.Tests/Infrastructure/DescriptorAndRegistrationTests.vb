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
            Assert.Equal(58, DescriptorAndRegistrationTests.GetAnalyzerTypes().Count)
            Assert.Equal(40, DescriptorAndRegistrationTests.GetProviderTypes().Count)
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
                Assert.Matches("^DCA0(0[1-9]|[1-4][0-9]|5[0-8])$", descriptor.Id)
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
            Dim errors As String() = {DiagnosticIds.MissingConfigureAwait, DiagnosticIds.TaskResultAccess, DiagnosticIds.RegexWithoutTimeout}
            Dim infos As String() = {DiagnosticIds.ThrowHelperMigration, DiagnosticIds.NetFrameworkApiMigration, DiagnosticIds.ArrayCreationInsteadOfLiteral,
                                     DiagnosticIds.ArrayRankOnVariable, DiagnosticIds.GoToStatement, DiagnosticIds.DuplicateBranch,
                                     DiagnosticIds.DateTimeNowForTiming, DiagnosticIds.ThrowInPropertyGetter,
                                     DiagnosticIds.SingleCharacterString, DiagnosticIds.UnvalidatedParameter,
                                     DiagnosticIds.MisusedSenderParameter, DiagnosticIds.MethodCallWithoutParentheses,
                                     DiagnosticIds.ZeroLengthArrayAllocation, DiagnosticIds.AnyInsteadOfEmptinessCheck,
                                     DiagnosticIds.CollectionNameOnEnumerableParameter, DiagnosticIds.StringBuilderArgumentConvertedToString}
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

            ' Rules that only report: there is no change that keeps the behavior of the code.
            Dim reportOnly As String() = {
                DiagnosticIds.RecursiveCall, DiagnosticIds.EndStatement, DiagnosticIds.OnErrorStatement, DiagnosticIds.GoToStatement,
                DiagnosticIds.DoEventsInLoop, DiagnosticIds.LockOnWeakIdentity, DiagnosticIds.MonitorEnterWithoutFinally,
                DiagnosticIds.ThreadSuspendResume, DiagnosticIds.BeginInvokeWithoutEndInvoke, DiagnosticIds.DuplicateCondition,
                DiagnosticIds.AllBranchesIdentical, DiagnosticIds.DuplicateBranch, DiagnosticIds.IdenticalOperands,
                DiagnosticIds.DateTimeNowForTiming, DiagnosticIds.ThrowInFinally, DiagnosticIds.ThrowInFinalize, DiagnosticIds.ThrowInPropertyGetter,
                DiagnosticIds.DiscardedAwaiter}
            knownIds.ExceptWith(reportOnly)
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
                {DevCaseDescriptors.AsyncSubNotEventHandler, {"Refresh"}},
                {DevCaseDescriptors.ThreadSleepInAsyncMethod, Array.Empty(Of Object)()},
                {DevCaseDescriptors.MissingStaThread, {"Program.Main"}},
                {DevCaseDescriptors.StringConcatenationWithPlus, {"+", "&"}},
                {DevCaseDescriptors.ArrayCreationInsteadOfLiteral, {"Integer()"}},
                {DevCaseDescriptors.ArrayRankOnVariable, {"()", "values"}},
                {DevCaseDescriptors.EndStatement, Array.Empty(Of Object)()},
                {DevCaseDescriptors.OnErrorStatement, {"On Error Resume Next"}},
                {DevCaseDescriptors.GoToStatement, {"Retry"}},
                {DevCaseDescriptors.DoEventsInLoop, {"Application.DoEvents"}},
                {DevCaseDescriptors.LockOnWeakIdentity, {"'Me'"}},
                {DevCaseDescriptors.MonitorEnterWithoutFinally, {"Enter"}},
                {DevCaseDescriptors.ThreadSuspendResume, {"Suspend"}},
                {DevCaseDescriptors.BeginInvokeWithoutEndInvoke, {"Func`1"}},
                {DevCaseDescriptors.DuplicateCondition, {12}},
                {DevCaseDescriptors.AllBranchesIdentical, {"If"}},
                {DevCaseDescriptors.DuplicateBranch, {12}},
                {DevCaseDescriptors.IdenticalOperands, {"="}},
                {DevCaseDescriptors.DateTimeNowForTiming, {"DateTime.Now"}},
                {DevCaseDescriptors.InsecureTempFile, {"Path.GetTempFileName"}},
                {DevCaseDescriptors.ThrowInFinally, Array.Empty(Of Object)()},
                {DevCaseDescriptors.ThrowInFinalize, Array.Empty(Of Object)()},
                {DevCaseDescriptors.ThrowInPropertyGetter, {"Value", "ArgumentException"}},
                {DevCaseDescriptors.SingleCharacterString, {"""&""c", "String.Join"}},
                {DevCaseDescriptors.UnvalidatedParameter, {"directory", "UtilPaths.ContainsDirectory"}},
                {DevCaseDescriptors.DiscardedAwaiter, {"GetAwaiter"}},
                {DevCaseDescriptors.MisusedSenderParameter, {"UtilDrawing.ToPen", " to 'source'"}},
                {DevCaseDescriptors.MethodCallWithoutParentheses, {"ToArray"}},
                {DevCaseDescriptors.ComImportClassCode, {"Sub New", "ProgressDialog"}},
                {DevCaseDescriptors.ZeroLengthArrayAllocation, {"String"}},
                {DevCaseDescriptors.AnyInsteadOfEmptinessCheck, {"Count", "Queue(Of Integer)"}},
                {DevCaseDescriptors.CollectionNameOnEnumerableParameter, {"collection", "UtilCollections.AddRange", " to 'sequence'"}},
                {DevCaseDescriptors.StringBuilderArgumentConvertedToString, {"Append", "Char, Integer"}}}

            For Each pair As KeyValuePair(Of DiagnosticDescriptor, Object()) In samples
                Dim message As String = Diagnostic.Create(pair.Key, Location.None, pair.Value).GetMessage(Globalization.CultureInfo.InvariantCulture)
                Assert.DoesNotContain("{", message, StringComparison.Ordinal)
            Next pair
        End Sub

#End Region

    End Class

End Namespace

#End Region
