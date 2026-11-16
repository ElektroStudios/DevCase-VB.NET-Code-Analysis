#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " SilentGuardClauseAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="If"/> statements that evaluate a parameter and silently leave the member 
    ''' (rule <see cref="DiagnosticIds.SilentGuardClause"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Supported atomic conditions: <c>p Is Nothing</c>, <c>p IsNot Nothing</c>, <c>p = Nothing</c>, <c>IsNothing(p)</c>, 
    ''' <c>String.IsNullOrEmpty(p)</c>, <c>String.IsNullOrWhiteSpace(p)</c>, relational comparisons such as <c>p &lt; 0</c>, 
    ''' and <c>p.Length</c> / <c>p.Count</c> comparisons; combined with <see langword="OrElse"/> or <see langword="Or"/>.
    ''' <para></para>
    ''' Supported silent exits: <see langword="Return"/>, <c>Return Nothing</c>, <c>Exit Sub</c>, <c>Exit Function</c>, <c>Exit Property</c>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class SilentGuardClauseAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.SilentGuardClause)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SilentGuardClauseAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf SilentGuardClauseAnalyzer.AnalyzeIf,
                                             SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a multi-line or single-line <see langword="If"/> statement.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIf(context As SyntaxNodeAnalysisContext)

            Dim exitStatement As StatementSyntax = Nothing
            Dim conditions As ImmutableArray(Of GuardCondition) = Nothing
            If Not UtilGuardClauses.TryAnalyze(context.Node, context.SemanticModel, context.CancellationToken, exitStatement, conditions) Then
                Exit Sub
            End If

            Dim parameterNames As String = String.Join("', '", conditions.Select(Function(c As GuardCondition) c.ParameterName).Distinct(StringComparer.OrdinalIgnoreCase))
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.SilentGuardClause, exitStatement.GetLocation(), parameterNames))
        End Sub

#End Region

    End Class

End Namespace

#End Region
