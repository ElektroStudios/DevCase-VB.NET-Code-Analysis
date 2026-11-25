#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " UtilGuardedCalls "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Recognizes a call guarded by a redundant check of the same collection and argument: 
    ''' <c>If map.ContainsKey(key) Then map.Remove(key)</c>, <c>If Not items.Contains(item) Then items.Add(item)</c>...
    ''' </summary>
    Friend NotInheritable Class UtilGuardedCalls

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilGuardedCalls"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the condition and the statements of an <see langword="If"/> statement without <see langword="ElseIf"/> or 
        ''' <see langword="Else"/> parts.
        ''' </summary>
        '''
        ''' <param name="ifNode">
        ''' A <see cref="MultiLineIfBlockSyntax"/> or a <see cref="SingleLineIfStatementSyntax"/>.
        ''' </param>
        '''
        ''' <param name="condition">
        ''' When this method returns <see langword="True"/>, the condition.
        ''' </param>
        '''
        ''' <param name="statements">
        ''' When this method returns <see langword="True"/>, the statements executed when the condition is <see langword="True"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the node is an <see langword="If"/> statement without other branches; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function TryGetSingleBranch(ifNode As SyntaxNode, ByRef condition As ExpressionSyntax, ByRef statements As SyntaxList(Of StatementSyntax)) As Boolean

            condition = Nothing
            statements = Nothing
            Dim block As MultiLineIfBlockSyntax = TryCast(ifNode, MultiLineIfBlockSyntax)
            Dim singleLine As SingleLineIfStatementSyntax = TryCast(ifNode, SingleLineIfStatementSyntax)
            Select Case True
                Case block IsNot Nothing AndAlso block.ElseIfBlocks.Count = 0 AndAlso block.ElseBlock Is Nothing
                    condition = block.IfStatement.Condition
                    statements = block.Statements
                Case singleLine IsNot Nothing AndAlso singleLine.ElseClause Is Nothing
                    condition = singleLine.Condition
                    statements = singleLine.Statements
                Case Else
                    Return False
            End Select
            Return condition IsNot Nothing AndAlso statements.Count > 0
        End Function

        ''' <summary>
        ''' Gets the invocation of a call statement (<c>map.Remove(key)</c> or <c>Call map.Remove(key)</c>).
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The invocation; <see langword="Nothing"/> if the statement is not a call.
        ''' </returns>
        Friend Shared Function GetCallInvocation(statement As StatementSyntax) As InvocationExpressionSyntax

            Dim expressionStatement As ExpressionStatementSyntax = TryCast(statement, ExpressionStatementSyntax)
            Return If(expressionStatement IsNot Nothing,
                      TryCast(expressionStatement.Expression, InvocationExpressionSyntax),
                      TryCast(TryCast(statement, CallStatementSyntax)?.Invocation, InvocationExpressionSyntax))
        End Function

        ''' <summary>
        ''' Recognizes a guard whose first statement calls a method on the same receiver and with the same single argument as the 
        ''' check in the condition.
        ''' </summary>
        '''
        ''' <param name="ifNode">
        ''' A <see cref="MultiLineIfBlockSyntax"/> or a <see cref="SingleLineIfStatementSyntax"/>.
        ''' </param>
        '''
        ''' <param name="checkName">
        ''' The name of the checking method (<c>ContainsKey</c>, <c>Contains</c>).
        ''' </param>
        '''
        ''' <param name="check">
        ''' When this method returns <see langword="True"/>, the invocation of the checking method.
        ''' </param>
        '''
        ''' <param name="negated">
        ''' When this method returns <see langword="True"/>, <see langword="True"/> if the condition is <c>Not check</c>.
        ''' </param>
        '''
        ''' <param name="guarded">
        ''' When this method returns <see langword="True"/>, the invocation of the first statement.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the <see langword="If"/> statement has that form; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <remarks>
        ''' The receiver must be a simple reference and the argument must have no side effects, because the code fix evaluates them 
        ''' once instead of twice. A statement with preprocessor directives inside is not recognized: another configuration may 
        ''' compile other statements.
        ''' </remarks>
        Friend Shared Function TryMatch(ifNode As SyntaxNode, checkName As String, ByRef check As InvocationExpressionSyntax,
                                        ByRef negated As Boolean, ByRef guarded As InvocationExpressionSyntax) As Boolean

            check = Nothing
            negated = False
            guarded = Nothing
            Dim condition As ExpressionSyntax = Nothing
            Dim statements As SyntaxList(Of StatementSyntax) = Nothing
            If Not UtilGuardedCalls.TryGetSingleBranch(ifNode, condition, statements) OrElse UtilAnalyzerSyntax.HasInnerDirectives(ifNode) Then
                Return False ' Another configuration may compile other statements.
            End If

            Dim bare As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(condition)
            If bare.IsKind(SyntaxKind.NotExpression) Then
                negated = True
                bare = UtilAnalyzerSyntax.SkipParentheses(DirectCast(bare, UnaryExpressionSyntax).Operand)
            End If

            check = TryCast(bare, InvocationExpressionSyntax)
            guarded = UtilGuardedCalls.GetCallInvocation(statements(0))
            Dim checkAccess As MemberAccessExpressionSyntax = TryCast(check?.Expression, MemberAccessExpressionSyntax)
            Dim guardedAccess As MemberAccessExpressionSyntax = TryCast(guarded?.Expression, MemberAccessExpressionSyntax)
            If checkAccess Is Nothing OrElse guardedAccess Is Nothing OrElse
               Not checkAccess.Name.Identifier.ValueText.Equals(checkName, StringComparison.OrdinalIgnoreCase) OrElse
               Not UtilAnalyzerSyntax.IsSimpleReference(checkAccess.Expression) OrElse
               Not UtilAnalyzerSyntax.AreEquivalentExpressions(checkAccess.Expression, guardedAccess.Expression) Then
                Return False
            End If

            Dim checkArgument As ExpressionSyntax = UtilGuardedCalls.GetSingleArgument(check)
            Dim guardedArgument As ExpressionSyntax = UtilGuardedCalls.GetSingleArgument(guarded)
            Return checkArgument IsNot Nothing AndAlso Not UtilAnalyzerSyntax.MayHaveSideEffects(checkArgument) AndAlso
                   UtilAnalyzerSyntax.AreEquivalentExpressions(checkArgument, guardedArgument)
        End Function

        ''' <summary>
        ''' Reports a guarded call, with a code fix that calls the method directly and, when other statements follow it, uses its 
        ''' result as the condition (<c>If map.Remove(key) Then</c>).
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context of the <see langword="If"/> statement.
        ''' </param>
        '''
        ''' <param name="descriptor">
        ''' The descriptor of the rule.
        ''' </param>
        '''
        ''' <param name="check">
        ''' The invocation of the checking method.
        ''' </param>
        '''
        ''' <param name="guarded">
        ''' The invocation of the guarded method.
        ''' </param>
        '''
        ''' <param name="title">
        ''' The title of the code action.
        ''' </param>
        '''
        ''' <param name="messageArguments">
        ''' The arguments of the message.
        ''' </param>
        Friend Shared Sub Report(context As SyntaxNodeAnalysisContext, descriptor As DiagnosticDescriptor, check As InvocationExpressionSyntax,
                                 guarded As InvocationExpressionSyntax, title As String, ParamArray messageArguments As Object())

            Dim condition As ExpressionSyntax = Nothing
            Dim statements As SyntaxList(Of StatementSyntax) = Nothing
            UtilGuardedCalls.TryGetSingleBranch(context.Node, condition, statements)

            Dim spans As New List(Of Location)()
            Dim texts As New List(Of String)()
            Dim statement As StatementSyntax = statements(0)
            Dim comments As String = Nothing
            Dim commentLines As String = Nothing
            Dim block As MultiLineIfBlockSyntax = TryCast(context.Node, MultiLineIfBlockSyntax)
            Dim lines As SyntaxNode() = If(block IsNot Nothing, {block.IfStatement, statement}, Array.Empty(Of SyntaxNode)())
            If statements.Count = 1 Then
                ' "If map.ContainsKey(key) Then map.Remove(key)": the call alone, followed by the comments at the end of the lines, 
                ' and preceded by the comments on lines of their own.
                If UtilAnalyzerSyntax.TryGetLineComments(context.Node, lines, comments, commentLines) Then
                    spans.Add(context.Node.GetLocation())
                    texts.Add($"{commentLines}{statement}{comments}")
                End If
            ElseIf TypeOf context.Node Is MultiLineIfBlockSyntax AndAlso Not UtilAnalyzerSyntax.HasInnerComments(condition) AndAlso
                   Not statement.GetLeadingTrivia().Any(Function(t As SyntaxTrivia) t.IsDirective) AndAlso
                   Not statement.GetTrailingTrivia().Any(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.CommentTrivia)) AndAlso
                   Not UtilAnalyzerSyntax.HasInnerComments(statement) Then
                ' The call becomes the condition, and the line of its statement (with its indentation and line break) is removed; 
                ' the comment lines above it stay in the block.
                Dim text As SourceText = statement.SyntaxTree.GetText()
                Dim lineStart As Integer = text.Lines.GetLineFromPosition(statement.SpanStart).Start
                spans.Add(condition.GetLocation())
                texts.Add(guarded.ToString())
                spans.Add(Location.Create(statement.SyntaxTree, TextSpan.FromBounds(lineStart, statement.FullSpan.End)))
                texts.Add(String.Empty)
            End If

            Dim actions As New List(Of FixReplacement)()
            If spans.Count > 0 Then
                actions.Add(New FixReplacement(title, "RemoveCheck", texts.ToArray()))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(descriptor, check.GetLocation(), spans, actions, messageArguments))
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the expression of the only argument of an invocation, when it is positional.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <returns>
        ''' The expression; <see langword="Nothing"/> if the invocation does not have exactly one positional argument.
        ''' </returns>
        Private Shared Function GetSingleArgument(invocation As InvocationExpressionSyntax) As ExpressionSyntax

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = If(invocation?.ArgumentList Is Nothing, Nothing, invocation.ArgumentList.Arguments)
            Return If(arguments.Count <> 1 OrElse TypeOf arguments(0) IsNot SimpleArgumentSyntax OrElse arguments(0).IsNamed,
                      Nothing, arguments(0).GetExpression())
        End Function

#End Region

    End Class

End Namespace

#End Region
