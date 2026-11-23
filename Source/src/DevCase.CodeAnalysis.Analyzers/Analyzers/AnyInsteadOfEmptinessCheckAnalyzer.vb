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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " AnyInsteadOfEmptinessCheckAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to <see cref="Enumerable.Any(Of TSource)(IEnumerable(Of TSource))"/> (without a predicate) on a collection that
    ''' exposes an <c>IsEmpty</c>, <c>Length</c> or <c>Count</c> property (rule <see cref="DiagnosticIds.AnyInsteadOfEmptinessCheck"/>;
    ''' equivalent of CA1860 for .NET Framework projects), for example <c>While queue.Any()</c> → <c>While queue.Count &lt;&gt; 0</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The property is chosen as CA1860 does: <c>IsEmpty</c> first (concurrent collections, whose <c>Count</c> is expensive),
    ''' then <c>Length</c> (arrays and strings), then <c>Count</c>. It must be an accessible, readable instance property,
    ''' <see cref="Boolean"/> for <c>IsEmpty</c> and <see cref="Integer"/> for the others, and the only one with that name
    ''' (an interface that inherits <c>Count</c> from two interfaces, such as <see cref="ICollection(Of T)"/> and
    ''' <see cref="IReadOnlyCollection(Of T)"/>, makes it ambiguous). Both call forms are reported: <c>items.Any()</c> and
    ''' <c>Enumerable.Any(items)</c>; <c>items?.Any()</c> is not, because it returns a nullable value.
    ''' </para>
    ''' <para>
    ''' The rule runs only for .NET Framework compilations: modern .NET projects get the same suggestion from the built-in rule CA1860.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class AnyInsteadOfEmptinessCheckAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the name of the property to use (<c>IsEmpty</c>, <c>Length</c> or <c>Count</c>).
        ''' </summary>
        Friend Const PropertyNamePropertyKey As String = "PropertyName"

        ''' <summary>
        ''' The key of the diagnostic property that is present when the call has the static form (<c>Enumerable.Any(items)</c>).
        ''' </summary>
        Friend Const StaticFormPropertyKey As String = "StaticForm"

        ''' <summary>
        ''' The name of the <c>IsEmpty</c> property.
        ''' </summary>
        Friend Const IsEmptyName As String = "IsEmpty"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The names of the properties that replace the call, in order of preference.
        ''' </summary>
        Private Shared ReadOnly PropertyNames As ImmutableArray(Of String) = ImmutableArray.Create(AnyInsteadOfEmptinessCheckAnalyzer.IsEmptyName, "Length", "Count")

        ''' <summary>
        ''' The format of the type of the collection in the message: the name with its containing types and type arguments, 
        ''' without namespaces, and with the Visual Basic keywords of the special types (<c>Dictionary(Of String, Integer).KeyCollection</c>).
        ''' </summary>
        Private Shared ReadOnly TypeDisplayFormat As New SymbolDisplayFormat(
            typeQualificationStyle:=SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions:=SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions:=SymbolDisplayMiscellaneousOptions.UseSpecialTypes)

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.AnyInsteadOfEmptinessCheck)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AnyInsteadOfEmptinessCheckAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf AnyInsteadOfEmptinessCheckAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis for .NET Framework compilations that reference <see cref="Enumerable"/>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilAnalyzerSemantic.IsNetFramework(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in rule CA1860 already reports these calls.
            End If

            Dim enumerableType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Linq.Enumerable")
            If enumerableType Is Nothing Then
                Exit Sub
            End If

            context.RegisterOperationAction(
                UtilAnalyzerFiles.SkipExcludedOperations(Sub(c As OperationAnalysisContext) AnyInsteadOfEmptinessCheckAnalyzer.AnalyzeInvocation(c, enumerableType)),
                OperationKind.Invocation)
        End Sub

        ''' <summary>
        ''' Reports a call to <see cref="Enumerable.Any(Of TSource)(IEnumerable(Of TSource))"/> on a collection with an
        ''' <c>IsEmpty</c>, <c>Length</c> or <c>Count</c> property.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="enumerableType">
        ''' The <see cref="Enumerable"/> type.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext, enumerableType As INamedTypeSymbol)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = If(invocation.TargetMethod.ReducedFrom, invocation.TargetMethod)
            If Not method.Name.Equals(NameOf(Enumerable.Any), StringComparison.Ordinal) OrElse method.Parameters.Length <> 1 OrElse
               Not SymbolEqualityComparer.Default.Equals(method.ContainingType, enumerableType) Then
                Exit Sub
            End If

            ' The collection: the instance of the reduced form (items.Any()), or the argument of the static form (Enumerable.Any(items)).
            Dim source As IOperation = If(invocation.Instance, invocation.Arguments.FirstOrDefault()?.Value)
            While TypeOf source Is IConversionOperation AndAlso source.IsImplicit
                source = DirectCast(source, IConversionOperation).Operand
            End While
            If source Is Nothing OrElse source.IsImplicit OrElse source.Type Is Nothing OrElse TypeOf source Is IConditionalAccessInstanceOperation Then
                Exit Sub
            End If

            Dim isStaticForm As Boolean
            If Not AnyInsteadOfEmptinessCheckAnalyzer.TryGetForm(invocation.Syntax, source.Syntax, isStaticForm) Then
                Exit Sub
            End If

            Dim propertyName As String = AnyInsteadOfEmptinessCheckAnalyzer.PropertyNames.FirstOrDefault(
                Function(name As String) AnyInsteadOfEmptinessCheckAnalyzer.HasProperty(invocation.SemanticModel, invocation.Syntax.SpanStart, source.Type, name))
            If propertyName Is Nothing Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) =
                ImmutableDictionary(Of String, String).Empty.Add(AnyInsteadOfEmptinessCheckAnalyzer.PropertyNamePropertyKey, propertyName)
            If isStaticForm Then
                properties = properties.Add(AnyInsteadOfEmptinessCheckAnalyzer.StaticFormPropertyKey, Boolean.TrueString)
            End If
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.AnyInsteadOfEmptinessCheck, invocation.Syntax.GetLocation(), properties,
                                                       propertyName, source.Type.ToDisplayString(AnyInsteadOfEmptinessCheckAnalyzer.TypeDisplayFormat)))
        End Sub

        ''' <summary>
        ''' Determines the form of the call from its syntax: the reduced form (<c>items.Any()</c>, <c>items.Any</c>) or
        ''' the static form (<c>Enumerable.Any(items)</c>).
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The syntax of the call.
        ''' </param>
        '''
        ''' <param name="source">
        ''' The syntax of the collection.
        ''' </param>
        '''
        ''' <param name="isStaticForm">
        ''' When this method returns <see langword="True"/>, indicates whether the call has the static form.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the syntax has one of the supported forms; otherwise, <see langword="False"/>
        ''' (for example, inside a <see langword="With"/> block, <c>.Any()</c> has no written collection).
        ''' </returns>
        Private Shared Function TryGetForm(invocation As SyntaxNode, source As SyntaxNode, ByRef isStaticForm As Boolean) As Boolean

            Dim invocationSyntax As InvocationExpressionSyntax = TryCast(invocation, InvocationExpressionSyntax)
            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(If(invocationSyntax IsNot Nothing, invocationSyntax.Expression, invocation), MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing AndAlso memberAccess.Expression Is source Then
                isStaticForm = False
                Return True
            End If

            Dim arguments As ArgumentListSyntax = invocationSyntax?.ArgumentList
            isStaticForm = True
            Return arguments IsNot Nothing AndAlso arguments.Arguments.Count = 1 AndAlso
                   arguments.Arguments(0).GetExpression() Is source
        End Function

        ''' <summary>
        ''' Determines whether a type has the specified property, accessible at a position: a readable, parameterless instance
        ''' property, <see cref="Boolean"/> for <c>IsEmpty</c> and <see cref="Integer"/> for the others, without ambiguity.
        ''' </summary>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position of the call.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The type of the collection.
        ''' </param>
        '''
        ''' <param name="name">
        ''' The name of the property.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the property can replace the call; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function HasProperty(semanticModel As SemanticModel, position As Integer, type As ITypeSymbol, name As String) As Boolean

            Dim expectedType As SpecialType = If(name.Equals(AnyInsteadOfEmptinessCheckAnalyzer.IsEmptyName, StringComparison.Ordinal),
                                                 SpecialType.System_Boolean, SpecialType.System_Int32)
            Dim candidates As ImmutableArray(Of ISymbol) = semanticModel.LookupSymbols(position, type, name)
            If candidates.Length <> 1 Then
                Return False
            End If

            Dim [property] As IPropertySymbol = TryCast(candidates(0), IPropertySymbol)
            Return [property] IsNot Nothing AndAlso Not [property].IsStatic AndAlso [property].Parameters.IsEmpty AndAlso
                   [property].GetMethod IsNot Nothing AndAlso [property].Type.SpecialType = expectedType AndAlso
                   semanticModel.IsAccessible(position, [property].GetMethod)
        End Function

#End Region

    End Class

End Namespace

#End Region
