#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ThreadSleepAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to <c>Thread.Sleep</c> inside <see langword="Async"/> methods and lambdas (rule <see cref="DiagnosticIds.ThreadSleepInAsyncMethod"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>Thread.Sleep(0)</c> is not reported: it does not wait, it only yields the rest of the time slice. 
    ''' Calls inside a nested lambda that is not <see langword="Async"/> are not reported either, because the lambda runs synchronously.
    ''' <para></para>
    ''' The diagnostic tells the code fix whether <see langword="Await"/> can replace the call: the call must be a whole statement, 
    ''' and it cannot be inside a <see langword="Catch"/>, <see langword="Finally"/> or <see langword="SyncLock"/> block, 
    ''' where Visual Basic does not allow <see langword="Await"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThreadSleepAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that tells whether the call can be replaced by an <see langword="Await"/> statement 
        ''' (<c>"True"</c> or <c>"False"</c>).
        ''' </summary>
        Public Const CanAwaitPropertyKey As String = "CanAwait"
#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThreadSleepInAsyncMethod)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThreadSleepAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(
                Sub(c As CompilationStartAnalysisContext)
                    Dim threadType As INamedTypeSymbol = c.Compilation.GetTypeByMetadataName("System.Threading.Thread")
                    If threadType IsNot Nothing Then
                        c.RegisterOperationAction(Sub(o As OperationAnalysisContext) ThreadSleepAnalyzer.AnalyzeInvocation(o, threadType),
                                                  OperationKind.Invocation)
                    End If
                End Sub)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether an <see langword="Await"/> statement is allowed at the position of a statement: 
        ''' it must not be inside a <see langword="Catch"/>, <see langword="Finally"/> or <see langword="SyncLock"/> block 
        ''' of its executable boundary.
        ''' </summary>
        ''' 
        ''' <param name="statement">
        ''' The statement.
        ''' </param>
        ''' 
        ''' <param name="boundary">
        ''' The executable boundary of the statement.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if <see langword="Await"/> is allowed; otherwise, <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsAwaitAllowed(statement As SyntaxNode, boundary As SyntaxNode) As Boolean

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If

            Dim current As SyntaxNode = statement.Parent
            While current IsNot Nothing AndAlso current IsNot boundary
                If TypeOf current Is CatchBlockSyntax OrElse TypeOf current Is FinallyBlockSyntax OrElse TypeOf current Is SyncLockBlockSyntax Then
                    Return False
                End If
                current = current.Parent
            End While
            Return True
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an invocation of <c>Thread.Sleep</c> inside an <see langword="Async"/> method or lambda.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        ''' 
        ''' <param name="threadType">
        ''' The <see cref="System.Threading.Thread"/> type symbol.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext, threadType As INamedTypeSymbol)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            If Not invocation.TargetMethod.Name.Equals("Sleep", StringComparison.Ordinal) OrElse
               Not SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, threadType) OrElse
               invocation.Arguments.Length <> 1 Then
                Exit Sub
            End If

            ' Thread.Sleep(0) does not wait: it yields the rest of the time slice.
            Dim constant As [Optional](Of Object) = invocation.Arguments(0).Value.ConstantValue
            If constant.HasValue AndAlso TypeOf constant.Value Is Integer AndAlso CInt(constant.Value) = 0 Then
                Exit Sub
            End If

            Dim boundary As SyntaxNode = UtilAnalyzerSyntax.GetExecutableBoundary(invocation.Syntax)
            If Not UtilAnalyzerSyntax.IsAsyncBoundary(boundary) Then
                Exit Sub
            End If

            Dim statement As SyntaxNode = invocation.Syntax.Parent
            Dim canAwait As Boolean = (TypeOf statement Is ExpressionStatementSyntax OrElse TypeOf statement Is CallStatementSyntax) AndAlso
                                      ThreadSleepAnalyzer.IsAwaitAllowed(statement, boundary)
            Dim properties As ImmutableDictionary(Of String, String) =
                ImmutableDictionary(Of String, String).Empty.Add(ThreadSleepAnalyzer.CanAwaitPropertyKey, canAwait.ToString())
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThreadSleepInAsyncMethod, invocation.Syntax.GetLocation(), properties))
        End Sub

#End Region

    End Class

End Namespace

#End Region
