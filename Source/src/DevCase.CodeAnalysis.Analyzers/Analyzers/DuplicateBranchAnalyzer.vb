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

#Region " DuplicateBranchAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports a branch of an <c>If</c>/<c>ElseIf</c>/<c>Else</c> chain or of a <c>Select Case</c> statement whose implementation 
    ''' is the same as the implementation of an earlier branch (rule <see cref="DiagnosticIds.DuplicateBranch"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Branches of an <c>If</c> chain with a single statement are not reported (e.g. <c>Return False</c> in several branches is common 
    ''' and readable); <c>Select Case</c> branches are reported whatever their size, because their <c>Case</c> clauses can simply be merged. 
    ''' Empty branches are not reported. When every branch is identical, rule <see cref="DiagnosticIds.AllBranchesIdentical"/> is reported instead.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DuplicateBranchAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DuplicateBranch)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DuplicateBranchAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf DuplicateBranchAnalyzer.AnalyzeIfBlock), SyntaxKind.MultiLineIfBlock)
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf DuplicateBranchAnalyzer.AnalyzeSelectBlock), SyntaxKind.SelectBlock)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports the duplicated branches of an <c>If</c> chain.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIfBlock(context As SyntaxNodeAnalysisContext)

            Dim block As MultiLineIfBlockSyntax = DirectCast(context.Node, MultiLineIfBlockSyntax)
            Dim statements As List(Of SyntaxList(Of StatementSyntax)) = AllBranchesIdenticalAnalyzer.GetBranches(block)
            If block.ElseBlock IsNot Nothing AndAlso AllBranchesIdenticalAnalyzer.AreAllIdentical(statements) Then
                Exit Sub ' Reported by DCA041.
            End If

            Dim headers As New List(Of SyntaxNode) From {block.IfStatement}
            headers.AddRange(block.ElseIfBlocks.Select(Function(b As ElseIfBlockSyntax) DirectCast(b.ElseIfStatement, SyntaxNode)))
            If block.ElseBlock IsNot Nothing Then
                headers.Add(block.ElseBlock.ElseStatement)
            End If
            DuplicateBranchAnalyzer.ReportDuplicates(context, statements, headers, minimumStatements:=2)
        End Sub

        ''' <summary>
        ''' Reports the duplicated branches of a <c>Select Case</c> statement.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeSelectBlock(context As SyntaxNodeAnalysisContext)

            Dim block As SelectBlockSyntax = DirectCast(context.Node, SelectBlockSyntax)
            Dim statements As List(Of SyntaxList(Of StatementSyntax)) = block.CaseBlocks.Select(Function(c As CaseBlockSyntax) c.Statements).ToList()
            If block.CaseBlocks.Any(Function(c As CaseBlockSyntax) c.IsKind(SyntaxKind.CaseElseBlock)) AndAlso AllBranchesIdenticalAnalyzer.AreAllIdentical(statements) Then
                Exit Sub ' Reported by DCA041.
            End If

            Dim headers As List(Of SyntaxNode) = block.CaseBlocks.Select(Function(c As CaseBlockSyntax) DirectCast(c.CaseStatement, SyntaxNode)).ToList()
            DuplicateBranchAnalyzer.ReportDuplicates(context, statements, headers, minimumStatements:=1)
        End Sub

        ''' <summary>
        ''' Reports every branch that is equivalent to an earlier branch.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        ''' 
        ''' <param name="statements">
        ''' The statement lists of the branches.
        ''' </param>
        ''' 
        ''' <param name="headers">
        ''' The header statement of each branch (where the diagnostic is reported).
        ''' </param>
        ''' 
        ''' <param name="minimumStatements">
        ''' The minimum number of statements of a branch to be compared.
        ''' </param>
        Private Shared Sub ReportDuplicates(context As SyntaxNodeAnalysisContext, statements As List(Of SyntaxList(Of StatementSyntax)),
                                            headers As List(Of SyntaxNode), minimumStatements As Integer)

            For index As Integer = 1 To statements.Count - 1
                If statements(index).Count < minimumStatements Then
                    Continue For
                End If
                For earlier As Integer = 0 To index - 1
                    If UtilAnalyzerSyntax.AreEquivalentStatements(statements(earlier), statements(index)) Then
                        Dim line As Integer = headers(earlier).GetLocation().GetLineSpan().StartLinePosition.Line + 1
                        context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DuplicateBranch, headers(index).GetLocation(), line))
                        Exit For
                    End If
                Next earlier
            Next index
        End Sub

#End Region

    End Class

End Namespace

#End Region
