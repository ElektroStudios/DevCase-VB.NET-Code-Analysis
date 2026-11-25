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

#Region " ReferenceEqualsValueTypeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports value-type arguments of <c>Object.ReferenceEquals</c>
    ''' (rule <see cref="DiagnosticIds.ValueTypeInReferenceEquals"/>; equivalent of CA2013 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Each argument is reported on its own: <c>ReferenceEquals(left, right)</c> with two <see cref="Integer"/> values reports both.
    ''' A value-type argument is boxed into a new object for each call, so the comparison is always <see langword="False"/> (or
    ''' <see langword="True"/> for two <see langword="Nothing"/> nullable values). Value types include nullable value types and type
    ''' parameters with the <see langword="Structure"/> constraint. There is no code fix: the intended comparison (<c>=</c>,
    ''' <c>Equals</c>...) depends on the type.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ReferenceEqualsValueTypeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ValueTypeInReferenceEquals)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ReferenceEqualsValueTypeAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ReferenceEqualsValueTypeAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2013 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf ReferenceEqualsValueTypeAnalyzer.AnalyzeInvocation),
                                            OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports the value-type arguments of a call to <c>Object.ReferenceEquals</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            If Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.Object", NameOf(Object.ReferenceEquals)) Then
                Exit Sub
            End If

            For Each argument As IArgumentOperation In invocation.Arguments
                Dim conversion As IConversionOperation = TryCast(argument.Value, IConversionOperation)
                If conversion Is Nothing OrElse conversion.Operand.Type Is Nothing OrElse Not conversion.Operand.Type.IsValueType Then
                    Continue For
                End If

                Dim typeName As String = conversion.Operand.Type.ToMinimalDisplayString(invocation.SemanticModel, argument.Syntax.SpanStart)
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ValueTypeInReferenceEquals, argument.Syntax.GetLocation(), typeName))
            Next argument
        End Sub

#End Region

    End Class

End Namespace

#End Region
