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

#Region " CancellationCheckAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports an <see langword="If"/> statement that throws <c>OperationCanceledException</c> when
    ''' <c>token.IsCancellationRequested</c> is <see langword="True"/> (rule <see cref="DiagnosticIds.ManualCancellationCheck"/>;
    ''' equivalent of CA2250 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The statement must have no <see langword="ElseIf"/> or <see langword="Else"/> part, and its only statement must be
    ''' <c>Throw New OperationCanceledException()</c> or <c>Throw New OperationCanceledException(token)</c> with the same token (the exact
    ''' type: a derived exception such as <c>TaskCanceledException</c> is not reported). The token expression must not have side effects,
    ''' and the statement must not contain preprocessor directives (another configuration may compile other statements).
    ''' </para>
    ''' <para>
    ''' <c>token.ThrowIfCancellationRequested()</c> does the same in one call, and its exception carries the token. The code fix replaces
    ''' the statement and keeps the comments at the end of its lines after the call; it is not offered when the statement contains
    ''' other comments.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class CancellationCheckAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ManualCancellationCheck)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CancellationCheckAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf CancellationCheckAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2250 reports the same code.
            End If

            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf CancellationCheckAnalyzer.AnalyzeIf),
                                             SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement)
        End Sub


        ''' <summary>
        ''' Reports an <see langword="If"/> statement that throws <c>OperationCanceledException</c> after checking 
        ''' <c>IsCancellationRequested</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIf(context As SyntaxNodeAnalysisContext)

            Dim condition As ExpressionSyntax
            Dim statements As SyntaxList(Of StatementSyntax)
            Dim block As MultiLineIfBlockSyntax = TryCast(context.Node, MultiLineIfBlockSyntax)
            If block IsNot Nothing Then
                If block.ElseIfBlocks.Count > 0 OrElse block.ElseBlock IsNot Nothing Then
                    Exit Sub
                End If
                condition = block.IfStatement.Condition
                statements = block.Statements
            Else
                Dim singleLine As SingleLineIfStatementSyntax = DirectCast(context.Node, SingleLineIfStatementSyntax)
                If singleLine.ElseClause IsNot Nothing Then
                    Exit Sub
                End If
                condition = singleLine.Condition
                statements = singleLine.Statements
            End If

            If UtilAnalyzerSyntax.HasInnerDirectives(context.Node) Then
                Exit Sub ' Another configuration may compile other statements.
            End If

            Dim access As MemberAccessExpressionSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(condition), MemberAccessExpressionSyntax)
            Dim throwStatement As ThrowStatementSyntax = If(statements.Count = 1, TryCast(statements(0), ThrowStatementSyntax), Nothing)
            Dim creation As ObjectCreationExpressionSyntax = TryCast(throwStatement?.Expression, ObjectCreationExpressionSyntax)
            If access Is Nothing OrElse access.Expression Is Nothing OrElse creation Is Nothing OrElse creation.Initializer IsNot Nothing OrElse
               UtilAnalyzerSyntax.MayHaveSideEffects(access.Expression) Then
                Exit Sub
            End If

            Dim model As SemanticModel = context.SemanticModel
            Dim checkedProperty As ISymbol = model.GetSymbolInfo(access, context.CancellationToken).Symbol
            Dim exceptionType As ITypeSymbol = model.GetTypeInfo(creation, context.CancellationToken).Type
            If Not UtilAnalyzerSemantic.IsMember(checkedProperty, "System.Threading.CancellationToken", NameOf(Threading.CancellationToken.IsCancellationRequested)) OrElse
               Not UtilAnalyzerSemantic.HasMetadataName(exceptionType, "System.OperationCanceledException") OrElse
               Not CancellationCheckAnalyzer.PassesToken(creation, access.Expression) Then
                Exit Sub
            End If

            ' The comments at the end of the "If" and "Throw" lines move to the end of the call, and those on lines of their own before it.
            Dim tokenText As String = UtilAnalyzerSyntax.GetReceiverText(access.Expression)
            Dim replacement As String = $"{tokenText}.ThrowIfCancellationRequested()"
            Dim lines As SyntaxNode() = If(block IsNot Nothing, {block.IfStatement, throwStatement}, Array.Empty(Of SyntaxNode)())
            Dim comments As String = Nothing
            Dim commentLines As String = Nothing
            Dim actions As New List(Of FixReplacement)()
            If UtilAnalyzerSyntax.TryGetLineComments(context.Node, lines, comments, commentLines) AndAlso
               UtilAnalyzerSemantic.IsMember(UtilSpeculativeBinding.GetSymbol(model, context.Node.SpanStart, replacement),
                                             "System.Threading.CancellationToken", NameOf(Threading.CancellationToken.ThrowIfCancellationRequested)) Then
                actions.Add(New FixReplacement("Use ThrowIfCancellationRequested", "ThrowIfCancellationRequested", $"{commentLines}{replacement}{comments}"))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.ManualCancellationCheck, condition.GetLocation(),
                                                                        {context.Node.GetLocation()}, actions, access.Expression.ToString()))
        End Sub

        ''' <summary>
        ''' Determines whether the arguments of an <c>OperationCanceledException</c> creation are none, or only the checked token.
        ''' </summary>
        '''
        ''' <param name="creation">
        ''' The creation of the exception.
        ''' </param>
        '''
        ''' <param name="token">
        ''' The checked token.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for <c>New OperationCanceledException()</c> and <c>New OperationCanceledException(token)</c>; 
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function PassesToken(creation As ObjectCreationExpressionSyntax, token As ExpressionSyntax) As Boolean

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = If(creation.ArgumentList Is Nothing, Nothing, creation.ArgumentList.Arguments)
            Select Case arguments.Count
                Case 0
                    Return True
                Case 1
                    Return TypeOf arguments(0) Is SimpleArgumentSyntax AndAlso UtilAnalyzerSyntax.AreEquivalentExpressions(arguments(0).GetExpression(), token)
                Case Else
                    Return False
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
