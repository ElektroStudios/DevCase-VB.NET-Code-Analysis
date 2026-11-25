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

#Region " HasFlagEnumTypeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Enum.HasFlag</c> calls whose argument is a value of another enum type than the instance
    ''' (rule <see cref="DiagnosticIds.HasFlagWithDifferentEnum"/>; equivalent of CA2248 for .NET Framework projects), for example
    ''' <c>style.HasFlag(ConsoleModifiers.Alt)</c> with a <c>FileAttributes</c> value.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <see cref="[Enum].HasFlag(System.Enum)"/> throws <see cref="ArgumentException"/> when the type of the argument is not the type
    ''' of the instance. Only arguments whose type is known at compile time are reported (an argument typed as <see cref="[Enum]"/> is not).
    ''' There is no code fix: the right flag depends on what the code meant.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class HasFlagEnumTypeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.HasFlagWithDifferentEnum)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="HasFlagEnumTypeAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf HasFlagEnumTypeAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2248 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf HasFlagEnumTypeAnalyzer.AnalyzeInvocation),
                                            OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Enum.HasFlag</c> with an argument of another enum type.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            If Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.Enum", NameOf([Enum].HasFlag)) OrElse
               invocation.Instance Is Nothing OrElse invocation.Arguments.Length <> 1 Then
                Exit Sub
            End If

            Dim instanceType As ITypeSymbol = UtilAnalyzerSemantic.SkipImplicitConversions(invocation.Instance).Type
            Dim argument As IArgumentOperation = invocation.Arguments(0)
            Dim argumentType As ITypeSymbol = UtilAnalyzerSemantic.SkipImplicitConversions(argument.Value).Type
            If instanceType Is Nothing OrElse argumentType Is Nothing OrElse instanceType.TypeKind <> TypeKind.Enum OrElse
               argumentType.TypeKind <> TypeKind.Enum OrElse SymbolEqualityComparer.Default.Equals(instanceType, argumentType) Then
                Exit Sub
            End If

            Dim position As Integer = argument.Syntax.SpanStart
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.HasFlagWithDifferentEnum, argument.Syntax.GetLocation(),
                                                       instanceType.ToMinimalDisplayString(invocation.SemanticModel, position),
                                                       argumentType.ToMinimalDisplayString(invocation.SemanticModel, position)))
        End Sub

#End Region

    End Class

End Namespace

#End Region
