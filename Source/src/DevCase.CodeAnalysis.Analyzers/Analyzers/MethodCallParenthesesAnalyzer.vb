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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " MethodCallParenthesesAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports method calls written without parentheses (rule <see cref="DiagnosticIds.MethodCallWithoutParentheses"/>),
    ''' for example <c>query.ToArray</c> or <c>Me.Close</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The analysis is semantic: only explicit calls to methods (<c>IInvocationOperation</c>) whose syntax is a name or a member
    ''' access, instead of an argument list, are reported. Properties, fields, events, <see langword="AddressOf"/> operands,
    ''' <see langword="NameOf"/> arguments and late-bound calls are never invocations of a method, so they are never reported.
    ''' Calls that the compiler generates implicitly (<c>GetEnumerator</c> of <see langword="For Each"/>, <c>Dispose</c> of
    ''' <see langword="Using"/>, <c>GetAwaiter</c> of <see langword="Await"/>, query operators, collection initializers) are ignored.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class MethodCallParenthesesAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MethodCallWithoutParentheses)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="MethodCallParenthesesAnalyzer"/> class.
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
            context.RegisterOperationAction(AddressOf MethodCallParenthesesAnalyzer.AnalyzeInvocation, OperationKind.Invocation)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an explicit method call whose syntax has no argument list.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim syntax As SyntaxNode = invocation.Syntax
            If invocation.IsImplicit Then
                Exit Sub
            End If

            ' As a statement ("Me.Work", "Call Work"), the parser wraps the name in an invocation without an argument list.
            Dim statementCall As InvocationExpressionSyntax = TryCast(syntax, InvocationExpressionSyntax)
            If statementCall IsNot Nothing Then
                If statementCall.ArgumentList IsNot Nothing Then
                    Exit Sub
                End If
                syntax = statementCall.Expression
            End If
            If TypeOf syntax IsNot MemberAccessExpressionSyntax AndAlso TypeOf syntax IsNot SimpleNameSyntax Then
                Exit Sub
            End If

            ' A name that is the target of an invocation with arguments ("GetItems(0)", an index applied to the result)
            ' is not reported: the argument list follows the name.
            Dim parentInvocation As InvocationExpressionSyntax = TryCast(syntax.Parent, InvocationExpressionSyntax)
            If parentInvocation IsNot Nothing AndAlso parentInvocation.Expression Is syntax AndAlso parentInvocation.ArgumentList IsNot Nothing Then
                Exit Sub
            End If

            ' Reported on the method name (with its type arguments, "Cast(Of Integer)"): the fix inserts "()" right after it.
            Dim name As SyntaxNode = If(TypeOf syntax Is MemberAccessExpressionSyntax, DirectCast(syntax, MemberAccessExpressionSyntax).Name, syntax)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MethodCallWithoutParentheses, name.GetLocation(), invocation.TargetMethod.Name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
