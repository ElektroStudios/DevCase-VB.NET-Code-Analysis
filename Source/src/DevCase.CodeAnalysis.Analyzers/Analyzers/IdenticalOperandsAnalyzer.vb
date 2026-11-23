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

#Region " IdenticalOperandsAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports binary operators whose two operands are the same expression (rule <see cref="DiagnosticIds.IdenticalOperands"/>): 
    ''' comparisons (<c>=</c>, <c>&lt;&gt;</c>, <c>&lt;</c>, <c>&lt;=</c>, <c>&gt;</c>, <c>&gt;=</c>, <see langword="Is"/>, <see langword="IsNot"/>), 
    ''' logical operators (<see langword="And"/>, <see langword="Or"/>, <see langword="Xor"/>, <see langword="AndAlso"/>, <see langword="OrElse"/>) 
    ''' and <c>-</c>, <c>/</c>, <c>\</c>, <see langword="Mod"/>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Not reported: operands that contain invocations, object creations, <see langword="Await"/> expressions or lambdas 
    ''' (<c>random.Next() = random.Next()</c> may be intentional), two literals (<c>While 1 = 1</c>), and <c>x = x</c> / <c>x &lt;&gt; x</c> 
    ''' on <see cref="Double"/> or <see cref="Single"/> values, which is the classic test for <c>NaN</c>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class IdenticalOperandsAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.IdenticalOperands)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="IdenticalOperandsAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf IdenticalOperandsAnalyzer.AnalyzeBinary),
                                             SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression, SyntaxKind.LessThanExpression,
                                             SyntaxKind.LessThanOrEqualExpression, SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanOrEqualExpression,
                                             SyntaxKind.IsExpression, SyntaxKind.IsNotExpression, SyntaxKind.AndExpression, SyntaxKind.OrExpression,
                                             SyntaxKind.ExclusiveOrExpression, SyntaxKind.AndAlsoExpression, SyntaxKind.OrElseExpression,
                                             SyntaxKind.SubtractExpression, SyntaxKind.DivideExpression, SyntaxKind.IntegerDivideExpression,
                                             SyntaxKind.ModuloExpression)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a binary expression whose operands are identical.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeBinary(context As SyntaxNodeAnalysisContext)

            Dim expression As BinaryExpressionSyntax = DirectCast(context.Node, BinaryExpressionSyntax)
            If Not SyntaxFactory.AreEquivalent(expression.Left, expression.Right, topLevel:=False) OrElse
               UtilAnalyzerSyntax.MayHaveSideEffects(expression.Left) OrElse
               (TypeOf expression.Left Is LiteralExpressionSyntax AndAlso TypeOf expression.Right Is LiteralExpressionSyntax) Then
                Exit Sub
            End If

            If expression.IsKind(SyntaxKind.EqualsExpression) OrElse expression.IsKind(SyntaxKind.NotEqualsExpression) Then
                Dim type As ITypeSymbol = context.SemanticModel.GetTypeInfo(expression.Left, context.CancellationToken).Type
                If type IsNot Nothing AndAlso (type.SpecialType = SpecialType.System_Double OrElse type.SpecialType = SpecialType.System_Single) Then
                    Exit Sub ' x <> x is the test for NaN.
                End If
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.IdenticalOperands, expression.GetLocation(), expression.OperatorToken.Text))
        End Sub

#End Region

    End Class

End Namespace

#End Region
