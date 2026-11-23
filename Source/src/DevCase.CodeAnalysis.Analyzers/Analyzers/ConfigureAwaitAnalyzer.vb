#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ConfigureAwaitAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="Await"/> expressions whose awaited value exposes <c>ConfigureAwait(Boolean)</c> 
    ''' but is not configured (rule <see cref="DiagnosticIds.MissingConfigureAwait"/>).
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ConfigureAwaitAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingConfigureAwait)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ConfigureAwaitAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ConfigureAwaitAnalyzer.AnalyzeAwaitExpression), SyntaxKind.AwaitExpression)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes an <see langword="Await"/> expression.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeAwaitExpression(context As SyntaxNodeAnalysisContext)

            Dim awaitExpression As AwaitExpressionSyntax = DirectCast(context.Node, AwaitExpressionSyntax)
            Dim awaitedExpression As ExpressionSyntax = awaitExpression.Expression
            If awaitedExpression Is Nothing OrElse awaitedExpression.IsMissing Then
                Exit Sub ' Incomplete code being typed; nothing to analyze yet.
            End If

            ' Fast syntactic path (original IsAlreadyConfigured logic).
            If UtilAnalyzerSyntax.IsAlreadyConfigured(awaitedExpression) Then
                Exit Sub
            End If

            ' Semantic path: only task-like values that can actually be configured are reported.
            Dim awaitedType As ITypeSymbol = context.SemanticModel.GetTypeInfo(awaitedExpression, context.CancellationToken).Type
            If Not UtilAnalyzerSemantic.HasBooleanConfigureAwait(awaitedType) Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingConfigureAwait, awaitExpression.GetLocation()))
        End Sub

#End Region

    End Class

End Namespace

#End Region
