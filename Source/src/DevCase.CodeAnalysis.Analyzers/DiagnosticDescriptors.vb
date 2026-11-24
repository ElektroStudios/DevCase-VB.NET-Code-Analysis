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
            messageFormat:="Use the indexer instead of 'Enumerable.{0}()' to avoid LINQ overhead",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="Types that implement IList(Of T) (List(Of T), arrays, Collection(Of T)...) can be indexed directly; First, Last and ElementAt go through LINQ type checks and interface calls. Equivalent to SonarQube rule S6608 (CA1826 only covers IReadOnlyList(Of T) types that do not implement IList(Of T)).")

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
            messageFormat:="Use a character literal instead of a string to invoke the Char overload",
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
            messageFormat:="Use the StringBuilder.{0}({1}) overload to avoid a temporary string",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Info,
            isEnabledByDefault:=True,
            description:="StringBuilder.Append and StringBuilder.Insert have overloads for every primitive type, and Append(Char, Integer) repeats a character. Converting the argument with ToString(), or creating the string with New String(Char, Integer), allocates a temporary string that the overload avoids, with the same result. Equivalent of CA1830, for .NET Framework projects.")

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
