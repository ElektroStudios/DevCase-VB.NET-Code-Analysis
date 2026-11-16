#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " RedundantNullCheckAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports null checks that are redundant because the next operand is a <see langword="TypeOf"/> expression on the same value 
    ''' (rule <see cref="DiagnosticIds.RedundantNullCheckBeforeTypeOf"/>): 
    ''' <c>x IsNot Nothing AndAlso TypeOf x Is T</c> and <c>x Is Nothing OrElse TypeOf x IsNot T</c>, including the equivalent forms 
    ''' <c>Not x Is Nothing</c>, <c>Nothing IsNot x</c> and <c>Not IsNothing(x)</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>TypeOf x Is T</c> is <see langword="False"/> when <c>x</c> is <see langword="Nothing"/>, and <c>TypeOf x IsNot T</c> is <see langword="True"/>. 
    ''' Only simple names and member access chains are analyzed (e.g. <c>value</c>, <c>Me.sender</c>), so removing the check never removes a side effect.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class RedundantNullCheckAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.RedundantNullCheckBeforeTypeOf)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RedundantNullCheckAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf RedundantNullCheckAnalyzer.AnalyzeLogical, SyntaxKind.AndAlsoExpression, SyntaxKind.OrElseExpression)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the operand that immediately precedes the right operand of a logical expression 
        ''' (the right operand of a left-nested expression of the same kind, or else the left operand).
        ''' </summary>
        ''' 
        ''' <param name="logical">
        ''' The <see langword="AndAlso"/> or <see langword="OrElse"/> expression.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The adjacent operand.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="logical"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetAdjacentOperand(logical As BinaryExpressionSyntax) As ExpressionSyntax

            If logical Is Nothing Then
                Throw New ArgumentNullException(NameOf(logical))
            End If
            Return If(logical.Left.IsKind(logical.Kind()), DirectCast(logical.Left, BinaryExpressionSyntax).Right, logical.Left)
        End Function

        ''' <summary>
        ''' Gets the value tested by a null check, in any of its Visual Basic forms: <c>x Is Nothing</c>, <c>x IsNot Nothing</c>, 
        ''' <c>Nothing Is x</c>, <c>IsNothing(x)</c>, and any of them negated with <see langword="Not"/> (e.g. <c>Not x Is Nothing</c>).
        ''' </summary>
        ''' 
        ''' <param name="expression">
        ''' The candidate null check.
        ''' </param>
        ''' 
        ''' <param name="semanticModel">
        ''' The semantic model, used to recognize <c>Microsoft.VisualBasic.Information.IsNothing</c>.
        ''' </param>
        ''' 
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        ''' 
        ''' <param name="checksIsNot">
        ''' When this method returns, <see langword="True"/> if the expression is <see langword="True"/> when the value is not 
        ''' <see langword="Nothing"/>; <see langword="False"/> if it is <see langword="True"/> when the value is <see langword="Nothing"/>.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The tested value, or <see langword="Nothing"/> if the expression is not a null check.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="expression"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetNullCheckTarget(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken,
                                                  ByRef checksIsNot As Boolean) As ExpressionSyntax

            If expression Is Nothing Then
                Throw New ArgumentNullException(NameOf(expression))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            checksIsNot = False

            ' Each "Not" inverts the meaning of the check.
            Dim negated As Boolean = False
            Dim current As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(expression)
            While current.IsKind(SyntaxKind.NotExpression)
                negated = Not negated
                current = UtilAnalyzerSyntax.SkipParentheses(DirectCast(current, UnaryExpressionSyntax).Operand)
            End While

            Dim target As ExpressionSyntax = Nothing
            Dim isNotOperator As Boolean = False
            If current.IsKind(SyntaxKind.IsExpression) OrElse current.IsKind(SyntaxKind.IsNotExpression) Then
                Dim binary As BinaryExpressionSyntax = DirectCast(current, BinaryExpressionSyntax)
                Dim left As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Left)
                Dim right As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Right)
                If right.IsKind(SyntaxKind.NothingLiteralExpression) Then
                    target = left
                ElseIf left.IsKind(SyntaxKind.NothingLiteralExpression) Then
                    target = right
                End If
                isNotOperator = current.IsKind(SyntaxKind.IsNotExpression)

            ElseIf current.IsKind(SyntaxKind.InvocationExpression) Then
                ' IsNothing(x) from Microsoft.VisualBasic.Information.
                Dim invocation As InvocationExpressionSyntax = DirectCast(current, InvocationExpressionSyntax)
                Dim method As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
                Dim isIsNothingCall As Boolean =
                    method IsNot Nothing AndAlso
                    method.Name.Equals("IsNothing", StringComparison.Ordinal) AndAlso
                    method.ContainingType.ToDisplayString().Equals("Microsoft.VisualBasic.Information", StringComparison.Ordinal) AndAlso
                    invocation.ArgumentList IsNot Nothing AndAlso invocation.ArgumentList.Arguments.Count = 1 AndAlso
                    Not invocation.ArgumentList.Arguments(0).IsNamed
                If isIsNothingCall Then
                    target = UtilAnalyzerSyntax.SkipParentheses(invocation.ArgumentList.Arguments(0).GetExpression())
                End If
            End If

            If target Is Nothing Then
                Return Nothing
            End If
            checksIsNot = isNotOperator Xor negated
            Return target
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes an <see langword="AndAlso"/> or <see langword="OrElse"/> expression.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeLogical(context As SyntaxNodeAnalysisContext)

            Dim logical As BinaryExpressionSyntax = DirectCast(context.Node, BinaryExpressionSyntax)
            Dim isAndAlso As Boolean = logical.IsKind(SyntaxKind.AndAlsoExpression)

            Dim typeOfExpression As TypeOfExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(logical.Right), TypeOfExpressionSyntax)
            If typeOfExpression Is Nothing OrElse
               Not typeOfExpression.IsKind(If(isAndAlso, SyntaxKind.TypeOfIsExpression, SyntaxKind.TypeOfIsNotExpression)) OrElse
               Not UtilAnalyzerSyntax.IsSimpleReference(typeOfExpression.Expression) Then
                Exit Sub
            End If

            ' AndAlso needs a check that the value IsNot Nothing; OrElse needs a check that it Is Nothing.
            Dim adjacent As ExpressionSyntax = RedundantNullCheckAnalyzer.GetAdjacentOperand(logical)
            Dim checksIsNot As Boolean
            Dim checkedValue As ExpressionSyntax = RedundantNullCheckAnalyzer.GetNullCheckTarget(adjacent, context.SemanticModel, context.CancellationToken, checksIsNot)
            Dim target As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(typeOfExpression.Expression)
            If checkedValue Is Nothing OrElse checksIsNot <> isAndAlso OrElse Not SyntaxFactory.AreEquivalent(checkedValue, target) Then
                Exit Sub
            End If

            Dim typeOfText As String = $"TypeOf {target} {If(isAndAlso, "Is", "IsNot")} {typeOfExpression.Type}"
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.RedundantNullCheckBeforeTypeOf, adjacent.GetLocation(), typeOfText))
        End Sub

#End Region

    End Class

End Namespace

#End Region
