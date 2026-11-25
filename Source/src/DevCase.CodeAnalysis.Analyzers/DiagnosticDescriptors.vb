#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.CodeAnalysis

#End Region

#Region " DevCaseDescriptors "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the <see cref="DiagnosticDescriptor"/> instances of every DevCase diagnostic rule.
    ''' </summary>
    Public NotInheritable Class DevCaseDescriptors

#Region " Constants "

        ''' <summary>
        ''' The diagnostic category used by reliability rules.
        ''' </summary>
        Private Const CategoryReliability As String = "Reliability"

        ''' <summary>
        ''' The diagnostic category used by style rules.
        ''' </summary>
        Private Const CategoryStyle As String = "Style"

        ''' <summary>
        ''' The diagnostic category used by design rules.
        ''' </summary>
        Private Const CategoryDesign As String = "Design"

        ''' <summary>
        ''' The diagnostic category used by documentation rules.
        ''' </summary>
        Private Const CategoryDocumentation As String = "Documentation"

        ''' <summary>
        ''' The diagnostic category used by globalization rules.
        ''' </summary>
        Private Const CategoryGlobalization As String = "Globalization"

        ''' <summary>
        ''' The diagnostic category used by portability (migration) rules.
        ''' </summary>
        Private Const CategoryPortability As String = "Portability"

        ''' <summary>
        ''' The diagnostic category used by security rules.
        ''' </summary>
        Private Const CategorySecurity As String = "Security"

        ''' <summary>
        ''' The diagnostic category used by performance rules.
        ''' </summary>
        Private Const CategoryPerformance As String = "Performance"

        ''' <summary>
        ''' The diagnostic category used by naming rules.
        ''' </summary>
        Private Const CategoryNaming As String = "Naming"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingConfigureAwait"/>.
        ''' </summary>
        Public Shared ReadOnly MissingConfigureAwait As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingConfigureAwait,
            title:="Await expression is missing ConfigureAwait(False)",
            messageFormat:="Add 'ConfigureAwait(False)' to this awaited task to avoid deadlocks",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Reusable library code must not capture the synchronization context. Every awaited task-like value that exposes a ConfigureAwait(Boolean) method must be configured explicitly.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.TaskResultAccess"/>.
        ''' </summary>
        Public Shared ReadOnly TaskResultAccess As New DiagnosticDescriptor(
            id:=DiagnosticIds.TaskResultAccess,
            title:="Blocking .Result access on a task",
            messageFormat:="Use '{0}.GetAwaiter().GetResult()' instead of '{0}.Result' to avoid 'AggregateException'",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Task(Of T).Result wraps faults inside an AggregateException and hides the original stack trace. GetAwaiter().GetResult() surfaces the root exception directly.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.SilentGuardClause"/>.
        ''' </summary>
        Public Shared ReadOnly SilentGuardClause As New DiagnosticDescriptor(
            id:=DiagnosticIds.SilentGuardClause,
            title:="Silent guard clause on a parameter",
            messageFormat:="Throw an argument exception instead of silently exiting when '{0}' is invalid",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="A broken precondition must never be hidden by a silent Return or Exit statement. Throw ArgumentNullException, ArgumentException or ArgumentOutOfRangeException instead.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingOptionStatements"/>.
        ''' </summary>
        Public Shared ReadOnly MissingOptionStatements As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingOptionStatements,
            title:="Required Option settings not in effect",
            messageFormat:="Declare {0} because this file compiles with {1}",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Every source file must be compiled with 'Option Strict On', 'Option Explicit On' and 'Option Infer Off'. Each setting is taken from the Option statement of the file or, when the file does not declare it, from the project settings: a file without Option statements in a project that already uses the required settings is not reported.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingExplicitConstructor"/>.
        ''' </summary>
        Public Shared ReadOnly MissingExplicitConstructor As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingExplicitConstructor,
            title:="Class lacks an explicit constructor",
            messageFormat:="Explicitly declare an instance constructor in class '{0}' to avoid an implicit public constructor",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Every class must explicitly declare its instance constructor so that its accessibility and documentation are intentional.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.TypeOutsideNamespace"/>.
        ''' </summary>
        Public Shared ReadOnly TypeOutsideNamespace As New DiagnosticDescriptor(
            id:=DiagnosticIds.TypeOutsideNamespace,
            title:="Type declared in the global namespace",
            messageFormat:="Declare type '{0}' inside a namespace to prevent global naming collisions",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Top-level types must not live in the global namespace: declare them inside an explicit Namespace block. Not reported when the project RootNamespace is set, because the types already belong to it. Equivalent of CA1050, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingXmlDocumentation"/>.
        ''' </summary>
        Public Shared ReadOnly MissingXmlDocumentation As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingXmlDocumentation,
            title:="Public member lacks XML documentation",
            messageFormat:="Add XML documentation <summary> tag to public member '{0}'",
            category:=DevCaseDescriptors.CategoryDocumentation,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Every member declared with the Public modifier must have an XML documentation comment. An '<inheritdoc/>' comment is accepted.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.XmlParameterMismatch"/>.
        ''' </summary>
        Public Shared ReadOnly XmlParameterMismatch As New DiagnosticDescriptor(
            id:=DiagnosticIds.XmlParameterMismatch,
            title:="XML <param> tags do not match the parameter list",
            messageFormat:="Fix the XML documentation <param> tags of '{0}' ({1})",
            category:=DevCaseDescriptors.CategoryDocumentation,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Every parameter must be documented by exactly one non-empty <param> tag, and every <param> tag must refer to an existing parameter.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingStringComparison"/>.
        ''' </summary>
        Public Shared ReadOnly MissingStringComparison As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingStringComparison,
            title:="String comparison argument omitted",
            messageFormat:="Pass an explicit '{1}' argument to '{0}' to avoid culture-dependent behavior",
            category:=DevCaseDescriptors.CategoryGlobalization,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="When an overload or optional parameter accepting StringComparison, StringComparer, IComparer(Of String) or IEqualityComparer(Of String) is available, the comparison must be specified explicitly instead of relying on culture-sensitive defaults.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.FlagsEnumWithoutBitShift"/>.
        ''' </summary>
        '''
        ''' <remarks>
        ''' This rule is inactive unless <c>devcase_analyzers.dca010_enabled = true</c> is set in an <c>.editorconfig</c>
        ''' or global analyzer configuration file.
        ''' </remarks>
        Public Shared ReadOnly FlagsEnumWithoutBitShift As New DiagnosticDescriptor(
            id:=DiagnosticIds.FlagsEnumWithoutBitShift,
            title:="Flags enum member does not use bit-shift syntax",
            messageFormat:="Declare Flags enum member '{0}' as '{1}' to make its bit explicit",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Members of an enumeration marked with <Flags> must declare single-bit values with the bit-shift operator (e.g. 1 << 3). Disabled by default; enable per project with 'devcase_analyzers.dca010_enabled = true'.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThrowHelperMigration"/>.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Reported only for .NET Framework compilations; on modern .NET the built-in CA1510-CA1513 rules apply.
        ''' </remarks>
        Public Shared ReadOnly ThrowHelperMigration As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThrowHelperMigration,
            title:="Argument guards can be paired with their modern throw helpers",
            messageFormat:="Conditionally use '{1}' for {0} argument guard(s) on modern .NET",
            category:=DevCaseDescriptors.CategoryPortability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="In .NET Framework code, classic guards such as 'If x Is Nothing Then Throw New ArgumentNullException(NameOf(x))' can be paired, inside an '#If NETCOREAPP' block, with the throw helpers recommended on modern .NET (ArgumentNullException.ThrowIfNull, ArgumentException.ThrowIfNullOrWhiteSpace, ArgumentOutOfRangeException.ThrowIfNegative, ...).")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingExceptionDocumentation"/>.
        ''' </summary>
        Public Shared ReadOnly MissingExceptionDocumentation As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingExceptionDocumentation,
            title:="Thrown exception is not documented",
            messageFormat:="Add XML documentation <exception> tags to '{0}' for thrown exceptions ({1})",
            category:=DevCaseDescriptors.CategoryDocumentation,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Every exception explicitly thrown by a documented member (Throw New ..., or a .NET throw helper such as ArgumentNullException.ThrowIfNull) must be listed with an <exception cref=""...""> tag. Exceptions caught within the same member and NotImplementedException are ignored.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.NetFrameworkApiMigration"/>.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Reported only for .NET Framework compilations.
        ''' </remarks>
        Public Shared ReadOnly NetFrameworkApiMigration As New DiagnosticDescriptor(
            id:=DiagnosticIds.NetFrameworkApiMigration,
            title:="API has a modern .NET equivalent",
            messageFormat:="Conditionally use '{1}' instead of '{0}' on modern .NET",
            category:=DevCaseDescriptors.CategoryPortability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="In .NET Framework code, some API usages have an exact, better equivalent that only exists on modern .NET (e.g. String.Contains(Char), String.Join(Char, ...), Dictionary.TryAdd, Enum.Parse(Of T), Convert.ToHexString). The code fix keeps the original code in the #Else branch of a conditional compilation block.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.FileSystemLoopWithoutMessagePumping"/>.
        ''' </summary>
        Public Shared ReadOnly FileSystemLoopWithoutMessagePumping As New DiagnosticDescriptor(
            id:=DiagnosticIds.FileSystemLoopWithoutMessagePumping,
            title:="File-system loop without Thread.CurrentThread.Join(0)",
            messageFormat:="Call 'Thread.CurrentThread.Join(0)' inside this enumeration loop to pump COM messages and prevent 'ContextSwitchDeadlock'",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Long For Each loops over Directory/DirectoryInfo enumeration methods (GetFiles, EnumerateFiles, GetDirectories, EnumerateDirectories, ...) running on an STA thread can block COM message pumping and trigger the ContextSwitchDeadlock MDA. Calling Thread.CurrentThread.Join(0) inside the loop pumps pending messages.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RecursiveCall"/>.
        ''' </summary>
        Public Shared ReadOnly RecursiveCall As New DiagnosticDescriptor(
            id:=DiagnosticIds.RecursiveCall,
            title:="Method calls itself",
            messageFormat:="Rewrite recursive method '{0}' iteratively to avoid a stack overflow",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Recursion is not allowed: deep inputs can overflow the stack, and accidental self-calls (e.g. a property getter that reads itself) cause infinite recursion. Tree walks must be implemented iteratively with an explicit Stack or Queue.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.AsyncSubNotEventHandler"/>.
        ''' </summary>
        Public Shared ReadOnly AsyncSubNotEventHandler As New DiagnosticDescriptor(
            id:=DiagnosticIds.AsyncSubNotEventHandler,
            title:="Async Sub is not an event handler",
            messageFormat:="Return a Task from Async Sub '{0}' so callers can await it",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="An Async Sub cannot be awaited and its exceptions are raised on the synchronization context, usually crashing the process. Only event handlers (Handles clause or (sender, e As EventArgs) signature), overrides and interface implementations may be Async Sub.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RegexWithoutTimeout"/>.
        ''' </summary>
        Public Shared ReadOnly RegexWithoutTimeout As New DiagnosticDescriptor(
            id:=DiagnosticIds.RegexWithoutTimeout,
            title:="Regular expression without match timeout",
            messageFormat:="Specify a match timeout for '{0}' to prevent ReDoS",
            category:=DevCaseDescriptors.CategorySecurity,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="A regular expression with catastrophic backtracking evaluated against malicious or unexpectedly large input can run for minutes or forever (Regular expression Denial of Service, CWE-1333). Pass a match timeout to the Regex constructor or to the static Regex methods; RegexMatchTimeoutException is thrown when it expires. Equivalent to SonarQube rule S6444.",
            helpLinkUri:="https://learn.microsoft.com/dotnet/standard/base-types/best-practices-regex#use-time-out-values")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DateTimeWithoutKind"/>.
        ''' </summary>
        Public Shared ReadOnly DateTimeWithoutKind As New DiagnosticDescriptor(
            id:=DiagnosticIds.DateTimeWithoutKind,
            title:="DateTime created without DateTimeKind",
            messageFormat:="Specify a DateTimeKind in this DateTime constructor to avoid ambiguous dates",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="A DateTime created without a DateTimeKind has the kind Unspecified, which ToUniversalTime and ToLocalTime treat as local time and serializers handle inconsistently. Stating the kind (Unspecified, Utc or Local) documents what the value represents. Equivalent to SonarQube rule S6562.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DateTimeParseWithoutProvider"/>.
        ''' </summary>
        Public Shared ReadOnly DateTimeParseWithoutProvider As New DiagnosticDescriptor(
            id:=DiagnosticIds.DateTimeParseWithoutProvider,
            title:="Date and time parsed without a format provider",
            messageFormat:="Pass an IFormatProvider to '{0}' to avoid culture-dependent parsing",
            category:=DevCaseDescriptors.CategoryGlobalization,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="DateTime, DateTimeOffset, TimeSpan, DateOnly and TimeOnly parsing methods without an IFormatProvider depend on the regional settings of the machine: the same text can produce a different value or fail on another computer. Equivalent to SonarQube rule S6580.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.FlagsEnumWithoutNone"/>.
        ''' </summary>
        Public Shared ReadOnly FlagsEnumWithoutNone As New DiagnosticDescriptor(
            id:=DiagnosticIds.FlagsEnumWithoutNone,
            title:="Flags enumeration without a zero value",
            messageFormat:="Add a zero-value member to Flags enum '{0}'",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="The zero value of a Flags enumeration means that no flag is set. Without a member for it, code has to use the literal 0 or CType(0, MyFlags). A zero member that already exists is never reported, whatever its name. The code fix adds a zero member named None, or [Nothing], Null, Zero or Empty when that name is already used.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.LinqInsteadOfCollectionMethod"/>.
        ''' </summary>
        Public Shared ReadOnly LinqInsteadOfCollectionMethod As New DiagnosticDescriptor(
            id:=DiagnosticIds.LinqInsteadOfCollectionMethod,
            title:="LINQ method used instead of the collection-specific method",
            messageFormat:="Use '{0}' instead of 'Enumerable.{1}()' to avoid enumerator boxing and allocation",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="List(Of T), arrays and ImmutableList(Of T) provide Find, Exists and TrueForAll methods that are faster than the equivalent LINQ FirstOrDefault, Any and All extensions with a predicate. Equivalent to SonarQube rules S6602, S6603 and S6605.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.LinqInsteadOfIndexer"/>.
        ''' </summary>
        Public Shared ReadOnly LinqInsteadOfIndexer As New DiagnosticDescriptor(
            id:=DiagnosticIds.LinqInsteadOfIndexer,
            title:="LINQ method used instead of the indexer",
            messageFormat:="Do not use Enumerable methods on indexable collections; instead use the collection directly",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Arrays, strings and types that implement IList(Of T) or IReadOnlyList(Of T) (List(Of T), Collection(Of T), ReadOnlyCollection(Of T), ImmutableArray(Of T)...) can be indexed directly; First, Last and ElementAt go through LINQ type checks and interface calls, and Last enumerates the whole sequence on strings and IReadOnlyList(Of T) types. Equivalent of SonarQube S6608 and CA1826 (in modern .NET projects, the types that CA1826 reports are left to it).")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.InvertedComparison"/>.
        ''' </summary>
        Public Shared ReadOnly InvertedComparison As New DiagnosticDescriptor(
            id:=DiagnosticIds.InvertedComparison,
            title:="Negated comparison",
            messageFormat:="Use the '{1}' operator to simplify the negated comparison",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Negating a comparison is harder to read than using the inverse operator: 'Not (a = b)' is 'a <> b'. Relational comparisons of floating-point values are not reported because they differ for NaN. Equivalent to SonarQube rule S1940.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RethrowOnlyCatch"/>.
        ''' </summary>
        Public Shared ReadOnly RethrowOnlyCatch As New DiagnosticDescriptor(
            id:=DiagnosticIds.RethrowOnlyCatch,
            title:="Catch block only rethrows the exception",
            messageFormat:="Remove this Catch block because it only rethrows the exception",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="A Catch block whose only statement is 'Throw' has no effect: the exception propagates exactly as it would without it. 'Throw ex' with the caught exception is reported too: it also discards the original stack trace. Catch blocks that only rethrow are reported only when no other Catch block follows them, because otherwise they prevent a later block from handling that exception. Catch blocks with a When filter or with conditional code (#If ... #End If) are not reported. Equivalent to SonarQube rule S2737.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RedundantNullCheckBeforeTypeOf"/>.
        ''' </summary>
        Public Shared ReadOnly RedundantNullCheckBeforeTypeOf As New DiagnosticDescriptor(
            id:=DiagnosticIds.RedundantNullCheckBeforeTypeOf,
            title:="Redundant null check before TypeOf",
            messageFormat:="Remove the redundant null check because 'TypeOf' already evaluates to 'False' for 'Nothing'",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="'TypeOf x Is T' is False when x is Nothing, and 'TypeOf x IsNot T' is True, so a preceding 'x IsNot Nothing AndAlso' or 'x Is Nothing OrElse' adds nothing. Equivalent to SonarQube rule S4201.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.EncodingNotSpecified"/>.
        ''' </summary>
        Public Shared ReadOnly EncodingNotSpecified As New DiagnosticDescriptor(
            id:=DiagnosticIds.EncodingNotSpecified,
            title:="Text encoding not specified",
            messageFormat:="Pass an explicit 'Encoding' to '{0}' to avoid relying on the default encoding",
            category:=DevCaseDescriptors.CategoryGlobalization,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Text APIs called without an Encoding use an implicit one (UTF-8 without BOM for System.IO, UTF-8 with BOM for Microsoft.VisualBasic.FileIO). Stating the encoding documents the format of the file or stream, and avoids surprises with programs that expect another encoding.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThreadSleepInAsyncMethod"/>.
        ''' </summary>
        Public Shared ReadOnly ThreadSleepInAsyncMethod As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThreadSleepInAsyncMethod,
            title:="Thread.Sleep in an Async method",
            messageFormat:="Use Await Task.Delay instead of Thread.Sleep in this Async method to avoid blocking the thread",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Thread.Sleep blocks the calling thread (the UI thread, or a thread-pool thread) for the whole delay, defeating the purpose of the Async method. Await Task.Delay waits without blocking any thread.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingStaThread"/>.
        ''' </summary>
        Public Shared ReadOnly MissingStaThread As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingStaThread,
            title:="Windows Forms entry point without <STAThread>",
            messageFormat:="Use <STAThread> on the Windows Forms entry point '{0}' to ensure COM apartment compatibility",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Windows Forms requires a single-threaded apartment (STA) thread: clipboard, drag and drop, common dialogs and ActiveX controls fail or hang on an MTA thread. A custom Sub Main must be marked with <STAThread>.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.StringConcatenationWithPlus"/>.
        ''' </summary>
        Public Shared ReadOnly StringConcatenationWithPlus As New DiagnosticDescriptor(
            id:=DiagnosticIds.StringConcatenationWithPlus,
            title:="String concatenated with '+'",
            messageFormat:="Concatenate strings using '{1}' instead of '{0}' to avoid operator ambiguity",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="In Visual Basic, '+' is the addition operator: with Option Strict Off, ""1"" + 1 is 2, not ""11"". The '&' operator always concatenates and states the intent.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ArrayCreationInsteadOfLiteral"/>.
        ''' </summary>
        Public Shared ReadOnly ArrayCreationInsteadOfLiteral As New DiagnosticDescriptor(
            id:=DiagnosticIds.ArrayCreationInsteadOfLiteral,
            title:="Array creation expression can be an array literal",
            messageFormat:="Use an array literal instead of an explicit array creation because the type is already inferred",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="When the target type of the expression is already the array type, the array literal {...} produces the same array with less noise.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ArrayRankOnVariable"/>.
        ''' </summary>
        Public Shared ReadOnly ArrayRankOnVariable As New DiagnosticDescriptor(
            id:=DiagnosticIds.ArrayRankOnVariable,
            title:="Array designator on the variable name",
            messageFormat:="Move the array specifier '{0}' from the variable name to the type for clearer readability",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Writing the array designator on the type (Dim values As Integer()) keeps the whole type in one place, as in every other declaration.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.EndStatement"/>.
        ''' </summary>
        Public Shared ReadOnly EndStatement As New DiagnosticDescriptor(
            id:=DiagnosticIds.EndStatement,
            title:="'End' statement",
            messageFormat:="Do not use the 'End' statement because it skips Finally blocks and cleanup code",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="End terminates the process immediately: Finally blocks, Using blocks, Dispose methods, FormClosing events and pending writes do not run. Close the main form, call Application.Exit or return from Sub Main instead.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.OnErrorStatement"/>.
        ''' </summary>
        Public Shared ReadOnly OnErrorStatement As New DiagnosticDescriptor(
            id:=DiagnosticIds.OnErrorStatement,
            title:="'On Error' statement",
            messageFormat:="Replace '{0}' with 'Try...Catch' for structured exception handling",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="On Error is unstructured error handling: On Error Resume Next silently ignores every error, and On Error GoTo jumps across the method. Try...Catch handles exceptions in a visible, scoped and typed way.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.GoToStatement"/>.
        ''' </summary>
        Public Shared ReadOnly GoToStatement As New DiagnosticDescriptor(
            id:=DiagnosticIds.GoToStatement,
            title:="'GoTo' statement",
            messageFormat:="Replace 'GoTo {0}' with loops, 'Exit', or 'Continue' statements for structured control flow",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="GoTo jumps make the control flow hard to follow. Loops, Exit, Continue and Return express the same flow in a structured way.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DoEventsInLoop"/>.
        ''' </summary>
        Public Shared ReadOnly DoEventsInLoop As New DiagnosticDescriptor(
            id:=DiagnosticIds.DoEventsInLoop,
            title:="Application.DoEvents inside a loop",
            messageFormat:="Move this loop to a background task instead of calling '{0}' to prevent overlapping UI events",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Application.DoEvents processes every pending message: the user can click the same button again, close the form or start another operation while the loop is still running. Run the work with Await and Task.Run, or a BackgroundWorker, instead.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.LockOnWeakIdentity"/>.
        ''' </summary>
        Public Shared ReadOnly LockOnWeakIdentity As New DiagnosticDescriptor(
            id:=DiagnosticIds.LockOnWeakIdentity,
            title:="SyncLock on a publicly accessible or weak-identity object",
            messageFormat:="Lock on a private object instead of {0} to avoid deadlocks",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Me, Type objects, strings (interned and shared by the whole process) and other weak-identity objects can be locked by code outside the type, causing deadlocks and contention. Lock on a dedicated Private ReadOnly object.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MonitorEnterWithoutFinally"/>.
        ''' </summary>
        Public Shared ReadOnly MonitorEnterWithoutFinally As New DiagnosticDescriptor(
            id:=DiagnosticIds.MonitorEnterWithoutFinally,
            title:="Monitor.Enter without Monitor.Exit in a Finally block",
            messageFormat:="Call Monitor.Exit in a Finally block after 'Monitor.{0}' to always release the lock",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="If an exception occurs between Monitor.Enter and Monitor.Exit, the lock is never released and every other thread that tries to acquire it hangs. Use SyncLock, or call Monitor.Exit in a Finally block.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThreadSuspendResume"/>.
        ''' </summary>
        Public Shared ReadOnly ThreadSuspendResume As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThreadSuspendResume,
            title:="Thread.Suspend or Thread.Resume",
            messageFormat:="Replace 'Thread.{0}' with a synchronization primitive to avoid deadlocks",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Thread.Suspend stops a thread at an arbitrary point, possibly while it holds a lock (also locks of the runtime), which deadlocks the process. Both methods are obsolete and not supported on modern .NET. Use ManualResetEventSlim, SemaphoreSlim or CancellationToken.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.BeginInvokeWithoutEndInvoke"/>.
        ''' </summary>
        Public Shared ReadOnly BeginInvokeWithoutEndInvoke As New DiagnosticDescriptor(
            id:=DiagnosticIds.BeginInvokeWithoutEndInvoke,
            title:="Delegate BeginInvoke without EndInvoke",
            messageFormat:="Call EndInvoke after '{0}.BeginInvoke' to observe its result and exceptions",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Every call to the BeginInvoke method of a delegate must be paired with EndInvoke: it returns the result, rethrows the exception of the call and releases the resources. Delegate BeginInvoke is not supported on modern .NET (PlatformNotSupportedException); use Task.Run instead.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DuplicateCondition"/>.
        ''' </summary>
        Public Shared ReadOnly DuplicateCondition As New DiagnosticDescriptor(
            id:=DiagnosticIds.DuplicateCondition,
            title:="Duplicated condition in If/ElseIf or Select Case",
            messageFormat:="Fix this condition because it duplicates the one on line {0}",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="In an If/ElseIf chain or a Select Case statement, only the first matching branch runs: a later branch with the same condition is dead code, usually a copy-paste mistake.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.AllBranchesIdentical"/>.
        ''' </summary>
        Public Shared ReadOnly AllBranchesIdentical As New DiagnosticDescriptor(
            id:=DiagnosticIds.AllBranchesIdentical,
            title:="All branches of a conditional structure are identical",
            messageFormat:="Simplify this '{0}' because all its branches are identical",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="When every branch, including Else, does the same thing, the condition does not change the behavior: either it is a copy-paste mistake, or the conditional structure can be removed.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DuplicateBranch"/>.
        ''' </summary>
        Public Shared ReadOnly DuplicateBranch As New DiagnosticDescriptor(
            id:=DiagnosticIds.DuplicateBranch,
            title:="Two branches of a conditional structure are identical",
            messageFormat:="Merge this branch with the identical branch on line {0}",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Two branches with the same implementation should be merged (OrElse in an If chain, a comma-separated Case clause in Select Case), or one of them is a copy-paste mistake.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.IdenticalOperands"/>.
        ''' </summary>
        Public Shared ReadOnly IdenticalOperands As New DiagnosticDescriptor(
            id:=DiagnosticIds.IdenticalOperands,
            title:="Identical expressions on both sides of a binary operator",
            messageFormat:="Fix the operands of '{0}' because both sides are identical",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Comparing an expression with itself, or combining it with itself through And, Or, Xor, '-', '/' or Mod, always produces the same result: it is almost always a copy-paste mistake.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DateTimeNowForTiming"/>.
        ''' </summary>
        Public Shared ReadOnly DateTimeNowForTiming As New DiagnosticDescriptor(
            id:=DiagnosticIds.DateTimeNowForTiming,
            title:="DateTime.Now used to measure elapsed time",
            messageFormat:="Use Stopwatch instead of '{0}' to measure elapsed time accurately",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="DateTime.Now has a resolution of about 15 ms, and it jumps when the system clock changes (daylight saving time, time synchronization). Stopwatch uses a monotonic, high-resolution counter.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.InsecureTempFile"/>.
        ''' </summary>
        Public Shared ReadOnly InsecureTempFile As New DiagnosticDescriptor(
            id:=DiagnosticIds.InsecureTempFile,
            title:="Path.GetTempFileName creates a predictable temporary file",
            messageFormat:="Replace '{0}' with 'Path.GetRandomFileName' to avoid predictable names",
            category:=DevCaseDescriptors.CategorySecurity,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="GetTempFileName uses a predictable name and throws IOException when the temporary folder holds 65535 'tmp*.tmp' files. A random name (Path.GetRandomFileName) created with FileMode.CreateNew cannot be predicted, and never reuses an existing file.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThrowInFinally"/>.
        ''' </summary>
        Public Shared ReadOnly ThrowInFinally As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThrowInFinally,
            title:="Exception thrown in a Finally block",
            messageFormat:="Do not throw exceptions in a Finally block to preserve the original exception",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="If the Try block threw an exception, an exception thrown in the Finally block replaces it: the original error and its stack trace are lost. Equivalent of CA2219, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThrowInFinalize"/>.
        ''' </summary>
        Public Shared ReadOnly ThrowInFinalize As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThrowInFinalize,
            title:="Exception thrown in Finalize",
            messageFormat:="Do not throw exceptions in Finalize to prevent process termination",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="An unhandled exception on the finalizer thread terminates the process immediately, without running other finalizers or Finally blocks.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThrowInPropertyGetter"/>.
        ''' </summary>
        Public Shared ReadOnly ThrowInPropertyGetter As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThrowInPropertyGetter,
            title:="Exception thrown in a property getter",
            messageFormat:="Do not throw '{1}' in the getter of '{0}'; move the check to a method or the setter",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Reading a property is expected to be safe: debuggers, data binding and serializers read properties freely. Validate in the setter or in methods; a getter should only throw when the object is in an invalid state.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.SingleCharacterString"/>.
        ''' </summary>
        Public Shared ReadOnly SingleCharacterString As New DiagnosticDescriptor(
            id:=DiagnosticIds.SingleCharacterString,
            title:="Single-character string where a Char can be used",
            messageFormat:="Use '{1}(Char)' instead of '{1}(String)' when the input is a constant unit string",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="String.Join, Split, Replace and Contains, StringBuilder.Append, Insert, Replace and AppendJoin, and TextWriter and Console Write/WriteLine have a Char overload that produces the same result. On .NET Framework, where String.Join has no Char overload, the character literal is converted to the same constant string at compile time.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.UnvalidatedParameter"/>.
        ''' </summary>
        Public Shared ReadOnly UnvalidatedParameter As New DiagnosticDescriptor(
            id:=DiagnosticIds.UnvalidatedParameter,
            title:="Parameter of an externally visible method is not validated",
            messageFormat:="Add a guard clause for parameter '{0}' of '{1}'",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Externally visible methods can be called with any argument. Without a guard clause, Nothing or an undefined Enum value fails later, far from the cause, with a NullReferenceException or a wrong result. A guard clause fails immediately with an exception that names the parameter.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DiscardedAwaiter"/>.
        ''' </summary>
        Public Shared ReadOnly DiscardedAwaiter As New DiagnosticDescriptor(
            id:=DiagnosticIds.DiscardedAwaiter,
            title:="Awaiter or ConfigureAwait result discarded",
            messageFormat:="Await the task returned by '{0}' instead of discarding it to observe exceptions and completion",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="GetAwaiter() and ConfigureAwait() do not wait for a task: they only prepare it to be awaited. When their result is discarded, the statement does nothing, the method returns before the task completes, and any exception of the task is lost.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MisusedSenderParameter"/>.
        ''' </summary>
        Public Shared ReadOnly MisusedSenderParameter As New DiagnosticDescriptor(
            id:=DiagnosticIds.MisusedSenderParameter,
            title:="Parameter named 'sender' outside an event handler",
            messageFormat:="Rename parameter 'sender'{1} in '{0}' because it is not an event handler",
            category:=DevCaseDescriptors.CategoryNaming,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="By convention, 'sender' is the object that raises an event, typed Object, followed by the event arguments. In other methods the name hides what the argument really is. The suggested name is the first of 'source', 'value', 'input', 'instance' and 'item' that is not already used in the method.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MethodCallWithoutParentheses"/>.
        ''' </summary>
        Public Shared ReadOnly MethodCallWithoutParentheses As New DiagnosticDescriptor(
            id:=DiagnosticIds.MethodCallWithoutParentheses,
            title:="Method call without parentheses",
            messageFormat:="Add parentheses to this method call to distinguish it from a property",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Visual Basic allows calling a method without parentheses, which makes the call look like a property or field access. Writing the parentheses shows that code runs (and may have side effects or be expensive), and is the form used by the documentation and by other .NET languages.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ComImportClassCode"/>.
        ''' </summary>
        Public Shared ReadOnly ComImportClassCode As New DiagnosticDescriptor(
            id:=DiagnosticIds.ComImportClassCode,
            title:="Code in a ComImport class never runs",
            messageFormat:="Remove {0} from <ComImport> class '{1}' because the compiler discards its body",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Error,
            isEnabledByDefault:=True,
            description:="A class marked with <ComImport> is created by the runtime through COM (CoCreateInstance). The Visual Basic compiler accepts constructors, methods and property accessors with code in such a class, but silently compiles them as runtime-implemented members without a body: the code never runs. C# rejects the same code (CS0669, CS0423).")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ZeroLengthArrayAllocation"/>.
        ''' </summary>
        Public Shared ReadOnly ZeroLengthArrayAllocation As New DiagnosticDescriptor(
            id:=DiagnosticIds.ZeroLengthArrayAllocation,
            title:="Zero-length array allocation",
            messageFormat:="Use 'Array.Empty(Of {0})()' instead of a new empty array to avoid unnecessary heap allocation",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Every 'New T() {}' allocates a new empty array, although an empty array can never change. Array.Empty(Of T)() returns a cached instance shared by the whole application (.NET Framework 4.6 or later). Equivalent of CA1825, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.AnyInsteadOfEmptinessCheck"/>.
        ''' </summary>
        Public Shared ReadOnly AnyInsteadOfEmptinessCheck As New DiagnosticDescriptor(
            id:=DiagnosticIds.AnyInsteadOfEmptinessCheck,
            title:="Enumerable.Any() instead of IsEmpty, Count or Length",
            messageFormat:="Use the '{0}' property instead of 'Enumerable.Any()' to avoid enumerator boxing and allocation",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Enumerable.Any() without a predicate checks the type of the collection at run time, or creates an enumerator, to know whether it has elements. The IsEmpty, Count and Length properties answer the same question directly and state the intent more clearly. Equivalent of CA1860, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.CollectionNameOnEnumerableParameter"/>.
        ''' </summary>
        Public Shared ReadOnly CollectionNameOnEnumerableParameter As New DiagnosticDescriptor(
            id:=DiagnosticIds.CollectionNameOnEnumerableParameter,
            title:="IEnumerable parameter named as a collection",
            messageFormat:="Rename parameter '{0}'{2} in '{1}' to match its 'IEnumerable' type",
            category:=DevCaseDescriptors.CategoryNaming,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="A parameter typed IEnumerable or IEnumerable(Of T) is a sequence: it may be lazy, it may be enumerated only once, and it has no Count or indexer. Names such as 'collection', 'list' or 'array' (or 'col', 'coll', 'lst', 'arr') suggest otherwise. The suggested name is 'sequence' ('seq' for an abbreviation), or 'source', 'value' or 'items' when that name is already used in the member.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.StringBuilderArgumentConvertedToString"/>.
        ''' </summary>
        Public Shared ReadOnly StringBuilderArgumentConvertedToString As New DiagnosticDescriptor(
            id:=DiagnosticIds.StringBuilderArgumentConvertedToString,
            title:="StringBuilder argument converted to a string",
            messageFormat:="Prefer strongly-typed StringBuilder overload",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="StringBuilder.Append and StringBuilder.Insert have overloads for every primitive type, and Append(Char, Integer) repeats a character. Converting the argument with ToString(), or creating the string with New String(Char, Integer), allocates a temporary string that the overload avoids, with the same result. Equivalent of CA1830, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.GenericOverloadInsteadOfTypeArgument"/>.
        ''' </summary>
        Public Shared ReadOnly GenericOverloadInsteadOfTypeArgument As New DiagnosticDescriptor(
            id:=DiagnosticIds.GenericOverloadInsteadOfTypeArgument,
            title:="System.Type argument instead of a generic overload",
            messageFormat:="Use the generic overload '{0}' instead of 'GetType({1})' for compile-time type safety",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="When the type is known at compile time (GetType(T)), the generic overload of the same method, such as Activator.CreateInstance(Of T)(), Marshal.SizeOf(Of T)() or Marshal.PtrToStructure(Of T)(ptr), returns the right type without a cast and is checked by the compiler. Equivalent of CA2263, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingDesignerSerializationConfiguration"/>.
        ''' </summary>
        Public Shared ReadOnly MissingDesignerSerializationConfiguration As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingDesignerSerializationConfiguration,
            title:="Component property without designer serialization configuration",
            messageFormat:="Add <DesignerSerializationVisibility> or <DefaultValue> to property '{0}' to control how the designer serializes it",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="The Windows Forms designer writes the value of every public read-write property of a component into the InitializeComponent code. A property should state how it is serialized with DesignerSerializationVisibilityAttribute, DefaultValueAttribute or a ShouldSerialize method, so the designer does not persist values (or content) by accident. Equivalent of WFO1000, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ArgumentNullExceptionForValueType"/>.
        ''' </summary>
        Public Shared ReadOnly ArgumentNullExceptionForValueType As New DiagnosticDescriptor(
            id:=DiagnosticIds.ArgumentNullExceptionForValueType,
            title:="ArgumentNullException for a value-type parameter",
            messageFormat:="Do not throw 'ArgumentNullException' for '{0}' because a '{1}' value can never be Nothing",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="A parameter of a non-nullable value type (Integer, Date, a Structure...) can never be Nothing, so a guard that throws ArgumentNullException for it is dead code or, with 'value = Nothing', a check for the default value (0, #12:00:00 AM#...) that reports the wrong exception. Similar to CA2264, for the classic guards of .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.TaskContinuationOptionsAsState"/>.
        ''' </summary>
        Public Shared ReadOnly TaskContinuationOptionsAsState As New DiagnosticDescriptor(
            id:=DiagnosticIds.TaskContinuationOptionsAsState,
            title:="TaskContinuationOptions passed to a TaskCompletionSource constructor",
            messageFormat:="Use 'TaskCreationOptions' instead of 'TaskContinuationOptions' because this constructor treats the argument as state",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="TaskCompletionSource has a constructor that takes TaskCreationOptions and another that takes an Object state. A TaskContinuationOptions value binds to the state constructor: the options are stored as state and never applied (RunContinuationsAsynchronously, for example, has no effect). Equivalent of CA2247, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.EndOfStreamInAsyncMethod"/>.
        ''' </summary>
        Public Shared ReadOnly EndOfStreamInAsyncMethod As New DiagnosticDescriptor(
            id:=DiagnosticIds.EndOfStreamInAsyncMethod,
            title:="StreamReader.EndOfStream in an Async method",
            messageFormat:="Check the result of 'ReadLineAsync' for Nothing instead of reading 'EndOfStream' in this Async method to avoid blocking the thread",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="StreamReader.EndOfStream reads from the stream synchronously when no data is buffered, which blocks the thread of an Async method. ReadLineAsync returns Nothing at the end of the stream, so the loop can test its result instead. Equivalent of CA2024, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.IncompatibleCastOrOfType"/>.
        ''' </summary>
        Public Shared ReadOnly IncompatibleCastOrOfType As New DiagnosticDescriptor(
            id:=DiagnosticIds.IncompatibleCastOrOfType,
            title:="Enumerable.Cast or OfType with an incompatible type",
            messageFormat:="Do not call '{0}(Of {1})' on a sequence of '{2}' because {3}",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Enumerable.Cast(Of T) and Enumerable.OfType(Of T) use a run-time type check, without widening or user-defined conversions. When the element type of the sequence can never be T, Cast throws InvalidCastException for every element and OfType always returns an empty sequence. Equivalent of CA2021, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.BlockCopyLengthAsByteCount"/>.
        ''' </summary>
        Public Shared ReadOnly BlockCopyLengthAsByteCount As New DiagnosticDescriptor(
            id:=DiagnosticIds.BlockCopyLengthAsByteCount,
            title:="Buffer.BlockCopy count is an array length",
            messageFormat:="Use 'Buffer.ByteLength({0})' instead of '{0}.Length' because 'Buffer.BlockCopy' counts bytes, not elements",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="The count argument of Buffer.BlockCopy is a number of bytes. The Length of an array whose elements are larger than one byte (Integer, Char, Double...) is a number of elements, so only part of the array is copied. Equivalent of CA2018, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ValueTypeInReferenceEquals"/>.
        ''' </summary>
        Public Shared ReadOnly ValueTypeInReferenceEquals As New DiagnosticDescriptor(
            id:=DiagnosticIds.ValueTypeInReferenceEquals,
            title:="Value type passed to ReferenceEquals",
            messageFormat:="Do not pass value type '{0}' to 'ReferenceEquals' because each call boxes it into a new object, causing the reference comparison to always return 'False'",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Object.ReferenceEquals compares references. A value-type argument is boxed into a new object for each call, so the comparison is False even for the same value (or True for two Nothing nullable values), never a meaningful identity check. Equivalent of CA2013, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.StringLiteralInsteadOfNameOf"/>.
        ''' </summary>
        Public Shared ReadOnly StringLiteralInsteadOfNameOf As New DiagnosticDescriptor(
            id:=DiagnosticIds.StringLiteralInsteadOfNameOf,
            title:="String literal instead of NameOf",
            messageFormat:="Use 'NameOf({0})' instead of the string literal to keep the name in sync with renames",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="A string literal that repeats the name of a parameter (paramName arguments) or of a property (propertyName arguments) is not updated when the parameter or property is renamed. NameOf produces the same string and is checked by the compiler. Equivalent of CA1507, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ManualCancellationCheck"/>.
        ''' </summary>
        Public Shared ReadOnly ManualCancellationCheck As New DiagnosticDescriptor(
            id:=DiagnosticIds.ManualCancellationCheck,
            title:="IsCancellationRequested check that throws OperationCanceledException",
            messageFormat:="Use '{0}.ThrowIfCancellationRequested()' instead of checking 'IsCancellationRequested' and throwing 'OperationCanceledException'",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="CancellationToken.ThrowIfCancellationRequested checks whether the token has been canceled and throws an OperationCanceledException with the token if it has: the If statement that does the same is longer and loses the token. Equivalent of CA2250, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.HasFlagWithDifferentEnum"/>.
        ''' </summary>
        Public Shared ReadOnly HasFlagWithDifferentEnum As New DiagnosticDescriptor(
            id:=DiagnosticIds.HasFlagWithDifferentEnum,
            title:="Enum.HasFlag with a different enum type",
            messageFormat:="Pass a '{0}' value to 'HasFlag' instead of '{1}' because a different enum type throws 'ArgumentException'",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Enum.HasFlag requires an argument of the same enum type as the instance; a value of another enum type throws ArgumentException at run time. Equivalent of CA2248, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ThreadStaticFieldInitialization"/>.
        ''' </summary>
        Public Shared ReadOnly ThreadStaticFieldInitialization As New DiagnosticDescriptor(
            id:=DiagnosticIds.ThreadStaticFieldInitialization,
            title:="ThreadStatic field initialized inline or in a Shared constructor",
            messageFormat:="Initialize ThreadStatic field '{0}' lazily on each thread because an initializer only runs on one thread",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="A <ThreadStatic> field has one value per thread. Its initializer, like an assignment in the Shared constructor, runs once, on the thread that initializes the type: every other thread sees Nothing or the default value. Equivalent of CA2019, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.CancellationTokenNotForwarded"/>.
        ''' </summary>
        Public Shared ReadOnly CancellationTokenNotForwarded As New DiagnosticDescriptor(
            id:=DiagnosticIds.CancellationTokenNotForwarded,
            title:="CancellationToken parameter not forwarded",
            messageFormat:="Forward the '{0}' parameter to '{1}' to propagate cancellation",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="A method that receives a CancellationToken should pass it to the methods it calls that accept one, as an overload with an additional CancellationToken parameter or as an optional parameter, so that canceling the operation also cancels them. Equivalent of CA2016, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.CountInsteadOfAny"/>.
        ''' </summary>
        Public Shared ReadOnly CountInsteadOfAny As New DiagnosticDescriptor(
            id:=DiagnosticIds.CountInsteadOfAny,
            title:="Count() compared with zero instead of Any()",
            messageFormat:="Use 'Any()' instead of '{0}()' to stop enumerating at the first element",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Comparing Enumerable.Count() or LongCount() with 0 or 1 enumerates the whole sequence when it is not a collection (a LINQ query, an iterator...), and Count(predicate) always does; Any() stops at the first element, or at the first element that matches the predicate. Equivalent of CA1827, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.CountMethodInsteadOfProperty"/>.
        ''' </summary>
        Public Shared ReadOnly CountMethodInsteadOfProperty As New DiagnosticDescriptor(
            id:=DiagnosticIds.CountMethodInsteadOfProperty,
            title:="Enumerable.Count() instead of the Length or Count property",
            messageFormat:="Use the '{0}' property instead of 'Enumerable.Count()' to avoid enumerating the sequence",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Arrays and strings expose their length directly; Enumerable.Count() on a string enumerates every character, and on an array it goes through a type check and an interface call. Equivalent of CA1829, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.CountInsteadOfIsEmpty"/>.
        ''' </summary>
        Public Shared ReadOnly CountInsteadOfIsEmpty As New DiagnosticDescriptor(
            id:=DiagnosticIds.CountInsteadOfIsEmpty,
            title:="Count compared with zero instead of IsEmpty",
            messageFormat:="Use 'IsEmpty' instead of 'Count' to check whether this '{0}' is empty without counting its elements",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Collections that expose both Count and IsEmpty, such as ConcurrentQueue, ConcurrentStack, ConcurrentBag and ConcurrentDictionary, compute Count by walking or locking the whole collection, while IsEmpty only checks whether there is an element. Equivalent of CA1836, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.WhenAllWithSingleTask"/>.
        ''' </summary>
        Public Shared ReadOnly WhenAllWithSingleTask As New DiagnosticDescriptor(
            id:=DiagnosticIds.WhenAllWithSingleTask,
            title:="Task.WhenAll with a single task",
            messageFormat:="Await or return the task directly instead of passing it alone to 'Task.WhenAll'",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Task.WhenAll with a single task allocates an array and a new task that completes when that task completes: the task itself can be awaited or returned. Equivalent of CA1842, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.WaitAllWithSingleTask"/>.
        ''' </summary>
        Public Shared ReadOnly WaitAllWithSingleTask As New DiagnosticDescriptor(
            id:=DiagnosticIds.WaitAllWithSingleTask,
            title:="Task.WaitAll with a single task",
            messageFormat:="Call 'Wait()' on the task directly instead of passing it alone to 'Task.WaitAll'",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Task.WaitAll with a single task allocates an array to wait for one task; Task.Wait() waits for it directly and throws the same AggregateException. Equivalent of CA1843, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ContainsKeyBeforeRemove"/>.
        ''' </summary>
        Public Shared ReadOnly ContainsKeyBeforeRemove As New DiagnosticDescriptor(
            id:=DiagnosticIds.ContainsKeyBeforeRemove,
            title:="Dictionary.Remove guarded by ContainsKey",
            messageFormat:="Do not guard 'Remove' with 'ContainsKey' because 'Remove' already checks whether the key exists",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Dictionary.Remove(key) looks up the key, removes it if it exists and returns whether it did, without throwing when it does not. Checking ContainsKey first looks up the key twice. Equivalent of CA1853, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ContainsBeforeSetAddOrRemove"/>.
        ''' </summary>
        Public Shared ReadOnly ContainsBeforeSetAddOrRemove As New DiagnosticDescriptor(
            id:=DiagnosticIds.ContainsBeforeSetAddOrRemove,
            title:="Set Add or Remove guarded by Contains",
            messageFormat:="Do not guard '{0}' with 'Contains' because '{0}' already checks whether the item exists",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="HashSet.Add and HashSet.Remove (and the members of every ISet) look up the item, add or remove it only when needed and return whether they did. Checking Contains first looks up the item twice. Equivalent of CA1868, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.KeysOrValuesContains"/>.
        ''' </summary>
        Public Shared ReadOnly KeysOrValuesContains As New DiagnosticDescriptor(
            id:=DiagnosticIds.KeysOrValuesContains,
            title:="Keys.Contains or Values.Contains on a dictionary",
            messageFormat:="Use '{0}' instead of '{1}.Contains' to look up the dictionary directly",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Dictionary.Keys.Contains binds to the LINQ Contains method on .NET Framework, which walks every key, while ContainsKey is a hash lookup; Values.Contains also goes through the Values collection and LINQ, which ContainsValue avoids. Equivalent of CA1841, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.CurrentThreadManagedThreadId"/>.
        ''' </summary>
        Public Shared ReadOnly CurrentThreadManagedThreadId As New DiagnosticDescriptor(
            id:=DiagnosticIds.CurrentThreadManagedThreadId,
            title:="Thread.CurrentThread.ManagedThreadId instead of Environment.CurrentManagedThreadId",
            messageFormat:="Use 'Environment.CurrentManagedThreadId' instead of 'Thread.CurrentThread.ManagedThreadId' because it is simpler and faster",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Environment.CurrentManagedThreadId (.NET Framework 4.5 or later) returns the identifier of the current managed thread without getting its Thread object first. Equivalent of CA1840, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ContainsKeyThenIndexer"/>.
        ''' </summary>
        Public Shared ReadOnly ContainsKeyThenIndexer As New DiagnosticDescriptor(
            id:=DiagnosticIds.ContainsKeyThenIndexer,
            title:="ContainsKey followed by the indexer instead of TryGetValue",
            messageFormat:="Use 'TryGetValue' instead of 'ContainsKey' and the indexer to avoid a double lookup",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Checking ContainsKey and then reading the indexer looks up the key twice; TryGetValue looks it up once and returns the value. Equivalent of CA1854, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.IndexOfComparedWithZero"/>.
        ''' </summary>
        Public Shared ReadOnly IndexOfComparedWithZero As New DiagnosticDescriptor(
            id:=DiagnosticIds.IndexOfComparedWithZero,
            title:="IndexOf compared with zero instead of StartsWith",
            messageFormat:="Use 'StartsWith' instead of comparing 'IndexOf' with 0 to avoid searching the whole string",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="String.IndexOf(value) = 0 checks whether the string starts with value, but IndexOf keeps searching the whole string when it does not; StartsWith only compares the beginning. Equivalent of CA1858, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RedundantSubstringLength"/>.
        ''' </summary>
        Public Shared ReadOnly RedundantSubstringLength As New DiagnosticDescriptor(
            id:=DiagnosticIds.RedundantSubstringLength,
            title:="Redundant length argument of Substring",
            messageFormat:="Remove the redundant length argument of 'Substring' because it already reads to the end of the string",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="text.Substring(start, text.Length - start) computes the length of the rest of the string, which text.Substring(start) already does: the calculation is redundant and easy to get wrong. Equivalent of CA1514, for .NET Framework projects.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.ConstantArrayArgument"/>.
        ''' </summary>
        Public Shared ReadOnly ConstantArrayArgument As New DiagnosticDescriptor(
            id:=DiagnosticIds.ConstantArrayArgument,
            title:="Constant array passed as an argument",
            messageFormat:="Extract the constant array argument of '{0}' into a Shared ReadOnly field to avoid allocating it on every call",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="An array of constants created in an argument is allocated again on every call. When the called method does not modify the array, a Shared ReadOnly field created once can be passed instead. Equivalent of CA1861, for .NET Framework projects.")

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DevCaseDescriptors"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

End Namespace

#End Region
