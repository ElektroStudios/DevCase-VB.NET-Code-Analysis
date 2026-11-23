#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " GuardExceptionKind "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Specifies the argument exception type that a silent guard clause must throw instead of exiting.
    ''' </summary>
    Friend Enum GuardExceptionKind As Integer

        ''' <summary>
        ''' Throw <see cref="System.ArgumentNullException"/>.
        ''' </summary>
        ArgumentNull = 0

        ''' <summary>
        ''' Throw <see cref="System.ArgumentException"/>.
        ''' </summary>
        Argument = 1

        ''' <summary>
        ''' Throw <see cref="System.ArgumentOutOfRangeException"/>.
        ''' </summary>
        ArgumentOutOfRange = 2

    End Enum

End Namespace

#End Region

#Region " GuardCondition "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Represents one atomic condition of a silent guard clause that evaluates a parameter.
    ''' </summary>
    Friend NotInheritable Class GuardCondition

#Region " Properties "

        ''' <summary>
        ''' Gets the atomic condition expression.
        ''' </summary>
        Friend ReadOnly Property Condition As ExpressionSyntax

        ''' <summary>
        ''' Gets the name of the evaluated parameter.
        ''' </summary>
        Friend ReadOnly Property ParameterName As String

        ''' <summary>
        ''' Gets the kind of exception that must be thrown when the condition is met.
        ''' </summary>
        Friend ReadOnly Property ExceptionKind As GuardExceptionKind

        ''' <summary>
        ''' Gets the exception message used for <see cref="GuardExceptionKind.Argument"/>,
        ''' or <see langword="Nothing"/> for the other kinds.
        ''' </summary>
        Friend ReadOnly Property Message As String

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="GuardCondition"/> class.
        ''' </summary>
        '''
        ''' <param name="condition">
        ''' The atomic condition expression.
        ''' </param>
        '''
        ''' <param name="parameterName">
        ''' The name of the evaluated parameter.
        ''' </param>
        '''
        ''' <param name="exceptionKind">
        ''' The kind of exception that must be thrown.
        ''' </param>
        '''
        ''' <param name="message">
        ''' The exception message for <see cref="GuardExceptionKind.Argument"/>; otherwise <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="condition"/> or <paramref name="parameterName"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Sub New(condition As ExpressionSyntax, parameterName As String, exceptionKind As GuardExceptionKind, message As String)

            If condition Is Nothing Then
                Throw New ArgumentNullException(NameOf(condition))
            End If
            If String.IsNullOrWhiteSpace(parameterName) Then
                Throw New ArgumentNullException(NameOf(parameterName))
            End If

            Me.Condition = condition
            Me.ParameterName = parameterName
            Me.ExceptionKind = exceptionKind
            Me.Message = message
        End Sub

#End Region

    End Class

End Namespace

#End Region

#Region " UtilGuardClauses "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the detection logic of silent parameter guard clauses (rule <see cref="DiagnosticIds.SilentGuardClause"/>),
    ''' shared by the analyzer and its code fix provider.
    ''' </summary>
    Friend NotInheritable Class UtilGuardClauses

#Region " Constants "

        ''' <summary>
        ''' The message used when a parameter must be <see langword="Nothing"/>.
        ''' </summary>
        Friend Const MustBeNothingMessage As String = "Value must be Nothing."

        ''' <summary>
        ''' The message used when a collection or string parameter must not be empty.
        ''' </summary>
        Friend Const MustNotBeEmptyMessage As String = "Value cannot be empty."

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilGuardClauses"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Analyzes an <see langword="If"/> statement (multi-line block or single-line statement) and determines
        ''' whether it is a silent guard clause on parameters of the enclosing member.
        ''' </summary>
        '''
        ''' <param name="ifNode">
        ''' A <see cref="MultiLineIfBlockSyntax"/> or <see cref="SingleLineIfStatementSyntax"/> node.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model of the syntax tree containing <paramref name="ifNode"/>.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <param name="exitStatement">
        ''' When this method returns <see langword="True"/>, receives the silent <see langword="Return"/>/<see langword="Exit"/> statement.
        ''' </param>
        '''
        ''' <param name="conditions">
        ''' When this method returns <see langword="True"/>, receives the atomic guard conditions in source order.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the node is a silent guard clause whose every atomic condition evaluates a parameter;
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="ifNode"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="ifNode"/> is neither a multi-line nor a single-line <see langword="If"/> statement.
        ''' </exception>
        Friend Shared Function TryAnalyze(ifNode As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken,
                                          ByRef exitStatement As StatementSyntax,
                                          ByRef conditions As ImmutableArray(Of GuardCondition)) As Boolean

            If ifNode Is Nothing Then
                Throw New ArgumentNullException(NameOf(ifNode))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            exitStatement = Nothing
            conditions = ImmutableArray(Of GuardCondition).Empty

            Dim statements As SyntaxList(Of StatementSyntax)


            Dim condition As ExpressionSyntax
            Select Case ifNode.Kind()
                Case SyntaxKind.MultiLineIfBlock
                    Dim block As MultiLineIfBlockSyntax = DirectCast(ifNode, MultiLineIfBlockSyntax)
                    If block.ElseIfBlocks.Count <> 0 OrElse block.ElseBlock IsNot Nothing Then
                        Return False
                    End If
                    condition = block.IfStatement.Condition
                    statements = block.Statements

                Case SyntaxKind.SingleLineIfStatement
                    Dim singleLine As SingleLineIfStatementSyntax = DirectCast(ifNode, SingleLineIfStatementSyntax)
                    If singleLine.ElseClause IsNot Nothing Then
                        Return False
                    End If
                    condition = singleLine.Condition
                    statements = singleLine.Statements

                Case Else
                    Throw New ArgumentException($"Unsupported node kind: {ifNode.Kind()}.", NameOf(ifNode))
            End Select

            If condition Is Nothing OrElse statements.Count <> 1 OrElse Not UtilGuardClauses.IsSilentExit(statements(0)) Then
                Return False
            End If

            Dim enclosingMethod As IMethodSymbol = UtilGuardClauses.GetEnclosingMethod(ifNode, semanticModel, cancellationToken)
            If enclosingMethod Is Nothing Then
                Return False
            End If

            Dim builder As ImmutableArray(Of GuardCondition).Builder = ImmutableArray.CreateBuilder(Of GuardCondition)()
            For Each atom As ExpressionSyntax In UtilAnalyzerSyntax.FlattenOrElseChain(condition)
                Dim guard As GuardCondition = UtilGuardClauses.ClassifyAtom(atom, enclosingMethod, semanticModel, cancellationToken)
                If guard Is Nothing Then
                    Return False
                End If
                builder.Add(guard)
            Next atom

            exitStatement = statements(0)
            conditions = builder.ToImmutable()
            Return True
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Determines whether a statement silently leaves the current member
        ''' (<see langword="Return"/>, <c>Return Nothing</c>, <c>Exit Sub</c>, <c>Exit Function</c> or <c>Exit Property</c>).
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement to evaluate.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the statement is a silent exit; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsSilentExit(statement As StatementSyntax) As Boolean

            Select Case statement.Kind()
                Case SyntaxKind.ExitSubStatement, SyntaxKind.ExitFunctionStatement, SyntaxKind.ExitPropertyStatement
                    Return True

                Case SyntaxKind.ReturnStatement
                    Dim returnStatement As ReturnStatementSyntax = DirectCast(statement, ReturnStatementSyntax)
                    Return returnStatement.Expression Is Nothing OrElse
                           returnStatement.Expression.IsKind(SyntaxKind.NothingLiteralExpression)

                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Gets the method (or accessor) symbol whose body directly contains the specified node;
        ''' nodes located inside lambda expressions are rejected because their <see langword="Return"/> has a different meaning.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node to locate.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The enclosing method symbol, or <see langword="Nothing"/> if the node is not directly inside a member body.
        ''' </returns>
        Friend Shared Function GetEnclosingMethod(node As SyntaxNode, semanticModel As SemanticModel,
                                                   cancellationToken As CancellationToken) As IMethodSymbol

            For Each ancestor As SyntaxNode In node.Ancestors()
                If TypeOf ancestor Is LambdaExpressionSyntax Then
                    Return Nothing
                End If

                Dim methodBlock As MethodBlockBaseSyntax = TryCast(ancestor, MethodBlockBaseSyntax)
                If methodBlock IsNot Nothing Then
                    Return TryCast(semanticModel.GetDeclaredSymbol(methodBlock.BlockStatement, cancellationToken), IMethodSymbol)
                End If
            Next ancestor
            Return Nothing
        End Function

        ''' <summary>
        ''' Classifies one atomic condition and determines which parameter it evaluates and which exception it requires.
        ''' </summary>
        '''
        ''' <param name="atom">
        ''' The atomic condition.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The enclosing method symbol.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The classified guard condition, or <see langword="Nothing"/> if the atom is not a supported parameter check.
        ''' </returns>
        Private Shared Function ClassifyAtom(atom As ExpressionSyntax, method As IMethodSymbol,
                                             semanticModel As SemanticModel, cancellationToken As CancellationToken) As GuardCondition

            Select Case atom.Kind()

                ' p Is Nothing / p IsNot Nothing / p = Nothing / p <> Nothing / p < 0 / p.Length = 0 / ...
                Case SyntaxKind.IsExpression, SyntaxKind.IsNotExpression,
                     SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression,
                     SyntaxKind.LessThanExpression, SyntaxKind.LessThanOrEqualExpression,
                     SyntaxKind.GreaterThanExpression, SyntaxKind.GreaterThanOrEqualExpression

                    Dim binary As BinaryExpressionSyntax = DirectCast(atom, BinaryExpressionSyntax)
                    Dim left As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Left)
                    Dim right As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Right)
                    Dim isNothingComparison As Boolean = left.IsKind(SyntaxKind.NothingLiteralExpression) OrElse
                                                         right.IsKind(SyntaxKind.NothingLiteralExpression)

                    Dim operand As ExpressionSyntax = If(left.IsKind(SyntaxKind.NothingLiteralExpression), right, left)
                    Dim otherOperand As ExpressionSyntax = If(operand Is left, right, left)

                    ' Direct parameter operand.
                    Dim parameter As IParameterSymbol = UtilGuardClauses.GetParameter(operand, method, semanticModel, cancellationToken)
                    If parameter Is Nothing AndAlso Not isNothingComparison Then
                        ' The parameter may also be the right-hand operand (e.g. 0 > p).
                        parameter = UtilGuardClauses.GetParameter(otherOperand, method, semanticModel, cancellationToken)
                        If parameter IsNot Nothing Then
                            Dim swap As ExpressionSyntax = operand
                            operand = otherOperand
                            otherOperand = swap
                        End If
                    End If

                    If parameter IsNot Nothing Then
                        If isNothingComparison Then
                            Dim isNegated As Boolean = atom.IsKind(SyntaxKind.IsNotExpression) OrElse atom.IsKind(SyntaxKind.NotEqualsExpression)
                            If isNegated Then
                                Return New GuardCondition(atom, parameter.Name, GuardExceptionKind.Argument, UtilGuardClauses.MustBeNothingMessage)
                            End If

                            ' "p = Nothing" on a non-nullable value type compares against its default value.
                            If parameter.Type.IsValueType AndAlso
                               parameter.Type.OriginalDefinition.SpecialType <> SpecialType.System_Nullable_T AndAlso
                               atom.IsKind(SyntaxKind.EqualsExpression) Then
                                Return New GuardCondition(atom, parameter.Name, GuardExceptionKind.ArgumentOutOfRange, Nothing)
                            End If

                            ' "p Is Nothing", or "p = Nothing" (which on a String also matches the empty string).
                            Dim isNullCheck As Boolean = atom.IsKind(SyntaxKind.IsExpression) OrElse atom.IsKind(SyntaxKind.EqualsExpression)
                            Return If(isNullCheck, New GuardCondition(atom, parameter.Name, GuardExceptionKind.ArgumentNull, Nothing), Nothing)
                        End If

                        ' Relational comparison against another value (e.g. p < 0), unless both sides reference the parameter.
                        Dim isRangeCheck As Boolean = Not atom.IsKind(SyntaxKind.IsExpression) AndAlso
                                                      Not atom.IsKind(SyntaxKind.IsNotExpression) AndAlso
                                                      Not UtilGuardClauses.ReferencesParameter(otherOperand, parameter, semanticModel, cancellationToken)
                        Return If(isRangeCheck, New GuardCondition(atom, parameter.Name, GuardExceptionKind.ArgumentOutOfRange, Nothing), Nothing)
                    End If

                    ' p.Length = 0 / p.Count < 1 / p.LongLength = 0
                    If Not isNothingComparison Then
                        Dim lengthParameter As IParameterSymbol =
                            If(UtilGuardClauses.GetLengthAccessParameter(left, method, semanticModel, cancellationToken),
                               UtilGuardClauses.GetLengthAccessParameter(right, method, semanticModel, cancellationToken))

                        If lengthParameter IsNot Nothing Then
                            Return New GuardCondition(atom, lengthParameter.Name, GuardExceptionKind.Argument, UtilGuardClauses.MustNotBeEmptyMessage)
                        End If
                    End If
                    Return Nothing

                ' String.IsNullOrEmpty(p) / String.IsNullOrWhiteSpace(p) / IsNothing(p)
                Case SyntaxKind.InvocationExpression
                    Dim invocation As InvocationExpressionSyntax = DirectCast(atom, InvocationExpressionSyntax)
                    If invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 Then
                        Return Nothing
                    End If

                    ' The single argument, positional or named (String.IsNullOrEmpty(value:=p)).
                    Dim argument As SimpleArgumentSyntax = TryCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax)
                    If argument Is Nothing Then
                        Return Nothing
                    End If

                    Dim parameter As IParameterSymbol = UtilGuardClauses.GetParameter(argument.Expression, method, semanticModel, cancellationToken)
                    If parameter Is Nothing Then
                        Return Nothing
                    End If

                    Dim invokedMethod As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
                    Dim isStringNullCheck As Boolean =
                        invokedMethod IsNot Nothing AndAlso
                        invokedMethod.ContainingType.SpecialType = SpecialType.System_String AndAlso
                        (invokedMethod.Name.Equals(NameOf(String.IsNullOrEmpty), StringComparison.Ordinal) OrElse
                         invokedMethod.Name.Equals(NameOf(String.IsNullOrWhiteSpace), StringComparison.Ordinal))
                    Dim isIsNothingCall As Boolean =
                        invokedMethod IsNot Nothing AndAlso
                        invokedMethod.Name.Equals("IsNothing", StringComparison.Ordinal) AndAlso
                        invokedMethod.ContainingType.ToDisplayString().Equals("Microsoft.VisualBasic.Information", StringComparison.Ordinal)
                    Return If(isStringNullCheck OrElse isIsNothingCall, New GuardCondition(atom, parameter.Name, GuardExceptionKind.ArgumentNull, Nothing), Nothing)

                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Gets the parameter of the enclosing method referenced by an expression, if the expression is a plain identifier.
        ''' </summary>
        '''
        ''' <param name="expr">
        ''' The expression to evaluate.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The enclosing method symbol.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The parameter symbol, or <see langword="Nothing"/> if the expression is not a parameter of the enclosing member.
        ''' </returns>
        Friend Shared Function GetParameter(expr As ExpressionSyntax, method As IMethodSymbol,
                                             semanticModel As SemanticModel, cancellationToken As CancellationToken) As IParameterSymbol

            Dim identifier As IdentifierNameSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(expr), IdentifierNameSyntax)
            If identifier Is Nothing Then
                Return Nothing
            End If

            Dim parameter As IParameterSymbol = TryCast(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, IParameterSymbol)
            If parameter Is Nothing Then
                Return Nothing
            End If

            ' ByRef parameters are outputs (or in/out values): checking them is not a precondition.
            If parameter.RefKind <> RefKind.None Then
                Return Nothing
            End If

            Dim owner As ISymbol = parameter.ContainingSymbol
            If Not (SymbolEqualityComparer.Default.Equals(owner, method) OrElse
                    (method.AssociatedSymbol IsNot Nothing AndAlso SymbolEqualityComparer.Default.Equals(owner, method.AssociatedSymbol))) Then
                Return Nothing
            End If

            ' A parameter reassigned before the check no longer holds the caller's argument.
            Return If(UtilGuardClauses.IsAssignedBefore(identifier, parameter, semanticModel, cancellationToken), Nothing, parameter)
        End Function

        ''' <summary>
        ''' Determines whether a parameter is the target of an assignment located before the specified reference, 
        ''' within the same member body.
        ''' </summary>
        '''
        ''' <param name="reference">
        ''' The parameter reference inside the guard condition.
        ''' </param>
        '''
        ''' <param name="parameter">
        ''' The parameter symbol.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the parameter is assigned before <paramref name="reference"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAssignedBefore(reference As IdentifierNameSyntax, parameter As IParameterSymbol,
                                                 semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            Dim body As MethodBlockBaseSyntax = reference.FirstAncestorOrSelf(Of MethodBlockBaseSyntax)()
            If body Is Nothing Then
                Return False
            End If

            For Each assignment As AssignmentStatementSyntax In body.DescendantNodes().OfType(Of AssignmentStatementSyntax)()
                If assignment.SpanStart >= reference.SpanStart Then
                    Exit For ' DescendantNodes yields nodes in source order.
                End If

                Dim target As IdentifierNameSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(assignment.Left), IdentifierNameSyntax)
                If target IsNot Nothing AndAlso
                   SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(target, cancellationToken).Symbol, parameter) Then
                    Return True
                End If
            Next assignment
            Return False
        End Function

        ''' <summary>
        ''' Gets the parameter referenced by a <c>p.Length</c>, <c>p.LongLength</c> or <c>p.Count</c> member access.
        ''' </summary>
        '''
        ''' <param name="expr">
        ''' The expression to evaluate.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The enclosing method symbol.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The parameter symbol, or <see langword="Nothing"/> if the expression is not such a member access.
        ''' </returns>
        Private Shared Function GetLengthAccessParameter(expr As ExpressionSyntax, method As IMethodSymbol,
                                                         semanticModel As SemanticModel, cancellationToken As CancellationToken) As IParameterSymbol

            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(expr), MemberAccessExpressionSyntax)
            If memberAccess Is Nothing OrElse memberAccess.Expression Is Nothing Then
                Return Nothing
            End If

            Dim memberName As String = memberAccess.Name.Identifier.ValueText
            Return If(Not (memberName.Equals("Length", StringComparison.OrdinalIgnoreCase) OrElse
                    memberName.Equals("LongLength", StringComparison.OrdinalIgnoreCase) OrElse
                    memberName.Equals("Count", StringComparison.OrdinalIgnoreCase)),
                Nothing,
                UtilGuardClauses.GetParameter(memberAccess.Expression, method, semanticModel, cancellationToken))
        End Function

        ''' <summary>
        ''' Determines whether an expression references the specified parameter anywhere in its sub-tree.
        ''' </summary>
        '''
        ''' <param name="expr">
        ''' The expression to inspect.
        ''' </param>
        '''
        ''' <param name="parameter">
        ''' The parameter symbol.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the parameter is referenced; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function ReferencesParameter(expr As ExpressionSyntax, parameter As IParameterSymbol,
                                                    semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            For Each identifier As IdentifierNameSyntax In expr.DescendantNodesAndSelf().OfType(Of IdentifierNameSyntax)()
                Dim symbol As ISymbol = semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol
                If SymbolEqualityComparer.Default.Equals(symbol, parameter) Then
                    Return True
                End If
            Next identifier
            Return False
        End Function

#End Region

    End Class

End Namespace

#End Region
