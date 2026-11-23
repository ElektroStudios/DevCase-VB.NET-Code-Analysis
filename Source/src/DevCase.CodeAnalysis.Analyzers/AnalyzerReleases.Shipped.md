; Shipped analyzer releases
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

## Release 1.0.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA001 | Reliability | Error | ConfigureAwaitAnalyzer
DCA002 | Reliability | Error | TaskResultAnalyzer
DCA003 | Reliability | Error | SilentGuardClauseAnalyzer
DCA004 | Style | Warning | OptionStatementsAnalyzer
DCA005 | Design | Warning | ExplicitConstructorAnalyzer
DCA006 | Design | Warning | NamespaceDeclarationAnalyzer
DCA007 | Documentation | Warning | PublicMemberDocumentationAnalyzer
DCA008 | Documentation | Warning | XmlParameterDocumentationAnalyzer
DCA009 | Globalization | Warning | StringComparisonAnalyzer
DCA010 | Style | Warning | FlagsEnumBitShiftAnalyzer
DCA011 | Portability | Info | ThrowHelperMigrationAnalyzer
DCA012 | Documentation | Warning | ExceptionDocumentationAnalyzer
DCA013 | Portability | Info | NetFrameworkApiMigrationAnalyzer
DCA014 | Reliability | Warning | FileSystemLoopAnalyzer
DCA015 | Design | Warning | RecursiveCallAnalyzer
DCA016 | Reliability | Warning | AsyncSubAnalyzer
DCA017 | Security | Error | RegexTimeoutAnalyzer
DCA018 | Reliability | Warning | DateTimeKindAnalyzer
DCA019 | Globalization | Warning | DateTimeParseAnalyzer
DCA020 | Design | Warning | FlagsEnumNoneAnalyzer
DCA021 | Performance | Warning | CollectionMethodAnalyzer
DCA022 | Performance | Warning | IndexerAnalyzer
DCA023 | Style | Warning | InvertedComparisonAnalyzer
DCA024 | Design | Warning | RethrowOnlyCatchAnalyzer
DCA025 | Style | Warning | RedundantNullCheckAnalyzer

## Release 1.0.1

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA026 | Globalization | Warning | EncodingAnalyzer

## Release 1.0.2

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA027 | Reliability | Warning | ThreadSleepAnalyzer
DCA028 | Reliability | Warning | StaThreadAnalyzer
DCA029 | Style | Warning | PlusConcatenationAnalyzer
DCA030 | Style | Info | ArrayLiteralAnalyzer
DCA031 | Style | Info | ArrayRankOnNameAnalyzer
DCA032 | Reliability | Warning | EndStatementAnalyzer
DCA033 | Reliability | Warning | OnErrorStatementAnalyzer
DCA034 | Design | Info | GoToStatementAnalyzer
DCA035 | Reliability | Warning | DoEventsInLoopAnalyzer
DCA036 | Reliability | Warning | LockOnWeakIdentityAnalyzer
DCA037 | Reliability | Warning | MonitorEnterAnalyzer
DCA038 | Reliability | Warning | ThreadSuspendResumeAnalyzer
DCA039 | Reliability | Warning | BeginInvokeAnalyzer
DCA040 | Reliability | Warning | DuplicateConditionAnalyzer
DCA041 | Reliability | Warning | AllBranchesIdenticalAnalyzer
DCA042 | Design | Info | DuplicateBranchAnalyzer
DCA043 | Reliability | Warning | IdenticalOperandsAnalyzer
DCA044 | Reliability | Info | DateTimeNowTimingAnalyzer
DCA045 | Security | Warning | InsecureTempFileAnalyzer
DCA046 | Reliability | Warning | ThrowInFinallyAnalyzer
DCA047 | Reliability | Warning | ThrowInFinalizeAnalyzer
DCA048 | Design | Info | ThrowInPropertyGetterAnalyzer

## Release 1.0.3

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA049 | Performance | Info | SingleCharacterStringAnalyzer
DCA050 | Design | Info | UnvalidatedParameterAnalyzer
DCA051 | Reliability | Warning | DiscardedAwaiterAnalyzer

## Release 1.0.4

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA052 | Naming | Info | SenderParameterAnalyzer
DCA053 | Style | Info | MethodCallParenthesesAnalyzer

## Release 1.0.5

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA054 | Reliability | Warning | ComImportClassCodeAnalyzer
DCA055 | Performance | Info | ZeroLengthArrayAnalyzer
DCA056 | Performance | Info | AnyInsteadOfEmptinessCheckAnalyzer
DCA057 | Naming | Info | EnumerableParameterNameAnalyzer
DCA058 | Performance | Info | StringBuilderOverloadAnalyzer

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
DCA003 | Reliability | Warning | Reliability | Error | SilentGuardClauseAnalyzer
