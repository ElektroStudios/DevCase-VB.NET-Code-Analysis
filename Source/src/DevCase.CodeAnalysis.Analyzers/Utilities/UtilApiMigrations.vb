#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Linq
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ApiMigration "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Represents one .NET Framework API usage that has an exact modern .NET equivalent.
    ''' </summary>
    Friend NotInheritable Class ApiMigration

#Region " Properties "

        ''' <summary>
        ''' Gets the node whose text is replaced in the modern branch.
        ''' </summary>
        Friend ReadOnly Property Node As SyntaxNode

        ''' <summary>
        ''' Gets the replacement text of <see cref="ApiMigration.Node"/>.
        ''' </summary>
        Friend ReadOnly Property Replacement As String

        ''' <summary>
        ''' Gets the conditional compilation symbol that guarantees the availability of the modern API.
        ''' </summary>
        Friend ReadOnly Property Condition As String

        ''' <summary>
        ''' Gets a short description of the original API (used in the diagnostic message).
        ''' </summary>
        Friend ReadOnly Property OriginalApi As String

        ''' <summary>
        ''' Gets a short description of the modern API (used in the diagnostic message).
        ''' </summary>
        Friend ReadOnly Property ModernApi As String

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ApiMigration"/> class.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node whose text is replaced in the modern branch.
        ''' </param>
        '''
        ''' <param name="replacement">
        ''' The replacement text.
        ''' </param>
        '''
        ''' <param name="condition">
        ''' The conditional compilation symbol.
        ''' </param>
        '''
        ''' <param name="originalApi">
        ''' A short description of the original API.
        ''' </param>
        '''
        ''' <param name="modernApi">
        ''' A short description of the modern API.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when any argument is <see langword="Nothing"/> or empty.
        ''' </exception>
        Friend Sub New(node As SyntaxNode, replacement As String, condition As String, originalApi As String, modernApi As String)

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If
            If String.IsNullOrWhiteSpace(replacement) Then
                Throw New ArgumentNullException(NameOf(replacement))
            End If
            If String.IsNullOrWhiteSpace(condition) Then
                Throw New ArgumentNullException(NameOf(condition))
            End If
            If String.IsNullOrWhiteSpace(originalApi) Then
                Throw New ArgumentNullException(NameOf(originalApi))
            End If
            If String.IsNullOrWhiteSpace(modernApi) Then
                Throw New ArgumentNullException(NameOf(modernApi))
            End If

            Me.Node = node
            Me.Replacement = replacement
            Me.Condition = condition
            Me.OriginalApi = originalApi
            Me.ModernApi = modernApi
        End Sub

#End Region

    End Class

End Namespace

#End Region

#Region " UtilApiMigrations "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the equivalence table of rule <see cref="DiagnosticIds.NetFrameworkApiMigration"/>:
    ''' .NET Framework API usages with an exact, better equivalent available only on modern .NET.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <list type="table">
    '''   <listheader><term>.NET Framework</term><description>Modern .NET (symbol)</description></listheader>
    '''   <item><term><c>s.Contains("x")</c></term><description><c>s.Contains("x"c)</c> (NETCOREAPP)</description></item>
    '''   <item><term><c>s.StartsWith("x", StringComparison.Ordinal)</c> / <c>EndsWith</c></term><description><c>s.StartsWith("x"c)</c> (NETCOREAPP)</description></item>
    '''   <item><term><c>s.IndexOf(v, comparison) &gt;= 0</c> / <c>&lt; 0</c> (also <c>IndexOf(c)</c>)</term><description><c>s.Contains(v, comparison)</c> / <c>Not s.Contains(...)</c> (NETCOREAPP)</description></item>
    '''   <item><term><c>s.Split({"ab"}, options)</c></term><description><c>s.Split("ab", options)</c> (NETCOREAPP)</description></item>
    '''   <item><term><c>If Not d.ContainsKey(k) Then d.Add(k, v)</c></term><description><c>d.TryAdd(k, v)</c> (NETCOREAPP)</description></item>
    '''   <item><term><c>DirectCast([Enum].Parse(GetType(T), s), T)</c></term><description><c>[Enum].Parse(Of T)(s)</c> (NETCOREAPP)</description></item>
    '''   <item><term><c>BitConverter.ToString(b).Replace("-", "")</c></term><description><c>Convert.ToHexString(b)</c> (NET5_0_OR_GREATER)</description></item>
    '''   <item><term><c>Process.GetCurrentProcess().Id</c></term><description><c>Environment.ProcessId</c> (NET5_0_OR_GREATER)</description></item>
    '''   <item><term><c>Process.GetCurrentProcess().MainModule.FileName</c></term><description><c>Environment.ProcessPath</c> (NET6_0_OR_GREATER)</description></item>
    '''   <item><term><c>File.ReadAllText(...)</c> (and other <c>File</c> I/O) inside an <see langword="Async"/> method</term><description><c>Await File.ReadAllTextAsync(...).ConfigureAwait(False)</c> (NETCOREAPP)</description></item>
    ''' </list>
    ''' Deliberately excluded because they are not exact equivalents: <c>Math.Max(min, Math.Min(v, max))</c> → <c>Math.Clamp</c>
    ''' (throws when min &gt; max), culture-sensitive <c>StartsWith(String)</c> → <c>StartsWith(Char)</c> (ordinal),
    ''' <c>New Random()</c> → <c>Random.Shared</c>, <c>Environment.TickCount</c> → <c>TickCount64</c>. 
    ''' <c>String.Join(",", values)</c> is not an entry: <c>String.Join(","c, values)</c> compiles and behaves the same on every 
    ''' target framework, so rule <see cref="DiagnosticIds.SingleCharacterString"/> reports it without a conditional block.
    ''' </remarks>
    Friend NotInheritable Class UtilApiMigrations

#Region " Constants "

        ''' <summary>
        ''' The symbol of APIs available since .NET Core 2.x.
        ''' </summary>
        Friend Const NetCoreCondition As String = "NETCOREAPP"

        ''' <summary>
        ''' The symbol of APIs available since .NET 5.
        ''' </summary>
        Friend Const Net5Condition As String = "NET5_0_OR_GREATER"

        ''' <summary>
        ''' The symbol of APIs available since .NET 6.
        ''' </summary>
        Friend Const Net6Condition As String = "NET6_0_OR_GREATER"

        ''' <summary>
        ''' The <see cref="StringComparison.Ordinal"/> numeric value.
        ''' </summary>
        Private Const OrdinalComparisonValue As Integer = 4

#End Region

#Region " Fields "

        ''' <summary>
        ''' The synchronous <c>System.IO.File</c> methods that have an asynchronous counterpart on modern .NET.
        ''' </summary>
        Private Shared ReadOnly FileMethodsWithAsyncCounterpart As String() = {
            "ReadAllText", "ReadAllBytes", "ReadAllLines", "WriteAllText", "WriteAllBytes", "WriteAllLines", "AppendAllText", "AppendAllLines"}

        ''' <summary>
        ''' The conditions, from the least to the most restrictive.
        ''' </summary>
        Private Shared ReadOnly ConditionOrder As String() = {UtilApiMigrations.NetCoreCondition, UtilApiMigrations.Net5Condition, UtilApiMigrations.Net6Condition}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilApiMigrations"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Tries to match a node against the equivalence table.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node (an invocation, comparison, cast, member access or <see langword="If"/> statement).
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
        ''' The migration, or <see langword="Nothing"/> if the node does not match any entry.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function TryMatch(node As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Select Case node.Kind()
                Case SyntaxKind.InvocationExpression
                    Dim invocation As InvocationExpressionSyntax = DirectCast(node, InvocationExpressionSyntax)
                    Dim method As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
                    If method Is Nothing Then
                        Return Nothing
                    End If

                    ' The first matcher that recognizes the invocation wins (they are evaluated lazily, in order).
                    Dim matchers As Func(Of ApiMigration)() = {
                        Function() UtilApiMigrations.MatchStringCharOverloads(invocation, method, semanticModel, cancellationToken),
                        Function() UtilApiMigrations.MatchStringSplit(invocation, method),
                        Function() UtilApiMigrations.MatchHexString(invocation, method, semanticModel, cancellationToken),
                        Function() UtilApiMigrations.MatchAsyncFileIo(invocation, method, semanticModel, cancellationToken)}
                    Return matchers.Select(Function(matcher As Func(Of ApiMigration)) matcher()).FirstOrDefault(Function(m As ApiMigration) m IsNot Nothing)

                Case SyntaxKind.GreaterThanOrEqualExpression, SyntaxKind.GreaterThanExpression, SyntaxKind.NotEqualsExpression,
                     SyntaxKind.LessThanExpression, SyntaxKind.LessThanOrEqualExpression, SyntaxKind.EqualsExpression
                    Return UtilApiMigrations.MatchIndexOfComparison(DirectCast(node, BinaryExpressionSyntax), semanticModel, cancellationToken)

                Case SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement
                    Return UtilApiMigrations.MatchTryAdd(node, semanticModel, cancellationToken)

                Case SyntaxKind.DirectCastExpression, SyntaxKind.CTypeExpression
                    Return UtilApiMigrations.MatchGenericEnumParse(DirectCast(node, CastExpressionSyntax), semanticModel, cancellationToken)

                Case SyntaxKind.SimpleMemberAccessExpression
                    Return UtilApiMigrations.MatchProcessMembers(DirectCast(node, MemberAccessExpressionSyntax), semanticModel, cancellationToken)

                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Gets the statement whose lines are duplicated in the conditional compilation block of a migration.
        ''' </summary>
        '''
        ''' <param name="migration">
        ''' The migration.
        ''' </param>
        '''
        ''' <returns>
        ''' The innermost statement that contains the migrated node, or <see langword="Nothing"/> if there is none.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="migration"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetHost(migration As ApiMigration) As StatementSyntax

            If migration Is Nothing Then
                Throw New ArgumentNullException(NameOf(migration))
            End If
            Return migration.Node.AncestorsAndSelf().OfType(Of StatementSyntax)().FirstOrDefault()
        End Function

        ''' <summary>
        ''' Determines whether the lines of a host statement can be wrapped in a conditional compilation block:
        ''' the statement must start and end its lines (only white-space or a trailing comment around it),
        ''' contain no preprocessor directive, and not be inside an existing <c>#If</c> block.
        ''' </summary>
        '''
        ''' <param name="host">
        ''' The host statement.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the lines can be wrapped; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="host"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function CanWrapHost(host As StatementSyntax) As Boolean

            If host Is Nothing Then
                Throw New ArgumentNullException(NameOf(host))
            End If

            Dim text As SourceText = host.SyntaxTree.GetText()
            Dim firstLine As TextLine = text.Lines.GetLineFromPosition(host.SpanStart)
            Dim lastLine As TextLine = text.Lines.GetLineFromPosition(host.Span.End)

            Dim before As String = text.ToString(TextSpan.FromBounds(firstLine.Start, host.SpanStart))
            Dim after As String = text.ToString(TextSpan.FromBounds(host.Span.End, lastLine.End)).Trim()
            ' The host must be alone on its lines (a trailing comment is allowed), contain no directive,
            ' and not already be inside a conditional compilation block.
            Dim isAloneOnItsLines As Boolean = before.Trim().Length = 0 AndAlso (after.Length = 0 OrElse after.StartsWith("'", StringComparison.Ordinal))
            Dim containsDirectives As Boolean = host.DescendantTrivia(descendIntoTrivia:=False).Any(Function(t As SyntaxTrivia) t.IsDirective AndAlso host.Span.Contains(t.Span))
            Return isAloneOnItsLines AndAlso Not containsDirectives AndAlso Not UtilThrowHelperGuards.IsInsideConditionalDirective(host)
        End Function

        ''' <summary>
        ''' Gets the most restrictive of two conditional compilation symbols.
        ''' </summary>
        '''
        ''' <param name="first">
        ''' The first symbol.
        ''' </param>
        '''
        ''' <param name="second">
        ''' The second symbol.
        ''' </param>
        '''
        ''' <returns>
        ''' The symbol that implies the other one.
        ''' </returns>
        Friend Shared Function MostRestrictive(first As String, second As String) As String

            Return If(Array.IndexOf(UtilApiMigrations.ConditionOrder, second) > Array.IndexOf(UtilApiMigrations.ConditionOrder, first), second, first)
        End Function

        ''' <summary>
        ''' Gets the single character of a one-character string literal argument.
        ''' </summary>
        '''
        ''' <param name="argument">
        ''' The argument.
        ''' </param>
        '''
        ''' <returns>
        ''' The Visual Basic character literal (e.g. <c>"x"c</c>), or <see langword="Nothing"/> if the argument is not such a literal.
        ''' </returns>
        Friend Shared Function GetCharLiteral(argument As ArgumentSyntax) As String

            Dim simple As SimpleArgumentSyntax = TryCast(argument, SimpleArgumentSyntax)
            If simple Is Nothing OrElse simple.IsNamed OrElse Not simple.Expression.IsKind(SyntaxKind.StringLiteralExpression) Then
                Return Nothing
            End If

            Dim value As String = DirectCast(simple.Expression, LiteralExpressionSyntax).Token.ValueText
            Return If(value.Length <> 1 OrElse Char.IsControl(value(0)) OrElse Char.IsSurrogate(value(0)), Nothing, If(value = """", """""""""c", $"""{value}""c"))
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Matches <c>s.Contains("x")</c>, <c>s.StartsWith("x", Ordinal)</c> and <c>s.EndsWith("x", Ordinal)</c>.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The invoked method.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchStringCharOverloads(invocation As InvocationExpressionSyntax, method As IMethodSymbol,
                                                         semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            If method.ContainingType.SpecialType <> SpecialType.System_String OrElse invocation.ArgumentList Is Nothing OrElse
               method.Parameters.Length = 0 OrElse method.Parameters(0).Type.SpecialType <> SpecialType.System_String Then
                Return Nothing
            End If

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = invocation.ArgumentList.Arguments
            If arguments.Count = 0 Then
                Return Nothing
            End If
            Dim charLiteral As String = UtilApiMigrations.GetCharLiteral(arguments(0))
            If charLiteral Is Nothing Then
                Return Nothing
            End If

            Select Case method.Name
                Case NameOf(String.Contains)
                    If Not method.IsStatic AndAlso method.Parameters.Length = 1 Then
                        Return New ApiMigration(DirectCast(arguments(0), SimpleArgumentSyntax).Expression, charLiteral, UtilApiMigrations.NetCoreCondition,
                                                "String.Contains(String)", "String.Contains(Char)")
                    End If

                Case NameOf(String.StartsWith), NameOf(String.EndsWith)
                    ' Only the ordinal overload is equivalent (StartsWith(String) is culture-sensitive, StartsWith(Char) is ordinal).
                    If Not method.IsStatic AndAlso method.Parameters.Length = 2 AndAlso arguments.Count = 2 AndAlso
                       method.Parameters(1).Type.ToDisplayString().Equals("System.StringComparison", StringComparison.Ordinal) Then
                        Dim comparison As SimpleArgumentSyntax = TryCast(arguments(1), SimpleArgumentSyntax)
                        Dim constant As [Optional](Of Object) = If(comparison Is Nothing, Nothing, semanticModel.GetConstantValue(comparison.Expression, cancellationToken))
                        If constant.HasValue AndAlso TypeOf constant.Value Is Integer AndAlso DirectCast(constant.Value, Integer) = UtilApiMigrations.OrdinalComparisonValue Then
                            Return New ApiMigration(invocation.ArgumentList, $"({charLiteral})", UtilApiMigrations.NetCoreCondition,
                                                    $"String.{method.Name}(String, StringComparison.Ordinal)", $"String.{method.Name}(Char)")
                        End If
                    End If
            End Select
            Return Nothing
        End Function

        ''' <summary>
        ''' Matches <c>s.Split({"ab"}, options)</c> and <c>s.Split(New String() {"ab"}, count, options)</c> with a single separator.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The invoked method.
        ''' </param>
        '''
        ''' <returns>
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchStringSplit(invocation As InvocationExpressionSyntax, method As IMethodSymbol) As ApiMigration

            If method.ContainingType.SpecialType <> SpecialType.System_String OrElse Not method.Name.Equals(NameOf(String.Split), StringComparison.Ordinal) OrElse
               method.Parameters.Length < 2 OrElse invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> method.Parameters.Length Then
                Return Nothing
            End If

            Dim arrayType As IArrayTypeSymbol = TryCast(method.Parameters(0).Type, IArrayTypeSymbol)
            If arrayType Is Nothing OrElse arrayType.ElementType.SpecialType <> SpecialType.System_String Then
                Return Nothing
            End If

            Dim first As SimpleArgumentSyntax = TryCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax)
            If first Is Nothing OrElse first.IsNamed Then
                Return Nothing
            End If

            Dim initializer As CollectionInitializerSyntax = Nothing
            Select Case first.Expression.Kind()
                Case SyntaxKind.CollectionInitializer
                    initializer = DirectCast(first.Expression, CollectionInitializerSyntax)
                Case SyntaxKind.ArrayCreationExpression
                    initializer = DirectCast(first.Expression, ArrayCreationExpressionSyntax).Initializer
            End Select

            Return If(initializer Is Nothing OrElse initializer.Initializers.Count <> 1,
                      Nothing,
                      New ApiMigration(first.Expression, initializer.Initializers(0).WithoutTrivia().ToString(), UtilApiMigrations.NetCoreCondition,
                                       "String.Split(String(), ...)", "String.Split(String, ...)"))
        End Function

        ''' <summary>
        ''' Matches <c>BitConverter.ToString(bytes).Replace("-", "")</c> (or <c>String.Empty</c>).
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The invoked method.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchHexString(invocation As InvocationExpressionSyntax, method As IMethodSymbol,
                                               semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            If method.ContainingType.SpecialType <> SpecialType.System_String OrElse Not method.Name.Equals(NameOf(String.Replace), StringComparison.Ordinal) OrElse
               method.Parameters.Length <> 2 OrElse method.Parameters(0).Type.SpecialType <> SpecialType.System_String OrElse
               invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 2 Then
                Return Nothing
            End If

            Dim oldValue As [Optional](Of Object) = semanticModel.GetConstantValue(DirectCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression, cancellationToken)
            Dim newValueExpression As ExpressionSyntax = DirectCast(invocation.ArgumentList.Arguments(1), SimpleArgumentSyntax).Expression
            Dim newValue As [Optional](Of Object) = semanticModel.GetConstantValue(newValueExpression, cancellationToken)
            Dim emptyField As IFieldSymbol = TryCast(semanticModel.GetSymbolInfo(newValueExpression, cancellationToken).Symbol, IFieldSymbol)
            Dim newIsEmpty As Boolean = (newValue.HasValue AndAlso String.Empty.Equals(newValue.Value)) OrElse
                                        (emptyField IsNot Nothing AndAlso emptyField.ContainingType.SpecialType = SpecialType.System_String AndAlso
                                         emptyField.Name.Equals(NameOf(String.Empty), StringComparison.Ordinal))
            If Not oldValue.HasValue OrElse Not "-".Equals(oldValue.Value) OrElse Not newIsEmpty Then
                Return Nothing
            End If

            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(invocation.Expression, MemberAccessExpressionSyntax)
            Dim inner As InvocationExpressionSyntax = If(memberAccess Is Nothing, Nothing, TryCast(memberAccess.Expression, InvocationExpressionSyntax))
            Dim innerMethod As IMethodSymbol = If(inner Is Nothing, Nothing, TryCast(semanticModel.GetSymbolInfo(inner, cancellationToken).Symbol, IMethodSymbol))
            Return If(innerMethod Is Nothing OrElse Not innerMethod.Name.Equals("ToString", StringComparison.Ordinal) OrElse
               Not innerMethod.ContainingType.ToDisplayString().Equals("System.BitConverter", StringComparison.Ordinal) OrElse
               innerMethod.Parameters.Length <> 1 OrElse inner.ArgumentList.Arguments.Count <> 1,
                Nothing,
                New ApiMigration(invocation, $"Convert.ToHexString({inner.ArgumentList.Arguments(0).WithoutTrivia()})", UtilApiMigrations.Net5Condition,
                                    "BitConverter.ToString(Byte()).Replace(""-"", """")", "Convert.ToHexString(Byte())"))
        End Function

        ''' <summary>
        ''' Matches synchronous <c>System.IO.File</c> I/O inside an <see langword="Async"/> method.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The invoked method.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchAsyncFileIo(invocation As InvocationExpressionSyntax, method As IMethodSymbol,
                                                 semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            If Not method.ContainingType.ToDisplayString().Equals("System.IO.File", StringComparison.Ordinal) OrElse
               Not UtilApiMigrations.FileMethodsWithAsyncCounterpart.Contains(method.Name, StringComparer.Ordinal) OrElse
               invocation.ArgumentList Is Nothing Then
                Return Nothing
            End If

            Dim container As IMethodSymbol = TryCast(semanticModel.GetEnclosingSymbol(invocation.SpanStart, cancellationToken), IMethodSymbol)
            If container Is Nothing OrElse Not container.IsAsync OrElse
               invocation.Ancestors().Any(Function(a As SyntaxNode) TypeOf a Is SyncLockBlockSyntax OrElse TypeOf a Is QueryExpressionSyntax) Then
                Return Nothing ' Await is not allowed there.
            End If

            Dim callText As String = $"Await {invocation.Expression.WithoutTrivia()}Async{invocation.ArgumentList.WithoutTrivia()}.ConfigureAwait(False)"
            Dim parent As SyntaxNode = invocation.Parent
            Dim needsParentheses As Boolean = (TypeOf parent Is MemberAccessExpressionSyntax AndAlso DirectCast(parent, MemberAccessExpressionSyntax).Expression Is invocation) OrElse
                                              TypeOf parent Is ConditionalAccessExpressionSyntax OrElse
                                              (TypeOf parent Is InvocationExpressionSyntax AndAlso DirectCast(parent, InvocationExpressionSyntax).Expression Is invocation)

            Return New ApiMigration(invocation, If(needsParentheses, $"({callText})", callText), UtilApiMigrations.NetCoreCondition,
                                    $"File.{method.Name}", $"Await File.{method.Name}Async")
        End Function

        ''' <summary>
        ''' Matches <c>s.IndexOf(value, comparison) &gt;= 0</c> (and the equivalent negative forms) on strings.
        ''' </summary>
        '''
        ''' <param name="binary">
        ''' The comparison.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchIndexOfComparison(binary As BinaryExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            Dim kind As SyntaxKind = binary.Kind()
            Dim invocation As InvocationExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(binary.Left), InvocationExpressionSyntax)
            Dim other As ExpressionSyntax = binary.Right
            If invocation Is Nothing Then
                invocation = TryCast(UtilAnalyzerSyntax.SkipParentheses(binary.Right), InvocationExpressionSyntax)
                other = binary.Left
                ' Normalize "0 <= x" into "x >= 0".
                Select Case kind
                    Case SyntaxKind.LessThanOrEqualExpression : kind = SyntaxKind.GreaterThanOrEqualExpression
                    Case SyntaxKind.LessThanExpression : kind = SyntaxKind.GreaterThanExpression
                    Case SyntaxKind.GreaterThanOrEqualExpression : kind = SyntaxKind.LessThanOrEqualExpression
                    Case SyntaxKind.GreaterThanExpression : kind = SyntaxKind.LessThanExpression
                End Select
            End If
            If invocation Is Nothing OrElse invocation.ArgumentList Is Nothing Then
                Return Nothing
            End If

            Dim method As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(invocation.Expression, MemberAccessExpressionSyntax)
            If method Is Nothing OrElse memberAccess Is Nothing OrElse memberAccess.Expression Is Nothing OrElse method.IsStatic OrElse
               method.ContainingType.SpecialType <> SpecialType.System_String OrElse Not method.Name.Equals(NameOf(String.IndexOf), StringComparison.Ordinal) Then
                Return Nothing
            End If

            ' IndexOf(Char) and IndexOf(String, StringComparison) only: IndexOf(String) is culture-sensitive, Contains(String) is ordinal.
            Dim isCharOverload As Boolean = method.Parameters.Length = 1 AndAlso method.Parameters(0).Type.SpecialType = SpecialType.System_Char
            Dim isComparisonOverload As Boolean = method.Parameters.Length = 2 AndAlso method.Parameters(0).Type.SpecialType = SpecialType.System_String AndAlso
                                                  method.Parameters(1).Type.ToDisplayString().Equals("System.StringComparison", StringComparison.Ordinal)
            If Not (isCharOverload OrElse isComparisonOverload) OrElse invocation.ArgumentList.Arguments.Count <> method.Parameters.Length Then
                Return Nothing
            End If

            Dim constant As [Optional](Of Object) = semanticModel.GetConstantValue(other, cancellationToken)
            If Not constant.HasValue OrElse TypeOf constant.Value IsNot Integer Then
                Return Nothing
            End If
            Dim value As Integer = DirectCast(constant.Value, Integer)

            Dim isContains As Boolean
            If (kind = SyntaxKind.GreaterThanOrEqualExpression AndAlso value = 0) OrElse
               (kind = SyntaxKind.GreaterThanExpression AndAlso value = -1) OrElse
               (kind = SyntaxKind.NotEqualsExpression AndAlso value = -1) Then
                isContains = True
            ElseIf (kind = SyntaxKind.LessThanExpression AndAlso value = 0) OrElse
                   (kind = SyntaxKind.EqualsExpression AndAlso value = -1) Then
                isContains = False
            Else
                Return Nothing
            End If

            Dim callText As String = $"{memberAccess.Expression.WithoutTrivia()}.Contains{invocation.ArgumentList.WithoutTrivia()}"
            Dim replacement As String = callText
            If Not isContains Then
                replacement = If(TypeOf binary.Parent Is ExpressionSyntax, $"(Not {callText})", $"Not {callText}")
            End If

            Return New ApiMigration(binary, replacement, UtilApiMigrations.NetCoreCondition,
                                    "String.IndexOf(...) comparison", "String.Contains(...)")
        End Function

        ''' <summary>
        ''' Matches <c>If Not d.ContainsKey(k) Then d.Add(k, v)</c> on a <c>Dictionary(Of TKey, TValue)</c>.
        ''' </summary>
        '''
        ''' <param name="ifNode">
        ''' The <see langword="If"/> statement.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchTryAdd(ifNode As SyntaxNode, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            Dim condition As ExpressionSyntax
            Dim statements As SyntaxList(Of StatementSyntax)
            If ifNode.IsKind(SyntaxKind.MultiLineIfBlock) Then
                Dim block As MultiLineIfBlockSyntax = DirectCast(ifNode, MultiLineIfBlockSyntax)
                If block.ElseIfBlocks.Count <> 0 OrElse block.ElseBlock IsNot Nothing Then
                    Return Nothing
                End If
                condition = block.IfStatement.Condition
                statements = block.Statements
            Else
                Dim singleLine As SingleLineIfStatementSyntax = DirectCast(ifNode, SingleLineIfStatementSyntax)
                If singleLine.ElseClause IsNot Nothing Then
                    Return Nothing
                End If
                condition = singleLine.Condition
                statements = singleLine.Statements
            End If

            Dim notExpression As UnaryExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(condition), UnaryExpressionSyntax)
            Dim expressionStatement As ExpressionStatementSyntax = If(statements.Count = 1, TryCast(statements(0), ExpressionStatementSyntax), Nothing)
            If notExpression Is Nothing OrElse Not notExpression.IsKind(SyntaxKind.NotExpression) OrElse expressionStatement Is Nothing Then
                Return Nothing
            End If

            Dim containsCall As InvocationExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(notExpression.Operand), InvocationExpressionSyntax)
            Dim addCall As InvocationExpressionSyntax = TryCast(expressionStatement.Expression, InvocationExpressionSyntax)
            If containsCall Is Nothing OrElse addCall Is Nothing OrElse containsCall.ArgumentList Is Nothing OrElse addCall.ArgumentList Is Nothing OrElse
               containsCall.ArgumentList.Arguments.Count <> 1 OrElse addCall.ArgumentList.Arguments.Count <> 2 Then
                Return Nothing
            End If

            Dim containsMethod As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(containsCall, cancellationToken).Symbol, IMethodSymbol)
            Dim addMethod As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(addCall, cancellationToken).Symbol, IMethodSymbol)
            Dim containsAccess As MemberAccessExpressionSyntax = TryCast(containsCall.Expression, MemberAccessExpressionSyntax)
            Dim addAccess As MemberAccessExpressionSyntax = TryCast(addCall.Expression, MemberAccessExpressionSyntax)
            If containsMethod Is Nothing OrElse addMethod Is Nothing OrElse containsAccess Is Nothing OrElse addAccess Is Nothing OrElse
               containsAccess.Expression Is Nothing OrElse addAccess.Expression Is Nothing OrElse
               Not containsMethod.Name.Equals("ContainsKey", StringComparison.Ordinal) OrElse Not addMethod.Name.Equals("Add", StringComparison.Ordinal) Then
                Return Nothing
            End If

            Dim dictionaryType As INamedTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2")
            If Not SymbolEqualityComparer.Default.Equals(containsMethod.ContainingType.OriginalDefinition, dictionaryType) OrElse
               Not SymbolEqualityComparer.Default.Equals(addMethod.ContainingType.OriginalDefinition, dictionaryType) Then
                Return Nothing
            End If

            ' Same dictionary and same key, compared textually (no guessing about side effects).
            Dim receiver As String = containsAccess.Expression.WithoutTrivia().ToString()
            Dim key As String = containsCall.ArgumentList.Arguments(0).WithoutTrivia().ToString()
            Return If(Not receiver.Equals(addAccess.Expression.WithoutTrivia().ToString(), StringComparison.Ordinal) OrElse
               Not key.Equals(addCall.ArgumentList.Arguments(0).WithoutTrivia().ToString(), StringComparison.Ordinal),
                Nothing,
                New ApiMigration(ifNode, $"{receiver}.TryAdd({key}, {addCall.ArgumentList.Arguments(1).WithoutTrivia()})", UtilApiMigrations.NetCoreCondition,
                                    "If Not Dictionary.ContainsKey(...) Then Dictionary.Add(...)", "Dictionary.TryAdd(...)"))
        End Function

        ''' <summary>
        ''' Matches <c>DirectCast([Enum].Parse(GetType(T), value), T)</c> (also <c>CType</c>, and the <c>ignoreCase</c> overload).
        ''' </summary>
        '''
        ''' <param name="cast">
        ''' The cast expression.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchGenericEnumParse(cast As CastExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            Dim invocation As InvocationExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(cast.Expression), InvocationExpressionSyntax)
            Dim method As IMethodSymbol = If(invocation Is Nothing, Nothing, TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol))
            If method Is Nothing OrElse Not method.Name.Equals("Parse", StringComparison.Ordinal) OrElse
               method.ContainingType.SpecialType <> SpecialType.System_Enum OrElse method.IsGenericMethod OrElse
               invocation.ArgumentList.Arguments.Count < 2 Then
                Return Nothing
            End If

            Dim typeArgument As SimpleArgumentSyntax = TryCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax)
            Dim getTypeExpression As GetTypeExpressionSyntax = If(typeArgument Is Nothing, Nothing, TryCast(typeArgument.Expression, GetTypeExpressionSyntax))
            If getTypeExpression Is Nothing Then
                Return Nothing
            End If

            Dim parsedType As ITypeSymbol = semanticModel.GetTypeInfo(getTypeExpression.Type, cancellationToken).Type
            Dim castType As ITypeSymbol = semanticModel.GetTypeInfo(cast.Type, cancellationToken).Type
            If parsedType Is Nothing OrElse parsedType.TypeKind <> TypeKind.Enum OrElse Not SymbolEqualityComparer.Default.Equals(parsedType, castType) Then
                Return Nothing
            End If

            Dim remaining As String = String.Join(", ", invocation.ArgumentList.Arguments.Skip(1).Select(Function(a As ArgumentSyntax) a.WithoutTrivia().ToString()))
            Return New ApiMigration(cast, $"[Enum].Parse(Of {getTypeExpression.Type.WithoutTrivia()})({remaining})", UtilApiMigrations.NetCoreCondition,
                                    "[Enum].Parse(Type, String)", "[Enum].Parse(Of TEnum)(String)")
        End Function

        ''' <summary>
        ''' Matches <c>Process.GetCurrentProcess().Id</c> and <c>Process.GetCurrentProcess().MainModule.FileName</c>.
        ''' </summary>
        '''
        ''' <param name="memberAccess">
        ''' The member access.
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
        ''' The migration, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function MatchProcessMembers(memberAccess As MemberAccessExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As ApiMigration

            If memberAccess.Expression Is Nothing Then
                Return Nothing
            End If

            Dim name As String = memberAccess.Name.Identifier.ValueText
            Dim processCall As ExpressionSyntax
            Dim replacement As String
            Dim condition As String
            Dim original As String

            If name.Equals("Id", StringComparison.Ordinal) Then
                processCall = memberAccess.Expression
                replacement = "Environment.ProcessId"
                condition = UtilApiMigrations.Net5Condition
                original = "Process.GetCurrentProcess().Id"

            ElseIf name.Equals("FileName", StringComparison.Ordinal) Then
                Dim mainModule As MemberAccessExpressionSyntax = TryCast(memberAccess.Expression, MemberAccessExpressionSyntax)
                If mainModule Is Nothing OrElse mainModule.Expression Is Nothing OrElse
                   Not mainModule.Name.Identifier.ValueText.Equals("MainModule", StringComparison.Ordinal) Then
                    Return Nothing
                End If
                processCall = mainModule.Expression
                replacement = "Environment.ProcessPath"
                condition = UtilApiMigrations.Net6Condition
                original = "Process.GetCurrentProcess().MainModule.FileName"

            Else
                Return Nothing
            End If

            Dim invocation As InvocationExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(processCall), InvocationExpressionSyntax)
            Dim method As IMethodSymbol = If(invocation Is Nothing, Nothing, TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol))
            Return If(method Is Nothing OrElse Not method.Name.Equals("GetCurrentProcess", StringComparison.Ordinal) OrElse
               Not method.ContainingType.ToDisplayString().Equals("System.Diagnostics.Process", StringComparison.Ordinal),
                Nothing,
                New ApiMigration(memberAccess, replacement, condition, original, replacement))
        End Function

#End Region

    End Class

End Namespace

#End Region
