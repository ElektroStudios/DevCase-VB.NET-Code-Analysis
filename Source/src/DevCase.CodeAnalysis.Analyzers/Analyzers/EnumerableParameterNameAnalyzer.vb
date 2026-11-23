#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " EnumerableParameterNameAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports parameters typed <see cref="System.Collections.IEnumerable"/> or <see cref="IEnumerable(Of T)"/> whose name
    ''' describes a collection instead of a sequence (rule <see cref="DiagnosticIds.CollectionNameOnEnumerableParameter"/>),
    ''' for example <c>Public Sub AddRange(collection As IEnumerable(Of String))</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Reported names (case-insensitive): <c>collection</c>, <c>list</c> and <c>array</c>, renamed to <c>sequence</c>;
    ''' and the abbreviations <c>col</c>, <c>coll</c>, <c>lst</c> and <c>arr</c>, renamed to <c>seq</c>. When that name is
    ''' already used in the member, the next free name is suggested: <c>sequence</c> (for abbreviations), <c>source</c>,
    ''' <c>value</c> and <c>items</c>; when all of them are used, the parameter is reported without a code fix.
    ''' </para>
    ''' <para>
    ''' Only the interfaces themselves are reported: a parameter typed <see cref="ICollection(Of T)"/>,
    ''' <see cref="List(Of T)"/>, an array or any other type that implements <see cref="System.Collections.IEnumerable"/> is
    ''' a collection, and its name is right. Not reported either, because the name is defined elsewhere: parameters of
    ''' <see langword="Overrides"/> and <see langword="Implements"/> members, of the implementation part of a partial method,
    ''' of events and of delegates.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class EnumerableParameterNameAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the suggested parameter name.
        ''' </summary>
        Friend Const NewNamePropertyKey As String = "NewName"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The candidate names for a parameter with a full collection name (<c>collection</c>, <c>list</c>, <c>array</c>),
        ''' in order of preference.
        ''' </summary>
        Private Shared ReadOnly FullNameCandidates As ImmutableArray(Of String) = ImmutableArray.Create("sequence", "source", "value", "items")

        ''' <summary>
        ''' The candidate names for a parameter with an abbreviated collection name (<c>col</c>, <c>coll</c>, <c>lst</c>, <c>arr</c>),
        ''' in order of preference.
        ''' </summary>
        Private Shared ReadOnly AbbreviationCandidates As ImmutableArray(Of String) = ImmutableArray.Create("seq", "sequence", "source", "value", "items")

        ''' <summary>
        ''' The reported names, with the candidate names of each one.
        ''' </summary>
        Private Shared ReadOnly ReportedNames As ImmutableDictionary(Of String, ImmutableArray(Of String)) =
            ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, {
                New KeyValuePair(Of String, ImmutableArray(Of String))("collection", EnumerableParameterNameAnalyzer.FullNameCandidates),
                New KeyValuePair(Of String, ImmutableArray(Of String))("list", EnumerableParameterNameAnalyzer.FullNameCandidates),
                New KeyValuePair(Of String, ImmutableArray(Of String))("array", EnumerableParameterNameAnalyzer.FullNameCandidates),
                New KeyValuePair(Of String, ImmutableArray(Of String))("col", EnumerableParameterNameAnalyzer.AbbreviationCandidates),
                New KeyValuePair(Of String, ImmutableArray(Of String))("coll", EnumerableParameterNameAnalyzer.AbbreviationCandidates),
                New KeyValuePair(Of String, ImmutableArray(Of String))("lst", EnumerableParameterNameAnalyzer.AbbreviationCandidates),
                New KeyValuePair(Of String, ImmutableArray(Of String))("arr", EnumerableParameterNameAnalyzer.AbbreviationCandidates)})

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.CollectionNameOnEnumerableParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="EnumerableParameterNameAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf EnumerableParameterNameAnalyzer.AnalyzeParameter),
                                             SyntaxKind.Parameter)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a parameter of type <see cref="System.Collections.IEnumerable"/> or <see cref="IEnumerable(Of T)"/>
        ''' with a collection name.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeParameter(context As SyntaxNodeAnalysisContext)

            Dim parameterSyntax As ParameterSyntax = DirectCast(context.Node, ParameterSyntax)
            Dim candidates As ImmutableArray(Of String) = Nothing
            If parameterSyntax.AsClause Is Nothing OrElse
               Not EnumerableParameterNameAnalyzer.ReportedNames.TryGetValue(parameterSyntax.Identifier.Identifier.ValueText, candidates) Then
                Exit Sub
            End If

            ' The name of an event or delegate parameter is part of a signature declared once and seen by every handler.
            Dim declaration As SyntaxNode = parameterSyntax.Parent?.Parent
            If TypeOf declaration Is EventStatementSyntax OrElse TypeOf declaration Is DelegateStatementSyntax Then
                Exit Sub
            End If

            Dim parameter As IParameterSymbol = context.SemanticModel.GetDeclaredSymbol(parameterSyntax, context.CancellationToken)
            If parameter Is Nothing OrElse Not EnumerableParameterNameAnalyzer.IsEnumerableInterface(parameter.Type) OrElse
               EnumerableParameterNameAnalyzer.IsNameDefinedElsewhere(parameter.ContainingSymbol) Then
                Exit Sub
            End If

            ' When every candidate is already used in the member, the rule still reports the parameter, without a code fix.
            Dim newName As String = UtilParameterNames.ChooseName(parameterSyntax, candidates)
            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty
            Dim suggestion As String = String.Empty
            If newName IsNot Nothing Then
                properties = properties.Add(EnumerableParameterNameAnalyzer.NewNamePropertyKey, newName)
                suggestion = $" to '{newName}'"
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.CollectionNameOnEnumerableParameter,
                                                       parameterSyntax.Identifier.Identifier.GetLocation(), properties,
                                                       parameter.Name, EnumerableParameterNameAnalyzer.GetMemberName(parameter.ContainingSymbol), suggestion))
        End Sub

        ''' <summary>
        ''' Determines whether a type is exactly <see cref="System.Collections.IEnumerable"/> or a construction of
        ''' <see cref="IEnumerable(Of T)"/>.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type is one of the enumerable interfaces; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsEnumerableInterface(type As ITypeSymbol) As Boolean

            Return type IsNot Nothing AndAlso
                   (type.SpecialType = SpecialType.System_Collections_IEnumerable OrElse
                    type.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IEnumerable_T)
        End Function

        ''' <summary>
        ''' Determines whether the parameter names of a member are defined by another declaration:
        ''' an overridden or implemented member, or the definition part of a partial method.
        ''' </summary>
        '''
        ''' <param name="member">
        ''' The member that declares the parameter.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the parameter must keep its name; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsNameDefinedElsewhere(member As ISymbol) As Boolean

            Dim method As IMethodSymbol = TryCast(member, IMethodSymbol)
            If method IsNot Nothing Then
                Return method.IsOverride OrElse Not method.ExplicitInterfaceImplementations.IsEmpty OrElse
                       method.PartialDefinitionPart IsNot Nothing
            End If

            Dim [property] As IPropertySymbol = TryCast(member, IPropertySymbol)
            Return If([property] IsNot Nothing,
                      [property].IsOverride OrElse Not [property].ExplicitInterfaceImplementations.IsEmpty,
                      member Is Nothing)
        End Function

        ''' <summary>
        ''' Gets the name of a member, as shown in the message: <c>Type.Member</c>, <c>Type.New</c> for a constructor,
        ''' and the name of the enclosing member for a lambda expression.
        ''' </summary>
        '''
        ''' <param name="member">
        ''' The member that declares the parameter.
        ''' </param>
        '''
        ''' <returns>
        ''' The display name of the member.
        ''' </returns>
        Private Shared Function GetMemberName(member As ISymbol) As String

            Dim current As ISymbol = member
            While TypeOf current Is IMethodSymbol AndAlso DirectCast(current, IMethodSymbol).MethodKind = MethodKind.LambdaMethod
                current = current.ContainingSymbol
            End While

            Dim method As IMethodSymbol = TryCast(current, IMethodSymbol)
            Dim name As String = If(method IsNot Nothing AndAlso method.MethodKind = MethodKind.Constructor, "New", current.Name)
            Return If(current.ContainingType Is Nothing, name, $"{current.ContainingType.Name}.{name}")
        End Function

#End Region

    End Class

End Namespace

#End Region
