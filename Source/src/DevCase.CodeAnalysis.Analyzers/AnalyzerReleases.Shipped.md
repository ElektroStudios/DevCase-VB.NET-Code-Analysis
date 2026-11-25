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

## Release 1.0.6

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
DCA006 | Design | Info | Design | Warning | NamespaceDeclarationAnalyzer
DCA046 | Reliability | Info | Reliability | Warning | ThrowInFinallyAnalyzer

## Release 1.0.8

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
DCA001 | Reliability | Warning | Reliability | Error | ConfigureAwaitAnalyzer
DCA002 | Reliability | Warning | Reliability | Error | TaskResultAnalyzer
DCA003 | Reliability | Info | Reliability | Warning | SilentGuardClauseAnalyzer
DCA005 | Design | Info | Design | Warning | ExplicitConstructorAnalyzer
DCA007 | Documentation | Info | Documentation | Warning | PublicMemberDocumentationAnalyzer
DCA008 | Documentation | Info | Documentation | Warning | XmlParameterDocumentationAnalyzer
DCA009 | Globalization | Info | Globalization | Warning | StringComparisonAnalyzer
DCA010 | Style | Info | Style | Warning | FlagsEnumBitShiftAnalyzer
DCA012 | Documentation | Info | Documentation | Warning | ExceptionDocumentationAnalyzer
DCA017 | Security | Warning | Security | Error | RegexTimeoutAnalyzer
DCA018 | Reliability | Info | Reliability | Warning | DateTimeKindAnalyzer
DCA019 | Globalization | Info | Globalization | Warning | DateTimeParseAnalyzer
DCA021 | Performance | Info | Performance | Warning | CollectionMethodAnalyzer
DCA022 | Performance | Info | Performance | Warning | IndexerAnalyzer
DCA023 | Style | Info | Style | Warning | InvertedComparisonAnalyzer
DCA025 | Style | Info | Style | Warning | RedundantNullCheckAnalyzer
DCA026 | Globalization | Info | Globalization | Warning | EncodingAnalyzer
DCA033 | Reliability | Info | Reliability | Warning | OnErrorStatementAnalyzer
DCA042 | Design | Warning | Design | Info | DuplicateBranchAnalyzer
DCA046 | Reliability | Warning | Reliability | Info | ThrowInFinallyAnalyzer
DCA054 | Reliability | Error | Reliability | Warning | ComImportClassCodeAnalyzer

## Release 1.0.9

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA059 | Design | Info | GenericOverloadAnalyzer
DCA060 | Design | Warning | DesignerSerializationAnalyzer
DCA061 | Reliability | Warning | ArgumentNullExceptionValueTypeAnalyzer
DCA062 | Reliability | Warning | TaskCompletionSourceOptionsAnalyzer
DCA063 | Reliability | Warning | EndOfStreamInAsyncAnalyzer
DCA064 | Reliability | Warning | IncompatibleCastAnalyzer
DCA065 | Reliability | Warning | BlockCopyCountAnalyzer
DCA066 | Reliability | Warning | ReferenceEqualsValueTypeAnalyzer
DCA067 | Style | Info | NameOfAnalyzer
DCA068 | Style | Info | CancellationCheckAnalyzer
DCA069 | Reliability | Warning | HasFlagEnumTypeAnalyzer
DCA070 | Reliability | Warning | ThreadStaticInitializationAnalyzer
DCA071 | Reliability | Info | CancellationTokenForwardingAnalyzer
DCA072 | Performance | Info | CountInsteadOfAnyAnalyzer
DCA073 | Performance | Info | CountMethodAnalyzer
DCA074 | Performance | Info | IsEmptyAnalyzer
DCA075 | Performance | Info | WhenAllSingleTaskAnalyzer
DCA076 | Performance | Info | WaitAllSingleTaskAnalyzer
DCA077 | Performance | Info | ContainsKeyBeforeRemoveAnalyzer
DCA078 | Performance | Info | ContainsBeforeSetChangeAnalyzer
DCA079 | Performance | Info | DictionaryContainsAnalyzer
DCA080 | Performance | Info | ManagedThreadIdAnalyzer
DCA081 | Performance | Info | TryGetValueAnalyzer
DCA082 | Performance | Info | IndexOfZeroAnalyzer
DCA083 | Style | Info | SubstringLengthAnalyzer
DCA084 | Performance | Info | ConstantArrayArgumentAnalyzer
