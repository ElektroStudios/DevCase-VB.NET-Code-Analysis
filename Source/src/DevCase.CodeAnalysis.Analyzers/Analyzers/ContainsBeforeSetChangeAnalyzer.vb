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

#Region " ContainsBeforeSetChangeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>If Not items.Contains(item) Then items.Add(item)</c> and <c>If items.Contains(item) Then items.Remove(item)</c> on
    ''' a set (rule <see cref="DiagnosticIds.ContainsBeforeSetAddOrRemove"/>; equivalent of CA1868 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>Add</c> returns <see langword="False"/> when the item already exists and <c>Remove</c> when it does not, so the check doubles
    ''' the lookup. The <see langword="If"/> statement must have no <see langword="ElseIf"/> or <see langword="Else"/> part, the call must
    ''' be its first statement, with the same receiver (a simple reference) and the same item (without side effects), the type must
    ''' implement <c>ISet(Of T)</c>, and the method must return <see cref="Boolean"/>.
    ''' Statements with preprocessor directives inside are not reported, because another configuration may compile other statements.
    ''' </para>
    ''' <para>
    ''' The code fix writes <c>items.Add(item)</c> when it is the only statement, or <c>If items.Add(item) Then</c> followed by the other
    ''' statements; the comments at the end of the lines of a removed statement are kept after the call. The fix is not offered when
    ''' other comments would be lost, nor for a single-line <see langword="If"/> with several statements.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ContainsBeforeSetChangeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ContainsBeforeSetAddOrRemove)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ContainsBeforeSetChangeAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ContainsBeforeSetChangeAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1868 reports the same code.
            End If

            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ContainsBeforeSetChangeAnalyzer.AnalyzeIf),
                                             SyntaxKind.MultiLineIfBlock, SyntaxKind.SingleLineIfStatement)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Add</c> or <c>Remove</c> of a set guarded by <c>Contains</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIf(context As SyntaxNodeAnalysisContext)

            Dim check As InvocationExpressionSyntax = Nothing
            Dim negated As Boolean
            Dim guarded As InvocationExpressionSyntax = Nothing
            If Not UtilGuardedCalls.TryMatch(context.Node, "Contains", check, negated, guarded) Then
                Exit Sub
            End If

            ' "Not Contains" guards Add; "Contains" guards Remove.
            Dim expectedName As String = If(negated, "Add", "Remove")
            Dim model As SemanticModel = context.SemanticModel
            Dim contains As IMethodSymbol = TryCast(model.GetSymbolInfo(check, context.CancellationToken).Symbol, IMethodSymbol)
            Dim change As IMethodSymbol = TryCast(model.GetSymbolInfo(guarded, context.CancellationToken).Symbol, IMethodSymbol)
            Dim receiverType As ITypeSymbol = model.GetTypeInfo(DirectCast(check.Expression, MemberAccessExpressionSyntax).Expression, context.CancellationToken).Type
            If contains Is Nothing OrElse change Is Nothing OrElse contains.IsExtensionMethod OrElse
               Not contains.Name.Equals("Contains", StringComparison.Ordinal) OrElse Not change.Name.Equals(expectedName, StringComparison.Ordinal) OrElse
               contains.Parameters.Length <> 1 OrElse change.Parameters.Length <> 1 OrElse change.ReturnType.SpecialType <> SpecialType.System_Boolean OrElse
               Not SymbolEqualityComparer.Default.Equals(contains.Parameters(0).Type, change.Parameters(0).Type) OrElse
               Not UtilAnalyzerSemantic.IsOrDerivesFrom(receiverType, "System.Collections.Generic.ISet`1") Then
                Exit Sub
            End If

            UtilGuardedCalls.Report(context, DevCaseDescriptors.ContainsBeforeSetAddOrRemove, check, guarded, "Remove the Contains check", expectedName)
        End Sub

#End Region

    End Class

End Namespace

#End Region
