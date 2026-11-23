#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ArrayLiteralAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports array creation expressions with an initializer (<c>New Integer() {1, 2}</c>) that can be written as an array literal 
    ''' (<c>{1, 2}</c>) without changing the type of the array (rule <see cref="DiagnosticIds.ArrayCreationInsteadOfLiteral"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The array literal takes its type from the target, so the expression is reported only where the target type is explicit and 
    ''' is exactly the created array type: the initializer of a variable, field or property declared with <c>As</c>, the right side of an 
    ''' assignment, or the <see langword="Return"/> statement of a method (not of a lambda, whose return type may be inferred). 
    ''' Arguments are not reported (an array literal may select another overload), and neither are expressions with bounds 
    ''' (<c>New Integer(9) {}</c>), multidimensional or jagged arrays, nor empty arrays (<c>New Integer() {}</c>), which should not be 
    ''' allocated at all (<c>Array.Empty(Of Integer)()</c>, rule <see cref="DiagnosticIds.ZeroLengthArrayAllocation"/>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ArrayLiteralAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ArrayCreationInsteadOfLiteral)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ArrayLiteralAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ArrayLiteralAnalyzer.AnalyzeArrayCreation), SyntaxKind.ArrayCreationExpression)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an array creation expression that can be an array literal.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeArrayCreation(context As SyntaxNodeAnalysisContext)

            Dim creation As ArrayCreationExpressionSyntax = DirectCast(context.Node, ArrayCreationExpressionSyntax)
            ' An empty array (New Integer() {}) is not reported: it must not be allocated at all (Array.Empty, rule DCA055 or CA1825).
            If creation.ArrayBounds IsNot Nothing OrElse creation.RankSpecifiers.Count <> 1 OrElse creation.RankSpecifiers(0).Rank <> 1 OrElse
               creation.AttributeLists.Count > 0 OrElse TypeOf creation.Type Is ArrayTypeSyntax OrElse
               creation.Initializer.Initializers.Count = 0 OrElse Not ArrayLiteralAnalyzer.IsTargetTyped(creation) Then
                Exit Sub
            End If

            Dim info As TypeInfo = context.SemanticModel.GetTypeInfo(creation, context.CancellationToken)
            If info.Type Is Nothing OrElse Not SymbolEqualityComparer.Default.Equals(info.Type, info.ConvertedType) Then
                Exit Sub
            End If

            Dim span As TextSpan = TextSpan.FromBounds(creation.NewKeyword.SpanStart, creation.RankSpecifiers(0).Span.End)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ArrayCreationInsteadOfLiteral,
                                                       Location.Create(creation.SyntaxTree, span),
                                                       $"{creation.Type}{creation.RankSpecifiers(0)}"))
        End Sub

        ''' <summary>
        ''' Determines whether an expression is in a position whose target type is explicit: the initializer of a declaration with an 
        ''' <c>As</c> clause, the right side of an assignment, or the <see langword="Return"/> statement of a method.
        ''' </summary>
        ''' 
        ''' <param name="expression">
        ''' The expression.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the expression is target-typed; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsTargetTyped(expression As ExpressionSyntax) As Boolean

            Dim parent As SyntaxNode = expression.Parent
            If TypeOf parent Is EqualsValueSyntax Then
                Dim declarator As VariableDeclaratorSyntax = TryCast(parent.Parent, VariableDeclaratorSyntax)
                Dim propertyStatement As PropertyStatementSyntax = TryCast(parent.Parent, PropertyStatementSyntax)
                Return (declarator IsNot Nothing AndAlso declarator.Names.Count = 1 AndAlso TypeOf declarator.AsClause Is SimpleAsClauseSyntax) OrElse
                       (propertyStatement IsNot Nothing AndAlso TypeOf propertyStatement.AsClause Is SimpleAsClauseSyntax)
            End If

            Dim isAssignedValue As Boolean = parent.IsKind(SyntaxKind.SimpleAssignmentStatement) AndAlso
                                             DirectCast(parent, AssignmentStatementSyntax).Right Is expression
            Dim isMethodReturn As Boolean = TypeOf parent Is ReturnStatementSyntax AndAlso
                                            TypeOf UtilAnalyzerSyntax.GetExecutableBoundary(parent) Is MethodBlockBaseSyntax
            Return isAssignedValue OrElse isMethodReturn
        End Function

#End Region

    End Class

End Namespace

#End Region
