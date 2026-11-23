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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " InvertedComparisonAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports negated comparisons such as <c>Not (a = b)</c> that can be written with the inverse operator (<c>a &lt;&gt; b</c>) 
    ''' (rule <see cref="DiagnosticIds.InvertedComparison"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Not reported:
    ''' <list type="bullet">
    '''   <item><description><c>Not (a &lt; b)</c> and the other relational operators when an operand is <see cref="Single"/> or <see cref="Double"/>: 
    '''   with <c>NaN</c>, <c>Not (NaN &lt; 1)</c> is <see langword="True"/> but <c>NaN &gt;= 1</c> is <see langword="False"/>.</description></item>
    '''   <item><description>User-defined operators, which may not be consistent with each other.</description></item>
    '''   <item><description><c>Not x Is y</c> and <c>Not TypeOf x Is T</c>, already covered by the built-in IDE0084 rule (<c>IsNot</c>).</description></item>
    '''   <item><description>Late-bound (<see cref="Object"/>) comparisons.</description></item>
    ''' </list>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class InvertedComparisonAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.InvertedComparison)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="InvertedComparisonAnalyzer"/> class.
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
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf InvertedComparisonAnalyzer.AnalyzeUnary), OperationKind.UnaryOperator)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the inverse of a comparison expression kind.
        ''' </summary>
        ''' 
        ''' <param name="kind">
        ''' The comparison expression kind.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The inverse kind, or <see cref="SyntaxKind.None"/> if <paramref name="kind"/> is not a supported comparison.
        ''' </returns>
        Friend Shared Function GetInverseKind(kind As SyntaxKind) As SyntaxKind

            Select Case kind
                Case SyntaxKind.EqualsExpression
                    Return SyntaxKind.NotEqualsExpression
                Case SyntaxKind.NotEqualsExpression
                    Return SyntaxKind.EqualsExpression
                Case SyntaxKind.LessThanExpression
                    Return SyntaxKind.GreaterThanOrEqualExpression
                Case SyntaxKind.GreaterThanOrEqualExpression
                    Return SyntaxKind.LessThanExpression
                Case SyntaxKind.GreaterThanExpression
                    Return SyntaxKind.LessThanOrEqualExpression
                Case SyntaxKind.LessThanOrEqualExpression
                    Return SyntaxKind.GreaterThanExpression
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

        ''' <summary>
        ''' Gets the operator token kind of a comparison expression kind.
        ''' </summary>
        ''' 
        ''' <param name="kind">
        ''' The comparison expression kind.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The operator token kind, or <see cref="SyntaxKind.None"/> if <paramref name="kind"/> is not a supported comparison.
        ''' </returns>
        Friend Shared Function GetOperatorTokenKind(kind As SyntaxKind) As SyntaxKind

            Select Case kind
                Case SyntaxKind.EqualsExpression
                    Return SyntaxKind.EqualsToken
                Case SyntaxKind.NotEqualsExpression
                    Return SyntaxKind.LessThanGreaterThanToken
                Case SyntaxKind.LessThanExpression
                    Return SyntaxKind.LessThanToken
                Case SyntaxKind.GreaterThanOrEqualExpression
                    Return SyntaxKind.GreaterThanEqualsToken
                Case SyntaxKind.GreaterThanExpression
                    Return SyntaxKind.GreaterThanToken
                Case SyntaxKind.LessThanOrEqualExpression
                    Return SyntaxKind.LessThanEqualsToken
                Case Else
                    Return SyntaxKind.None
            End Select
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a unary operation.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeUnary(context As OperationAnalysisContext)

            Dim unary As IUnaryOperation = DirectCast(context.Operation, IUnaryOperation)
            Dim notSyntax As UnaryExpressionSyntax = TryCast(unary.Syntax, UnaryExpressionSyntax)
            If unary.OperatorKind <> UnaryOperatorKind.Not OrElse unary.OperatorMethod IsNot Nothing OrElse
               notSyntax Is Nothing OrElse Not notSyntax.IsKind(SyntaxKind.NotExpression) Then
                Exit Sub
            End If

            Dim comparisonSyntax As BinaryExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(notSyntax.Operand), BinaryExpressionSyntax)
            Dim operand As IOperation = unary.Operand
            While TypeOf operand Is IParenthesizedOperation
                operand = DirectCast(operand, IParenthesizedOperation).Operand
            End While
            Dim comparison As IBinaryOperation = TryCast(operand, IBinaryOperation)
            If comparisonSyntax Is Nothing OrElse comparison Is Nothing OrElse comparison.OperatorMethod IsNot Nothing OrElse
               InvertedComparisonAnalyzer.GetInverseKind(comparisonSyntax.Kind()) = SyntaxKind.None Then
                Exit Sub
            End If

            Dim isEquality As Boolean = comparisonSyntax.IsKind(SyntaxKind.EqualsExpression) OrElse comparisonSyntax.IsKind(SyntaxKind.NotEqualsExpression)
            For Each side As IOperation In {comparison.LeftOperand, comparison.RightOperand}
                Dim type As ITypeSymbol = InvertedComparisonAnalyzer.GetUnderlyingType(side)
                If type Is Nothing OrElse type.SpecialType = SpecialType.System_Object Then
                    Exit Sub
                End If
                If Not isEquality AndAlso (type.SpecialType = SpecialType.System_Single OrElse type.SpecialType = SpecialType.System_Double) Then
                    Exit Sub ' NaN makes "Not (a < b)" and "a >= b" differ.
                End If
            Next side

            Dim inverseToken As String = SyntaxFactory.Token(InvertedComparisonAnalyzer.GetOperatorTokenKind(InvertedComparisonAnalyzer.GetInverseKind(comparisonSyntax.Kind()))).Text
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.InvertedComparison, notSyntax.GetLocation(), comparisonSyntax.OperatorToken.Text, inverseToken))
        End Sub

        ''' <summary>
        ''' Gets the type of an operand, unwrapping implicit conversions and <c>Nullable(Of T)</c>.
        ''' </summary>
        ''' 
        ''' <param name="operand">
        ''' The operand.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The underlying type, or <see langword="Nothing"/> if it is unknown.
        ''' </returns>
        Private Shared Function GetUnderlyingType(operand As IOperation) As ITypeSymbol

            Dim current As IOperation = operand
            While TypeOf current Is IConversionOperation AndAlso current.IsImplicit
                current = DirectCast(current, IConversionOperation).Operand
            End While

            Dim type As ITypeSymbol = If(current.Type, operand.Type)
            Dim named As INamedTypeSymbol = TryCast(type, INamedTypeSymbol)
            Dim isNullable As Boolean = named IsNot Nothing AndAlso named.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T
            Return If(isNullable, named.TypeArguments(0), type)
        End Function

#End Region

    End Class

End Namespace

#End Region
