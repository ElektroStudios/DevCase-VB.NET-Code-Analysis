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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " AllBranchesIdenticalAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports conditional structures whose branches all have the same implementation, including the <see langword="Else"/> branch 
    ''' (rule <see cref="DiagnosticIds.AllBranchesIdentical"/>): <c>If</c>/<c>ElseIf</c>/<c>Else</c> blocks, single-line 
    ''' <c>If ... Then ... Else</c> statements, <c>Select Case</c> statements with <c>Case Else</c>, and <c>If(condition, a, a)</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Structures without an <see langword="Else"/> branch are not reported, because doing nothing is a different behavior. 
    ''' Empty branches are not reported. Statements are compared syntactically (trivia and comments are ignored).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class AllBranchesIdenticalAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.AllBranchesIdentical)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AllBranchesIdenticalAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf AllBranchesIdenticalAnalyzer.AnalyzeIfBlock), SyntaxKind.MultiLineIfBlock)
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf AllBranchesIdenticalAnalyzer.AnalyzeSingleLineIf), SyntaxKind.SingleLineIfStatement)
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf AllBranchesIdenticalAnalyzer.AnalyzeSelectBlock), SyntaxKind.SelectBlock)
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf AllBranchesIdenticalAnalyzer.AnalyzeTernary), SyntaxKind.TernaryConditionalExpression)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether every statement list is non-empty and equivalent to the first one.
        ''' </summary>
        ''' 
        ''' <param name="branches">
        ''' The statement lists of the branches.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if all the branches are identical; otherwise, <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="branches"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function AreAllIdentical(branches As IList(Of SyntaxList(Of StatementSyntax))) As Boolean

            If branches Is Nothing Then
                Throw New ArgumentNullException(NameOf(branches))
            End If

            Return branches.Count > 1 AndAlso branches(0).Count > 0 AndAlso
                   branches.Skip(1).All(Function(b As SyntaxList(Of StatementSyntax)) UtilAnalyzerSyntax.AreEquivalentStatements(branches(0), b))
        End Function

        ''' <summary>
        ''' Gets the statement lists of the branches of an <c>If</c> block, in order.
        ''' </summary>
        ''' 
        ''' <param name="block">
        ''' The <c>If</c> block.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The statement lists of the <c>If</c>, <c>ElseIf</c> and <c>Else</c> branches.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="block"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetBranches(block As MultiLineIfBlockSyntax) As List(Of SyntaxList(Of StatementSyntax))

            If block Is Nothing Then
                Throw New ArgumentNullException(NameOf(block))
            End If

            Dim branches As New List(Of SyntaxList(Of StatementSyntax)) From {block.Statements}
            branches.AddRange(block.ElseIfBlocks.Select(Function(b As ElseIfBlockSyntax) b.Statements))
            If block.ElseBlock IsNot Nothing Then
                branches.Add(block.ElseBlock.Statements)
            End If
            Return branches
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an <c>If</c> block with an <see langword="Else"/> branch whose branches are all identical.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIfBlock(context As SyntaxNodeAnalysisContext)

            Dim block As MultiLineIfBlockSyntax = DirectCast(context.Node, MultiLineIfBlockSyntax)
            If block.ElseBlock IsNot Nothing AndAlso AllBranchesIdenticalAnalyzer.AreAllIdentical(AllBranchesIdenticalAnalyzer.GetBranches(block)) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.AllBranchesIdentical, block.IfStatement.GetLocation(), "If"))
            End If
        End Sub

        ''' <summary>
        ''' Reports a single-line <c>If ... Then ... Else</c> statement whose branches are identical.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeSingleLineIf(context As SyntaxNodeAnalysisContext)

            Dim statement As SingleLineIfStatementSyntax = DirectCast(context.Node, SingleLineIfStatementSyntax)
            If statement.ElseClause IsNot Nothing AndAlso
               AllBranchesIdenticalAnalyzer.AreAllIdentical({statement.Statements, statement.ElseClause.Statements}) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.AllBranchesIdentical, statement.GetLocation(), "If"))
            End If
        End Sub

        ''' <summary>
        ''' Reports a <c>Select Case</c> statement with <c>Case Else</c> whose branches are all identical.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeSelectBlock(context As SyntaxNodeAnalysisContext)

            Dim block As SelectBlockSyntax = DirectCast(context.Node, SelectBlockSyntax)
            If block.CaseBlocks.Any(Function(c As CaseBlockSyntax) c.IsKind(SyntaxKind.CaseElseBlock)) AndAlso
               AllBranchesIdenticalAnalyzer.AreAllIdentical(block.CaseBlocks.Select(Function(c As CaseBlockSyntax) c.Statements).ToList()) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.AllBranchesIdentical, block.SelectStatement.GetLocation(), "Select Case"))
            End If
        End Sub

        ''' <summary>
        ''' Reports an <c>If(condition, a, b)</c> expression whose two results are identical.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeTernary(context As SyntaxNodeAnalysisContext)

            Dim expression As TernaryConditionalExpressionSyntax = DirectCast(context.Node, TernaryConditionalExpressionSyntax)
            If SyntaxFactory.AreEquivalent(expression.WhenTrue, expression.WhenFalse, topLevel:=False) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.AllBranchesIdentical, expression.GetLocation(), "If()"))
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
