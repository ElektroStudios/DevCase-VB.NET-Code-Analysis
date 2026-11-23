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

#Region " PlusConcatenationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports the <c>+</c> and <c>+=</c> operators used to concatenate strings (rule <see cref="DiagnosticIds.StringConcatenationWithPlus"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' An operator is reported only when both operands are typed as <see cref="String"/> or <see cref="Char"/> (at least one of them a 
    ''' <see cref="String"/>) and the result is a <see cref="String"/>: then <c>+</c> is a concatenation, and <c>&amp;</c> produces exactly 
    ''' the same value. Operands typed as <see cref="Object"/> (late binding with <c>Option Strict Off</c>) are not reported, 
    ''' because <c>+</c> may add numbers at run time.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class PlusConcatenationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.StringConcatenationWithPlus)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="PlusConcatenationAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf PlusConcatenationAnalyzer.AnalyzeAddExpression), SyntaxKind.AddExpression)
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf PlusConcatenationAnalyzer.AnalyzeAddAssignment), SyntaxKind.AddAssignmentStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a <c>+</c> expression that concatenates strings.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeAddExpression(context As SyntaxNodeAnalysisContext)

            Dim expression As BinaryExpressionSyntax = DirectCast(context.Node, BinaryExpressionSyntax)
            Dim result As ITypeSymbol = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type
            If result IsNot Nothing AndAlso result.SpecialType = SpecialType.System_String AndAlso
               PlusConcatenationAnalyzer.AreTextOperands(context.SemanticModel, expression.Left, expression.Right, context) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.StringConcatenationWithPlus, expression.OperatorToken.GetLocation(), "+", "&"))
            End If
        End Sub

        ''' <summary>
        ''' Reports a <c>+=</c> statement that concatenates strings.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeAddAssignment(context As SyntaxNodeAnalysisContext)

            Dim statement As AssignmentStatementSyntax = DirectCast(context.Node, AssignmentStatementSyntax)
            Dim target As ITypeSymbol = context.SemanticModel.GetTypeInfo(statement.Left, context.CancellationToken).Type
            If target IsNot Nothing AndAlso target.SpecialType = SpecialType.System_String AndAlso
               PlusConcatenationAnalyzer.AreTextOperands(context.SemanticModel, statement.Left, statement.Right, context) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.StringConcatenationWithPlus, statement.OperatorToken.GetLocation(), "+=", "&="))
            End If
        End Sub

        ''' <summary>
        ''' Determines whether both operands are typed as <see cref="String"/> or <see cref="Char"/>, and at least one of them as <see cref="String"/>.
        ''' </summary>
        ''' 
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        ''' 
        ''' <param name="left">
        ''' The left operand.
        ''' </param>
        ''' 
        ''' <param name="right">
        ''' The right operand.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context (for cancellation).
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the operator is a text concatenation; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function AreTextOperands(model As SemanticModel, left As ExpressionSyntax, right As ExpressionSyntax,
                                                context As SyntaxNodeAnalysisContext) As Boolean

            Dim leftType As ITypeSymbol = model.GetTypeInfo(left, context.CancellationToken).Type
            Dim rightType As ITypeSymbol = model.GetTypeInfo(right, context.CancellationToken).Type
            If leftType Is Nothing OrElse rightType Is Nothing Then
                Return False
            End If

            Dim leftIsText As Boolean = leftType.SpecialType = SpecialType.System_String OrElse leftType.SpecialType = SpecialType.System_Char
            Dim rightIsText As Boolean = rightType.SpecialType = SpecialType.System_String OrElse rightType.SpecialType = SpecialType.System_Char
            Return leftIsText AndAlso rightIsText AndAlso
                   (leftType.SpecialType = SpecialType.System_String OrElse rightType.SpecialType = SpecialType.System_String)
        End Function

#End Region

    End Class

End Namespace

#End Region
