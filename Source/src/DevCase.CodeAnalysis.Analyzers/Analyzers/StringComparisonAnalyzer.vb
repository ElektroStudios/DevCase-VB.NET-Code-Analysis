#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Globalization

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " StringComparisonAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports invocations that omit a string comparison argument although the invoked method has an optional 
    ''' comparison parameter, or an overload that accepts one (rule <see cref="DiagnosticIds.MissingStringComparison"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Recognized comparison parameter types: <see cref="StringComparison"/>, <see cref="StringComparer"/>, 
    ''' <c>IComparer(Of String)</c> and <c>IEqualityComparer(Of String)</c>. 
    ''' Overloads are matched through the semantic model, so the rule adapts to the target framework 
    ''' (e.g. <c>String.Contains(String)</c> is only reported where <c>Contains(String, StringComparison)</c> exists).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class StringComparisonAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingStringComparison)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StringComparisonAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf StringComparisonAnalyzer.AnalyzeInvocation, SyntaxKind.InvocationExpression)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes an invocation expression.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As SyntaxNodeAnalysisContext)

            Dim invocation As InvocationExpressionSyntax = DirectCast(context.Node, InvocationExpressionSyntax)
            If UtilAnalyzerSyntax.IsInsideNameOf(invocation) Then
                Exit Sub
            End If

            Dim method As IMethodSymbol = Nothing
            Dim opportunity As StringComparisonOpportunity = UtilStringComparison.FindOpportunity(invocation, context.SemanticModel, context.CancellationToken, method)
            If opportunity Is Nothing Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(UtilStringComparison.ParameterNamePropertyKey, opportunity.ParameterName).
                Add(UtilStringComparison.ArgumentPositionPropertyKey, opportunity.ArgumentPosition.ToString(CultureInfo.InvariantCulture)).
                Add(UtilStringComparison.KindPropertyKey, opportunity.Kind.ToString()).
                Add(UtilStringComparison.OmittedOptionalPropertyKey, opportunity.IsOmittedOptionalParameter.ToString())

            Dim methodDisplay As String = $"{method.ContainingType.Name}.{method.Name}"
            Dim kindDisplay As String = If(opportunity.Kind = StringComparisonArgumentKind.Comparison, NameOf(StringComparison), NameOf(StringComparer))
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingStringComparison, invocation.GetLocation(), properties, methodDisplay, kindDisplay))
        End Sub

#End Region

    End Class

End Namespace

#End Region
