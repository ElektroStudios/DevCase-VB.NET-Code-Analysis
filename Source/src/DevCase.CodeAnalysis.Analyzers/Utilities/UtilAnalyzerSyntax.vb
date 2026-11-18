#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " UtilAnalyzerSyntax "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides purely syntactic helper methods shared by the DevCase analyzers and code fix providers.
    ''' </summary>
    Friend NotInheritable Class UtilAnalyzerSyntax

#Region " Constants "

        ''' <summary>
        ''' The identifier name of the <c>ConfigureAwait</c> method.
        ''' </summary>
        Friend Const ConfigureAwaitIdentifierName As String = "ConfigureAwait"

        ''' <summary>
        ''' The text that starts a Visual Basic XML documentation comment line.
        ''' </summary>
        Friend Const DocumentationCommentPrefix As String = "'''"

        ''' <summary>
        ''' The carriage-return character.
        ''' </summary>
        Friend Const CarriageReturn As Char = ChrW(13)

        ''' <summary>
        ''' The line-feed character.
        ''' </summary>
        Friend Const LineFeed As Char = ChrW(10)

        ''' <summary>
        ''' The horizontal tab character.
        ''' </summary>
        Friend Const Tab As Char = ChrW(9)

        ''' <summary>
        ''' The <c>CRLF</c> end-of-line sequence.
        ''' </summary>
        Friend Const CrLf As String = CarriageReturn & LineFeed

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilAnalyzerSyntax"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether the given awaited expression already ends with a <c>ConfigureAwait</c> invocation.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Mirrors the original <c>ConfigureAwaitInjector.IsAlreadyConfigured</c> logic, extended to also recognize
        ''' null-conditional invocations such as <c>obj?.RunAsync().ConfigureAwait(False)</c>.
        ''' </remarks>
        '''
        ''' <param name="expr">
        ''' The awaited expression to evaluate.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if <c>ConfigureAwait</c> is already present; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="expr"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsAlreadyConfigured(expr As ExpressionSyntax) As Boolean

            If expr Is Nothing Then
                Throw New ArgumentNullException(NameOf(expr))
            End If

            Dim currentExpr As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(expr)

            ' obj?.RunAsync().ConfigureAwait(False) -> inspect the WhenNotNull part.
            Dim conditionalAccess As ConditionalAccessExpressionSyntax = TryCast(currentExpr, ConditionalAccessExpressionSyntax)
            While conditionalAccess IsNot Nothing
                currentExpr = conditionalAccess.WhenNotNull
                conditionalAccess = TryCast(currentExpr, ConditionalAccessExpressionSyntax)
            End While

            Dim invocation As InvocationExpressionSyntax = TryCast(currentExpr, InvocationExpressionSyntax)
            If invocation Is Nothing Then
                Return False
            End If

            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(invocation.Expression, MemberAccessExpressionSyntax)
            Return (memberAccess IsNot Nothing) AndAlso
                   memberAccess.Name.Identifier.ValueText.Equals(UtilAnalyzerSyntax.ConfigureAwaitIdentifierName, StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Removes any number of enclosing parentheses from an expression.
        ''' </summary>
        '''
        ''' <param name="expr">
        ''' The expression to unwrap.
        ''' </param>
        '''
        ''' <returns>
        ''' The innermost non-parenthesized expression.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="expr"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function SkipParentheses(expr As ExpressionSyntax) As ExpressionSyntax

            If expr Is Nothing Then
                Throw New ArgumentNullException(NameOf(expr))
            End If

            Dim currentExpr As ExpressionSyntax = expr
            While TypeOf currentExpr Is ParenthesizedExpressionSyntax
                currentExpr = DirectCast(currentExpr, ParenthesizedExpressionSyntax).Expression
            End While
            Return currentExpr
        End Function

        ''' <summary>
        ''' Determines whether an expression must be wrapped in parentheses before a member access
        ''' (e.g. <c>.ConfigureAwait(False)</c>) can be appended to it without changing its meaning.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Appending a member access to a null-conditional expression (<c>a?.RunAsync()</c>) would bind the new
        ''' member inside the conditional chain, and appending it to an operator expression would bind it to the
        ''' right-most operand only. Both cases require parentheses.
        ''' </remarks>
        '''
        ''' <param name="expr">
        ''' The expression to evaluate.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if parentheses are required; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="expr"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function RequiresParenthesesForMemberAccess(expr As ExpressionSyntax) As Boolean

            If expr Is Nothing Then
                Throw New ArgumentNullException(NameOf(expr))
            End If

            Select Case expr.Kind()
                Case SyntaxKind.IdentifierName,
                     SyntaxKind.GenericName,
                     SyntaxKind.QualifiedName,
                     SyntaxKind.GlobalName,
                     SyntaxKind.SimpleMemberAccessExpression,
                     SyntaxKind.InvocationExpression,
                     SyntaxKind.ParenthesizedExpression,
                     SyntaxKind.TernaryConditionalExpression,
                     SyntaxKind.BinaryConditionalExpression,
                     SyntaxKind.CTypeExpression,
                     SyntaxKind.DirectCastExpression,
                     SyntaxKind.TryCastExpression,
                     SyntaxKind.PredefinedCastExpression,
                     SyntaxKind.MeExpression,
                     SyntaxKind.MyBaseExpression,
                     SyntaxKind.MyClassExpression

                    Return False

                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' Determines whether a syntax node is located inside a <see langword="NameOf"/> expression.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The syntax node to evaluate.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the node is inside a <see langword="NameOf"/> expression; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsInsideNameOf(node As SyntaxNode) As Boolean

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Return node.Ancestors().Any(Function(n As SyntaxNode) n.IsKind(SyntaxKind.NameOfExpression))
        End Function

        ''' <summary>
        ''' Gets the structured XML documentation comment attached to a declaration, if any.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The declaration node.
        ''' </param>
        '''
        ''' <returns>
        ''' The last <see cref="DocumentationCommentTriviaSyntax"/> in the leading trivia of the declaration,
        ''' or <see langword="Nothing"/> if there is none (or if the tree was parsed with <see cref="DocumentationMode.None"/>).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetDocumentationComment(node As SyntaxNode) As DocumentationCommentTriviaSyntax

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim result As DocumentationCommentTriviaSyntax = Nothing
            For Each trivia As SyntaxTrivia In node.GetLeadingTrivia()
                If trivia.IsKind(SyntaxKind.DocumentationCommentTrivia) Then
                    result = DirectCast(trivia.GetStructure(), DocumentationCommentTriviaSyntax)
                End If
            Next trivia
            Return result
        End Function

        ''' <summary>
        ''' Determines whether a declaration has an XML documentation comment.
        ''' </summary>
        '''
        ''' <remarks>
        ''' When the syntax tree was parsed with <see cref="DocumentationMode.None"/> (e.g. command-line builds without
        ''' <c>GenerateDocumentationFile</c>), documentation comments are exposed as plain comment trivia;
        ''' those are recognized by their <c>'''</c> prefix.
        ''' </remarks>
        '''
        ''' <param name="node">
        ''' The declaration node.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the declaration is documented; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function HasDocumentationComment(node As SyntaxNode) As Boolean

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            For Each trivia As SyntaxTrivia In node.GetLeadingTrivia()
                If trivia.IsKind(SyntaxKind.DocumentationCommentTrivia) Then
                    Return True
                End If
                If trivia.IsKind(SyntaxKind.CommentTrivia) AndAlso
                   trivia.ToString().StartsWith(UtilAnalyzerSyntax.DocumentationCommentPrefix, StringComparison.Ordinal) Then
                    Return True
                End If
            Next trivia
            Return False
        End Function

        ''' <summary>
        ''' Determines whether an XML documentation comment delegates its content through
        ''' an <c>&lt;inheritdoc/&gt;</c> or <c>&lt;include/&gt;</c> element.
        ''' </summary>
        '''
        ''' <param name="docComment">
        ''' The documentation comment to inspect.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the comment inherits or includes its documentation; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="docComment"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsInheritedOrIncludedDocumentation(docComment As DocumentationCommentTriviaSyntax) As Boolean

            If docComment Is Nothing Then
                Throw New ArgumentNullException(NameOf(docComment))
            End If

            For Each node As SyntaxNode In docComment.DescendantNodes()
                Dim elementName As String = UtilAnalyzerSyntax.GetXmlElementName(node)
                If elementName IsNot Nothing AndAlso
                   (elementName.Equals("inheritdoc", StringComparison.OrdinalIgnoreCase) OrElse
                    elementName.Equals("include", StringComparison.OrdinalIgnoreCase)) Then
                    Return True
                End If
            Next node
            Return False
        End Function

        ''' <summary>
        ''' Gets the local name of an XML element node (either <see cref="XmlElementSyntax"/> or <see cref="XmlEmptyElementSyntax"/>).
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node to inspect.
        ''' </param>
        '''
        ''' <returns>
        ''' The local element name, or <see langword="Nothing"/> if the node is not an XML element.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetXmlElementName(node As SyntaxNode) As String

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim nameNode As XmlNodeSyntax

            Select Case node.Kind()
                Case SyntaxKind.XmlElement
                    nameNode = DirectCast(node, XmlElementSyntax).StartTag.Name
                Case SyntaxKind.XmlEmptyElement
                    nameNode = DirectCast(node, XmlEmptyElementSyntax).Name
                Case Else
                    Return Nothing
            End Select

            Dim xmlName As XmlNameSyntax = TryCast(nameNode, XmlNameSyntax)
            Return xmlName?.LocalName.ValueText
        End Function

        ''' <summary>
        ''' Gets the value of the <c>name</c> attribute of an XML element node.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The XML element node (either <see cref="XmlElementSyntax"/> or <see cref="XmlEmptyElementSyntax"/>).
        ''' </param>
        '''
        ''' <returns>
        ''' The trimmed attribute value, or <see langword="Nothing"/> if the attribute is absent.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetXmlNameAttributeValue(node As SyntaxNode) As String

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim attributes As SyntaxList(Of XmlNodeSyntax)
            Select Case node.Kind()
                Case SyntaxKind.XmlElement
                    attributes = DirectCast(node, XmlElementSyntax).StartTag.Attributes
                Case SyntaxKind.XmlEmptyElement
                    attributes = DirectCast(node, XmlEmptyElementSyntax).Attributes
                Case Else
                    Return Nothing
            End Select

            For Each attribute As XmlNodeSyntax In attributes
                Select Case attribute.Kind()

                    Case SyntaxKind.XmlNameAttribute
                        Dim nameAttribute As XmlNameAttributeSyntax = DirectCast(attribute, XmlNameAttributeSyntax)
                        Return nameAttribute.Reference.Identifier.ValueText.Trim()

                    Case SyntaxKind.XmlAttribute
                        Dim xmlAttribute As XmlAttributeSyntax = DirectCast(attribute, XmlAttributeSyntax)
                        Dim attributeName As XmlNameSyntax = TryCast(xmlAttribute.Name, XmlNameSyntax)
                        If attributeName IsNot Nothing AndAlso
                           attributeName.LocalName.ValueText.Equals("name", StringComparison.OrdinalIgnoreCase) Then

                            Dim valueString As XmlStringSyntax = TryCast(xmlAttribute.Value, XmlStringSyntax)
                            Return If(valueString Is Nothing,
                                      String.Empty,
                                      String.Concat(valueString.TextTokens.Select(Function(t As SyntaxToken) t.ValueText)).Trim())
                        End If

                End Select
            Next attribute
            Return Nothing
        End Function

        ''' <summary>
        ''' Determines whether an XML element has no meaningful content (only white-space or line-break tokens).
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The XML element node (either <see cref="XmlElementSyntax"/> or <see cref="XmlEmptyElementSyntax"/>).
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the element is empty; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="node"/> is not an XML element.
        ''' </exception>
        Friend Shared Function IsXmlElementEmpty(node As SyntaxNode) As Boolean

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            If node.IsKind(SyntaxKind.XmlEmptyElement) Then
                Return True
            End If

            Dim element As XmlElementSyntax = TryCast(node, XmlElementSyntax)
            If element Is Nothing Then
                Throw New ArgumentException("The node is not an XML element.", NameOf(node))
            End If

            For Each contentNode As XmlNodeSyntax In element.Content
                Dim text As XmlTextSyntax = TryCast(contentNode, XmlTextSyntax)
                If text Is Nothing Then
                    ' Child elements (<see/>, <c>, <para>, ...) are meaningful content.
                    Return False
                End If

                For Each token As SyntaxToken In text.TextTokens
                    If token.IsKind(SyntaxKind.XmlTextLiteralToken) AndAlso Not String.IsNullOrWhiteSpace(token.ValueText) Then
                        Return False
                    End If
                Next token
            Next contentNode
            Return True
        End Function

        ''' <summary>
        ''' Gets the modifiers of a declaration statement.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The declaration statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The modifier token list, or an empty list if the node has no modifiers.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetModifiers(node As SyntaxNode) As SyntaxTokenList

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim modifiers As SyntaxTokenList = Nothing
            If TypeOf node Is TypeStatementSyntax Then
                modifiers = DirectCast(node, TypeStatementSyntax).Modifiers
            ElseIf TypeOf node Is EnumStatementSyntax Then
                modifiers = DirectCast(node, EnumStatementSyntax).Modifiers
            ElseIf TypeOf node Is MethodBaseSyntax Then
                modifiers = DirectCast(node, MethodBaseSyntax).Modifiers
            ElseIf TypeOf node Is FieldDeclarationSyntax Then
                modifiers = DirectCast(node, FieldDeclarationSyntax).Modifiers
            End If
            Return modifiers
        End Function

        ''' <summary>
        ''' Gets the token that best identifies a declaration statement (its name, <see langword="New"/> keyword or operator token).
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The declaration statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The identifying token.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when the node kind is not supported.
        ''' </exception>
        Friend Shared Function GetIdentifierToken(node As SyntaxNode) As SyntaxToken

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Select Case node.Kind()
                Case SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.ModuleStatement, SyntaxKind.InterfaceStatement
                    Return DirectCast(node, TypeStatementSyntax).Identifier
                Case SyntaxKind.EnumStatement
                    Return DirectCast(node, EnumStatementSyntax).Identifier
                Case SyntaxKind.SubStatement, SyntaxKind.FunctionStatement
                    Return DirectCast(node, MethodStatementSyntax).Identifier
                Case SyntaxKind.SubNewStatement
                    Return DirectCast(node, SubNewStatementSyntax).NewKeyword
                Case SyntaxKind.PropertyStatement
                    Return DirectCast(node, PropertyStatementSyntax).Identifier
                Case SyntaxKind.EventStatement
                    Return DirectCast(node, EventStatementSyntax).Identifier
                Case SyntaxKind.OperatorStatement
                    Return DirectCast(node, OperatorStatementSyntax).OperatorToken
                Case SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement
                    Return DirectCast(node, DelegateStatementSyntax).Identifier
                Case SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement
                    Return DirectCast(node, DeclareStatementSyntax).Identifier
                Case SyntaxKind.FieldDeclaration
                    Return DirectCast(node, FieldDeclarationSyntax).Declarators.First().Names.First().Identifier
                Case Else
                    Throw New ArgumentException($"Unsupported declaration kind: {node.Kind()}.", NameOf(node))
            End Select
        End Function

        ''' <summary>
        ''' Gets the parameter list of a method-like declaration statement.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The declaration statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The parameter list, or <see langword="Nothing"/> if the declaration has no parameter list.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetParameterList(node As SyntaxNode) As ParameterListSyntax

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim methodBase As MethodBaseSyntax = TryCast(node, MethodBaseSyntax)
            Return methodBase?.ParameterList
        End Function

        ''' <summary>
        ''' Gets the indentation (leading white-space) of the line on which a token starts.
        ''' </summary>
        '''
        ''' <param name="token">
        ''' The token whose line indentation is requested.
        ''' </param>
        '''
        ''' <returns>
        ''' The white-space text that precedes the first non-white-space character of the token line.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="token"/> does not belong to a syntax tree.
        ''' </exception>
        Friend Shared Function GetLineIndentation(token As SyntaxToken) As String

            Dim tree As SyntaxTree = token.SyntaxTree
            If tree Is Nothing Then
                Throw New ArgumentException("The token does not belong to a syntax tree.", NameOf(token))
            End If

            Dim text As SourceText = tree.GetText()
            Dim line As TextLine = text.Lines.GetLineFromPosition(token.SpanStart)
            Dim lineText As String = text.ToString(line.Span)
            Dim length As Integer = 0
            While length < lineText.Length AndAlso (lineText(length) = " "c OrElse lineText(length) = UtilAnalyzerSyntax.Tab)
                length += 1
            End While
            Return lineText.Substring(0, length)
        End Function

        ''' <summary>
        ''' Gets the end-of-line sequence used by a syntax tree (<c>CRLF</c> when the tree uses it, otherwise <c>LF</c>).
        ''' </summary>
        '''
        ''' <param name="tree">
        ''' The syntax tree to inspect.
        ''' </param>
        '''
        ''' <returns>
        ''' The end-of-line sequence.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="tree"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetEndOfLine(tree As SyntaxTree) As String

            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If

            Dim text As String = tree.GetText().ToString()
            Dim lfIndex As Integer = text.IndexOf(UtilAnalyzerSyntax.LineFeed.ToString(), StringComparison.Ordinal)
            ' No line break, or a CR before the first LF: CRLF (the Windows default). Otherwise: LF.
            Dim usesCrLf As Boolean = lfIndex < 0 OrElse (lfIndex > 0 AndAlso text(lfIndex - 1) = UtilAnalyzerSyntax.CarriageReturn)
            Return If(usesCrLf, UtilAnalyzerSyntax.CrLf, UtilAnalyzerSyntax.LineFeed.ToString())
        End Function

        ''' <summary>
        ''' Gets the statement list of a multi-line block (method, lambda, <see langword="If"/>, <see langword="ElseIf"/>, 
        ''' <see langword="Else"/>, <see langword="Case"/>, loops, <see langword="Try"/>, <see langword="Catch"/>, 
        ''' <see langword="Finally"/>, <see langword="Using"/>, <see langword="SyncLock"/>, <see langword="With"/>).
        ''' </summary>
        '''
        ''' <param name="block">
        ''' The block node. May be <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The statements of the block, or an empty list if the node is not a multi-line block 
        ''' (single-line <see langword="If"/> statements and lambdas have no own lines to insert statements into).
        ''' </returns>
        Friend Shared Function GetBlockStatements(block As SyntaxNode) As SyntaxList(Of StatementSyntax)

            Select Case True
                Case TypeOf block Is MethodBlockBaseSyntax
                    Return DirectCast(block, MethodBlockBaseSyntax).Statements
                Case TypeOf block Is MultiLineLambdaExpressionSyntax
                    Return DirectCast(block, MultiLineLambdaExpressionSyntax).Statements
                Case TypeOf block Is MultiLineIfBlockSyntax
                    Return DirectCast(block, MultiLineIfBlockSyntax).Statements
                Case TypeOf block Is ElseIfBlockSyntax
                    Return DirectCast(block, ElseIfBlockSyntax).Statements
                Case TypeOf block Is ElseBlockSyntax
                    Return DirectCast(block, ElseBlockSyntax).Statements
                Case TypeOf block Is CaseBlockSyntax
                    Return DirectCast(block, CaseBlockSyntax).Statements
                Case TypeOf block Is WhileBlockSyntax
                    Return DirectCast(block, WhileBlockSyntax).Statements
                Case TypeOf block Is DoLoopBlockSyntax
                    Return DirectCast(block, DoLoopBlockSyntax).Statements
                Case TypeOf block Is ForOrForEachBlockSyntax
                    Return DirectCast(block, ForOrForEachBlockSyntax).Statements
                Case TypeOf block Is TryBlockSyntax
                    Return DirectCast(block, TryBlockSyntax).Statements
                Case TypeOf block Is CatchBlockSyntax
                    Return DirectCast(block, CatchBlockSyntax).Statements
                Case TypeOf block Is FinallyBlockSyntax
                    Return DirectCast(block, FinallyBlockSyntax).Statements
                Case TypeOf block Is UsingBlockSyntax
                    Return DirectCast(block, UsingBlockSyntax).Statements
                Case TypeOf block Is SyncLockBlockSyntax
                    Return DirectCast(block, SyncLockBlockSyntax).Statements
                Case TypeOf block Is WithBlockSyntax
                    Return DirectCast(block, WithBlockSyntax).Statements
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Determines whether a file path follows a well-known naming convention for generated source files
        ''' (<c>*.Designer.vb</c>, <c>*.g.vb</c>, <c>*.g.i.vb</c>, <c>*.generated.vb</c>).
        ''' </summary>
        '''
        ''' <param name="filePath">
        ''' The file path to evaluate. May be <see langword="Nothing"/> for in-memory trees.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the path denotes a generated file; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsGeneratedFilePath(filePath As String) As Boolean

            If String.IsNullOrEmpty(filePath) Then
                Return False
            End If

            Dim fileName As String = IO.Path.GetFileNameWithoutExtension(filePath)
            Dim suffixes As String() = {".designer", ".g", ".g.i", ".generated"}
            Return Array.Exists(suffixes, Function(s As String) fileName.EndsWith(s, StringComparison.OrdinalIgnoreCase))
        End Function

        ''' <summary>
        ''' Flattens a chain of <see langword="OrElse"/> / <see langword="Or"/> operators into its atomic operands (left to right),
        ''' without recursion.
        ''' </summary>
        '''
        ''' <param name="condition">
        ''' The condition to flatten.
        ''' </param>
        '''
        ''' <returns>
        ''' The atomic operands in source order, with enclosing parentheses removed.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="condition"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FlattenOrElseChain(condition As ExpressionSyntax) As List(Of ExpressionSyntax)

            If condition Is Nothing Then
                Throw New ArgumentNullException(NameOf(condition))
            End If

            Dim result As New List(Of ExpressionSyntax)()
            Dim pending As New Stack(Of ExpressionSyntax)()
            pending.Push(condition)

            While pending.Count > 0
                Dim current As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(pending.Pop())
                If current.IsKind(SyntaxKind.OrElseExpression) OrElse current.IsKind(SyntaxKind.OrExpression) Then
                    Dim binary As BinaryExpressionSyntax = DirectCast(current, BinaryExpressionSyntax)
                    ' Push the right operand first so that the left operand is processed first.
                    pending.Push(binary.Right)
                    pending.Push(binary.Left)
                Else
                    result.Add(current)
                End If
            End While
            Return result
        End Function

        ''' <summary>
        ''' Determines whether an expression can be evaluated twice, or removed, without observable side effects: 
        ''' a simple name, <see langword="Me"/>, <see langword="MyBase"/>, <see langword="MyClass"/>, 
        ''' or a chain of member accesses on them (e.g. <c>Me.items</c>, <c>settings.Paths</c>).
        ''' </summary>
        ''' 
        ''' <param name="expression">
        ''' The expression. May be <see langword="Nothing"/>.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the expression is a simple name or member access chain; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsSimpleReference(expression As ExpressionSyntax) As Boolean

            If expression Is Nothing Then
                Return False ' Documented: Nothing is not a reference.
            End If

            Dim current As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(expression)
            While current IsNot Nothing
                Select Case current.Kind()
                    Case SyntaxKind.IdentifierName, SyntaxKind.MeExpression, SyntaxKind.MyBaseExpression, SyntaxKind.MyClassExpression
                        Return True

                    Case SyntaxKind.SimpleMemberAccessExpression
                        Dim access As MemberAccessExpressionSyntax = DirectCast(current, MemberAccessExpressionSyntax)
                        If access.Expression Is Nothing OrElse TypeOf access.Name IsNot IdentifierNameSyntax Then
                            Return False
                        End If
                        current = UtilAnalyzerSyntax.SkipParentheses(access.Expression)

                    Case Else
                        Return False
                End Select
            End While
            Return False
        End Function

        ''' <summary>
        ''' Gets the nearest executable boundary of a node: the enclosing lambda expression, or the enclosing method, 
        ''' constructor, operator or accessor block.
        ''' </summary>
        ''' 
        ''' <param name="node">
        ''' The node.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The boundary node, or <see langword="Nothing"/> if the node is not inside executable code.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetExecutableBoundary(node As SyntaxNode) As SyntaxNode

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim current As SyntaxNode = node.Parent
            While current IsNot Nothing
                If TypeOf current Is LambdaExpressionSyntax OrElse TypeOf current Is MethodBlockBaseSyntax Then
                    Return current
                End If
                current = current.Parent
            End While
            Return Nothing
        End Function

        ''' <summary>
        ''' Determines whether an executable boundary (see <see cref="GetExecutableBoundary"/>) is declared with the <see langword="Async"/> modifier.
        ''' </summary>
        ''' 
        ''' <param name="boundary">
        ''' The boundary node. May be <see langword="Nothing"/>.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the lambda or method is <see langword="Async"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsAsyncBoundary(boundary As SyntaxNode) As Boolean

            Dim modifiers As SyntaxTokenList
            If TypeOf boundary Is LambdaExpressionSyntax Then
                modifiers = DirectCast(boundary, LambdaExpressionSyntax).SubOrFunctionHeader.Modifiers
            ElseIf TypeOf boundary Is MethodBlockBaseSyntax Then
                modifiers = DirectCast(boundary, MethodBlockBaseSyntax).BlockStatement.Modifiers
            Else
                Return False ' Documented: Nothing or another node is not an Async boundary.
            End If
            Return modifiers.Any(Function(t As SyntaxToken) t.IsKind(SyntaxKind.AsyncKeyword))
        End Function

        ''' <summary>
        ''' Determines whether a <see langword="Throw"/> statement is handled inside its executable boundary, because it is located 
        ''' in the <see langword="Try"/> section of a <see langword="Try"/> block that has at least one <see langword="Catch"/> block.
        ''' </summary>
        ''' 
        ''' <param name="throwStatement">
        ''' The <see langword="Throw"/> statement.
        ''' </param>
        ''' 
        ''' <param name="stopAt">
        ''' The ancestor where the search stops (e.g. a <see langword="Finally"/> block or the executable boundary).
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if a <see langword="Catch"/> block between the statement and <paramref name="stopAt"/> may handle the exception; 
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="throwStatement"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsCaughtLocally(throwStatement As SyntaxNode, stopAt As SyntaxNode) As Boolean

            If throwStatement Is Nothing Then
                Throw New ArgumentNullException(NameOf(throwStatement))
            End If

            Dim previous As SyntaxNode = throwStatement
            Dim current As SyntaxNode = throwStatement.Parent
            While current IsNot Nothing AndAlso current IsNot stopAt
                Dim tryBlock As TryBlockSyntax = TryCast(current, TryBlockSyntax)
                If tryBlock IsNot Nothing AndAlso tryBlock.CatchBlocks.Count > 0 AndAlso
                   TypeOf previous IsNot CatchBlockSyntax AndAlso TypeOf previous IsNot FinallyBlockSyntax Then
                    Return True
                End If
                previous = current
                current = current.Parent
            End While
            Return False
        End Function

        ''' <summary>
        ''' Determines whether an expression contains a construct whose evaluation may have side effects or produce a different value 
        ''' each time: an invocation, an object or array creation, an <see langword="Await"/> expression or a lambda.
        ''' </summary>
        ''' 
        ''' <param name="expression">
        ''' The expression.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the expression may have side effects; otherwise, <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="expression"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function MayHaveSideEffects(expression As SyntaxNode) As Boolean

            If expression Is Nothing Then
                Throw New ArgumentNullException(NameOf(expression))
            End If

            Return expression.DescendantNodesAndSelf().Any(
                Function(n As SyntaxNode) TypeOf n Is InvocationExpressionSyntax OrElse TypeOf n Is ObjectCreationExpressionSyntax OrElse
                                          TypeOf n Is ArrayCreationExpressionSyntax OrElse TypeOf n Is AwaitExpressionSyntax OrElse
                                          TypeOf n Is LambdaExpressionSyntax)
        End Function

        ''' <summary>
        ''' Determines whether two statement lists are syntactically equivalent (trivia and comments are ignored).
        ''' </summary>
        ''' 
        ''' <param name="first">
        ''' The first statement list.
        ''' </param>
        ''' 
        ''' <param name="second">
        ''' The second statement list.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if both lists have the same statements; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function AreEquivalentStatements(first As SyntaxList(Of StatementSyntax), second As SyntaxList(Of StatementSyntax)) As Boolean

            If first.Count <> second.Count Then
                Return False
            End If
            For index As Integer = 0 To first.Count - 1
                If Not SyntaxFactory.AreEquivalent(first(index), second(index), topLevel:=False) Then
                    Return False
                End If
            Next index
            Return True
        End Function

#End Region

    End Class

End Namespace

#End Region
