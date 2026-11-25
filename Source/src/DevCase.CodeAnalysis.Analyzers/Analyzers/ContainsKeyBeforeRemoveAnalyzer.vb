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

#Region " ContainsKeyBeforeRemoveAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>If map.ContainsKey(key) Then map.Remove(key)</c> on a dictionary (rule
    ''' <see cref="DiagnosticIds.ContainsKeyBeforeRemove"/>; equivalent of CA1853 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>Remove</c> already looks the key up and returns <see langword="False"/> when it does not exist, so the check doubles the
    ''' lookup. The <see langword="If"/> statement must have no <see langword="ElseIf"/> or <see langword="Else"/> part, <c>Remove</c>
    ''' must be its first statement, with the same receiver (a simple reference) and the same key (without side effects), and the type
    ''' must implement <c>IDictionary(Of TKey, TValue)</c>. Statements with preprocessor directives inside are not reported, because
    ''' another configuration may compile other statements.
    ''' </para>
    ''' <para>
    ''' The code fix writes <c>map.Remove(key)</c> when it is the only statement, or <c>If map.Remove(key) Then</c> followed by the other
    ''' statements; the comments at the end of the lines of a removed statement are kept after the call. The fix is not offered when
    ''' other comments would be lost, nor for a single-line <see langword="If"/> with several statements.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ContainsKeyBeforeRemoveAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ContainsKeyBeforeRemove)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ContainsKeyBeforeRemoveAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ContainsKeyBeforeRemoveAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1853 reports the same code.
            End If

            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ContainsKeyBeforeRemoveAnalyzer.AnalyzeIf),
                                             SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Remove</c> guarded by <c>ContainsKey</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIf(context As SyntaxNodeAnalysisContext)

            Dim check As InvocationExpressionSyntax = Nothing
            Dim negated As Boolean
            Dim guarded As InvocationExpressionSyntax = Nothing
            If Not UtilGuardedCalls.TryMatch(context.Node, "ContainsKey", check, negated, guarded) OrElse negated Then
                Exit Sub
            End If

            Dim model As SemanticModel = context.SemanticModel
            Dim containsKey As IMethodSymbol = TryCast(model.GetSymbolInfo(check, context.CancellationToken).Symbol, IMethodSymbol)
            Dim remove As IMethodSymbol = TryCast(model.GetSymbolInfo(guarded, context.CancellationToken).Symbol, IMethodSymbol)
            Dim receiverType As ITypeSymbol = model.GetTypeInfo(DirectCast(check.Expression, MemberAccessExpressionSyntax).Expression, context.CancellationToken).Type
            If containsKey Is Nothing OrElse remove Is Nothing OrElse Not containsKey.Name.Equals("ContainsKey", StringComparison.Ordinal) OrElse
               Not remove.Name.Equals("Remove", StringComparison.Ordinal) OrElse containsKey.Parameters.Length <> 1 OrElse remove.Parameters.Length <> 1 OrElse
               remove.ReturnType.SpecialType <> SpecialType.System_Boolean OrElse
               Not SymbolEqualityComparer.Default.Equals(containsKey.Parameters(0).Type, remove.Parameters(0).Type) OrElse
               Not UtilAnalyzerSemantic.IsOrDerivesFrom(receiverType, "System.Collections.Generic.IDictionary`2") Then
                Exit Sub
            End If

            UtilGuardedCalls.Report(context, DevCaseDescriptors.ContainsKeyBeforeRemove, check, guarded, "Remove the ContainsKey check")
        End Sub

#End Region

    End Class

End Namespace

#End Region
