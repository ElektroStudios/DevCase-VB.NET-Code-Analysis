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

#Region " BeginInvokeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to the <c>BeginInvoke</c> method of a delegate that are not paired with <c>EndInvoke</c> 
    ''' (rule <see cref="DiagnosticIds.BeginInvokeWithoutEndInvoke"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Only delegate types are analyzed: <c>Control.BeginInvoke</c> and <c>Dispatcher.BeginInvoke</c> do not require <c>EndInvoke</c>. 
    ''' A call is paired when <c>EndInvoke</c> is called in the same method or lambda, or in the callback passed to <c>BeginInvoke</c> 
    ''' (a lambda, or a method of the same compilation referenced with <see langword="AddressOf"/>). 
    ''' Calls whose callback is any other expression (a variable, a field) are not reported, because the callback cannot be inspected.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BeginInvokeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.BeginInvokeWithoutEndInvoke)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="BeginInvokeAnalyzer"/> class.
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
            context.RegisterOperationAction(AddressOf BeginInvokeAnalyzer.AnalyzeInvocation, OperationKind.Invocation)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a delegate <c>BeginInvoke</c> invocation that is not paired with <c>EndInvoke</c>.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            If Not method.Name.Equals("BeginInvoke", StringComparison.Ordinal) OrElse method.ContainingType.TypeKind <> TypeKind.Delegate Then
                Exit Sub
            End If

            Dim boundary As SyntaxNode = UtilAnalyzerSyntax.GetExecutableBoundary(invocation.Syntax)
            If boundary IsNot Nothing AndAlso BeginInvokeAnalyzer.ContainsEndInvoke(boundary) Then
                Exit Sub
            End If

            Dim callback As IArgumentOperation = invocation.Arguments.FirstOrDefault(
                Function(a As IArgumentOperation) a.Parameter IsNot Nothing AndAlso a.Parameter.Type.ToDisplayString().Equals("System.AsyncCallback", StringComparison.Ordinal))
            If callback Is Nothing Then
                Exit Sub
            End If

            Dim value As IOperation = callback.Value
            While TypeOf value Is IConversionOperation
                value = DirectCast(value, IConversionOperation).Operand
            End While

            If value.ConstantValue.HasValue AndAlso value.ConstantValue.Value Is Nothing Then
                BeginInvokeAnalyzer.Report(context, method)
                Exit Sub
            End If

            Dim creation As IDelegateCreationOperation = TryCast(value, IDelegateCreationOperation)
            If creation Is Nothing Then
                Exit Sub ' A variable or a field: the callback cannot be inspected.
            End If

            If TypeOf creation.Target Is IAnonymousFunctionOperation Then
                If Not BeginInvokeAnalyzer.ContainsEndInvoke(creation.Target.Syntax) Then
                    BeginInvokeAnalyzer.Report(context, method)
                End If
            ElseIf TypeOf creation.Target Is IMethodReferenceOperation Then
                Dim target As IMethodSymbol = DirectCast(creation.Target, IMethodReferenceOperation).Method
                If target.DeclaringSyntaxReferences.Length > 0 AndAlso
                   Not target.DeclaringSyntaxReferences.Any(Function(r As SyntaxReference) BeginInvokeAnalyzer.ContainsEndInvoke(r.GetSyntax(context.CancellationToken).Parent)) Then
                    BeginInvokeAnalyzer.Report(context, method)
                End If
            End If
        End Sub

        ''' <summary>
        ''' Determines whether a node contains a call to an <c>EndInvoke</c> method.
        ''' </summary>
        ''' 
        ''' <param name="node">
        ''' The node to search. May be <see langword="Nothing"/>.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the node contains an <c>EndInvoke</c> call; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function ContainsEndInvoke(node As SyntaxNode) As Boolean

            If node Is Nothing Then
                Return False ' Documented: there is nothing to search.
            End If

            Return node.DescendantNodesAndSelf().OfType(Of MemberAccessExpressionSyntax)().Any(
                Function(m As MemberAccessExpressionSyntax) m.Name.Identifier.ValueText.Equals("EndInvoke", StringComparison.OrdinalIgnoreCase) AndAlso
                                                             TypeOf m.Parent Is InvocationExpressionSyntax)
        End Function

        ''' <summary>
        ''' Reports the current invocation.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        ''' 
        ''' <param name="method">
        ''' The <c>BeginInvoke</c> method.
        ''' </param>
        Private Shared Sub Report(context As OperationAnalysisContext, method As IMethodSymbol)

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.BeginInvokeWithoutEndInvoke, context.Operation.Syntax.GetLocation(),
                                                       method.ContainingType.Name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
