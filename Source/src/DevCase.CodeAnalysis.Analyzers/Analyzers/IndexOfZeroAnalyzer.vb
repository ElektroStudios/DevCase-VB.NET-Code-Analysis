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

#Region " IndexOfZeroAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>String.IndexOf(String)</c> compared with zero (rule <see cref="DiagnosticIds.IndexOfComparedWithZero"/>;
    ''' equivalent of CA1858 for .NET Framework projects), for example <c>If path.IndexOf(prefix, StringComparison.Ordinal) = 0 Then</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>IndexOf</c> searches the whole string when the value is not at the start; <c>StartsWith</c> only compares the start. Only
    ''' <c>= 0</c> and <c>&lt;&gt; 0</c> are reported (<c>IndexOf</c> returns -1 when the value is not found, so <c>&gt; 0</c> means
    ''' something else), with the <c>IndexOf(String)</c> and <c>IndexOf(String, StringComparison)</c> overloads; the <see cref="Char"/>
    ''' overload of <c>StartsWith</c> does not exist in .NET Framework.
    ''' </para>
    ''' <para>
    ''' The code fix writes <c>path.StartsWith(prefix, StringComparison.Ordinal)</c>, or <c>Not path.StartsWith(...)</c> for
    ''' <c>&lt;&gt; 0</c>, with the same arguments, so it compares the same way.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class IndexOfZeroAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.IndexOfComparedWithZero)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="IndexOfZeroAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf IndexOfZeroAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1858 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf IndexOfZeroAnalyzer.AnalyzeBinary), OperationKind.Binary)
        End Sub


        ''' <summary>
        ''' Reports the comparison of <c>String.IndexOf</c> with zero.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeBinary(context As OperationAnalysisContext)

            Dim binary As IBinaryOperation = DirectCast(context.Operation, IBinaryOperation)
            Dim operand As IOperation = Nothing
            Dim isZero As Boolean
            If Not UtilZeroComparisons.TryMatch(binary, equalityOnly:=True, operand:=operand, meansEmpty:=isZero) Then
                Exit Sub
            End If

            Dim invocation As IInvocationOperation = TryCast(operand, IInvocationOperation)
            If invocation Is Nothing OrElse Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.String", NameOf(String.IndexOf)) OrElse
               invocation.Instance Is Nothing OrElse invocation.Instance.IsImplicit OrElse
               Not IndexOfZeroAnalyzer.IsStringOverload(invocation.TargetMethod) Then
                Exit Sub
            End If

            Dim syntax As InvocationExpressionSyntax = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            If syntax Is Nothing OrElse syntax.ArgumentList Is Nothing Then
                Exit Sub
            End If

            Dim model As SemanticModel = binary.SemanticModel
            Dim position As Integer = binary.Syntax.SpanStart
            Dim startsWith As String = $"{UtilAnalyzerSyntax.GetReceiverText(invocation.Instance.Syntax)}.StartsWith{syntax.ArgumentList}"
            Dim speculative As IMethodSymbol = TryCast(UtilSpeculativeBinding.GetSymbol(model, position, startsWith), IMethodSymbol)
            Dim actions As New List(Of FixReplacement)()
            If UtilAnalyzerSemantic.IsMember(speculative, "System.String", NameOf(String.StartsWith)) AndAlso
               speculative.Parameters.Length = invocation.TargetMethod.Parameters.Length Then
                Dim replacement As String = If(isZero, startsWith, UtilAnalyzerSyntax.Negate(startsWith, binary.Syntax))
                actions.Add(New FixReplacement("Use StartsWith", "StartsWith", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.IndexOfComparedWithZero, binary.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions))
        End Sub

        ''' <summary>
        ''' Determines whether an <c>IndexOf</c> overload searches a <see cref="String"/>, with an optional <see cref="StringComparison"/>.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The <c>IndexOf</c> overload.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for <c>IndexOf(String)</c> and <c>IndexOf(String, StringComparison)</c>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsStringOverload(method As IMethodSymbol) As Boolean

            Dim parameters As ImmutableArray(Of IParameterSymbol) = method.Parameters
            Return parameters.Length > 0 AndAlso parameters(0).Type.SpecialType = SpecialType.System_String AndAlso
                   (parameters.Length = 1 OrElse
                    (parameters.Length = 2 AndAlso UtilAnalyzerSemantic.HasMetadataName(parameters(1).Type, "System.StringComparison")))
        End Function

#End Region

    End Class

End Namespace

#End Region
