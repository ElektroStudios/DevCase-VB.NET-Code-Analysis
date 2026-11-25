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

#Region " ManagedThreadIdAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Thread.CurrentThread.ManagedThreadId</c> (rule <see cref="DiagnosticIds.CurrentThreadManagedThreadId"/>;
    ''' equivalent of CA1840 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <c>Environment.CurrentManagedThreadId</c> returns the same value without getting the <see cref="Threading.Thread"/> object of
    ''' the current thread. The rule is only enabled when the property exists (.NET Framework 4.5 and later). The code fix writes
    ''' <c>Environment.CurrentManagedThreadId</c>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ManagedThreadIdAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.CurrentThreadManagedThreadId)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ManagedThreadIdAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ManagedThreadIdAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1840 reports the same code.
            End If

            Dim environmentType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Environment")
            If environmentType Is Nothing OrElse environmentType.GetMembers("CurrentManagedThreadId").IsEmpty Then
                Exit Sub
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext) ManagedThreadIdAnalyzer.AnalyzePropertyReference(c, environmentType)), OperationKind.PropertyReference)
        End Sub


        ''' <summary>
        ''' Reports <c>Thread.CurrentThread.ManagedThreadId</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="environmentType">
        ''' The <c>System.Environment</c> type.
        ''' </param>
        Private Shared Sub AnalyzePropertyReference(context As OperationAnalysisContext, environmentType As INamedTypeSymbol)

            Dim reference As IPropertyReferenceOperation = DirectCast(context.Operation, IPropertyReferenceOperation)
            If Not UtilAnalyzerSemantic.IsMember(reference.Property, "System.Threading.Thread", NameOf(Threading.Thread.ManagedThreadId)) Then
                Exit Sub
            End If

            Dim current As IPropertyReferenceOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(reference.Instance), IPropertyReferenceOperation)
            If current Is Nothing OrElse Not UtilAnalyzerSemantic.IsMember(current.Property, "System.Threading.Thread", NameOf(Threading.Thread.CurrentThread)) Then
                Exit Sub
            End If

            Dim model As SemanticModel = reference.SemanticModel
            Dim position As Integer = reference.Syntax.SpanStart
            Dim typeName As String = UtilSpeculativeBinding.GetTypeName(model, position, environmentType)
            Dim replacement As String = $"{typeName}.CurrentManagedThreadId"
            Dim actions As New List(Of FixReplacement)()
            If typeName IsNot Nothing AndAlso
               UtilAnalyzerSemantic.IsMember(UtilSpeculativeBinding.GetSymbol(model, position, replacement), "System.Environment", "CurrentManagedThreadId") Then
                actions.Add(New FixReplacement("Use Environment.CurrentManagedThreadId", "CurrentManagedThreadId", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.CurrentThreadManagedThreadId, reference.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions))
        End Sub

#End Region

    End Class

End Namespace

#End Region
