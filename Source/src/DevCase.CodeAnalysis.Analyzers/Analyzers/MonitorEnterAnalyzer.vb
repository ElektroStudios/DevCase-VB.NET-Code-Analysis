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
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " MonitorEnterAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to <c>Monitor.Enter</c> and <c>Monitor.TryEnter</c> that are not paired with <c>Monitor.Exit</c> in a 
    ''' <see langword="Finally"/> block (rule <see cref="DiagnosticIds.MonitorEnterWithoutFinally"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' A call is paired when a <c>Monitor.Exit</c> call on an equivalent lock expression is located in the <see langword="Finally"/> 
    ''' block of a <see langword="Try"/> statement that:
    ''' <list type="bullet">
    '''   <item><description>contains the call in its <see langword="Try"/> section (<c>Monitor.Enter(x, lockTaken)</c> pattern), or</description></item>
    '''   <item><description>immediately follows the statement of the call (<c>Monitor.Enter(x)</c> followed by <c>Try</c>), or</description></item>
    '''   <item><description>is a statement of the <see langword="If"/> block whose condition contains the call 
    '''   (<c>If Monitor.TryEnter(x) Then Try ... Finally Monitor.Exit(x)</c>).</description></item>
    ''' </list>
    ''' Locks acquired in one method and released in another one are reported too: the release cannot be guaranteed.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class MonitorEnterAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MonitorEnterWithoutFinally)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="MonitorEnterAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(
                Sub(c As CompilationStartAnalysisContext)
                    Dim monitorType As INamedTypeSymbol = c.Compilation.GetTypeByMetadataName("System.Threading.Monitor")
                    If monitorType IsNot Nothing Then
                        c.RegisterOperationAction(Sub(o As OperationAnalysisContext) MonitorEnterAnalyzer.AnalyzeInvocation(o, monitorType),
                                                  OperationKind.Invocation)
                    End If
                End Sub)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an invocation of <c>Monitor.Enter</c> or <c>Monitor.TryEnter</c> that is not paired with <c>Monitor.Exit</c> in a <see langword="Finally"/> block.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        ''' 
        ''' <param name="monitorType">
        ''' The <see cref="System.Threading.Monitor"/> type symbol.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext, monitorType As INamedTypeSymbol)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim name As String = invocation.TargetMethod.Name
            If (Not name.Equals("Enter", StringComparison.Ordinal) AndAlso Not name.Equals("TryEnter", StringComparison.Ordinal)) OrElse
               Not SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, monitorType) Then
                Exit Sub
            End If

            Dim lockArgument As IArgumentOperation = invocation.Arguments.FirstOrDefault(Function(a As IArgumentOperation) a.Parameter IsNot Nothing AndAlso a.Parameter.Ordinal = 0)
            If lockArgument Is Nothing Then
                Exit Sub
            End If

            Dim lockExpression As SyntaxNode = If(TryCast(lockArgument.Syntax, ArgumentSyntax)?.GetExpression(), lockArgument.Syntax)
            If Not MonitorEnterAnalyzer.IsReleasedInFinally(invocation.Syntax, lockExpression, context.Operation.SemanticModel, monitorType, context) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MonitorEnterWithoutFinally, invocation.Syntax.GetLocation(), name))
            End If
        End Sub

        ''' <summary>
        ''' Determines whether the lock acquired by a call is released in a <see langword="Finally"/> block of one of the accepted patterns.
        ''' </summary>
        ''' 
        ''' <param name="call">
        ''' The <c>Monitor.Enter</c> or <c>Monitor.TryEnter</c> invocation.
        ''' </param>
        ''' 
        ''' <param name="lockExpression">
        ''' The lock expression.
        ''' </param>
        ''' 
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        ''' 
        ''' <param name="monitorType">
        ''' The <see cref="System.Threading.Monitor"/> type symbol.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context (for cancellation).
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the lock is released in a <see langword="Finally"/> block; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsReleasedInFinally([call] As SyntaxNode, lockExpression As SyntaxNode, model As SemanticModel,
                                                    monitorType As INamedTypeSymbol, context As OperationAnalysisContext) As Boolean

            Dim candidates As New List(Of TryBlockSyntax)()
            Dim boundary As SyntaxNode = UtilAnalyzerSyntax.GetExecutableBoundary([call])

            ' Pattern 1: the call is in the Try section. Pattern 3: the call is in the condition of an If block.
            Dim previous As SyntaxNode = [call]
            Dim current As SyntaxNode = [call].Parent
            While current IsNot Nothing AndAlso current IsNot boundary
                Dim tryBlock As TryBlockSyntax = TryCast(current, TryBlockSyntax)
                If tryBlock IsNot Nothing AndAlso TypeOf previous IsNot CatchBlockSyntax AndAlso TypeOf previous IsNot FinallyBlockSyntax Then
                    candidates.Add(tryBlock)
                End If
                If TypeOf current Is MultiLineIfBlockSyntax AndAlso TypeOf previous Is IfStatementSyntax Then
                    candidates.AddRange(DirectCast(current, MultiLineIfBlockSyntax).Statements.OfType(Of TryBlockSyntax)())
                End If
                previous = current
                current = current.Parent
            End While

            ' Pattern 2: the statement of the call is immediately followed by a Try block.
            Dim statement As SyntaxNode = [call].Parent
            If (TypeOf statement Is ExpressionStatementSyntax OrElse TypeOf statement Is CallStatementSyntax) AndAlso statement.Parent IsNot Nothing Then
                Dim siblings As List(Of SyntaxNode) = statement.Parent.ChildNodes().ToList()
                Dim index As Integer = siblings.IndexOf(statement)
                If index >= 0 AndAlso index + 1 < siblings.Count AndAlso TypeOf siblings(index + 1) Is TryBlockSyntax Then
                    candidates.Add(DirectCast(siblings(index + 1), TryBlockSyntax))
                End If
            End If

            Return candidates.Exists(
                Function(t As TryBlockSyntax) t.FinallyBlock IsNot Nothing AndAlso
                                              t.FinallyBlock.DescendantNodes().OfType(Of InvocationExpressionSyntax)().Any(
                                                  Function(i As InvocationExpressionSyntax) MonitorEnterAnalyzer.IsExitOn(i, lockExpression, model, monitorType, context)))
        End Function

        ''' <summary>
        ''' Determines whether an invocation is <c>Monitor.Exit</c> on an expression equivalent to the lock expression.
        ''' </summary>
        ''' 
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        ''' 
        ''' <param name="lockExpression">
        ''' The lock expression.
        ''' </param>
        ''' 
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        ''' 
        ''' <param name="monitorType">
        ''' The <see cref="System.Threading.Monitor"/> type symbol.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context (for cancellation).
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the invocation releases the same lock; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsExitOn(invocation As InvocationExpressionSyntax, lockExpression As SyntaxNode, model As SemanticModel,
                                         monitorType As INamedTypeSymbol, context As OperationAnalysisContext) As Boolean

            If invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 Then
                Return False
            End If

            Dim method As IMethodSymbol = TryCast(model.GetSymbolInfo(invocation, context.CancellationToken).Symbol, IMethodSymbol)
            Return method IsNot Nothing AndAlso method.Name.Equals("Exit", StringComparison.Ordinal) AndAlso
                   SymbolEqualityComparer.Default.Equals(method.ContainingType, monitorType) AndAlso
                   SyntaxFactory.AreEquivalent(invocation.ArgumentList.Arguments(0).GetExpression(), lockExpression, topLevel:=False)
        End Function

#End Region

    End Class

End Namespace

#End Region
