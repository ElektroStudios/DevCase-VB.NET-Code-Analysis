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

#End Region

#Region " Fields "

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingConfigureAwait"/>.
        ''' </summary>
        Public Shared ReadOnly MissingConfigureAwait As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingConfigureAwait,
            title:="Await expression is missing ConfigureAwait(False)",
            messageFormat:="Await expression must be configured with '.ConfigureAwait(False)' to avoid synchronization-context deadlocks",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Error,
            isEnabledByDefault:=True,
            description:="Reusable library code must not capture the synchronization context. Every awaited task-like value that exposes a ConfigureAwait(Boolean) method must be configured explicitly.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.TaskResultAccess"/>.
        ''' </summary>
        Public Shared ReadOnly TaskResultAccess As New DiagnosticDescriptor(
            id:=DiagnosticIds.TaskResultAccess,
            title:="Blocking .Result access on a task",
            messageFormat:="Do not access '.Result' on '{0}'; use '.GetAwaiter().GetResult()' (or Await) so exceptions are not wrapped in an AggregateException",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Error,
            isEnabledByDefault:=True,
            description:="Task(Of T).Result wraps faults inside an AggregateException and hides the original stack trace. GetAwaiter().GetResult() surfaces the root exception directly.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.SilentGuardClause"/>.
        ''' </summary>
        Public Shared ReadOnly SilentGuardClause As New DiagnosticDescriptor(
            id:=DiagnosticIds.SilentGuardClause,
            title:="Silent guard clause on a parameter",
            messageFormat:="Guard clause on parameter '{0}' exits silently; throw an explicit argument exception instead (fail-fast)",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Error,
            isEnabledByDefault:=True,
            description:="A broken precondition must never be hidden by a silent Return or Exit statement. Throw ArgumentNullException, ArgumentException or ArgumentOutOfRangeException instead.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingOptionStatements"/>.
        ''' </summary>
        Public Shared ReadOnly MissingOptionStatements As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingOptionStatements,
            title:="Missing Option statements",
            messageFormat:="Source file is missing the required Option statement(s): {0}",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Every source file must explicitly declare 'Option Strict On', 'Option Explicit On' and 'Option Infer Off'.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingExplicitConstructor"/>.
        ''' </summary>
        Public Shared ReadOnly MissingExplicitConstructor As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingExplicitConstructor,
            title:="Class lacks an explicit constructor",
            messageFormat:="Class '{0}' does not explicitly declare an instance constructor",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Every class must explicitly declare its instance constructor so that its accessibility and documentation are intentional.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.TypeOutsideNamespace"/>.
        ''' </summary>
        Public Shared ReadOnly TypeOutsideNamespace As New DiagnosticDescriptor(
            id:=DiagnosticIds.TypeOutsideNamespace,
            title:="Type declared in the global namespace",
            messageFormat:="Type '{0}' is declared in the global namespace; declare it inside a Namespace block",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Top-level types must not live in the global namespace: declare them inside an explicit Namespace block. Not reported when the project RootNamespace is set, because the types already belong to it.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingXmlDocumentation"/>.
        ''' </summary>
        Public Shared ReadOnly MissingXmlDocumentation As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingXmlDocumentation,
            title:="Public member lacks XML documentation",
            messageFormat:="Public member '{0}' is missing an XML documentation comment",
            category:=DevCaseDescriptors.CategoryDocumentation,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Every member declared with the Public modifier must have an XML documentation comment. An '<inheritdoc/>' comment is accepted.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.XmlParameterMismatch"/>.
        ''' </summary>
        Public Shared ReadOnly XmlParameterMismatch As New DiagnosticDescriptor(
            id:=DiagnosticIds.XmlParameterMismatch,
            title:="XML <param> tags do not match the parameter list",
            messageFormat:="XML documentation of '{0}' does not match its parameters: {1}",
            category:=DevCaseDescriptors.CategoryDocumentation,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Every parameter must be documented by exactly one non-empty <param> tag, and every <param> tag must refer to an existing parameter.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.MissingStringComparison"/>.
        ''' </summary>
        Public Shared ReadOnly MissingStringComparison As New DiagnosticDescriptor(
            id:=DiagnosticIds.MissingStringComparison,
            title:="String comparison argument omitted",
            messageFormat:="'{0}' accepts a '{1}' argument; specify the string comparison explicitly",
            category:=DevCaseDescriptors.CategoryGlobalization,
            defaultSeverity:=DiagnosticSeverity.Warning,
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
            messageFormat:="Flags enum member '{0}' should be declared as '{1}' instead of a direct numeric literal",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Warning,
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
            messageFormat:="{0} argument guard(s) have a modern .NET throw-helper equivalent ({1}); add a conditional compilation block for migration",
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
            messageFormat:="'{0}' throws exception(s) not documented with an <exception> tag: {1}",
            category:=DevCaseDescriptors.CategoryDocumentation,
            defaultSeverity:=DiagnosticSeverity.Warning,
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
            messageFormat:="'{0}' can be replaced on modern .NET by '{1}'; add a conditional compilation block for migration",
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
            messageFormat:="Loop over '{0}' must call 'Thread.CurrentThread.Join(0)' to prevent ContextSwitchDeadlock on long-running enumerations",
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
            messageFormat:="'{0}' calls itself recursively; rewrite the algorithm iteratively using an explicit Stack or Queue",
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
            messageFormat:="Async Sub '{0}' is not an event handler; declare it as an Async Function that returns a Task so callers can await it and observe its exceptions",
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
            messageFormat:="'{0}' evaluates a regular expression without a match timeout; pass a TimeSpan to limit its execution time (ReDoS)",
            category:=DevCaseDescriptors.CategorySecurity,
            defaultSeverity:=DiagnosticSeverity.Error,
            isEnabledByDefault:=True,
            description:="A regular expression with catastrophic backtracking evaluated against malicious or unexpectedly large input can run for minutes or forever (Regular expression Denial of Service, CWE-1333). Pass a match timeout to the Regex constructor or to the static Regex methods; RegexMatchTimeoutException is thrown when it expires. Equivalent to SonarQube rule S6444.",
            helpLinkUri:="https://learn.microsoft.com/dotnet/standard/base-types/best-practices-regex#use-time-out-values")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DateTimeWithoutKind"/>.
        ''' </summary>
        Public Shared ReadOnly DateTimeWithoutKind As New DiagnosticDescriptor(
            id:=DiagnosticIds.DateTimeWithoutKind,
            title:="DateTime created without DateTimeKind",
            messageFormat:="'New DateTime' without a DateTimeKind creates a value of kind Unspecified; state the kind explicitly",
            category:=DevCaseDescriptors.CategoryReliability,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="A DateTime created without a DateTimeKind has the kind Unspecified, which ToUniversalTime and ToLocalTime treat as local time and serializers handle inconsistently. Stating the kind (Unspecified, Utc or Local) documents what the value represents. Equivalent to SonarQube rule S6562.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.DateTimeParseWithoutProvider"/>.
        ''' </summary>
        Public Shared ReadOnly DateTimeParseWithoutProvider As New DiagnosticDescriptor(
            id:=DiagnosticIds.DateTimeParseWithoutProvider,
            title:="Date and time parsed without a format provider",
            messageFormat:="'{0}' parses with the culture of the current thread; pass an IFormatProvider such as CultureInfo.InvariantCulture",
            category:=DevCaseDescriptors.CategoryGlobalization,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="DateTime, DateTimeOffset, TimeSpan, DateOnly and TimeOnly parsing methods without an IFormatProvider depend on the regional settings of the machine: the same text can produce a different value or fail on another computer. Equivalent to SonarQube rule S6580.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.FlagsEnumWithoutNone"/>.
        ''' </summary>
        Public Shared ReadOnly FlagsEnumWithoutNone As New DiagnosticDescriptor(
            id:=DiagnosticIds.FlagsEnumWithoutNone,
            title:="Flags enumeration without a 'None' zero value",
            messageFormat:="Flags enumeration '{0}' must declare its zero value as a member named 'None'",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="The zero value of a Flags enumeration means that no flag is set; naming it None makes code such as 'value = MyFlags.None' readable and avoids using the literal 0. Equivalent to SonarQube rule S2346.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.LinqInsteadOfCollectionMethod"/>.
        ''' </summary>
        Public Shared ReadOnly LinqInsteadOfCollectionMethod As New DiagnosticDescriptor(
            id:=DiagnosticIds.LinqInsteadOfCollectionMethod,
            title:="LINQ method used instead of the collection-specific method",
            messageFormat:="Use '{0}' instead of the LINQ '{1}' extension on '{2}'",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="List(Of T), arrays and ImmutableList(Of T) provide Find, Exists and TrueForAll methods that are faster than the equivalent LINQ FirstOrDefault, Any and All extensions with a predicate. Equivalent to SonarQube rules S6602, S6603 and S6605.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.LinqInsteadOfIndexer"/>.
        ''' </summary>
        Public Shared ReadOnly LinqInsteadOfIndexer As New DiagnosticDescriptor(
            id:=DiagnosticIds.LinqInsteadOfIndexer,
            title:="LINQ method used instead of the indexer",
            messageFormat:="Use the indexer instead of the LINQ '{0}' extension on '{1}'",
            category:=DevCaseDescriptors.CategoryPerformance,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Types that implement IList(Of T) (List(Of T), arrays, Collection(Of T)...) can be indexed directly; First, Last and ElementAt go through LINQ type checks and interface calls. Equivalent to SonarQube rule S6608 (CA1826 only covers IReadOnlyList(Of T) types that do not implement IList(Of T)).")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.InvertedComparison"/>.
        ''' </summary>
        Public Shared ReadOnly InvertedComparison As New DiagnosticDescriptor(
            id:=DiagnosticIds.InvertedComparison,
            title:="Negated comparison",
            messageFormat:="Replace 'Not' and the '{0}' operator with the inverse operator '{1}'",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="Negating a comparison is harder to read than using the inverse operator: 'Not (a = b)' is 'a <> b'. Relational comparisons of floating-point values are not reported because they differ for NaN. Equivalent to SonarQube rule S1940.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RethrowOnlyCatch"/>.
        ''' </summary>
        Public Shared ReadOnly RethrowOnlyCatch As New DiagnosticDescriptor(
            id:=DiagnosticIds.RethrowOnlyCatch,
            title:="Catch block only rethrows the exception",
            messageFormat:="This Catch block only rethrows the exception; remove it",
            category:=DevCaseDescriptors.CategoryDesign,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="A Catch block whose only statement is 'Throw' has no effect: the exception propagates exactly as it would without it. 'Throw ex' with the caught exception is reported too: it also discards the original stack trace. Catch blocks that only rethrow are reported only when no other Catch block follows them, because otherwise they prevent a later block from handling that exception. Equivalent to SonarQube rule S2737.")

        ''' <summary>
        ''' The descriptor of rule <see cref="DiagnosticIds.RedundantNullCheckBeforeTypeOf"/>.
        ''' </summary>
        Public Shared ReadOnly RedundantNullCheckBeforeTypeOf As New DiagnosticDescriptor(
            id:=DiagnosticIds.RedundantNullCheckBeforeTypeOf,
            title:="Redundant null check before TypeOf",
            messageFormat:="The null check is redundant: '{0}' already handles Nothing",
            category:=DevCaseDescriptors.CategoryStyle,
            defaultSeverity:=DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:="'TypeOf x Is T' is False when x is Nothing, and 'TypeOf x IsNot T' is True, so a preceding 'x IsNot Nothing AndAlso' or 'x Is Nothing OrElse' adds nothing. Equivalent to SonarQube rule S4201.")

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
