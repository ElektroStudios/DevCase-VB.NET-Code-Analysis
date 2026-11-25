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

#Region " SubstringLengthAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>text.Substring(start, text.Length - start)</c>, whose length argument reads to the end of the string
    ''' (rule <see cref="DiagnosticIds.RedundantSubstringLength"/>; equivalent of CA1514 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' The receiver and the start index must be the same code in both places and must not have side effects (an invocation, a
    ''' creation...), because the code fix removes one evaluation of each: it writes <c>text.Substring(start)</c>. Named arguments are not
    ''' analyzed.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class SubstringLengthAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.RedundantSubstringLength)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SubstringLengthAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf SubstringLengthAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1514 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf SubstringLengthAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Substring(start, length)</c> whose length is the rest of the string.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim syntax As InvocationExpressionSyntax = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            If Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.String", NameOf(String.Substring)) OrElse
               invocation.TargetMethod.Parameters.Length <> 2 OrElse invocation.Instance Is Nothing OrElse invocation.Instance.IsImplicit OrElse
               syntax?.ArgumentList Is Nothing OrElse syntax.ArgumentList.Arguments.Count <> 2 OrElse
               syntax.ArgumentList.Arguments.Any(Function(a As ArgumentSyntax) TypeOf a IsNot SimpleArgumentSyntax OrElse a.IsNamed) Then
                Exit Sub
            End If

            Dim start As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(UtilAnalyzerSemantic.GetArgument(invocation.Arguments, 0)?.Value)
            Dim difference As IBinaryOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(UtilAnalyzerSemantic.GetArgument(invocation.Arguments, 1)?.Value), IBinaryOperation)
            If start Is Nothing OrElse difference Is Nothing OrElse difference.OperatorKind <> BinaryOperatorKind.Subtract Then
                Exit Sub
            End If

            Dim length As IPropertyReferenceOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(difference.LeftOperand), IPropertyReferenceOperation)
            If length Is Nothing OrElse length.Instance Is Nothing OrElse Not UtilAnalyzerSemantic.IsMember(length.Property, "System.String", NameOf(String.Length)) OrElse
               Not UtilAnalyzerSyntax.AreEquivalentExpressions(length.Instance.Syntax, invocation.Instance.Syntax) OrElse
               Not UtilAnalyzerSyntax.AreEquivalentExpressions(UtilAnalyzerSemantic.SkipImplicitConversions(difference.RightOperand).Syntax, start.Syntax) OrElse
               UtilAnalyzerSyntax.MayHaveSideEffects(invocation.Instance.Syntax) OrElse UtilAnalyzerSyntax.MayHaveSideEffects(start.Syntax) Then
                Exit Sub
            End If

            ' The fix deletes ", text.Length - start".
            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = syntax.ArgumentList.Arguments
            Dim removed As Location = Location.Create(syntax.SyntaxTree, Text.TextSpan.FromBounds(arguments(0).Span.End, arguments(1).Span.End))
            Dim actions As New List(Of FixReplacement)()
            If Not UtilAnalyzerSyntax.HasInnerComments(syntax.ArgumentList) Then
                actions.Add(New FixReplacement("Remove the redundant length", "RemoveLength", String.Empty))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.RedundantSubstringLength, arguments(1).GetLocation(),
                                                                        {removed}, actions))
        End Sub

#End Region

    End Class

End Namespace

#End Region
