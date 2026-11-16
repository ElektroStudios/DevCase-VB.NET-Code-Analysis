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

#Region " IndexerAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports LINQ <c>First()</c>, <c>Last()</c> and <c>ElementAt(index)</c> calls on types that implement <c>IList(Of T)</c> 
    ''' and expose an indexer (rule <see cref="DiagnosticIds.LinqInsteadOfIndexer"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Arrays use <c>Length</c> and other types their <c>Count</c> property for <c>Last()</c>. 
    ''' Calls with a predicate (<c>First(predicate)</c>) and the <c>OrDefault</c> variants are not reported, because the indexer has no equivalent. 
    ''' <c>Last()</c> on a receiver that is not a simple name or member access (e.g. a method call) is reported without a fix, 
    ''' because the receiver would be evaluated twice.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class IndexerAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that indicates whether a code fix can be applied (<c>"True"</c> or <c>"False"</c>).
        ''' </summary>
        Public Const CanFixPropertyKey As String = "CanFix"

        ''' <summary>
        ''' The diagnostic property that carries the name of the element count property (<c>Count</c> or <c>Length</c>).
        ''' </summary>
        Public Const CountPropertyPropertyKey As String = "CountProperty"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.LinqInsteadOfIndexer)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="IndexerAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf IndexerAnalyzer.AnalyzeInvocation, SyntaxKind.InvocationExpression)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the name of the property that returns the number of elements of an indexable collection type.
        ''' </summary>
        ''' 
        ''' <param name="type">
        ''' The collection type.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <c>Length</c> for arrays, <c>Count</c> when the type exposes it; otherwise, <see langword="Nothing"/>.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetCountPropertyName(type As ITypeSymbol) As String

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            If TypeOf type Is IArrayTypeSymbol Then
                Return NameOf(Array.Length)
            End If

            Dim hasCount As Boolean = IndexerAnalyzer.GetAccessibleProperties(type).Any(
                Function(p As IPropertySymbol) p.Name.Equals("Count", StringComparison.Ordinal) AndAlso p.Parameters.IsEmpty AndAlso
                                               p.Type.SpecialType = SpecialType.System_Int32)
            Return If(hasCount, "Count", Nothing)
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

            Dim methodName As String = access.Name.Identifier.ValueText
            Dim argumentCount As Integer = If(invocation.ArgumentList Is Nothing, 0, invocation.ArgumentList.Arguments.Count)
            Select Case methodName
                Case "First", "Last"
                    If argumentCount <> 0 Then
                        Exit Sub
                    End If
                Case "ElementAt"
                    If argumentCount <> 1 OrElse invocation.ArgumentList.Arguments(0).IsNamed OrElse
                       TypeOf invocation.ArgumentList.Arguments(0) Is OmittedArgumentSyntax Then
                        Exit Sub
                    End If
                Case Else
                    Exit Sub
            End Select

            Dim method As IMethodSymbol = TryCast(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol, IMethodSymbol)
            If method Is Nothing OrElse method.ReducedFrom Is Nothing OrElse
               Not method.ReducedFrom.ContainingType.ToDisplayString().Equals("System.Linq.Enumerable", StringComparison.Ordinal) OrElse
               (methodName.Equals("ElementAt", StringComparison.Ordinal) AndAlso method.Parameters(0).Type.SpecialType <> SpecialType.System_Int32) Then
                Exit Sub
            End If

            Dim receiverType As ITypeSymbol = context.SemanticModel.GetTypeInfo(access.Expression, context.CancellationToken).Type
            If receiverType Is Nothing OrElse Not IndexerAnalyzer.IsIndexableList(receiverType) Then
                Exit Sub
            End If

            Dim receiverSymbol As ISymbol = context.SemanticModel.GetSymbolInfo(access.Expression, context.CancellationToken).Symbol
            Dim countName As String = IndexerAnalyzer.GetCountPropertyName(receiverType)
            Dim canFix As Boolean = Not methodName.Equals("Last", StringComparison.Ordinal) OrElse
                                    (countName IsNot Nothing AndAlso UtilAnalyzerSyntax.IsSimpleReference(access.Expression) AndAlso TypeOf receiverSymbol IsNot IMethodSymbol)

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(IndexerAnalyzer.CanFixPropertyKey, canFix.ToString(CultureInfo.InvariantCulture)).
                Add(IndexerAnalyzer.CountPropertyPropertyKey, If(countName, String.Empty))
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.LinqInsteadOfIndexer, invocation.GetLocation(), properties,
                                                       methodName, receiverType.ToMinimalDisplayString(context.SemanticModel, invocation.SpanStart)))
        End Sub

        ''' <summary>
        ''' Determines whether a type implements <c>IList(Of T)</c> and exposes a public <see cref="Integer"/> indexer.
        ''' </summary>
        ''' 
        ''' <param name="type">
        ''' The type.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the type can be indexed directly; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsIndexableList(type As ITypeSymbol) As Boolean

            If TypeOf type Is IArrayTypeSymbol Then
                Return DirectCast(type, IArrayTypeSymbol).IsSZArray
            End If

            Dim implementsList As Boolean =
                type.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IList_T OrElse
                type.AllInterfaces.Any(Function(i As INamedTypeSymbol) i.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IList_T)
            Return implementsList AndAlso IndexerAnalyzer.GetAccessibleProperties(type).Any(
                Function(p As IPropertySymbol) p.IsIndexer AndAlso p.Parameters.Length = 1 AndAlso p.Parameters(0).Type.SpecialType = SpecialType.System_Int32)
        End Function

        ''' <summary>
        ''' Gets the public instance properties of a type, its base types and, for interfaces, its base interfaces.
        ''' </summary>
        ''' 
        ''' <param name="type">
        ''' The type.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The properties.
        ''' </returns>
        Private Shared Function GetAccessibleProperties(type As ITypeSymbol) As IEnumerable(Of IPropertySymbol)

            Dim types As New List(Of ITypeSymbol)()
            Dim current As ITypeSymbol = type
            While current IsNot Nothing
                types.Add(current)
                current = current.BaseType
            End While
            If type.TypeKind = TypeKind.Interface Then
                types.AddRange(type.AllInterfaces)
            End If

            Return types.SelectMany(Function(t As ITypeSymbol) t.GetMembers().OfType(Of IPropertySymbol)()).
                         Where(Function(p As IPropertySymbol) Not p.IsStatic AndAlso p.DeclaredAccessibility = Accessibility.Public)
        End Function

#End Region

    End Class

End Namespace

#End Region
