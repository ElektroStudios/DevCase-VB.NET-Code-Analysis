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

#Region " TaskResultAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports blocking <c>.Result</c> property accesses on <see cref="System.Threading.Tasks.Task(Of TResult)"/> and 
    ''' <c>ValueTask(Of TResult)</c> instances (rule <see cref="DiagnosticIds.TaskResultAccess"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Unlike the original text-based <c>TaskResultRewriter</c> heuristic (which matched the words "Task", "Async" or "Await" 
    ''' anywhere in the expression and therefore produced false positives such as <c>myAsyncSettings.Result</c>), 
    ''' this analyzer binds the property symbol through the semantic model.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class TaskResultAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The identifier name of the <c>Result</c> property.
        ''' </summary>
        Friend Const ResultIdentifierName As String = "Result"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.TaskResultAccess)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TaskResultAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf TaskResultAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Resolves the task types of the compilation and registers the member access analysis.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            Dim taskOfT As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1")
            Dim valueTaskOfT As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask`1")
            If taskOfT Is Nothing AndAlso valueTaskOfT Is Nothing Then
                Exit Sub ' The compilation does not reference the TPL; nothing can be reported.
            End If

            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(
                Sub(nodeContext As SyntaxNodeAnalysisContext) TaskResultAnalyzer.AnalyzeMemberAccess(nodeContext, taskOfT, valueTaskOfT)),
                SyntaxKind.SimpleMemberAccessExpression)
        End Sub

        ''' <summary>
        ''' Analyzes a member access expression.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <param name="taskOfT">
        ''' The <c>Task(Of T)</c> type symbol, or <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <param name="valueTaskOfT">
        ''' The <c>ValueTask(Of T)</c> type symbol, or <see langword="Nothing"/>.
        ''' </param>
        Private Shared Sub AnalyzeMemberAccess(context As SyntaxNodeAnalysisContext, taskOfT As INamedTypeSymbol, valueTaskOfT As INamedTypeSymbol)

            Dim memberAccess As MemberAccessExpressionSyntax = DirectCast(context.Node, MemberAccessExpressionSyntax)
            If Not memberAccess.Name.Identifier.ValueText.Equals(TaskResultAnalyzer.ResultIdentifierName, StringComparison.OrdinalIgnoreCase) Then
                Exit Sub
            End If

            If UtilAnalyzerSyntax.IsInsideNameOf(memberAccess) Then
                Exit Sub
            End If

            Dim propertySymbol As IPropertySymbol = TryCast(context.SemanticModel.GetSymbolInfo(memberAccess, context.CancellationToken).Symbol, IPropertySymbol)
            If propertySymbol Is Nothing OrElse propertySymbol.IsStatic Then
                Exit Sub
            End If

            Dim containingDefinition As INamedTypeSymbol = propertySymbol.ContainingType.OriginalDefinition
            If Not (SymbolEqualityComparer.Default.Equals(containingDefinition, taskOfT) OrElse
                    SymbolEqualityComparer.Default.Equals(containingDefinition, valueTaskOfT)) Then
                Exit Sub
            End If

            ' "task.Result()" is also a valid property access in Visual Basic; report the whole invocation.
            Dim reportedNode As SyntaxNode = memberAccess
            Dim parentInvocation As InvocationExpressionSyntax = TryCast(memberAccess.Parent, InvocationExpressionSyntax)
            If parentInvocation IsNot Nothing AndAlso
               parentInvocation.Expression Is memberAccess AndAlso
               (parentInvocation.ArgumentList Is Nothing OrElse parentInvocation.ArgumentList.Arguments.Count = 0) Then
                reportedNode = parentInvocation
            End If

            Dim typeDisplay As String = propertySymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.TaskResultAccess, reportedNode.GetLocation(), typeDisplay))
        End Sub

#End Region

    End Class

End Namespace

#End Region
