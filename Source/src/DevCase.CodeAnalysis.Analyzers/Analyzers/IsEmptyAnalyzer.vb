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

#Region " IsEmptyAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports the <c>Count</c> property compared with zero or one on a type that also has an <c>IsEmpty</c> property
    ''' (rule <see cref="DiagnosticIds.CountInsteadOfIsEmpty"/>; equivalent of CA1836 for .NET Framework projects), for example
    ''' <c>queue.Count &gt; 0</c> on a <c>ConcurrentQueue(Of T)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' In the concurrent collections, <c>Count</c> takes a snapshot of the whole collection (<c>ConcurrentDictionary</c> acquires every
    ''' lock) while <c>IsEmpty</c> only checks whether there is an element. The recognized comparisons are <c>= 0</c>, <c>&lt;&gt; 0</c>,
    ''' <c>&gt; 0</c>, <c>&gt;= 1</c>, <c>&lt; 1</c> and <c>&lt;= 0</c>, with the constant on either side.
    ''' </para>
    ''' <para>
    ''' The type must have an accessible, non-indexed <see cref="Boolean"/> <c>IsEmpty</c> instance property. The code fix writes
    ''' <c>queue.IsEmpty</c> or <c>Not queue.IsEmpty</c>.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class IsEmptyAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.CountInsteadOfIsEmpty)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="IsEmptyAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf IsEmptyAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1836 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf IsEmptyAnalyzer.AnalyzeBinary), OperationKind.Binary)
        End Sub


        ''' <summary>
        ''' Reports the comparison of a <c>Count</c> property with zero or one on a type with <c>IsEmpty</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeBinary(context As OperationAnalysisContext)

            Dim binary As IBinaryOperation = DirectCast(context.Operation, IBinaryOperation)
            Dim operand As IOperation = Nothing
            Dim meansEmpty As Boolean
            If Not UtilZeroComparisons.TryMatch(binary, equalityOnly:=False, operand:=operand, meansEmpty:=meansEmpty) Then
                Exit Sub
            End If

            Dim count As IPropertyReferenceOperation = TryCast(operand, IPropertyReferenceOperation)
            If count Is Nothing OrElse count.Instance Is Nothing OrElse Not count.Property.Name.Equals("Count", StringComparison.Ordinal) OrElse
               Not count.Property.Parameters.IsEmpty OrElse TypeOf count.Instance Is IConditionalAccessInstanceOperation Then
                Exit Sub
            End If

            Dim model As SemanticModel = binary.SemanticModel
            Dim position As Integer = binary.Syntax.SpanStart
            Dim instanceType As ITypeSymbol = count.Instance.Type
            Dim isEmpty As IPropertySymbol = UtilAnalyzerSemantic.FindInstanceProperty(model, position, instanceType, "IsEmpty")
            If isEmpty Is Nothing OrElse isEmpty.Type.SpecialType <> SpecialType.System_Boolean Then
                Exit Sub
            End If

            Dim access As String = If(count.Instance.IsImplicit, "IsEmpty", $"{UtilAnalyzerSyntax.GetReceiverText(count.Instance.Syntax)}.IsEmpty")
            Dim speculative As IPropertySymbol = TryCast(UtilSpeculativeBinding.GetSymbol(model, position, access), IPropertySymbol)
            Dim actions As New List(Of FixReplacement)()
            If speculative IsNot Nothing AndAlso speculative.Name.Equals("IsEmpty", StringComparison.Ordinal) Then
                Dim replacement As String = If(meansEmpty, access, UtilAnalyzerSyntax.Negate(access, binary.Syntax))
                actions.Add(New FixReplacement("Use IsEmpty", "IsEmpty", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.CountInsteadOfIsEmpty, binary.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions,
                                                                        instanceType.ToMinimalDisplayString(model, position)))
        End Sub

#End Region

    End Class

End Namespace

#End Region
