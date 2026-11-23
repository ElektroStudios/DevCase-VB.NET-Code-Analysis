#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " DuplicateConditionAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports conditions of an <c>If</c>/<c>ElseIf</c> chain that repeat an earlier condition of the same chain, and <c>Case</c> 
    ''' values of a <c>Select Case</c> statement that repeat an earlier value (rule <see cref="DiagnosticIds.DuplicateCondition"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Conditions are compared syntactically (trivia and comments are ignored). Conditions that contain invocations, object creations, 
    ''' <see langword="Await"/> expressions or lambdas are not compared, because evaluating them again may produce another value. 
    ''' <c>Case</c> values are compared only when both are constants (<c>Case 1</c>, <c>Case "a"</c>, <c>Case Color.Red</c>); 
    ''' ranges (<c>Case 1 To 5</c>) and relational clauses (<c>Case Is &gt; 5</c>) are not analyzed.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DuplicateConditionAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DuplicateCondition)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DuplicateConditionAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf DuplicateConditionAnalyzer.AnalyzeIfBlock), SyntaxKind.MultiLineIfBlock)
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf DuplicateConditionAnalyzer.AnalyzeSelectBlock), SyntaxKind.SelectBlock)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports the conditions of an <c>If</c>/<c>ElseIf</c> chain that repeat an earlier condition.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIfBlock(context As SyntaxNodeAnalysisContext)

            Dim block As MultiLineIfBlockSyntax = DirectCast(context.Node, MultiLineIfBlockSyntax)
            Dim conditions As New List(Of ExpressionSyntax) From {block.IfStatement.Condition}
            For Each elseIfBlock As ElseIfBlockSyntax In block.ElseIfBlocks
                conditions.Add(elseIfBlock.ElseIfStatement.Condition)
            Next elseIfBlock

            For index As Integer = 1 To conditions.Count - 1
                Dim condition As ExpressionSyntax = conditions(index)
                If UtilAnalyzerSyntax.MayHaveSideEffects(condition) Then
                    Continue For
                End If
                For earlier As Integer = 0 To index - 1
                    If SyntaxFactory.AreEquivalent(conditions(earlier), condition, topLevel:=False) Then
                        DuplicateConditionAnalyzer.Report(context, condition, conditions(earlier))
                        Exit For
                    End If
                Next earlier
            Next index
        End Sub

        ''' <summary>
        ''' Reports the constant <c>Case</c> values of a <c>Select Case</c> statement that repeat an earlier value.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeSelectBlock(context As SyntaxNodeAnalysisContext)

            Dim block As SelectBlockSyntax = DirectCast(context.Node, SelectBlockSyntax)
            Dim seen As New List(Of KeyValuePair(Of Object, SyntaxNode))()
            For Each caseBlock As CaseBlockSyntax In block.CaseBlocks
                For Each clause As CaseClauseSyntax In caseBlock.CaseStatement.Cases
                    Dim simple As SimpleCaseClauseSyntax = TryCast(clause, SimpleCaseClauseSyntax)
                    If simple Is Nothing Then
                        Continue For
                    End If

                    Dim constant As [Optional](Of Object) = context.SemanticModel.GetConstantValue(simple.Value, context.CancellationToken)
                    If Not constant.HasValue Then
                        Continue For
                    End If

                    Dim duplicate As SyntaxNode = Nothing
                    For Each pair As KeyValuePair(Of Object, SyntaxNode) In seen
                        If Object.Equals(pair.Key, constant.Value) Then
                            duplicate = pair.Value
                            Exit For
                        End If
                    Next pair

                    If duplicate IsNot Nothing Then
                        DuplicateConditionAnalyzer.Report(context, simple, duplicate)
                    Else
                        seen.Add(New KeyValuePair(Of Object, SyntaxNode)(constant.Value, simple))
                    End If
                Next clause
            Next caseBlock
        End Sub

        ''' <summary>
        ''' Reports a duplicated condition.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        ''' 
        ''' <param name="duplicate">
        ''' The duplicated condition.
        ''' </param>
        ''' 
        ''' <param name="original">
        ''' The earlier condition.
        ''' </param>
        Private Shared Sub Report(context As SyntaxNodeAnalysisContext, duplicate As SyntaxNode, original As SyntaxNode)

            Dim line As Integer = original.GetLocation().GetLineSpan().StartLinePosition.Line + 1
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DuplicateCondition, duplicate.GetLocation(), line))
        End Sub

#End Region

    End Class

End Namespace

#End Region
