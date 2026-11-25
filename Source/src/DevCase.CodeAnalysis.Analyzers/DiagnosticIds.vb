#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " DiagnosticIds "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the unique identifiers of every diagnostic rule exposed by the DevCase analyzers.
    ''' </summary>
    Public NotInheritable Class DiagnosticIds

#Region " Constants "

        ''' <summary>
        ''' The identifier of the rule that reports <see langword="Await"/> expressions lacking <c>ConfigureAwait(False)</c>.
        ''' </summary>
        Public Const MissingConfigureAwait As String = "DCA001"

        ''' <summary>
        ''' The identifier of the rule that reports blocking <c>.Result</c> property accesses on tasks.
        ''' </summary>
        Public Const TaskResultAccess As String = "DCA002"

        ''' <summary>
        ''' The identifier of the rule that reports silent parameter guard clauses.
        ''' </summary>
        Public Const SilentGuardClause As String = "DCA003"

        ''' <summary>
        ''' The identifier of the rule that reports missing <c>Option</c> statements.
        ''' </summary>
        Public Const MissingOptionStatements As String = "DCA004"

        ''' <summary>
        ''' The identifier of the rule that reports classes lacking an explicitly declared instance constructor.
        ''' </summary>
        Public Const MissingExplicitConstructor As String = "DCA005"

        ''' <summary>
        ''' The identifier of the rule that reports top-level types declared outside a <see langword="Namespace"/> block.
        ''' </summary>
        Public Const TypeOutsideNamespace As String = "DCA006"

        ''' <summary>
        ''' The identifier of the rule that reports <see langword="Public"/> members lacking XML documentation.
        ''' </summary>
        Public Const MissingXmlDocumentation As String = "DCA007"

        ''' <summary>
        ''' The identifier of the rule that reports mismatches between parameter lists and <c>&lt;param&gt;</c> tags.
        ''' </summary>
        Public Const XmlParameterMismatch As String = "DCA008"

        ''' <summary>
        ''' The identifier of the rule that reports invocations omitting an available string comparison argument.
        ''' </summary>
        Public Const MissingStringComparison As String = "DCA009"

        ''' <summary>
        ''' The identifier of the rule that reports <c>&lt;Flags&gt;</c> enum members declared with direct numeric literals.
        ''' </summary>
        Public Const FlagsEnumWithoutBitShift As String = "DCA010"

        ''' <summary>
        ''' The identifier of the rule that reports .NET Framework argument guards that have a modern throw-helper equivalent.
        ''' </summary>
        Public Const ThrowHelperMigration As String = "DCA011"

        ''' <summary>
        ''' The identifier of the rule that reports thrown exceptions that are not documented with an <c>&lt;exception&gt;</c> tag.
        ''' </summary>
        Public Const MissingExceptionDocumentation As String = "DCA012"

        ''' <summary>
        ''' The identifier of the rule that reports .NET Framework API usages that have a modern .NET equivalent.
        ''' </summary>
        Public Const NetFrameworkApiMigration As String = "DCA013"

        ''' <summary>
        ''' The identifier of the rule that reports file-system enumeration loops without <c>Thread.CurrentThread.Join(0)</c>.
        ''' </summary>
        Public Const FileSystemLoopWithoutMessagePumping As String = "DCA014"

        ''' <summary>
        ''' The identifier of the rule that reports methods and properties that call themselves.
        ''' </summary>
        Public Const RecursiveCall As String = "DCA015"

        ''' <summary>
        ''' The identifier of the rule that reports <see langword="Async"/> <see langword="Sub"/> methods that are not event handlers.
        ''' </summary>
        Public Const AsyncSubNotEventHandler As String = "DCA016"

        ''' <summary>
        ''' The identifier of the rule that reports regular expressions evaluated without a match timeout (ReDoS).
        ''' </summary>
        Public Const RegexWithoutTimeout As String = "DCA017"

        ''' <summary>
        ''' The identifier of the rule that reports <c>New DateTime(...)</c> without a <see cref="DateTimeKind"/>.
        ''' </summary>
        Public Const DateTimeWithoutKind As String = "DCA018"

        ''' <summary>
        ''' The identifier of the rule that reports date and time parsing without an <see cref="IFormatProvider"/>.
        ''' </summary>
        Public Const DateTimeParseWithoutProvider As String = "DCA019"

        ''' <summary>
        ''' The identifier of the rule that reports <c>&lt;Flags&gt;</c> enumerations whose zero value is not a member named <c>None</c>.
        ''' </summary>
        Public Const FlagsEnumWithoutNone As String = "DCA020"

        ''' <summary>
        ''' The identifier of the rule that reports LINQ <c>FirstOrDefault</c>/<c>Any</c>/<c>All</c> calls on lists and arrays that have a specific <c>Find</c>/<c>Exists</c>/<c>TrueForAll</c> method.
        ''' </summary>
        Public Const LinqInsteadOfCollectionMethod As String = "DCA021"

        ''' <summary>
        ''' The identifier of the rule that reports LINQ <c>First</c>/<c>Last</c>/<c>ElementAt</c> calls on <c>IList(Of T)</c> collections that can be indexed directly.
        ''' </summary>
        Public Const LinqInsteadOfIndexer As String = "DCA022"

        ''' <summary>
        ''' The identifier of the rule that reports negated comparisons that can use the inverse operator.
        ''' </summary>
        Public Const InvertedComparison As String = "DCA023"

        ''' <summary>
        ''' The identifier of the rule that reports <see langword="Catch"/> blocks that only rethrow the exception.
        ''' </summary>
        Public Const RethrowOnlyCatch As String = "DCA024"

        ''' <summary>
        ''' The identifier of the rule that reports null checks that are redundant before a <see langword="TypeOf"/> expression.
        ''' </summary>
        Public Const RedundantNullCheckBeforeTypeOf As String = "DCA025"

        ''' <summary>
        ''' The identifier of the rule that reports text APIs called without an explicit <see cref="System.Text.Encoding"/>.
        ''' </summary>
        Public Const EncodingNotSpecified As String = "DCA026"

        ''' <summary>
        ''' The identifier of the rule: Thread.Sleep in an Async method.
        ''' </summary>
        Public Const ThreadSleepInAsyncMethod As String = "DCA027"

        ''' <summary>
        ''' The identifier of the rule: Windows Forms entry point without <c>&lt;STAThread&gt;</c>.
        ''' </summary>
        Public Const MissingStaThread As String = "DCA028"

        ''' <summary>
        ''' The identifier of the rule: String concatenated with '+'.
        ''' </summary>
        Public Const StringConcatenationWithPlus As String = "DCA029"

        ''' <summary>
        ''' The identifier of the rule: Array creation expression can be an array literal.
        ''' </summary>
        Public Const ArrayCreationInsteadOfLiteral As String = "DCA030"

        ''' <summary>
        ''' The identifier of the rule: Array designator on the variable name.
        ''' </summary>
        Public Const ArrayRankOnVariable As String = "DCA031"

        ''' <summary>
        ''' The identifier of the rule: 'End' statement.
        ''' </summary>
        Public Const EndStatement As String = "DCA032"

        ''' <summary>
        ''' The identifier of the rule: 'On Error' statement.
        ''' </summary>
        Public Const OnErrorStatement As String = "DCA033"

        ''' <summary>
        ''' The identifier of the rule: 'GoTo' statement.
        ''' </summary>
        Public Const GoToStatement As String = "DCA034"

        ''' <summary>
        ''' The identifier of the rule: Application.DoEvents inside a loop.
        ''' </summary>
        Public Const DoEventsInLoop As String = "DCA035"

        ''' <summary>
        ''' The identifier of the rule: SyncLock on a publicly accessible or weak-identity object.
        ''' </summary>
        Public Const LockOnWeakIdentity As String = "DCA036"

        ''' <summary>
        ''' The identifier of the rule: Monitor.Enter without Monitor.Exit in a Finally block.
        ''' </summary>
        Public Const MonitorEnterWithoutFinally As String = "DCA037"

        ''' <summary>
        ''' The identifier of the rule: Thread.Suspend or Thread.Resume.
        ''' </summary>
        Public Const ThreadSuspendResume As String = "DCA038"

        ''' <summary>
        ''' The identifier of the rule: Delegate BeginInvoke without EndInvoke.
        ''' </summary>
        Public Const BeginInvokeWithoutEndInvoke As String = "DCA039"

        ''' <summary>
        ''' The identifier of the rule: Duplicated condition in If/ElseIf or Select Case.
        ''' </summary>
        Public Const DuplicateCondition As String = "DCA040"

        ''' <summary>
        ''' The identifier of the rule: All branches of a conditional structure are identical.
        ''' </summary>
        Public Const AllBranchesIdentical As String = "DCA041"

        ''' <summary>
        ''' The identifier of the rule: Two branches of a conditional structure are identical.
        ''' </summary>
        Public Const DuplicateBranch As String = "DCA042"

        ''' <summary>
        ''' The identifier of the rule: Identical expressions on both sides of a binary operator.
        ''' </summary>
        Public Const IdenticalOperands As String = "DCA043"

        ''' <summary>
        ''' The identifier of the rule: DateTime.Now used to measure elapsed time.
        ''' </summary>
        Public Const DateTimeNowForTiming As String = "DCA044"

        ''' <summary>
        ''' The identifier of the rule: Path.GetTempFileName creates a predictable temporary file.
        ''' </summary>
        Public Const InsecureTempFile As String = "DCA045"

        ''' <summary>
        ''' The identifier of the rule: Exception thrown in a Finally block.
        ''' </summary>
        Public Const ThrowInFinally As String = "DCA046"

        ''' <summary>
        ''' The identifier of the rule: Exception thrown in Finalize.
        ''' </summary>
        Public Const ThrowInFinalize As String = "DCA047"

        ''' <summary>
        ''' The identifier of the rule: Exception thrown in a property getter.
        ''' </summary>
        Public Const ThrowInPropertyGetter As String = "DCA048"

        ''' <summary>
        ''' The identifier of the rule: Single-character string where a Char can be used.
        ''' </summary>
        Public Const SingleCharacterString As String = "DCA049"

        ''' <summary>
        ''' The identifier of the rule: Parameter of an externally visible method is not validated.
        ''' </summary>
        Public Const UnvalidatedParameter As String = "DCA050"

        ''' <summary>
        ''' The identifier of the rule: Awaiter or ConfigureAwait result discarded.
        ''' </summary>
        Public Const DiscardedAwaiter As String = "DCA051"

        ''' <summary>
        ''' The identifier of the rule: Parameter named 'sender' outside an event handler.
        ''' </summary>
        Public Const MisusedSenderParameter As String = "DCA052"

        ''' <summary>
        ''' The identifier of the rule: Method call without parentheses.
        ''' </summary>
        Public Const MethodCallWithoutParentheses As String = "DCA053"

        ''' <summary>
        ''' The identifier of the rule: Code in a ComImport class never runs.
        ''' </summary>
        Public Const ComImportClassCode As String = "DCA054"

        ''' <summary>
        ''' The identifier of the rule: Zero-length array allocation (.NET Framework).
        ''' </summary>
        Public Const ZeroLengthArrayAllocation As String = "DCA055"

        ''' <summary>
        ''' The identifier of the rule: Enumerable.Any() instead of IsEmpty, Count or Length (.NET Framework).
        ''' </summary>
        Public Const AnyInsteadOfEmptinessCheck As String = "DCA056"

        ''' <summary>
        ''' The identifier of the rule: IEnumerable parameter named as a collection.
        ''' </summary>
        Public Const CollectionNameOnEnumerableParameter As String = "DCA057"

        ''' <summary>
        ''' The identifier of the rule: StringBuilder argument converted to a string (.NET Framework).
        ''' </summary>
        Public Const StringBuilderArgumentConvertedToString As String = "DCA058"

        ''' <summary>
        ''' The identifier of the rule: System.Type argument instead of a generic overload (.NET Framework).
        ''' </summary>
        Public Const GenericOverloadInsteadOfTypeArgument As String = "DCA059"

        ''' <summary>
        ''' The identifier of the rule: Component property without designer serialization configuration (.NET Framework).
        ''' </summary>
        Public Const MissingDesignerSerializationConfiguration As String = "DCA060"

        ''' <summary>
        ''' The identifier of the rule: ArgumentNullException for a value-type parameter (.NET Framework).
        ''' </summary>
        Public Const ArgumentNullExceptionForValueType As String = "DCA061"

        ''' <summary>
        ''' The identifier of the rule: TaskContinuationOptions passed to a TaskCompletionSource constructor (.NET Framework).
        ''' </summary>
        Public Const TaskContinuationOptionsAsState As String = "DCA062"

        ''' <summary>
        ''' The identifier of the rule: StreamReader.EndOfStream in an Async method (.NET Framework).
        ''' </summary>
        Public Const EndOfStreamInAsyncMethod As String = "DCA063"

        ''' <summary>
        ''' The identifier of the rule: Enumerable.Cast or OfType with an incompatible type (.NET Framework).
        ''' </summary>
        Public Const IncompatibleCastOrOfType As String = "DCA064"

        ''' <summary>
        ''' The identifier of the rule: Buffer.BlockCopy count is an array length (.NET Framework).
        ''' </summary>
        Public Const BlockCopyLengthAsByteCount As String = "DCA065"

        ''' <summary>
        ''' The identifier of the rule: Value type passed to ReferenceEquals (.NET Framework).
        ''' </summary>
        Public Const ValueTypeInReferenceEquals As String = "DCA066"

        ''' <summary>
        ''' The identifier of the rule: String literal instead of NameOf (.NET Framework).
        ''' </summary>
        Public Const StringLiteralInsteadOfNameOf As String = "DCA067"

        ''' <summary>
        ''' The identifier of the rule: IsCancellationRequested check that throws OperationCanceledException (.NET Framework).
        ''' </summary>
        Public Const ManualCancellationCheck As String = "DCA068"

        ''' <summary>
        ''' The identifier of the rule: Enum.HasFlag with a different enum type (.NET Framework).
        ''' </summary>
        Public Const HasFlagWithDifferentEnum As String = "DCA069"

        ''' <summary>
        ''' The identifier of the rule: ThreadStatic field initialized inline or in a Shared constructor (.NET Framework).
        ''' </summary>
        Public Const ThreadStaticFieldInitialization As String = "DCA070"

        ''' <summary>
        ''' The identifier of the rule: CancellationToken parameter not forwarded (.NET Framework).
        ''' </summary>
        Public Const CancellationTokenNotForwarded As String = "DCA071"

        ''' <summary>
        ''' The identifier of the rule: Count() compared with zero instead of Any() (.NET Framework).
        ''' </summary>
        Public Const CountInsteadOfAny As String = "DCA072"

        ''' <summary>
        ''' The identifier of the rule: Enumerable.Count() instead of the Length or Count property (.NET Framework).
        ''' </summary>
        Public Const CountMethodInsteadOfProperty As String = "DCA073"

        ''' <summary>
        ''' The identifier of the rule: Count compared with zero instead of IsEmpty (.NET Framework).
        ''' </summary>
        Public Const CountInsteadOfIsEmpty As String = "DCA074"

        ''' <summary>
        ''' The identifier of the rule: Task.WhenAll with a single task (.NET Framework).
        ''' </summary>
        Public Const WhenAllWithSingleTask As String = "DCA075"

        ''' <summary>
        ''' The identifier of the rule: Task.WaitAll with a single task (.NET Framework).
        ''' </summary>
        Public Const WaitAllWithSingleTask As String = "DCA076"

        ''' <summary>
        ''' The identifier of the rule: Dictionary.Remove guarded by ContainsKey (.NET Framework).
        ''' </summary>
        Public Const ContainsKeyBeforeRemove As String = "DCA077"

        ''' <summary>
        ''' The identifier of the rule: Set Add or Remove guarded by Contains (.NET Framework).
        ''' </summary>
        Public Const ContainsBeforeSetAddOrRemove As String = "DCA078"

        ''' <summary>
        ''' The identifier of the rule: Keys.Contains or Values.Contains on a dictionary (.NET Framework).
        ''' </summary>
        Public Const KeysOrValuesContains As String = "DCA079"

        ''' <summary>
        ''' The identifier of the rule: Thread.CurrentThread.ManagedThreadId instead of Environment.CurrentManagedThreadId (.NET Framework).
        ''' </summary>
        Public Const CurrentThreadManagedThreadId As String = "DCA080"

        ''' <summary>
        ''' The identifier of the rule: ContainsKey followed by the indexer instead of TryGetValue (.NET Framework).
        ''' </summary>
        Public Const ContainsKeyThenIndexer As String = "DCA081"

        ''' <summary>
        ''' The identifier of the rule: IndexOf compared with zero instead of StartsWith (.NET Framework).
        ''' </summary>
        Public Const IndexOfComparedWithZero As String = "DCA082"

        ''' <summary>
        ''' The identifier of the rule: Redundant length argument of Substring (.NET Framework).
        ''' </summary>
        Public Const RedundantSubstringLength As String = "DCA083"

        ''' <summary>
        ''' The identifier of the rule: Constant array passed as an argument (.NET Framework).
        ''' </summary>
        Public Const ConstantArrayArgument As String = "DCA084"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DiagnosticIds"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

End Namespace

#End Region
