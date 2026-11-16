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
