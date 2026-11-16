#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " NetFrameworkApiMigrationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports .NET Framework API usages that have an exact modern .NET equivalent 
    ''' (rule <see cref="DiagnosticIds.NetFrameworkApiMigration"/>); see <see cref="UtilApiMigrations"/> for the equivalence table.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Only .NET Framework compilations are analyzed. A usage is reported only when the lines of its statement can be 
    ''' wrapped in a conditional compilation block (the statement owns its lines and is not already inside an <c>#If</c> block).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class NetFrameworkApiMigrationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.NetFrameworkApiMigration)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="NetFrameworkApiMigrationAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf NetFrameworkApiMigrationAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis for .NET Framework compilations only.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilThrowHelperGuards.IsNetFrameworkWithoutThrowHelpers(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in CA rules already suggest these APIs.
            End If

            context.RegisterSyntaxNodeAction(AddressOf NetFrameworkApiMigrationAnalyzer.AnalyzeNode,
                                             SyntaxKind.InvocationExpression, SyntaxKind.SimpleMemberAccessExpression,
                                             SyntaxKind.DirectCastExpression, SyntaxKind.CTypeExpression,
                                             SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement,
                                             SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanExpression,
                                             SyntaxKind.NotEqualsExpression, SyntaxKind.LessThanExpression,
                                             SyntaxKind.LessThanOrEqualExpression, SyntaxKind.EqualsExpression)
        End Sub

        ''' <summary>
        ''' Analyzes a candidate node.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)

            Dim migration As ApiMigration = UtilApiMigrations.TryMatch(context.Node, context.SemanticModel, context.CancellationToken)
            If migration Is Nothing Then
                Exit Sub
            End If

            Dim host As StatementSyntax = UtilApiMigrations.GetHost(migration)
            If host Is Nothing OrElse Not UtilApiMigrations.CanWrapHost(host) Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.NetFrameworkApiMigration, context.Node.GetLocation(),
                                                       migration.OriginalApi, migration.ModernApi))
        End Sub

#End Region

    End Class

End Namespace

#End Region
