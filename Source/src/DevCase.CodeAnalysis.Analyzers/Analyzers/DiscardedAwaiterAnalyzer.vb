#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations

#End Region

#Region " DiscardedAwaiterAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports statements that call <c>GetAwaiter()</c> or <c>ConfigureAwait(...)</c> on a task and discard the result
    ''' (rule <see cref="DiagnosticIds.DiscardedAwaiter"/>), for example <c>Task.Run(AddressOf Work).GetAwaiter()</c>
    ''' or <c>Me.WorkAsync().ConfigureAwait(False)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Neither call waits for the task: <c>GetAwaiter()</c> returns the object that <see langword="Await"/> uses, and
    ''' <c>ConfigureAwait</c> returns a configured version of the task for <see langword="Await"/>. As a statement, the call has no effect:
    ''' the method continues while the task runs, and an exception thrown by the task is never observed.
    ''' Analyzed types: <c>Task</c>, <c>Task(Of T)</c>, <c>ValueTask</c>, <c>ValueTask(Of T)</c> and their configured awaitables.
    ''' There is no code fix: awaiting the task, blocking on it, or explicitly running it in the background are different behaviors
    ''' that only the author can choose.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DiscardedAwaiterAnalyzer : Inherits DiagnosticAnalyzer

#Region " Fields "

        ''' <summary>
        ''' The metadata names of the analyzed types, without generic arity.
        ''' </summary>
        Private Shared ReadOnly AnalyzedTypes As ImmutableHashSet(Of String) = ImmutableHashSet.Create(StringComparer.Ordinal,
            "System.Threading.Tasks.Task", "System.Threading.Tasks.ValueTask",
            "System.Runtime.CompilerServices.ConfiguredTaskAwaitable", "System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable")

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DiscardedAwaiter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DiscardedAwaiterAnalyzer"/> class.
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
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf DiscardedAwaiterAnalyzer.AnalyzeExpressionStatement), OperationKind.ExpressionStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an expression statement that discards the result of <c>GetAwaiter</c> or <c>ConfigureAwait</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeExpressionStatement(context As OperationAnalysisContext)

            Dim statement As IExpressionStatementOperation = DirectCast(context.Operation, IExpressionStatementOperation)
            Dim invocation As IInvocationOperation = TryCast(statement.Operation, IInvocationOperation)
            If invocation Is Nothing OrElse invocation.TargetMethod.IsStatic Then
                Exit Sub
            End If

            Dim method As IMethodSymbol = invocation.TargetMethod
            Dim isAwaiterCall As Boolean = method.Name.Equals("GetAwaiter", StringComparison.Ordinal) OrElse
                                           method.Name.Equals("ConfigureAwait", StringComparison.Ordinal)
            Dim containingType As INamedTypeSymbol = method.ContainingType.OriginalDefinition
            Dim typeName As String = $"{containingType.ContainingNamespace.ToDisplayString()}.{containingType.MetadataName.Split("`"c)(0)}"
            If isAwaiterCall AndAlso DiscardedAwaiterAnalyzer.AnalyzedTypes.Contains(typeName) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DiscardedAwaiter, invocation.Syntax.GetLocation(), method.Name))
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
