#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " InsecureTempFileAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to <c>Path.GetTempFileName</c> and <c>My.Computer.FileSystem.GetTempFileName</c> 
    ''' (rule <see cref="DiagnosticIds.InsecureTempFile"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The diagnostic tells the code fix how the returned path is used (<see cref="UsagePropertyKey"/>): 
    ''' as the initializer of a local variable, as the right side of an assignment, in a <see langword="Return"/> statement, 
    ''' or inside a larger expression that can be moved before its statement (<see cref="ExpressionUsage"/>). 
    ''' Other usages (conditional evaluation, lambdas, queries, single-line statements) are reported without a code fix.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class InsecureTempFileAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that tells how the returned path is used: <see cref="DeclarationUsage"/>, 
        ''' <see cref="AssignmentUsage"/>, <see cref="ReturnUsage"/> or an empty string (no code fix).
        ''' </summary>
        Public Const UsagePropertyKey As String = "Usage"

        ''' <summary>
        ''' The call initializes a local variable: <c>Dim path As String = Path.GetTempFileName()</c>.
        ''' </summary>
        Public Const DeclarationUsage As String = "Declaration"

        ''' <summary>
        ''' The call is the right side of an assignment: <c>path = Path.GetTempFileName()</c>.
        ''' </summary>
        Public Const AssignmentUsage As String = "Assignment"

        ''' <summary>
        ''' The call is returned: <c>Return Path.GetTempFileName()</c>.
        ''' </summary>
        Public Const ReturnUsage As String = "Return"

        ''' <summary>
        ''' The call is part of a larger expression of a statement that evaluates it exactly once, before any other statement 
        ''' of its block runs: <c>Dim file As New FileInfo(Path.GetTempFileName())</c>. The fix declares the path in a new 
        ''' variable before the statement (see <see cref="IsExtractable"/>).
        ''' </summary>
        Public Const ExpressionUsage As String = "Expression"
#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.InsecureTempFile)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="InsecureTempFileAnalyzer"/> class.
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
            context.RegisterOperationAction(AddressOf InsecureTempFileAnalyzer.AnalyzeInvocation, OperationKind.Invocation)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an invocation of <c>GetTempFileName</c>.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            If Not method.Name.Equals("GetTempFileName", StringComparison.Ordinal) OrElse method.Parameters.Length <> 0 Then
                Exit Sub
            End If

            Dim typeName As String = method.ContainingType.ToDisplayString()
            If Not typeName.Equals("System.IO.Path", StringComparison.Ordinal) AndAlso
               Not typeName.Equals("Microsoft.VisualBasic.FileIO.FileSystem", StringComparison.Ordinal) AndAlso
               Not typeName.Equals("Microsoft.VisualBasic.MyServices.FileSystemProxy", StringComparison.Ordinal) Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(InsecureTempFileAnalyzer.UsagePropertyKey, InsecureTempFileAnalyzer.GetUsage(invocation.Syntax))
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.InsecureTempFile, invocation.Syntax.GetLocation(), properties,
                                                       $"{method.ContainingType.Name}.GetTempFileName"))
        End Sub

        ''' <summary>
        ''' Gets how the path returned by a call is used.
        ''' </summary>
        ''' 
        ''' <param name="call">
        ''' The invocation syntax.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see cref="DeclarationUsage"/>, <see cref="AssignmentUsage"/>, <see cref="ReturnUsage"/>, or an empty string.
        ''' </returns>
        Private Shared Function GetUsage([call] As SyntaxNode) As String

            Dim parent As SyntaxNode = [call].Parent
            If TypeOf parent Is EqualsValueSyntax Then
                Dim declarator As VariableDeclaratorSyntax = TryCast(parent.Parent, VariableDeclaratorSyntax)
                Dim declaration As LocalDeclarationStatementSyntax = TryCast(declarator?.Parent, LocalDeclarationStatementSyntax)
                If declaration IsNot Nothing AndAlso declaration.Declarators.Count = 1 AndAlso declarator.Names.Count = 1 AndAlso
                   declarator.Names(0).ArrayRankSpecifiers.Count = 0 Then
                    Return InsecureTempFileAnalyzer.DeclarationUsage
                End If
            ElseIf parent.IsKind(SyntaxKind.SimpleAssignmentStatement) Then
                Dim assignment As AssignmentStatementSyntax = DirectCast(parent, AssignmentStatementSyntax)
                If assignment.Right Is [call] AndAlso UtilAnalyzerSyntax.IsSimpleReference(assignment.Left) AndAlso
                   TypeOf assignment.Parent IsNot SingleLineIfStatementSyntax AndAlso TypeOf assignment.Parent IsNot SingleLineElseClauseSyntax AndAlso
                   TypeOf assignment.Parent IsNot SingleLineLambdaExpressionSyntax Then
                    Return InsecureTempFileAnalyzer.AssignmentUsage
                End If
            ElseIf TypeOf parent Is ReturnStatementSyntax AndAlso TypeOf parent.Parent IsNot SingleLineIfStatementSyntax AndAlso
                   TypeOf parent.Parent IsNot SingleLineElseClauseSyntax AndAlso TypeOf parent.Parent IsNot SingleLineLambdaExpressionSyntax Then
                Return InsecureTempFileAnalyzer.ReturnUsage
            End If
            Return If(InsecureTempFileAnalyzer.IsExtractable([call]), InsecureTempFileAnalyzer.ExpressionUsage, String.Empty)
        End Function

        ''' <summary>
        ''' Determines whether a call can be moved to a variable declared just before its statement, without changing 
        ''' whether or how many times it runs.
        ''' </summary>
        ''' 
        ''' <param name="call">
        ''' The invocation syntax.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> when every expression between the call and its statement always evaluates the call once 
        ''' (no lambda, query, <c>If(...)</c> branch, right side of <see langword="AndAlso"/> / <see langword="OrElse"/>, or <c>?.</c>), 
        ''' and the statement is the first one of its line in the statement list of a multi-line block; otherwise, <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="call"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsExtractable([call] As SyntaxNode) As Boolean

            If [call] Is Nothing Then
                Throw New ArgumentNullException(NameOf([call]))
            End If

            Dim node As SyntaxNode = [call]
            Dim parent As SyntaxNode = node.Parent
            While parent IsNot Nothing AndAlso TypeOf parent IsNot StatementSyntax
                Dim conditional As Boolean =
                    TypeOf parent Is LambdaExpressionSyntax OrElse TypeOf parent Is QueryExpressionSyntax OrElse TypeOf parent Is QueryClauseSyntax OrElse
                    (TypeOf parent Is TernaryConditionalExpressionSyntax AndAlso DirectCast(parent, TernaryConditionalExpressionSyntax).Condition IsNot node) OrElse
                    (TypeOf parent Is BinaryConditionalExpressionSyntax AndAlso DirectCast(parent, BinaryConditionalExpressionSyntax).SecondExpression Is node) OrElse
                    ((parent.IsKind(SyntaxKind.AndAlsoExpression) OrElse parent.IsKind(SyntaxKind.OrElseExpression)) AndAlso DirectCast(parent, BinaryExpressionSyntax).Right Is node) OrElse
                    (TypeOf parent Is ConditionalAccessExpressionSyntax AndAlso DirectCast(parent, ConditionalAccessExpressionSyntax).WhenNotNull Is node)
                If conditional Then
                    Return False
                End If
                node = parent
                parent = node.Parent
            End While

            Dim statement As StatementSyntax = TryCast(parent, StatementSyntax)
            If statement Is Nothing OrElse Not UtilAnalyzerSyntax.GetBlockStatements(statement.Parent).Contains(statement) Then
                Return False
            End If

            ' Only white space before the statement on its line (no "a = 1 : statement").
            Dim source As SourceText = statement.SyntaxTree.GetText()
            Dim lineStart As Integer = source.Lines.GetLineFromPosition(statement.SpanStart).Start
            Return String.IsNullOrWhiteSpace(source.ToString(TextSpan.FromBounds(lineStart, statement.SpanStart)))
        End Function

#End Region

    End Class

End Namespace

#End Region
