#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " TryGetValueAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>map.ContainsKey(key)</c> followed by reads of <c>map(key)</c> that only run when the key exists (rule
    ''' <see cref="DiagnosticIds.ContainsKeyThenIndexer"/>; equivalent of CA1854 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>ContainsKey</c> and each read of the indexer look the key up again; <c>TryGetValue</c> looks it up once and returns the value.
    ''' The call can be the condition, or a part of the condition (with <see langword="Not"/>, <see langword="AndAlso"/>,
    ''' <see langword="OrElse"/> and parentheses), of an <see langword="If"/> or <see langword="ElseIf"/> statement or of an
    ''' <c>If(condition, x, y)</c> expression. The reads are searched where the key is known to exist: the branch that the condition
    ''' selects (the <see langword="Else"/> part for <c>Not map.ContainsKey(key)</c>), the operands that <see langword="AndAlso"/> and
    ''' <see langword="OrElse"/> evaluate after the call, and the statements that follow a guard clause
    ''' (<c>If Not map.ContainsKey(key) Then Return ...</c>).
    ''' </para>
    ''' <para>
    ''' The dictionary must be a variable, parameter, field or property (optionally qualified with <see langword="Me"/>), and the key a
    ''' constant or one of those; in those regions, the dictionary may only be read with that key, the key must not be assigned, and no
    ''' read may be inside a lambda (which could run after the dictionary changes). The type must have <c>TryGetValue(key, ByRef value)</c>.
    ''' </para>
    ''' <para>
    ''' The code fix declares the value before the statement and uses it: <c>Dim value As TValue = Nothing</c>,
    ''' <c>map.TryGetValue(key, value)</c> in place of the call, and <c>value</c> in place of each read. The name is <c>value</c>, or
    ''' <c>value1</c>, <c>value2</c>... when the method already uses it or contains several <c>ContainsKey</c> calls; the fix is not
    ''' offered when the statement is the body of a single-line <see langword="If"/> or lambda, or when the expression is not in a
    ''' statement of a block.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class TryGetValueAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ContainsKeyThenIndexer)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TryGetValueAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf TryGetValueAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis for .NET Framework compilations.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilAnalyzerSemantic.IsNetFramework(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in rule CA1854 reports the same code.
            End If

            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf TryGetValueAnalyzer.AnalyzeInvocation),
                                             SyntaxKind.InvocationExpression)
        End Sub

        ''' <summary>
        ''' Reports a call to <c>ContainsKey</c> whose result guards reads of the indexer with the same key.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As SyntaxNodeAnalysisContext)

            Dim check As InvocationExpressionSyntax = DirectCast(context.Node, InvocationExpressionSyntax)
            Dim access As MemberAccessExpressionSyntax = TryCast(check.Expression, MemberAccessExpressionSyntax)
            If access Is Nothing OrElse access.Expression Is Nothing OrElse
               Not access.Name.Identifier.ValueText.Equals("ContainsKey", StringComparison.OrdinalIgnoreCase) OrElse
               check.ArgumentList Is Nothing OrElse check.ArgumentList.Arguments.Count <> 1 OrElse
               TypeOf check.ArgumentList.Arguments(0) IsNot SimpleArgumentSyntax OrElse check.ArgumentList.Arguments(0).IsNamed Then
                Exit Sub
            End If

            Dim regions As New List(Of SyntaxNode)()
            Dim anchor As StatementSyntax = Nothing
            If Not TryGetValueAnalyzer.TryFindRegions(check, regions, anchor) Then
                Exit Sub
            End If

            Dim model As SemanticModel = context.SemanticModel
            Dim key As ExpressionSyntax = check.ArgumentList.Arguments(0).GetExpression()
            Dim dictionary As ISymbol = TryGetValueAnalyzer.GetReferencedSymbol(access.Expression, model, context)
            Dim keySymbol As ISymbol = TryGetValueAnalyzer.GetReferencedSymbol(key, model, context)
            Dim containsKey As IMethodSymbol = TryCast(model.GetSymbolInfo(check, context.CancellationToken).Symbol, IMethodSymbol)
            If dictionary Is Nothing OrElse (keySymbol Is Nothing AndAlso Not model.GetConstantValue(key, context.CancellationToken).HasValue) OrElse
               containsKey Is Nothing OrElse containsKey.Parameters.Length <> 1 OrElse containsKey.ReturnType.SpecialType <> SpecialType.System_Boolean Then
                Exit Sub
            End If

            Dim dictionaryType As ITypeSymbol = model.GetTypeInfo(access.Expression, context.CancellationToken).Type
            Dim tryGetValue As IMethodSymbol = If(dictionaryType Is Nothing, Nothing,
                model.LookupSymbols(check.SpanStart, container:=dictionaryType, name:="TryGetValue").OfType(Of IMethodSymbol)().FirstOrDefault(
                    Function(m As IMethodSymbol) Not m.IsStatic AndAlso m.Parameters.Length = 2 AndAlso m.Parameters(1).RefKind <> RefKind.None AndAlso
                                                 m.ReturnType.SpecialType = SpecialType.System_Boolean AndAlso
                                                 SymbolEqualityComparer.Default.Equals(m.Parameters(0).Type, containsKey.Parameters(0).Type)))
            If tryGetValue Is Nothing Then
                Exit Sub
            End If

            ' Every reference to the dictionary in the regions must be a read with the key; the key must not change, and no read 
            ' may be inside a lambda of the region (it could run after the dictionary changes).
            Dim valueType As ITypeSymbol = tryGetValue.Parameters(1).Type
            Dim reads As New List(Of InvocationExpressionSyntax)()
            For Each region As SyntaxNode In regions
                Dim regionReads As New List(Of InvocationExpressionSyntax)()
                If Not TryGetValueAnalyzer.CollectReads(region, dictionary, key, valueType, model, context, regionReads) OrElse
                   (keySymbol IsNot Nothing AndAlso TryGetValueAnalyzer.IsAssigned(region, keySymbol, model, context)) OrElse
                   regionReads.Exists(Function(r As InvocationExpressionSyntax) r.AncestorsAndSelf().TakeWhile(Function(a As SyntaxNode) a IsNot region).OfType(Of LambdaExpressionSyntax)().Any()) Then
                    Exit Sub
                End If
                reads.AddRange(regionReads)
            Next region
            If reads.Count = 0 Then
                Exit Sub
            End If

            Dim spans As New List(Of Location)()
            Dim actions As New List(Of FixReplacement)()
            Dim name As String = If(anchor Is Nothing, Nothing, TryGetValueAnalyzer.ChooseName(anchor, check, model))
            Dim typeName As String = If(anchor Is Nothing, Nothing, UtilSpeculativeBinding.GetTypeName(model, anchor.SpanStart, valueType))
            If name IsNot Nothing AndAlso typeName IsNot Nothing Then
                Dim tree As SyntaxTree = check.SyntaxTree
                Dim texts As New List(Of String)()
                Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(anchor.GetFirstToken())
                spans.Add(Location.Create(tree, New Text.TextSpan(anchor.SpanStart, 0)))
                texts.Add($"Dim {name} As {typeName} = Nothing{UtilAnalyzerSyntax.GetEndOfLine(tree)}{indentation}")
                spans.Add(check.GetLocation())
                texts.Add($"{access.Expression}.TryGetValue({key}, {name})")
                For Each read As InvocationExpressionSyntax In reads.OrderBy(Function(r As InvocationExpressionSyntax) r.SpanStart)
                    spans.Add(read.GetLocation())
                    texts.Add(name)
                Next read
                actions.Add(New FixReplacement("Use TryGetValue", "TryGetValue", texts.ToArray()))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.ContainsKeyThenIndexer, check.GetLocation(), spans, actions))
        End Sub

        ''' <summary>
        ''' Finds the code that only runs when a <c>ContainsKey</c> call returned <see langword="True"/>: the <see langword="If"/>, 
        ''' <see langword="ElseIf"/>, <see langword="Else"/> or <c>If(...)</c> branch that its condition selects, the operands that 
        ''' <see langword="AndAlso"/> and <see langword="OrElse"/> evaluate after it, and the statements that follow a guard clause 
        ''' (<c>If Not map.ContainsKey(key) Then Return</c>).
        ''' </summary>
        '''
        ''' <param name="check">
        ''' The <c>ContainsKey</c> call.
        ''' </param>
        '''
        ''' <param name="regions">
        ''' The list that receives the nodes (statements, blocks or expressions) in which the key is known to exist.
        ''' </param>
        '''
        ''' <param name="anchor">
        ''' When this method returns, the statement that contains the condition, before which the code fix declares the value; 
        ''' <see langword="Nothing"/> when no declaration can be inserted there (the body of a single-line <see langword="If"/> or 
        ''' lambda, a field initializer...).
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the call is the condition (or a part of the condition) of an <see langword="If"/> statement or 
        ''' expression, and some code runs only when the key exists; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function TryFindRegions(check As ExpressionSyntax, regions As List(Of SyntaxNode), ByRef anchor As StatementSyntax) As Boolean

            ' What the value of the current expression says about the key: 1 = it exists, -1 = it does not exist, 0 = unknown.
            Dim whenTrue As Integer = 1
            Dim whenFalse As Integer = -1
            Dim current As ExpressionSyntax = check
            Dim walking As Boolean = True
            While walking
                Dim parent As SyntaxNode = current.Parent
                Dim binary As BinaryExpressionSyntax = TryCast(parent, BinaryExpressionSyntax)
                Select Case True
                    Case TypeOf parent Is ParenthesizedExpressionSyntax
                        ' The same value.
                    Case parent.IsKind(SyntaxKind.NotExpression)
                        Dim swapped As Integer = whenTrue
                        whenTrue = whenFalse
                        whenFalse = swapped
                    Case binary IsNot Nothing AndAlso binary.IsKind(SyntaxKind.AndAlsoExpression)
                        ' The right operand runs when the left one is True; the whole is False when either one is False.
                        If binary.Left Is current AndAlso whenTrue = 1 Then
                            regions.Add(binary.Right)
                        End If
                        whenFalse = 0
                    Case binary IsNot Nothing AndAlso binary.IsKind(SyntaxKind.OrElseExpression)
                        ' The right operand runs when the left one is False; the whole is True when either one is True.
                        If binary.Left Is current AndAlso whenFalse = 1 Then
                            regions.Add(binary.Right)
                        End If
                        whenTrue = 0
                    Case Else
                        walking = False
                End Select
                If walking Then
                    current = DirectCast(parent, ExpressionSyntax)
                End If
            End While

            Dim holder As SyntaxNode = current.Parent
            Dim thenRegions As New List(Of SyntaxNode)()
            Dim elseRegions As New List(Of SyntaxNode)()
            Dim thenStatements As SyntaxList(Of StatementSyntax) = Nothing
            Dim hasElse As Boolean
            Dim ternary As TernaryConditionalExpressionSyntax = TryCast(holder, TernaryConditionalExpressionSyntax)
            Dim singleLine As SingleLineIfStatementSyntax = TryCast(holder, SingleLineIfStatementSyntax)
            Dim block As MultiLineIfBlockSyntax = TryCast(holder?.Parent, MultiLineIfBlockSyntax)
            Dim elseIfBlock As ElseIfBlockSyntax = TryCast(holder?.Parent, ElseIfBlockSyntax)
            Select Case True
                Case ternary IsNot Nothing AndAlso ternary.Condition Is current
                    thenRegions.Add(ternary.WhenTrue)
                    elseRegions.Add(ternary.WhenFalse)
                    hasElse = True
                    Dim statement As StatementSyntax = ternary.FirstAncestorOrSelf(Of StatementSyntax)()
                    Dim inLambda As Boolean = statement IsNot Nothing AndAlso
                                              ternary.Ancestors().TakeWhile(Function(a As SyntaxNode) a IsNot statement).OfType(Of LambdaExpressionSyntax)().Any()
                    anchor = If(inLambda, Nothing, statement) ' In a lambda of the statement, the declaration cannot go before it.
                Case singleLine IsNot Nothing AndAlso singleLine.Condition Is current
                    thenStatements = singleLine.Statements
                    thenRegions.AddRange(singleLine.Statements)
                    If singleLine.ElseClause IsNot Nothing Then
                        elseRegions.AddRange(singleLine.ElseClause.Statements)
                        hasElse = True
                    End If
                    anchor = singleLine
                Case TypeOf holder Is IfStatementSyntax AndAlso block IsNot Nothing
                    thenStatements = block.Statements
                    thenRegions.AddRange(block.Statements)
                    elseRegions.AddRange(block.ElseIfBlocks)
                    If block.ElseBlock IsNot Nothing Then
                        elseRegions.Add(block.ElseBlock)
                    End If
                    hasElse = elseRegions.Count > 0
                    anchor = block
                Case TypeOf holder Is ElseIfStatementSyntax AndAlso elseIfBlock IsNot Nothing
                    Dim outer As MultiLineIfBlockSyntax = DirectCast(elseIfBlock.Parent, MultiLineIfBlockSyntax)
                    thenRegions.AddRange(elseIfBlock.Statements)
                    elseRegions.AddRange(outer.ElseIfBlocks.Where(Function(b As ElseIfBlockSyntax) b.SpanStart > elseIfBlock.SpanStart))
                    If outer.ElseBlock IsNot Nothing Then
                        elseRegions.Add(outer.ElseBlock)
                    End If
                    hasElse = True
                    anchor = outer
                Case Else
                    Return False
            End Select

            If whenTrue = 1 Then
                regions.AddRange(thenRegions)
            End If
            If whenFalse = 1 Then
                regions.AddRange(elseRegions)

                ' A guard clause: "If Not map.ContainsKey(key) Then Return", the statements that follow it run with the key.
                If Not hasElse AndAlso thenStatements.Count > 0 AndAlso TryGetValueAnalyzer.IsExit(thenStatements(thenStatements.Count - 1)) Then
                    Dim siblings As SyntaxList(Of StatementSyntax) = UtilAnalyzerSyntax.GetBlockStatements(anchor.Parent)
                    Dim index As Integer = siblings.IndexOf(anchor)
                    If index >= 0 Then
                        regions.AddRange(siblings.Skip(index + 1))
                    End If
                End If
            End If

            ' The declaration can only be inserted before a statement of a block.
            If anchor IsNot Nothing AndAlso Not UtilAnalyzerSyntax.GetBlockStatements(anchor.Parent).Contains(anchor) Then
                anchor = Nothing
            End If
            Return regions.Count > 0
        End Function

        ''' <summary>
        ''' Determines whether a statement always leaves the block that contains it.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for <see langword="Return"/>, <see langword="Throw"/>, <see langword="Exit"/> and 
        ''' <see langword="Continue"/> statements; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsExit(statement As StatementSyntax) As Boolean

            Return TypeOf statement Is ReturnStatementSyntax OrElse TypeOf statement Is ThrowStatementSyntax OrElse
                   TypeOf statement Is ExitStatementSyntax OrElse TypeOf statement Is ContinueStatementSyntax
        End Function

        ''' <summary>
        ''' Gets the variable, parameter, field or property that an expression references.
        ''' </summary>
        '''
        ''' <param name="expression">
        ''' The expression: a name, optionally qualified with <see langword="Me"/> or <see langword="MyClass"/>.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <returns>
        ''' The symbol; <see langword="Nothing"/> for other expressions.
        ''' </returns>
        Private Shared Function GetReferencedSymbol(expression As ExpressionSyntax, model As SemanticModel, context As SyntaxNodeAnalysisContext) As ISymbol

            Dim bare As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(expression)
            Dim access As MemberAccessExpressionSyntax = TryCast(bare, MemberAccessExpressionSyntax)
            If access IsNot Nothing AndAlso (access.Expression.IsKind(SyntaxKind.MeExpression) OrElse access.Expression.IsKind(SyntaxKind.MyClassExpression)) Then
                bare = access.Name
            End If
            If TypeOf bare IsNot IdentifierNameSyntax Then
                Return Nothing
            End If

            Dim symbol As ISymbol = model.GetSymbolInfo(bare, context.CancellationToken).Symbol
            Select Case True
                Case TypeOf symbol Is ILocalSymbol, TypeOf symbol Is IParameterSymbol, TypeOf symbol Is IFieldSymbol,
                     TypeOf symbol Is IPropertySymbol AndAlso DirectCast(symbol, IPropertySymbol).Parameters.IsEmpty
                    Return symbol
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Gets the expressions that reference a symbol in a region: <c>name</c> or <c>Me.name</c>.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The region: a statement, a block or an expression.
        ''' </param>
        '''
        ''' <param name="symbol">
        ''' The symbol.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <returns>
        ''' The references; a <see langword="Nothing"/> item for a reference through another object (<c>other.name</c>) or a 
        ''' <see langword="With"/> block, whose target is unknown.
        ''' </returns>
        Private Shared Function GetReferences(node As SyntaxNode, symbol As ISymbol, model As SemanticModel, context As SyntaxNodeAnalysisContext) As List(Of ExpressionSyntax)

            Dim references As New List(Of ExpressionSyntax)()
            For Each identifier As IdentifierNameSyntax In node.DescendantNodesAndSelf().OfType(Of IdentifierNameSyntax)()
                If Not identifier.Identifier.ValueText.Equals(symbol.Name, StringComparison.OrdinalIgnoreCase) OrElse
                   Not SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(identifier, context.CancellationToken).Symbol, symbol) Then
                    Continue For
                End If

                Dim parentAccess As MemberAccessExpressionSyntax = TryCast(identifier.Parent, MemberAccessExpressionSyntax)
                If parentAccess Is Nothing OrElse parentAccess.Name IsNot identifier Then
                    references.Add(identifier)
                ElseIf parentAccess.Expression IsNot Nothing AndAlso
                       (parentAccess.Expression.IsKind(SyntaxKind.MeExpression) OrElse parentAccess.Expression.IsKind(SyntaxKind.MyClassExpression)) Then
                    references.Add(parentAccess)
                Else
                    references.Add(Nothing)
                End If
            Next identifier
            Return references
        End Function

        ''' <summary>
        ''' Collects the indexer reads of the dictionary with the key in a region.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The region: a statement, a block or an expression.
        ''' </param>
        '''
        ''' <param name="dictionary">
        ''' The dictionary variable, parameter, field or property.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The key expression of the <c>ContainsKey</c> call.
        ''' </param>
        '''
        ''' <param name="valueType">
        ''' The type of the values.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <param name="reads">
        ''' The list that receives the reads.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if every reference to the dictionary is a read of the indexer with the key; otherwise, 
        ''' <see langword="False"/>.
        ''' </returns>
        Private Shared Function CollectReads(node As SyntaxNode, dictionary As ISymbol, key As ExpressionSyntax, valueType As ITypeSymbol,
                                             model As SemanticModel, context As SyntaxNodeAnalysisContext, reads As List(Of InvocationExpressionSyntax)) As Boolean

            For Each reference As ExpressionSyntax In TryGetValueAnalyzer.GetReferences(node, dictionary, model, context)
                Dim read As InvocationExpressionSyntax = TryCast(reference?.Parent, InvocationExpressionSyntax)
                If read Is Nothing OrElse read.Expression IsNot reference OrElse read.ArgumentList Is Nothing OrElse read.ArgumentList.Arguments.Count <> 1 OrElse
                   TypeOf read.ArgumentList.Arguments(0) IsNot SimpleArgumentSyntax OrElse read.ArgumentList.Arguments(0).IsNamed OrElse
                   Not UtilAnalyzerSyntax.AreEquivalentExpressions(read.ArgumentList.Arguments(0).GetExpression(), key) Then
                    Return False
                End If

                Dim indexer As IPropertyReferenceOperation = TryCast(model.GetOperation(read, context.CancellationToken), IPropertyReferenceOperation)
                If indexer Is Nothing OrElse indexer.Property.Parameters.Length <> 1 OrElse
                   Not SymbolEqualityComparer.Default.Equals(indexer.Property.Type, valueType) OrElse TryGetValueAnalyzer.IsWritten(indexer) Then
                    Return False
                End If
                reads.Add(read)
            Next reference
            Return True
        End Function

        ''' <summary>
        ''' Determines whether a region assigns a variable, parameter, field or property, or passes it <see langword="ByRef"/>.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The region: a statement, a block or an expression.
        ''' </param>
        '''
        ''' <param name="symbol">
        ''' The symbol.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the region may change the value; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAssigned(node As SyntaxNode, symbol As ISymbol, model As SemanticModel, context As SyntaxNodeAnalysisContext) As Boolean

            For Each reference As ExpressionSyntax In TryGetValueAnalyzer.GetReferences(node, symbol, model, context)
                If reference Is Nothing Then
                    Continue For ' Another object: its member is not the key.
                End If
                Dim operation As IOperation = model.GetOperation(reference, context.CancellationToken)
                If operation Is Nothing OrElse TryGetValueAnalyzer.IsWritten(operation) Then
                    Return True
                End If
            Next reference
            Return False
        End Function

        ''' <summary>
        ''' Determines whether a reference is the target of an assignment or a <see langword="ByRef"/> argument.
        ''' </summary>
        '''
        ''' <param name="operation">
        ''' The reference.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the reference is written; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsWritten(operation As IOperation) As Boolean

            Dim parent As IOperation = operation.Parent
            Dim assignment As IAssignmentOperation = TryCast(parent, IAssignmentOperation)
            Dim argument As IArgumentOperation = TryCast(parent, IArgumentOperation)
            Return (assignment IsNot Nothing AndAlso assignment.Target Is operation) OrElse
                   (argument IsNot Nothing AndAlso argument.Parameter IsNot Nothing AndAlso argument.Parameter.RefKind <> RefKind.None)
        End Function

        ''' <summary>
        ''' Chooses the name of the variable that receives the value: <c>value</c>, <c>value1</c>, <c>value2</c>...
        ''' </summary>
        '''
        ''' <param name="anchor">
        ''' The statement before which the variable is declared.
        ''' </param>
        '''
        ''' <param name="check">
        ''' The reported <c>ContainsKey</c> call.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <returns>
        ''' The name; <see langword="Nothing"/> if the statement is not inside a method or lambda.
        ''' </returns>
        '''
        ''' <remarks>
        ''' The number starts at the position of the call among the <c>ContainsKey</c> calls of the method, and skips ahead by their 
        ''' count when the name is in use, so that the diagnostics of a method never choose the same name and Fix All declares a 
        ''' different variable for each one.
        ''' </remarks>
        Private Shared Function ChooseName(anchor As SyntaxNode, check As SyntaxNode, model As SemanticModel) As String

            Dim scope As SyntaxNode = If(anchor.FirstAncestorOrSelf(Of MethodBlockBaseSyntax)(), DirectCast(anchor.Ancestors().OfType(Of LambdaExpressionSyntax)().LastOrDefault(), SyntaxNode))
            If scope Is Nothing Then
                Return Nothing
            End If

            Dim used As New HashSet(Of String)(scope.DescendantTokens().Where(Function(t As SyntaxToken) t.IsKind(SyntaxKind.IdentifierToken)).
                                                   Select(Function(t As SyntaxToken) t.ValueText), StringComparer.OrdinalIgnoreCase)
            Dim checks As List(Of SyntaxNode) = scope.DescendantNodes().Where(AddressOf TryGetValueAnalyzer.IsContainsKeyCall).ToList()
            Dim number As Integer = checks.IndexOf(check)
            For attempt As Integer = 0 To 99
                Dim candidate As String = If(number = 0, "value", $"value{number}")
                Dim inScope As Boolean = model.LookupSymbols(anchor.SpanStart, name:=candidate).Any(
                    Function(s As ISymbol) s.Kind = SymbolKind.Local OrElse s.Kind = SymbolKind.Parameter OrElse s.Kind = SymbolKind.RangeVariable)
                If number >= 0 AndAlso Not used.Contains(candidate) AndAlso Not inScope Then
                    Return candidate
                End If
                number += checks.Count
            Next attempt
            Return Nothing
        End Function

        ''' <summary>
        ''' Determines whether a node is a call to a method named <c>ContainsKey</c>.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for <c>x.ContainsKey(...)</c>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsContainsKeyCall(node As SyntaxNode) As Boolean

            Dim access As MemberAccessExpressionSyntax = TryCast(TryCast(node, InvocationExpressionSyntax)?.Expression, MemberAccessExpressionSyntax)
            Return access IsNot Nothing AndAlso access.Name.Identifier.ValueText.Equals("ContainsKey", StringComparison.OrdinalIgnoreCase)
        End Function

#End Region

    End Class

End Namespace

#End Region
