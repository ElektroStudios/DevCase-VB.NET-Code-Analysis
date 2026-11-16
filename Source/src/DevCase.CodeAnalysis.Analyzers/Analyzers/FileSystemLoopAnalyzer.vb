#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text.RegularExpressions
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " FileSystemLoopAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="For Each"/> loops over file-system enumeration methods of <c>System.IO.Directory</c> or 
    ''' <c>System.IO.DirectoryInfo</c> whose body does not call <c>Thread.CurrentThread.Join(0)</c> 
    ''' (rule <see cref="DiagnosticIds.FileSystemLoopWithoutMessagePumping"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' The enumerated expression may contain the call directly (also inside a LINQ chain, e.g. <c>Directory.EnumerateFiles(...).Where(...)</c>) 
    ''' or be a local variable whose declaration initializer contains it.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class FileSystemLoopAnalyzer : Inherits DiagnosticAnalyzer

#Region " Fields "

        ''' <summary>
        ''' The enumeration methods of <c>System.IO.Directory</c> and <c>System.IO.DirectoryInfo</c>.
        ''' </summary>
        Private Shared ReadOnly EnumerationMethodNames As String() = {
            "GetFiles", "GetDirectories", "GetFileSystemEntries", "GetFileSystemInfos",
            "EnumerateFiles", "EnumerateDirectories", "EnumerateFileSystemEntries", "EnumerateFileSystemInfos"}

#End Region

#Region " Fields "

        ''' <summary>
        ''' Matches <c>Thread.CurrentThread.Join(0)</c> (optionally qualified) inside inactive conditional compilation text.
        ''' </summary>
        Private Shared ReadOnly DisabledJoinPattern As New Regex("(?<![\w.])(?:(?:Global\.)?System\.)?(?:Threading\.)?Thread\s*\.\s*CurrentThread\s*\.\s*Join\s*\(\s*0\s*\)",
                                                                 RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1))

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.FileSystemLoopWithoutMessagePumping)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FileSystemLoopAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf FileSystemLoopAnalyzer.AnalyzeForEach, SyntaxKind.ForEachBlock)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether a loop body calls <c>Thread.Join(0)</c> (on any thread instance, typically <c>Thread.CurrentThread</c>).
        ''' </summary>
        ''' 
        ''' <remarks>
        ''' The call also counts when it is inside a conditional compilation block that is inactive in the current configuration 
        ''' (e.g. <c>#If DEBUG Then</c> in a Release build), where it is only text: it is then recognized by its text, 
        ''' <c>Thread.CurrentThread.Join(0)</c>.
        ''' </remarks>
        '''
        ''' <param name="forEachBlock">
        ''' The loop block.
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
        ''' <see langword="True"/> if the call is present; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="forEachBlock"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function CallsThreadJoinZero(forEachBlock As ForEachBlockSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            If forEachBlock Is Nothing Then
                Throw New ArgumentNullException(NameOf(forEachBlock))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            For Each statement As StatementSyntax In forEachBlock.Statements
                For Each invocation As InvocationExpressionSyntax In statement.DescendantNodesAndSelf().OfType(Of InvocationExpressionSyntax)()
                    Dim method As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
                    If method Is Nothing OrElse Not method.Name.Equals("Join", StringComparison.Ordinal) OrElse
                       Not method.ContainingType.ToDisplayString().Equals("System.Threading.Thread", StringComparison.Ordinal) OrElse
                       invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 Then
                        Continue For
                    End If

                    Dim argument As SimpleArgumentSyntax = TryCast(invocation.ArgumentList.Arguments(0), SimpleArgumentSyntax)
                    Dim constant As [Optional](Of Object) = If(argument Is Nothing, Nothing, semanticModel.GetConstantValue(argument.Expression, cancellationToken))
                    If constant.HasValue AndAlso TypeOf constant.Value Is Integer AndAlso DirectCast(constant.Value, Integer) = 0 Then
                        Return True
                    End If
                Next invocation
            Next statement

            ' Inactive conditional blocks (e.g. "#If DEBUG Then" in Release) are disabled text, not statements.
            Return forEachBlock.DescendantTrivia(descendIntoTrivia:=True).Any(
                Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.DisabledTextTrivia) AndAlso
                                             FileSystemLoopAnalyzer.DisabledJoinPattern.IsMatch(t.ToString()))
        End Function

        ''' <summary>
        ''' Gets the file-system enumeration method enumerated by a loop, if any.
        ''' </summary>
        '''
        ''' <param name="forEachBlock">
        ''' The loop block.
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
        ''' The enumeration method, or <see langword="Nothing"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="forEachBlock"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetEnumerationMethod(forEachBlock As ForEachBlockSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IMethodSymbol

            If forEachBlock Is Nothing Then
                Throw New ArgumentNullException(NameOf(forEachBlock))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim source As ExpressionSyntax = forEachBlock.ForEachStatement.Expression
            Dim found As IMethodSymbol = FileSystemLoopAnalyzer.FindEnumerationCall(source, semanticModel, cancellationToken)
            If found IsNot Nothing Then
                Return found
            End If

            ' A local variable initialized with the enumeration call: "Dim files As String() = Directory.GetFiles(...)".
            Dim identifier As IdentifierNameSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(source), IdentifierNameSyntax)
            Dim local As ILocalSymbol = If(identifier Is Nothing, Nothing, TryCast(semanticModel.GetSymbolInfo(identifier, cancellationToken).Symbol, ILocalSymbol))
            If local Is Nothing Then
                Return Nothing
            End If

            For Each reference As SyntaxReference In local.DeclaringSyntaxReferences
                Dim declarator As VariableDeclaratorSyntax = reference.GetSyntax(cancellationToken).FirstAncestorOrSelf(Of VariableDeclaratorSyntax)()
                If declarator IsNot Nothing AndAlso declarator.Initializer IsNot Nothing Then
                    Return FileSystemLoopAnalyzer.FindEnumerationCall(declarator.Initializer.Value, semanticModel, cancellationToken)
                End If
            Next reference
            Return Nothing
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a <see langword="For Each"/> loop.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeForEach(context As SyntaxNodeAnalysisContext)

            Dim forEachBlock As ForEachBlockSyntax = DirectCast(context.Node, ForEachBlockSyntax)
            Dim method As IMethodSymbol = FileSystemLoopAnalyzer.GetEnumerationMethod(forEachBlock, context.SemanticModel, context.CancellationToken)
            If method Is Nothing OrElse FileSystemLoopAnalyzer.CallsThreadJoinZero(forEachBlock, context.SemanticModel, context.CancellationToken) Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.FileSystemLoopWithoutMessagePumping, forEachBlock.ForEachStatement.GetLocation(),
                                                       $"{method.ContainingType.Name}.{method.Name}"))
        End Sub

        ''' <summary>
        ''' Finds a call to a file-system enumeration method inside an expression.
        ''' </summary>
        '''
        ''' <param name="expression">
        ''' The expression to inspect.
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
        ''' The enumeration method, or <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function FindEnumerationCall(expression As ExpressionSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As IMethodSymbol

            For Each invocation As InvocationExpressionSyntax In expression.DescendantNodesAndSelf().OfType(Of InvocationExpressionSyntax)()
                Dim method As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
                If method Is Nothing OrElse Not FileSystemLoopAnalyzer.EnumerationMethodNames.Contains(method.Name, StringComparer.Ordinal) Then
                    Continue For
                End If

                Dim typeName As String = method.ContainingType.ToDisplayString()
                If typeName.Equals("System.IO.Directory", StringComparison.Ordinal) OrElse typeName.Equals("System.IO.DirectoryInfo", StringComparison.Ordinal) Then
                    Return method
                End If
            Next invocation
            Return Nothing
        End Function

#End Region

    End Class

End Namespace

#End Region
