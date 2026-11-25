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

#End Region

#Region " WhenAllSingleTaskAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Task.WhenAll</c> called with a single task (rule <see cref="DiagnosticIds.WhenAllWithSingleTask"/>; equivalent of
    ''' CA1842 for .NET Framework projects), for example <c>Await Task.WhenAll(work)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Only the non-generic <c>WhenAll(ParamArray tasks As Task())</c> overload with exactly one element is reported: the generic
    ''' overload returns an array of results, which the task itself does not. <c>WhenAll</c> allocates an array and a combined task to wait
    ''' for one task; the code fix replaces the call with the task (<c>Await work</c>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class WhenAllSingleTaskAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.WhenAllWithSingleTask)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="WhenAllSingleTaskAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf WhenAllSingleTaskAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the only task passed to <c>Task.WhenAll</c> or <c>Task.WaitAll</c> through their <see langword="ParamArray"/> overload.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <param name="methodName">
        ''' The name of the method: <c>WhenAll</c> or <c>WaitAll</c>.
        ''' </param>
        '''
        ''' <returns>
        ''' The task, without its implicit conversion to <see cref="Threading.Tasks.Task"/>; <see langword="Nothing"/> if the invocation 
        ''' is not a call to the non-generic <see langword="ParamArray"/> overload with exactly one task.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="invocation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetSingleTask(invocation As IInvocationOperation, methodName As String) As IOperation

            If invocation Is Nothing Then
                Throw New ArgumentNullException(NameOf(invocation))
            End If

            Dim method As IMethodSymbol = invocation.TargetMethod
            If Not UtilAnalyzerSemantic.IsMember(method, "System.Threading.Tasks.Task", methodName) OrElse method.IsGenericMethod OrElse
               method.Parameters.Length <> 1 OrElse Not method.Parameters(0).IsParams OrElse invocation.Arguments.Length <> 1 OrElse
               invocation.Arguments(0).ArgumentKind <> ArgumentKind.ParamArray Then
                Return Nothing
            End If

            Dim creation As IArrayCreationOperation = TryCast(invocation.Arguments(0).Value, IArrayCreationOperation)
            If creation Is Nothing OrElse creation.Initializer Is Nothing OrElse creation.Initializer.ElementValues.Length <> 1 Then
                Return Nothing
            End If

            Dim task As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(creation.Initializer.ElementValues(0))
            Return If(task Is Nothing OrElse task.Type Is Nothing OrElse task.Type.TypeKind = TypeKind.Error, Nothing, task)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis for .NET Framework compilations.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilAnalyzerSemantic.IsNetFramework(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in rule CA1842 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf WhenAllSingleTaskAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Task.WhenAll</c> with a single task.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim task As IOperation = WhenAllSingleTaskAnalyzer.GetSingleTask(invocation, NameOf(Threading.Tasks.Task.WhenAll))
            If task Is Nothing Then
                Exit Sub
            End If

            Dim model As SemanticModel = invocation.SemanticModel
            Dim replacement As String = UtilAnalyzerSyntax.GetReceiverText(task.Syntax)
            Dim replacementType As ITypeSymbol = UtilSpeculativeBinding.GetExpressionType(model, invocation.Syntax.SpanStart, replacement)
            Dim actions As New List(Of FixReplacement)()
            If UtilAnalyzerSemantic.IsOrDerivesFrom(replacementType, "System.Threading.Tasks.Task") Then
                actions.Add(New FixReplacement("Use the task directly", "Task", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.WhenAllWithSingleTask, invocation.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions))
        End Sub

#End Region

    End Class

End Namespace

#End Region
