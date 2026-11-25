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
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " SyntaxVariation "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' A rewrite of a test source that keeps its meaning but changes how it is written, used to check that the rules do 
    ''' not depend on one way of writing the code (see <see cref="SyntaxVariations"/>).
    ''' </summary>
    Friend NotInheritable Class SyntaxVariation

#Region " Properties "

        ''' <summary>
        ''' Gets the name of the variation, used in the messages of the tests and in the exemptions.
        ''' </summary>
        Friend ReadOnly Property Name As String

        ''' <summary>
        ''' Gets the function that rewrites a source; it returns <see langword="Nothing"/> when the source has nothing to rewrite.
        ''' </summary>
        Friend ReadOnly Property Rewrite As Func(Of String, String)

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SyntaxVariation"/> class.
        ''' </summary>
        '''
        ''' <param name="name">
        ''' The name of the variation.
        ''' </param>
        '''
        ''' <param name="rewrite">
        ''' The function that rewrites a source.
        ''' </param>
        Friend Sub New(name As String, rewrite As Func(Of String, String))

            Me.Name = name
            Me.Rewrite = rewrite
        End Sub

#End Region

    End Class

End Namespace

#End Region

#Region " SyntaxVariations "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Rewrites test sources into equivalent code written in another way: redundant parentheses, explicit line continuations 
    ''' (<c> _</c>), <c>#Region</c> and <c>#If True Then</c> blocks inside method bodies, method bodies nested in a 
    ''' <see langword="Using"/> block, and comment lines between statements.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The test host runs every analyzer and code fix test again on each variation of its source: an analyzer must report 
    ''' as many diagnostics as for the original source, and a code fix must still produce code that compiles and fixes them. 
    ''' This catches the rules that only recognize one way of writing the code (for example a condition without parentheses), 
    ''' for the existing rules and for every new one, without writing the variations by hand.
    ''' </remarks>
    Friend NotInheritable Class SyntaxVariations

#Region " Fields "

        ''' <summary>
        ''' The variations, in the order in which they run.
        ''' </summary>
        Friend Shared ReadOnly All As ImmutableArray(Of SyntaxVariation) = ImmutableArray.Create(
            New SyntaxVariation("Parentheses", AddressOf SyntaxVariations.AddParentheses),
            New SyntaxVariation("LineContinuations", AddressOf SyntaxVariations.AddLineContinuations),
            New SyntaxVariation("Regions", AddressOf SyntaxVariations.AddRegions),
            New SyntaxVariation("UsingBlock", AddressOf SyntaxVariations.AddUsingBlocks),
            New SyntaxVariation("ConditionalCompilation", AddressOf SyntaxVariations.AddConditionalCompilation),
            New SyntaxVariation("Comments", AddressOf SyntaxVariations.AddComments))

        ''' <summary>
        ''' The rules whose diagnostics may change in a variation, with the reason: <c>"DCAnnn:Variation"</c>.
        ''' </summary>
        Private Shared ReadOnly Exemptions As New Dictionary(Of String, String)(StringComparer.Ordinal) From {
            {"DCA011:ConditionalCompilation", "By design, the rule is silent inside an existing #If block, whose code is already written per configuration."},
            {"DCA013:ConditionalCompilation", "By design, the rule is silent inside an existing #If block, whose code is already written per configuration."}
        }

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="SyntaxVariations"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether some diagnostics belong to a rule that is exempted from a variation.
        ''' </summary>
        '''
        ''' <param name="diagnostics">
        ''' The diagnostics.
        ''' </param>
        '''
        ''' <param name="variation">
        ''' The variation.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if one of the diagnostics is exempted; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsExempt(diagnostics As ImmutableArray(Of Diagnostic), variation As SyntaxVariation) As Boolean

            Return diagnostics.Any(Function(d As Diagnostic) SyntaxVariations.Exemptions.ContainsKey($"{d.Id}:{variation.Name}"))
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Wraps in redundant parentheses the conditions of <see langword="If"/>, <see langword="ElseIf"/>, <see langword="While"/>, 
        ''' <see langword="Do"/>/<see langword="Loop"/> and <c>If(...)</c>, the values of <see langword="Return"/> statements, of 
        ''' local declarations and of assignments.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if it has no such expression.
        ''' </returns>
        '''
        ''' <remarks>
        ''' Arguments are not wrapped, because parentheses pass a <see langword="ByRef"/> argument by value. Array literals, 
        ''' lambdas and <see langword="AddressOf"/> are not wrapped either, because their type comes from the target.
        ''' </remarks>
        Private Shared Function AddParentheses(source As String) As String

            Dim root As SyntaxNode = VisualBasicSyntaxTree.ParseText(source).GetRoot()
            Dim targets As New List(Of ExpressionSyntax)()
            For Each node As SyntaxNode In root.DescendantNodes()
                Select Case True
                    Case TypeOf node Is IfStatementSyntax
                        targets.Add(DirectCast(node, IfStatementSyntax).Condition)
                    Case TypeOf node Is ElseIfStatementSyntax
                        targets.Add(DirectCast(node, ElseIfStatementSyntax).Condition)
                    Case TypeOf node Is SingleLineIfStatementSyntax
                        targets.Add(DirectCast(node, SingleLineIfStatementSyntax).Condition)
                    Case TypeOf node Is WhileStatementSyntax
                        targets.Add(DirectCast(node, WhileStatementSyntax).Condition)
                    Case TypeOf node Is WhileOrUntilClauseSyntax
                        targets.Add(DirectCast(node, WhileOrUntilClauseSyntax).Condition)
                    Case TypeOf node Is TernaryConditionalExpressionSyntax
                        targets.Add(DirectCast(node, TernaryConditionalExpressionSyntax).Condition)
                    Case TypeOf node Is ReturnStatementSyntax
                        targets.Add(DirectCast(node, ReturnStatementSyntax).Expression)
                    Case TypeOf node Is EqualsValueSyntax AndAlso node.Parent?.Parent IsNot Nothing AndAlso TypeOf node.Parent.Parent Is LocalDeclarationStatementSyntax
                        targets.Add(DirectCast(node, EqualsValueSyntax).Value)
                    Case node.IsKind(SyntaxKind.SimpleAssignmentStatement)
                        targets.Add(DirectCast(node, AssignmentStatementSyntax).Right)
                End Select
            Next node

            Dim edits As New List(Of (Position As Integer, Text As String))()
            For Each target As ExpressionSyntax In targets.Where(AddressOf SyntaxVariations.CanParenthesize)
                edits.Add((target.SpanStart, "("))
                edits.Add((target.Span.End, ")"))
            Next target
            Return SyntaxVariations.ApplyInsertions(source, edits)
        End Function

        ''' <summary>
        ''' Determines whether an expression can be wrapped in parentheses without changing its meaning.
        ''' </summary>
        '''
        ''' <param name="expression">
        ''' The expression; may be <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the expression can be wrapped; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function CanParenthesize(expression As ExpressionSyntax) As Boolean

            Return expression IsNot Nothing AndAlso Not expression.IsMissing AndAlso
                   TypeOf expression IsNot ParenthesizedExpressionSyntax AndAlso TypeOf expression IsNot CollectionInitializerSyntax AndAlso
                   TypeOf expression IsNot LambdaExpressionSyntax AndAlso Not expression.IsKind(SyntaxKind.AddressOfExpression)
        End Function

        ''' <summary>
        ''' Adds an explicit line continuation (<c> _</c> and a line break) after the commas and the opening parenthesis of 
        ''' argument lists, after the dot of member accesses, and after the <see langword="AndAlso"/>, <see langword="OrElse"/> 
        ''' and <c>&amp;</c> operators.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if it has no such token.
        ''' </returns>
        '''
        ''' <remarks>
        ''' Tokens inside interpolated strings, attributes, XML literals, conditional accesses (<c>?.</c>) and 
        ''' <see langword="With"/> member accesses (<c>.Name</c>), and tokens already followed by a comment or a line break, are 
        ''' left alone.
        ''' </remarks>
        Private Shared Function AddLineContinuations(source As String) As String

            Dim root As SyntaxNode = VisualBasicSyntaxTree.ParseText(source).GetRoot()
            Dim endOfLine As String = If(source.Contains(vbCrLf, StringComparison.Ordinal), vbCrLf, vbLf)
            Dim edits As New List(Of (Position As Integer, Text As String))()
            For Each token As SyntaxToken In root.DescendantTokens()
                ' A token followed by a comment or a line break already ends its line.
                If Not SyntaxVariations.CanContinueAfter(token) OrElse
                   token.TrailingTrivia.Any(Function(t As SyntaxTrivia) Not t.IsKind(SyntaxKind.WhitespaceTrivia)) Then
                    Continue For
                End If

                ' " _" and a line break end the line after the token; the next line is indented deeper.
                Dim lineStart As Integer = source.LastIndexOf(vbLf, Math.Max(0, token.SpanStart - 1), StringComparison.Ordinal) + 1
                Dim indentation As New String(" "c, source.Skip(lineStart).TakeWhile(Function(c As Char) c = " "c).Count() + 8)
                edits.Add((token.Span.End, $" _{endOfLine}{indentation}"))
            Next token
            Return SyntaxVariations.ApplyInsertions(source, edits)
        End Function

        ''' <summary>
        ''' Determines whether an explicit line continuation is added after a token.
        ''' </summary>
        '''
        ''' <param name="token">
        ''' The token.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for the tokens described in <see cref="AddLineContinuations"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function CanContinueAfter(token As SyntaxToken) As Boolean

            Dim parent As SyntaxNode = token.Parent
            If parent Is Nothing OrElse token.GetNextToken().IsKind(SyntaxKind.None) OrElse
               parent.AncestorsAndSelf().Any(Function(a As SyntaxNode) TypeOf a Is InterpolatedStringExpressionSyntax OrElse TypeOf a Is AttributeListSyntax OrElse
                                                                       TypeOf a Is ConditionalAccessExpressionSyntax OrElse TypeOf a Is XmlNodeSyntax) Then
                Return False
            End If

            Dim argumentList As ArgumentListSyntax = TryCast(parent, ArgumentListSyntax)
            Dim access As MemberAccessExpressionSyntax = TryCast(parent, MemberAccessExpressionSyntax)
            Select Case True
                Case token.IsKind(SyntaxKind.CommaToken)
                    Return argumentList IsNot Nothing
                Case token.IsKind(SyntaxKind.OpenParenToken)
                    Return argumentList IsNot Nothing AndAlso argumentList.Arguments.Count > 0
                Case token.IsKind(SyntaxKind.DotToken)
                    Return access IsNot Nothing AndAlso access.Expression IsNot Nothing AndAlso access.OperatorToken = token
                Case token.IsKind(SyntaxKind.AndAlsoKeyword), token.IsKind(SyntaxKind.OrElseKeyword), token.IsKind(SyntaxKind.AmpersandToken)
                    Return TypeOf parent Is BinaryExpressionSyntax
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Wraps the statements of each method, property accessor and constructor in a <c>#Region</c> block.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if no method has statements.
        ''' </returns>
        Private Shared Function AddRegions(source As String) As String

            Return SyntaxVariations.WrapBodies(source, Function(block As MethodBlockBaseSyntax) True, "#Region "" Variation """, "#End Region")
        End Function

        ''' <summary>
        ''' Nests the statements of each method and property accessor in a <c>Using variationScope As New IO.MemoryStream()</c> block.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if no method has statements.
        ''' </returns>
        '''
        ''' <remarks>
        ''' Constructors (whose first statement may be <c>MyBase.New</c>), operators and methods with <see langword="On Error"/>, 
        ''' <see langword="GoTo"/>, <see langword="Resume"/> or labels (which cannot jump into a <see langword="Using"/> block) 
        ''' are left alone.
        ''' </remarks>
        Private Shared Function AddUsingBlocks(source As String) As String

            Return SyntaxVariations.WrapBodies(
                source,
                Function(block As MethodBlockBaseSyntax) TypeOf block IsNot ConstructorBlockSyntax AndAlso TypeOf block IsNot OperatorBlockSyntax AndAlso
                                                         Not block.DescendantNodes().Any(Function(n As SyntaxNode) TypeOf n Is OnErrorGoToStatementSyntax OrElse
                                                                                                                   TypeOf n Is OnErrorResumeNextStatementSyntax OrElse
                                                                                                                   TypeOf n Is GoToStatementSyntax OrElse
                                                                                                                   TypeOf n Is ResumeStatementSyntax OrElse
                                                                                                                   TypeOf n Is LabelStatementSyntax),
                "Using variationScope As New IO.MemoryStream()", "End Using")
        End Function

        ''' <summary>
        ''' Wraps the statements of each method, property accessor and constructor in an <c>#If True Then</c> block.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if no method has statements.
        ''' </returns>
        Private Shared Function AddConditionalCompilation(source As String) As String

            Return SyntaxVariations.WrapBodies(source, Function(block As MethodBlockBaseSyntax) True, "#If True Then", "#End If")
        End Function

        ''' <summary>
        ''' Adds a comment line before every statement that starts a line inside a multi-line block.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if there are no statements in multi-line blocks.
        ''' </returns>
        Private Shared Function AddComments(source As String) As String

            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText(source)
            Dim text As SourceText = tree.GetText()
            Dim endOfLine As String = If(source.Contains(vbCrLf, StringComparison.Ordinal), vbCrLf, vbLf)
            Dim edits As New List(Of (Position As Integer, Text As String))()
            For Each statement As StatementSyntax In tree.GetRoot().DescendantNodes().OfType(Of StatementSyntax)()
                If Not UtilAnalyzerSyntax.GetBlockStatements(statement.Parent).Contains(statement) Then
                    Continue For
                End If

                Dim line As TextLine = text.Lines.GetLineFromPosition(statement.SpanStart)
                Dim prefix As String = text.ToString(TextSpan.FromBounds(line.Start, statement.SpanStart))
                If prefix.Trim().Length = 0 Then
                    edits.Add((line.Start, $"{prefix}' Variation comment.{endOfLine}"))
                End If
            Next statement
            Return SyntaxVariations.ApplyInsertions(source, edits)
        End Function

        ''' <summary>
        ''' Adds a line before the first statement and a line before the end of the body of the selected methods.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <param name="selector">
        ''' Selects the methods to rewrite.
        ''' </param>
        '''
        ''' <param name="opening">
        ''' The text of the line added before the first statement.
        ''' </param>
        '''
        ''' <param name="closing">
        ''' The text of the line added before the end of the body.
        ''' </param>
        '''
        ''' <returns>
        ''' The rewritten source; <see langword="Nothing"/> if no selected method has statements on their own lines.
        ''' </returns>
        Private Shared Function WrapBodies(source As String, selector As Func(Of MethodBlockBaseSyntax, Boolean), opening As String, closing As String) As String

            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText(source)
            Dim text As SourceText = tree.GetText()
            Dim endOfLine As String = If(source.Contains(vbCrLf, StringComparison.Ordinal), vbCrLf, vbLf)
            Dim edits As New List(Of (Position As Integer, Text As String))()
            For Each block As MethodBlockBaseSyntax In tree.GetRoot().DescendantNodes().OfType(Of MethodBlockBaseSyntax)().Where(selector)
                If block.Statements.Count = 0 Then
                    Continue For
                End If

                Dim firstLine As TextLine = text.Lines.GetLineFromPosition(block.Statements(0).SpanStart)
                Dim endLine As TextLine = text.Lines.GetLineFromPosition(block.EndBlockStatement.SpanStart)
                Dim headerLine As TextLine = text.Lines.GetLineFromPosition(block.BlockStatement.Span.End)
                Dim lastLine As TextLine = text.Lines.GetLineFromPosition(block.Statements(block.Statements.Count - 1).Span.End)
                If firstLine.LineNumber = headerLine.LineNumber OrElse endLine.LineNumber = lastLine.LineNumber OrElse
                   block.DescendantNodes().Any(Function(n As SyntaxNode) TypeOf n Is MethodBlockBaseSyntax) Then
                    Continue For ' Single-line bodies, and nested blocks (handled by their own method).
                End If

                Dim indentation As New String(" "c, text.ToString(firstLine.Span).TakeWhile(Function(c As Char) c = " "c).Count())
                edits.Add((firstLine.Start, $"{indentation}{opening}{endOfLine}"))
                edits.Add((endLine.Start, $"{indentation}{closing}{endOfLine}"))
            Next block
            Return SyntaxVariations.ApplyInsertions(source, edits)
        End Function

        ''' <summary>
        ''' Inserts texts at positions of a source.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The source.
        ''' </param>
        '''
        ''' <param name="edits">
        ''' The positions and the texts to insert there.
        ''' </param>
        '''
        ''' <returns>
        ''' The source with the insertions; <see langword="Nothing"/> if there are none.
        ''' </returns>
        Private Shared Function ApplyInsertions(source As String, edits As List(Of (Position As Integer, Text As String))) As String

            Dim builder As New System.Text.StringBuilder(source)
            For Each edit As (Position As Integer, Text As String) In edits.OrderByDescending(Function(e As (Position As Integer, Text As String)) e.Position)
                builder.Insert(edit.Position, edit.Text)
            Next edit
            Return If(edits.Count = 0, Nothing, builder.ToString())
        End Function

#End Region

    End Class

End Namespace

#End Region
