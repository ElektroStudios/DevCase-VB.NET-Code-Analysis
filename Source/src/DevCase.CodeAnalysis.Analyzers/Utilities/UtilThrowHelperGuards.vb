#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Linq
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ThrowHelperGuard "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Represents a classic argument guard (<c>If ... Then Throw New ArgumentXxxException(NameOf(p))</c>)
    ''' together with its equivalent modern .NET throw-helper call.
    ''' </summary>
    Friend NotInheritable Class ThrowHelperGuard

#Region " Properties "

        ''' <summary>
        ''' Gets the guard statement (a <see cref="MultiLineIfBlockSyntax"/> or <see cref="SingleLineIfStatementSyntax"/>).
        ''' </summary>
        Friend ReadOnly Property Statement As StatementSyntax

        ''' <summary>
        ''' Gets the equivalent throw-helper call, e.g. <c>ArgumentNullException.ThrowIfNull(value)</c>.
        ''' </summary>
        Friend ReadOnly Property Replacement As String

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThrowHelperGuard"/> class.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The guard statement.
        ''' </param>
        '''
        ''' <param name="replacement">
        ''' The equivalent throw-helper call.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> or <paramref name="replacement"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Sub New(statement As StatementSyntax, replacement As String)

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If
            If String.IsNullOrWhiteSpace(replacement) Then
                Throw New ArgumentNullException(NameOf(replacement))
            End If

            Me.Statement = statement
            Me.Replacement = replacement
        End Sub

#End Region

    End Class

End Namespace

#End Region

#Region " UtilThrowHelperGuards "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the detection logic of rule <see cref="DiagnosticIds.ThrowHelperMigration"/>: runs of classic argument guards
    ''' in .NET Framework code that have a modern throw-helper equivalent (<c>ArgumentNullException.ThrowIfNull</c>,
    ''' <c>ArgumentException.ThrowIfNullOrWhiteSpace</c>, <c>ArgumentOutOfRangeException.ThrowIfNegative</c>, ...).
    ''' </summary>
    Friend NotInheritable Class UtilThrowHelperGuards

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilThrowHelperGuards"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether a compilation targets the .NET Framework (whose core library is <c>mscorlib</c>)
        ''' and therefore lacks the modern throw helpers.
        ''' </summary>
        '''
        ''' <param name="compilation">
        ''' The compilation.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for .NET Framework compilations without <c>ArgumentNullException.ThrowIfNull</c>; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="compilation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsNetFrameworkWithoutThrowHelpers(compilation As Compilation) As Boolean

            If compilation Is Nothing Then
                Throw New ArgumentNullException(NameOf(compilation))
            End If

            Dim objectType As INamedTypeSymbol = compilation.GetSpecialType(SpecialType.System_Object)
            If objectType Is Nothing OrElse objectType.ContainingAssembly Is Nothing OrElse
               Not objectType.ContainingAssembly.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase) Then
                Return False
            End If

            Dim argumentNullException As INamedTypeSymbol = compilation.GetTypeByMetadataName("System.ArgumentNullException")
            Return argumentNullException IsNot Nothing AndAlso argumentNullException.GetMembers("ThrowIfNull").IsEmpty
        End Function

        ''' <summary>
        ''' Classifies a statement as a convertible argument guard.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement to classify.
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
        ''' The guard, or <see langword="Nothing"/> if the statement has no exact throw-helper equivalent.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function TryCreate(statement As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ThrowHelperGuard

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim condition As ExpressionSyntax
            Dim statements As SyntaxList(Of StatementSyntax)
            Select Case statement.Kind()
                Case SyntaxKind.MultiLineIfBlock
                    Dim block As MultiLineIfBlockSyntax = DirectCast(statement, MultiLineIfBlockSyntax)
                    If block.ElseIfBlocks.Count <> 0 OrElse block.ElseBlock IsNot Nothing Then
                        Return Nothing
                    End If
                    condition = block.IfStatement.Condition
                    statements = block.Statements

                Case SyntaxKind.SingleLineIfStatement
                    Dim singleLine As SingleLineIfStatementSyntax = DirectCast(statement, SingleLineIfStatementSyntax)
                    If singleLine.ElseClause IsNot Nothing Then
                        Return Nothing
                    End If
                    condition = singleLine.Condition
                    statements = singleLine.Statements

                Case Else
                    Return Nothing
            End Select

            If condition Is Nothing OrElse statements.Count <> 1 Then
                Return Nothing
            End If

            Dim throwStatement As ThrowStatementSyntax = TryCast(statements(0), ThrowStatementSyntax)
            Dim creation As ObjectCreationExpressionSyntax = If(throwStatement Is Nothing, Nothing, TryCast(throwStatement.Expression, ObjectCreationExpressionSyntax))
            If creation Is Nothing OrElse creation.ArgumentList Is Nothing OrElse creation.ArgumentList.Arguments.Count <> 1 Then
                Return Nothing ' Custom messages would be lost by the throw helpers.
            End If

            Dim method As IMethodSymbol = UtilGuardClauses.GetEnclosingMethod(statement, semanticModel, cancellationToken)
            If method Is Nothing Then
                Return Nothing
            End If

            ' The single argument must be NameOf(<parameter>).
            Dim argument As SimpleArgumentSyntax = TryCast(creation.ArgumentList.Arguments(0), SimpleArgumentSyntax)
            Dim nameOfExpression As NameOfExpressionSyntax = If(argument Is Nothing OrElse argument.IsNamed, Nothing, TryCast(argument.Expression, NameOfExpressionSyntax))
            If nameOfExpression Is Nothing Then
                Return Nothing
            End If
            Dim parameter As IParameterSymbol = UtilGuardClauses.GetParameter(nameOfExpression.Argument, method, semanticModel, cancellationToken)
            If parameter Is Nothing Then
                Return Nothing
            End If

            Dim exceptionType As ITypeSymbol = semanticModel.GetTypeInfo(creation, cancellationToken).Type
            Dim exceptionName As String = If(exceptionType Is Nothing, String.Empty, exceptionType.ToDisplayString())
            Dim replacement As String

            Select Case exceptionName
                Case "System.ArgumentNullException"
                    replacement = UtilThrowHelperGuards.GetNullReplacement(UtilAnalyzerSyntax.SkipParentheses(condition), parameter, method, semanticModel, cancellationToken)
                Case "System.ArgumentOutOfRangeException"
                    replacement = UtilThrowHelperGuards.GetRangeReplacement(UtilAnalyzerSyntax.SkipParentheses(condition), parameter, method, semanticModel, cancellationToken)
                Case Else
                    replacement = Nothing
            End Select

            Return If(replacement Is Nothing, Nothing, New ThrowHelperGuard(DirectCast(statement, StatementSyntax), replacement))
        End Function

        ''' <summary>
        ''' Gets the run of consecutive convertible guards that starts at the specified statement.
        ''' </summary>
        '''
        ''' <param name="first">
        ''' The first guard statement.
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
        ''' The guards of the run, in source order (empty if <paramref name="first"/> is not convertible).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="first"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetRun(first As StatementSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As List(Of ThrowHelperGuard)

            If first Is Nothing Then
                Throw New ArgumentNullException(NameOf(first))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim result As New List(Of ThrowHelperGuard)()
            Dim siblings As List(Of StatementSyntax) = UtilThrowHelperGuards.GetSiblingStatements(first)
            Dim startIndex As Integer = siblings.IndexOf(first)

            For index As Integer = startIndex To siblings.Count - 1
                ' A preprocessor directive between two guards ends the run.
                If index > startIndex AndAlso siblings(index).GetLeadingTrivia().Any(Function(t As SyntaxTrivia) t.IsDirective) Then
                    Exit For
                End If

                Dim guard As ThrowHelperGuard = UtilThrowHelperGuards.TryCreate(siblings(index), semanticModel, cancellationToken)
                If guard Is Nothing Then
                    Exit For
                End If
                result.Add(guard)
            Next index
            Return result
        End Function

        ''' <summary>
        ''' Determines whether a guard statement starts a run (its previous sibling statement is not a convertible guard).
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The guard statement.
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
        ''' <see langword="True"/> if the statement starts a run; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsFirstOfRun(statement As StatementSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            If statement.GetLeadingTrivia().Any(Function(t As SyntaxTrivia) t.IsDirective) Then
                Return True
            End If

            Dim siblings As List(Of StatementSyntax) = UtilThrowHelperGuards.GetSiblingStatements(statement)
            Dim index As Integer = siblings.IndexOf(statement)
            Return index <= 0 OrElse UtilThrowHelperGuards.TryCreate(siblings(index - 1), semanticModel, cancellationToken) Is Nothing
        End Function

        ''' <summary>
        ''' Determines whether a node is located inside a conditional compilation block (<c>#If</c> ... <c>#End If</c>).
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the node is inside a conditional block; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsInsideConditionalDirective(node As SyntaxNode) As Boolean

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim depth As Integer = 0
            For Each trivia As SyntaxTrivia In node.SyntaxTree.GetRoot().DescendantTrivia()
                If trivia.SpanStart >= node.SpanStart Then
                    Exit For
                End If
                Select Case trivia.Kind()
                    Case SyntaxKind.IfDirectiveTrivia
                        depth += 1
                    Case SyntaxKind.EndIfDirectiveTrivia
                        depth -= 1
                End Select
            Next trivia
            Return depth > 0
        End Function

        ''' <summary>
        ''' Determines whether a guard statement can host preprocessor directives around it
        ''' (i.e. it is not nested inside a single-line construct).
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The guard statement.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if directives can be placed on the lines around it; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function CanHostDirectives(statement As SyntaxNode) As Boolean

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If

            Return Not (TypeOf statement.Parent Is SingleLineIfStatementSyntax OrElse
                        TypeOf statement.Parent Is SingleLineElseClauseSyntax OrElse
                        TypeOf statement.Parent Is SingleLineLambdaExpressionSyntax)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the statements that share the parent of a statement, in source order.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The sibling statements (including <paramref name="statement"/>).
        ''' </returns>
        Private Shared Function GetSiblingStatements(statement As SyntaxNode) As List(Of StatementSyntax)

            Return If(statement.Parent Is Nothing,
                      New List(Of StatementSyntax) From {DirectCast(statement, StatementSyntax)},
                      statement.Parent.ChildNodes().OfType(Of StatementSyntax)().ToList())
        End Function

        ''' <summary>
        ''' Gets the throw helper that replaces a null/empty check guarded by <see cref="ArgumentNullException"/>.
        ''' </summary>
        '''
        ''' <param name="condition">
        ''' The (unparenthesized) guard condition.
        ''' </param>
        '''
        ''' <param name="parameter">
        ''' The parameter named by the exception.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The enclosing method.
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
        ''' The replacement call, or <see langword="Nothing"/> if there is no exact equivalent.
        ''' </returns>
        Private Shared Function GetNullReplacement(condition As ExpressionSyntax, parameter As IParameterSymbol, method As IMethodSymbol,
                                                   semanticModel As SemanticModel, cancellationToken As CancellationToken) As String

            ' p Is Nothing / Nothing Is p  ("p = Nothing" is NOT equivalent for strings: it also matches "").
            If condition.IsKind(SyntaxKind.IsExpression) Then
                Dim binary As BinaryExpressionSyntax = DirectCast(condition, BinaryExpressionSyntax)
                Dim left As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Left)
                Dim right As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Right)
                Dim operand As ExpressionSyntax = If(left.IsKind(SyntaxKind.NothingLiteralExpression), right,
                                                     If(right.IsKind(SyntaxKind.NothingLiteralExpression), left, Nothing))
                Dim isParameterCheck As Boolean =
                    operand IsNot Nothing AndAlso
                    SymbolEqualityComparer.Default.Equals(UtilGuardClauses.GetParameter(operand, method, semanticModel, cancellationToken), parameter)
                Return If(isParameterCheck, $"ArgumentNullException.ThrowIfNull({parameter.Name})", Nothing)
            End If

            ' String.IsNullOrEmpty(p) / String.IsNullOrWhiteSpace(p) / IsNothing(p)
            Dim invocation As InvocationExpressionSyntax = TryCast(condition, InvocationExpressionSyntax)
            If invocation Is Nothing OrElse invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 Then
                Return Nothing
            End If

            Dim argument As SimpleArgumentSyntax = TryCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax)
            If argument Is Nothing OrElse argument.IsNamed OrElse
               Not SymbolEqualityComparer.Default.Equals(UtilGuardClauses.GetParameter(argument.Expression, method, semanticModel, cancellationToken), parameter) Then
                Return Nothing
            End If

            Dim invoked As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
            If invoked Is Nothing Then
                Return Nothing
            End If

            If invoked.ContainingType.SpecialType = SpecialType.System_String Then
                Select Case invoked.Name
                    Case NameOf(String.IsNullOrWhiteSpace)
                        Return $"ArgumentException.ThrowIfNullOrWhiteSpace({parameter.Name})"
                    Case NameOf(String.IsNullOrEmpty)
                        Return $"ArgumentException.ThrowIfNullOrEmpty({parameter.Name})"
                End Select
            End If

            Return If(invoked.Name.Equals("IsNothing", StringComparison.Ordinal) AndAlso
               invoked.ContainingType.ToDisplayString().Equals("Microsoft.VisualBasic.Information", StringComparison.Ordinal),
                $"ArgumentNullException.ThrowIfNull({parameter.Name})",
                Nothing)
        End Function

        ''' <summary>
        ''' Gets the throw helper that replaces a numeric range check guarded by <see cref="ArgumentOutOfRangeException"/>.
        ''' </summary>
        '''
        ''' <param name="condition">
        ''' The (unparenthesized) guard condition.
        ''' </param>
        '''
        ''' <param name="parameter">
        ''' The parameter named by the exception.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The enclosing method.
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
        ''' The replacement call, or <see langword="Nothing"/> if there is no exact equivalent.
        ''' </returns>
        Private Shared Function GetRangeReplacement(condition As ExpressionSyntax, parameter As IParameterSymbol, method As IMethodSymbol,
                                                    semanticModel As SemanticModel, cancellationToken As CancellationToken) As String

            If Not UtilThrowHelperGuards.IsNumeric(parameter.Type) Then
                Return Nothing
            End If

            Dim binary As BinaryExpressionSyntax = TryCast(condition, BinaryExpressionSyntax)
            If binary Is Nothing Then
                Return Nothing
            End If

            Dim kind As SyntaxKind = binary.Kind()
            Dim left As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Left)
            Dim right As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(binary.Right)

            Dim other As ExpressionSyntax
            If SymbolEqualityComparer.Default.Equals(UtilGuardClauses.GetParameter(left, method, semanticModel, cancellationToken), parameter) Then
                other = right
            ElseIf SymbolEqualityComparer.Default.Equals(UtilGuardClauses.GetParameter(right, method, semanticModel, cancellationToken), parameter) Then
                other = left
                ' Normalize "0 > p" into "p < 0".
                Select Case kind
                    Case SyntaxKind.LessThanExpression : kind = SyntaxKind.GreaterThanExpression
                    Case SyntaxKind.LessThanOrEqualExpression : kind = SyntaxKind.GreaterThanOrEqualExpression
                    Case SyntaxKind.GreaterThanExpression : kind = SyntaxKind.LessThanExpression
                    Case SyntaxKind.GreaterThanOrEqualExpression : kind = SyntaxKind.LessThanOrEqualExpression
                End Select
            Else
                Return Nothing
            End If

            ' The other operand must not depend on the parameter itself.
            For Each identifier As IdentifierNameSyntax In other.DescendantNodesAndSelf().OfType(Of IdentifierNameSyntax)()
                If SymbolEqualityComparer.Default.Equals(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, parameter) Then
                    Return Nothing
                End If
            Next identifier

            Dim constant As [Optional](Of Object) = semanticModel.GetConstantValue(other, cancellationToken)
            Dim isZero As Boolean = constant.HasValue AndAlso
                                    TypeOf constant.Value Is IConvertible AndAlso
                                    Convert.ToDouble(constant.Value, CultureInfo.InvariantCulture) = 0.0R

            Dim name As String = parameter.Name
            Dim otherText As String = other.WithoutTrivia().ToString()

            Select Case kind
                Case SyntaxKind.LessThanExpression
                    Return If(isZero, $"ArgumentOutOfRangeException.ThrowIfNegative({name})", $"ArgumentOutOfRangeException.ThrowIfLessThan({name}, {otherText})")
                Case SyntaxKind.LessThanOrEqualExpression
                    Return If(isZero, $"ArgumentOutOfRangeException.ThrowIfNegativeOrZero({name})", $"ArgumentOutOfRangeException.ThrowIfLessThanOrEqual({name}, {otherText})")
                Case SyntaxKind.GreaterThanExpression
                    Return $"ArgumentOutOfRangeException.ThrowIfGreaterThan({name}, {otherText})"
                Case SyntaxKind.GreaterThanOrEqualExpression
                    Return $"ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual({name}, {otherText})"
                Case SyntaxKind.EqualsExpression
                    Return If(isZero, $"ArgumentOutOfRangeException.ThrowIfZero({name})", $"ArgumentOutOfRangeException.ThrowIfEqual({name}, {otherText})")
                Case SyntaxKind.NotEqualsExpression
                    Return $"ArgumentOutOfRangeException.ThrowIfNotEqual({name}, {otherText})"
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Determines whether a type is a primitive numeric type supported by the generic range throw helpers.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type is numeric; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsNumeric(type As ITypeSymbol) As Boolean

            Select Case type.SpecialType
                Case SpecialType.System_SByte, SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16,
                     SpecialType.System_Int32, SpecialType.System_UInt32, SpecialType.System_Int64, SpecialType.System_UInt64,
                     SpecialType.System_Single, SpecialType.System_Double, SpecialType.System_Decimal
                    Return True
                Case Else
                    Return False
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
