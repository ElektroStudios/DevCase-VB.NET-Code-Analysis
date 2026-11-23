#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " CollectionMethodAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports LINQ <c>FirstOrDefault</c>, <c>Any</c> and <c>All</c> calls with a predicate on <c>List(Of T)</c>, 
    ''' <c>ImmutableList(Of T)</c> and arrays, which provide the faster <c>Find</c>, <c>Exists</c> and <c>TrueForAll</c> methods 
    ''' (rule <see cref="DiagnosticIds.LinqInsteadOfCollectionMethod"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Only extension-method calls whose predicate is a lambda expression or an <see langword="AddressOf"/> expression are reported: 
    ''' a delegate variable of type <c>Func(Of T, Boolean)</c> cannot be passed where a <c>Predicate(Of T)</c> is expected. 
    ''' Overloads with a default value (<c>FirstOrDefault(predicate, defaultValue)</c>) and explicit type arguments are not reported.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class CollectionMethodAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that carries the name of the replacement method.
        ''' </summary>
        Public Const ReplacementPropertyKey As String = "Replacement"

        ''' <summary>
        ''' The diagnostic property that indicates whether the receiver is an array (<c>"True"</c> or <c>"False"</c>).
        ''' </summary>
        Public Const IsArrayPropertyKey As String = "IsArray"

        ''' <summary>
        ''' The full name of the <c>System.Linq.Enumerable</c> type.
        ''' </summary>
        Private Const EnumerableTypeName As String = "System.Linq.Enumerable"

        ''' <summary>
        ''' The display names of the generic list types that provide <c>Find</c>, <c>Exists</c> and <c>TrueForAll</c>.
        ''' </summary>
        Private Shared ReadOnly ListTypeNames As String() = {"System.Collections.Generic.List(Of T)", "System.Collections.Immutable.ImmutableList(Of T)"}

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.LinqInsteadOfCollectionMethod)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CollectionMethodAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf CollectionMethodAnalyzer.AnalyzeInvocation), SyntaxKind.InvocationExpression)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the name of the collection-specific method that replaces a LINQ method.
        ''' </summary>
        ''' 
        ''' <param name="linqMethodName">
        ''' The LINQ method name.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <c>Find</c>, <c>Exists</c> or <c>TrueForAll</c>; or <see langword="Nothing"/> if the method has no replacement.
        ''' </returns>
        Friend Shared Function GetReplacementName(linqMethodName As String) As String

            Select Case linqMethodName
                Case "FirstOrDefault"
                    Return "Find"
                Case "Any"
                    Return "Exists"
                Case "All"
                    Return "TrueForAll"
                Case Else
                    Return Nothing
            End Select
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes an invocation expression.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As SyntaxNodeAnalysisContext)

            Dim invocation As InvocationExpressionSyntax = DirectCast(context.Node, InvocationExpressionSyntax)
            Dim access As MemberAccessExpressionSyntax = TryCast(invocation.Expression, MemberAccessExpressionSyntax)
            If access Is Nothing OrElse access.Expression Is Nothing OrElse TypeOf access.Name IsNot IdentifierNameSyntax Then
                Exit Sub
            End If

            Dim linqName As String = access.Name.Identifier.ValueText
            Dim replacement As String = CollectionMethodAnalyzer.GetReplacementName(linqName)
            If replacement Is Nothing OrElse invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 Then
                Exit Sub
            End If

            ' Positional or named (Any(predicate:=...)): the fix writes it positionally, because the List method 
            ' names its parameter differently (Exists(match), TrueForAll(match)).
            Dim argument As ArgumentSyntax = invocation.ArgumentList.Arguments(0)
            If TypeOf argument Is OmittedArgumentSyntax Then
                Exit Sub
            End If
            Dim predicate As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(argument.GetExpression())
            If TypeOf predicate IsNot LambdaExpressionSyntax AndAlso Not predicate.IsKind(SyntaxKind.AddressOfExpression) Then
                Exit Sub
            End If

            Dim method As IMethodSymbol = TryCast(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol, IMethodSymbol)
            If method Is Nothing OrElse method.ReducedFrom Is Nothing OrElse
               Not method.ReducedFrom.ContainingType.ToDisplayString().Equals(CollectionMethodAnalyzer.EnumerableTypeName, StringComparison.Ordinal) Then
                Exit Sub
            End If

            Dim receiverType As ITypeSymbol = context.SemanticModel.GetTypeInfo(access.Expression, context.CancellationToken).Type
            Dim isArray As Boolean = TypeOf receiverType Is IArrayTypeSymbol AndAlso DirectCast(receiverType, IArrayTypeSymbol).IsSZArray
            Dim isList As Boolean = receiverType IsNot Nothing AndAlso
                                    CollectionMethodAnalyzer.ListTypeNames.Contains(receiverType.OriginalDefinition.ToDisplayString(), StringComparer.Ordinal) AndAlso
                                    receiverType.GetMembers(replacement).OfType(Of IMethodSymbol)().Any(Function(m As IMethodSymbol) m.DeclaredAccessibility = Accessibility.Public)
            If Not isArray AndAlso Not isList Then
                Exit Sub
            End If

            Dim display As String = If(isArray, $"Array.{replacement}", replacement)
            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(CollectionMethodAnalyzer.ReplacementPropertyKey, replacement).
                Add(CollectionMethodAnalyzer.IsArrayPropertyKey, isArray.ToString(CultureInfo.InvariantCulture))
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.LinqInsteadOfCollectionMethod, invocation.GetLocation(), properties,
                                                       display, linqName, receiverType.ToMinimalDisplayString(context.SemanticModel, invocation.SpanStart)))
        End Sub

#End Region

    End Class

End Namespace

#End Region
