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

#Region " ThreadSuspendResumeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to <c>Thread.Suspend</c> and <c>Thread.Resume</c> (rule <see cref="DiagnosticIds.ThreadSuspendResume"/>).
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThreadSuspendResumeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThreadSuspendResume)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThreadSuspendResumeAnalyzer"/> class.
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
                    Dim threadType As INamedTypeSymbol = c.Compilation.GetTypeByMetadataName("System.Threading.Thread")
                    If threadType IsNot Nothing Then
                        c.RegisterOperationAction(Sub(o As OperationAnalysisContext) ThreadSuspendResumeAnalyzer.AnalyzeInvocation(o, threadType),
                                                  OperationKind.Invocation)
                    End If
                End Sub)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an invocation of <c>Thread.Suspend</c> or <c>Thread.Resume</c>.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        ''' 
        ''' <param name="threadType">
        ''' The <see cref="System.Threading.Thread"/> type symbol.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext, threadType As INamedTypeSymbol)

            Dim method As IMethodSymbol = DirectCast(context.Operation, IInvocationOperation).TargetMethod
            If (method.Name.Equals("Suspend", StringComparison.Ordinal) OrElse method.Name.Equals("Resume", StringComparison.Ordinal)) AndAlso
               SymbolEqualityComparer.Default.Equals(method.ContainingType, threadType) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThreadSuspendResume, context.Operation.Syntax.GetLocation(), method.Name))
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
