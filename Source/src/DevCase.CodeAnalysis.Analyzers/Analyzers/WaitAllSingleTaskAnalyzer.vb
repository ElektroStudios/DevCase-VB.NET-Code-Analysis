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

#Region " WaitAllSingleTaskAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Task.WaitAll</c> called with a single task (rule <see cref="DiagnosticIds.WaitAllWithSingleTask"/>; equivalent of
    ''' CA1843 for .NET Framework projects), for example <c>Task.WaitAll(work)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Only the <c>WaitAll(ParamArray tasks As Task())</c> overload with exactly one element is reported (the overloads with a
    ''' timeout or a cancellation token take an explicit array). <c>WaitAll</c> allocates an array to wait for one task; the code fix
    ''' writes <c>work.Wait()</c>, which throws the same <see cref="AggregateException"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class WaitAllSingleTaskAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.WaitAllWithSingleTask)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="WaitAllSingleTaskAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf WaitAllSingleTaskAnalyzer.OnCompilationStart)
        End Sub

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
                Exit Sub ' Modern .NET: the built-in rule CA1843 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf WaitAllSingleTaskAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Task.WaitAll</c> with a single task.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim task As IOperation = WhenAllSingleTaskAnalyzer.GetSingleTask(invocation, NameOf(Threading.Tasks.Task.WaitAll))
            If task Is Nothing Then
                Exit Sub
            End If

            Dim replacement As String = $"{UtilAnalyzerSyntax.GetReceiverText(task.Syntax)}.Wait()"
            Dim speculative As ISymbol = UtilSpeculativeBinding.GetSymbol(invocation.SemanticModel, invocation.Syntax.SpanStart, replacement)
            Dim actions As New List(Of FixReplacement)()
            If UtilAnalyzerSemantic.IsMember(speculative, "System.Threading.Tasks.Task", NameOf(Threading.Tasks.Task.Wait)) AndAlso
               DirectCast(speculative, IMethodSymbol).Parameters.IsEmpty Then
                actions.Add(New FixReplacement("Call Wait on the task", "Wait", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.WaitAllWithSingleTask, invocation.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions))
        End Sub

#End Region

    End Class

End Namespace

#End Region
