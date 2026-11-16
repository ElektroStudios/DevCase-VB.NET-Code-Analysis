#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ThrowHelperMigrationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports runs of classic argument guards in .NET Framework code that have an exact modern throw-helper equivalent 
    ''' (rule <see cref="DiagnosticIds.ThrowHelperMigration"/>), so that they can be paired with it in an <c>#If NETCOREAPP</c> block.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Supported conversions (only guards whose exception receives exactly <c>NameOf(parameter)</c>, so no message is lost):
    ''' <list type="bullet">
    '''   <item><description><c>p Is Nothing</c>, <c>IsNothing(p)</c> → <c>ArgumentNullException.ThrowIfNull(p)</c></description></item>
    '''   <item><description><c>String.IsNullOrEmpty(p)</c> → <c>ArgumentException.ThrowIfNullOrEmpty(p)</c></description></item>
    '''   <item><description><c>String.IsNullOrWhiteSpace(p)</c> → <c>ArgumentException.ThrowIfNullOrWhiteSpace(p)</c></description></item>
    '''   <item><description><c>p &lt; 0</c>, <c>p &lt;= 0</c>, <c>p = 0</c> → <c>ThrowIfNegative</c>, <c>ThrowIfNegativeOrZero</c>, <c>ThrowIfZero</c></description></item>
    '''   <item><description><c>p &lt; x</c>, <c>p &lt;= x</c>, <c>p &gt; x</c>, <c>p &gt;= x</c>, <c>p = x</c>, <c>p &lt;&gt; x</c> → <c>ThrowIfLessThan</c>, ..., <c>ThrowIfNotEqual</c></description></item>
    ''' </list>
    ''' The rule is silent on modern .NET (the built-in CA1510-CA1513 rules apply there) and inside existing <c>#If</c> blocks.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThrowHelperMigrationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThrowHelperMigration)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThrowHelperMigrationAnalyzer"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Sub Initialize(context As AnalysisContext)

            If context Is Nothing Then
                Throw New ArgumentNullException(NameOf(context))
            End If

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
            context.EnableConcurrentExecution()
            context.RegisterCompilationStartAction(AddressOf ThrowHelperMigrationAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the guard analysis for .NET Framework compilations only.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilThrowHelperGuards.IsNetFrameworkWithoutThrowHelpers(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in CA1510-CA1513 rules already cover this.
            End If

            context.RegisterSyntaxNodeAction(AddressOf ThrowHelperMigrationAnalyzer.AnalyzeIf,
                                             SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement)
        End Sub

        ''' <summary>
        ''' Analyzes an <see langword="If"/> statement and reports the run of convertible guards it starts.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIf(context As SyntaxNodeAnalysisContext)

            Dim statement As StatementSyntax = DirectCast(context.Node, StatementSyntax)
            If Not UtilThrowHelperGuards.CanHostDirectives(statement) OrElse
               UtilThrowHelperGuards.TryCreate(statement, context.SemanticModel, context.CancellationToken) Is Nothing OrElse
               Not UtilThrowHelperGuards.IsFirstOfRun(statement, context.SemanticModel, context.CancellationToken) OrElse
               UtilThrowHelperGuards.IsInsideConditionalDirective(statement) Then
                Exit Sub
            End If

            Dim run As List(Of ThrowHelperGuard) = UtilThrowHelperGuards.GetRun(statement, context.SemanticModel, context.CancellationToken)
            Dim span As TextSpan = TextSpan.FromBounds(run(0).Statement.SpanStart, run(run.Count - 1).Statement.Span.End)
            Dim helpers As String = String.Join(", ", run.Select(Function(g As ThrowHelperGuard) g.Replacement.Substring(0, g.Replacement.IndexOf("(", StringComparison.Ordinal))).Distinct(StringComparer.Ordinal))

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThrowHelperMigration, Location.Create(statement.SyntaxTree, span),
                                                       run.Count, helpers))
        End Sub

#End Region

    End Class

End Namespace

#End Region
