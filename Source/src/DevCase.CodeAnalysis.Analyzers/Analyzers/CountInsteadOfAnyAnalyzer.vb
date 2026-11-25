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

#Region " CountInsteadOfAnyAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Enumerable.Count()</c> or <c>LongCount()</c> compared with zero or one to check whether a sequence is empty
    ''' (rule <see cref="DiagnosticIds.CountInsteadOfAny"/>; equivalent of CA1827 for .NET Framework projects), for example
    ''' <c>If query.Count() &gt; 0 Then</c> or <c>If items.Count(Function(i) i.IsValid) = 0 Then</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>Count</c> enumerates every element (or every element that matches the predicate) to compare the total with zero; <c>Any</c>
    ''' stops at the first one. The recognized comparisons are <c>= 0</c>, <c>&lt;&gt; 0</c>, <c>&gt; 0</c>, <c>&gt;= 1</c>,
    ''' <c>&lt; 1</c> and <c>&lt;= 0</c>, with the constant on either side.
    ''' </para>
    ''' <para>
    ''' Calls without a predicate on a type with a <c>Length</c> or <c>Count</c> property are not reported: rule
    ''' <see cref="DiagnosticIds.CountMethodInsteadOfProperty"/> suggests the property there, which is better than <c>Any()</c> in
    ''' .NET Framework (its <c>Any()</c> always creates an enumerator). The code fix writes <c>query.Any()</c> or <c>Not query.Any()</c>,
    ''' keeping the predicate.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class CountInsteadOfAnyAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.CountInsteadOfAny)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CountInsteadOfAnyAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf CountInsteadOfAnyAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1827 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf CountInsteadOfAnyAnalyzer.AnalyzeBinary), OperationKind.Binary)
        End Sub


        ''' <summary>
        ''' Reports the comparison of <c>Count()</c> or <c>LongCount()</c> with zero or one.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeBinary(context As OperationAnalysisContext)

            Dim binary As IBinaryOperation = DirectCast(context.Operation, IBinaryOperation)
            Dim operand As IOperation = Nothing
            Dim meansEmpty As Boolean
            If Not UtilZeroComparisons.TryMatch(binary, equalityOnly:=False, operand:=operand, meansEmpty:=meansEmpty) Then
                Exit Sub
            End If

            Dim invocation As IInvocationOperation = TryCast(operand, IInvocationOperation)
            If invocation Is Nothing OrElse
               (Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.Linq.Enumerable", NameOf(Enumerable.Count)) AndAlso
                Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.Linq.Enumerable", NameOf(Enumerable.LongCount))) Then
                Exit Sub
            End If

            Dim model As SemanticModel = binary.SemanticModel
            Dim position As Integer = binary.Syntax.SpanStart
            Dim source As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(UtilAnalyzerSemantic.GetExtensionSource(invocation))
            If source Is Nothing OrElse
               (UtilAnalyzerSemantic.GetExtensionParameterCount(invocation) = 0 AndAlso UtilAnalyzerSemantic.GetSizePropertyName(model, position, source.Type) IsNot Nothing) Then
                Exit Sub
            End If

            Dim actions As New List(Of FixReplacement)()
            Dim any As String = CountInsteadOfAnyAnalyzer.RenameInvokedMethod(invocation.Syntax, NameOf(Enumerable.Any))
            If any IsNot Nothing AndAlso UtilAnalyzerSemantic.IsMember(UtilSpeculativeBinding.GetSymbol(model, position, any), "System.Linq.Enumerable", NameOf(Enumerable.Any)) Then
                Dim replacement As String = If(meansEmpty, UtilAnalyzerSyntax.Negate(any, binary.Syntax), any)
                actions.Add(New FixReplacement("Use Any", "Any", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.CountInsteadOfAny, binary.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions, invocation.TargetMethod.Name))
        End Sub

        ''' <summary>
        ''' Gets the text of an invocation with another method name (<c>query.Count(predicate)</c> becomes <c>query.Any(predicate)</c>, 
        ''' and a call without parentheses, <c>query.Count</c>, becomes <c>query.Any()</c>).
        ''' </summary>
        '''
        ''' <param name="syntax">
        ''' The invocation: an <see cref="InvocationExpressionSyntax"/>, or the name or member access of a call without parentheses.
        ''' </param>
        '''
        ''' <param name="newName">
        ''' The new method name.
        ''' </param>
        '''
        ''' <returns>
        ''' The text of the invocation; <see langword="Nothing"/> if the invoked name cannot be found.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="syntax"/> is <see langword="Nothing"/>.
        ''' </exception>
        Private Shared Function RenameInvokedMethod(syntax As SyntaxNode, newName As String) As String

            If syntax Is Nothing Then
                Throw New ArgumentNullException(NameOf(syntax))
            End If

            Dim invocation As InvocationExpressionSyntax = TryCast(syntax, InvocationExpressionSyntax)
            Dim expression As ExpressionSyntax = If(invocation IsNot Nothing, invocation.Expression, TryCast(syntax, ExpressionSyntax))
            Dim access As MemberAccessExpressionSyntax = TryCast(expression, MemberAccessExpressionSyntax)
            Dim name As SimpleNameSyntax = If(access IsNot Nothing, access.Name, TryCast(expression, SimpleNameSyntax))
            If name Is Nothing Then
                Return Nothing
            End If

            Dim text As String = syntax.ToString()
            Dim start As Integer = name.Identifier.SpanStart - syntax.SpanStart
            Dim renamed As String = $"{text.Substring(0, start)}{newName}{text.Substring(start + name.Identifier.Span.Length)}"
            Return If(invocation Is Nothing OrElse invocation.ArgumentList Is Nothing, $"{renamed}()", renamed) ' Rule DCA053: parentheses.
        End Function

#End Region

    End Class

End Namespace

#End Region
