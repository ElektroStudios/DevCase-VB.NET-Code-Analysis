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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ZeroLengthArrayAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports the allocation of zero-length arrays, which can be replaced with <see cref="Array.Empty(Of T)"/>
    ''' (rule <see cref="DiagnosticIds.ZeroLengthArrayAllocation"/>; equivalent of CA1825 for .NET Framework projects),
    ''' for example <c>New Object() {New String() {}}</c> → <c>New Object() {Array.Empty(Of String)()}</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Reported forms: <c>New T() {}</c>, <c>New T(-1) {}</c> (any constant upper bound of <c>-1</c>) and the empty array
    ''' literal <c>{}</c> converted to an array type. Only one-dimensional arrays are reported (jagged arrays included:
    ''' <c>New String()() {}</c> → <c>Array.Empty(Of String())()</c>).
    ''' </para>
    ''' <para>
    ''' Not reported: arrays in attribute arguments (which must be constant expressions), the implicit arrays that the compiler
    ''' creates for <see langword="ParamArray"/> parameters and for <c>Dim values(-1) As T</c>, and arrays of anonymous types.
    ''' The rule runs only for .NET Framework compilations that have <see cref="Array.Empty(Of T)"/> (4.6 or later):
    ''' modern .NET projects get the same suggestion from the built-in rule CA1825.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ZeroLengthArrayAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the element type of the array, as Visual Basic code
        ''' (the type argument of <see cref="Array.Empty(Of T)"/>).
        ''' </summary>
        Friend Const ElementTypePropertyKey As String = "ElementType"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ZeroLengthArrayAllocation)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ZeroLengthArrayAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ZeroLengthArrayAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis for .NET Framework compilations that have <see cref="Array.Empty(Of T)"/>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilAnalyzerSemantic.IsNetFramework(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in rule CA1825 already reports these arrays.
            End If

            Dim arrayType As INamedTypeSymbol = context.Compilation.GetSpecialType(SpecialType.System_Array)
            Dim hasArrayEmpty As Boolean = arrayType IsNot Nothing AndAlso arrayType.GetMembers("Empty").OfType(Of IMethodSymbol)().
                Any(Function(m As IMethodSymbol) m.IsStatic AndAlso m.IsGenericMethod AndAlso m.Parameters.IsEmpty AndAlso
                                                 m.DeclaredAccessibility = Accessibility.Public)
            If Not hasArrayEmpty Then
                Exit Sub ' .NET Framework 4.5.2 or earlier.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf ZeroLengthArrayAnalyzer.AnalyzeArrayCreation),
                                            OperationKind.ArrayCreation)
        End Sub

        ''' <summary>
        ''' Reports an array creation with no elements.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeArrayCreation(context As OperationAnalysisContext)

            Dim creation As IArrayCreationOperation = DirectCast(context.Operation, IArrayCreationOperation)
            Dim arrayType As IArrayTypeSymbol = TryCast(creation.Type, IArrayTypeSymbol)
            If creation.IsImplicit OrElse arrayType Is Nothing OrElse arrayType.Rank <> 1 OrElse creation.DimensionSizes.Length <> 1 OrElse
               (creation.Initializer IsNot Nothing AndAlso Not creation.Initializer.ElementValues.IsEmpty) OrElse
               Not ZeroLengthArrayAnalyzer.IsZero(creation.DimensionSizes(0)) Then
                Exit Sub
            End If

            Dim syntax As SyntaxNode = creation.Syntax
            Dim elementType As ITypeSymbol = arrayType.ElementType
            If Not (TypeOf syntax Is ArrayCreationExpressionSyntax OrElse TypeOf syntax Is CollectionInitializerSyntax) OrElse
               syntax.FirstAncestorOrSelf(Of AttributeSyntax)() IsNot Nothing OrElse
               elementType.TypeKind = TypeKind.Error OrElse elementType.IsAnonymousType Then
                Exit Sub
            End If

            Dim typeName As String = elementType.ToMinimalDisplayString(creation.SemanticModel, syntax.SpanStart)
            Dim properties As ImmutableDictionary(Of String, String) =
                ImmutableDictionary(Of String, String).Empty.Add(ZeroLengthArrayAnalyzer.ElementTypePropertyKey, typeName)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ZeroLengthArrayAllocation, syntax.GetLocation(), properties, typeName))
        End Sub

        ''' <summary>
        ''' Determines whether the size of an array dimension is the constant zero.
        ''' </summary>
        '''
        ''' <param name="size">
        ''' The size operation (the upper bound plus one).
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the size is a constant zero; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsZero(size As IOperation) As Boolean

            If Not size.ConstantValue.HasValue OrElse size.ConstantValue.Value Is Nothing Then
                Return False
            End If

            Dim value As Object = size.ConstantValue.Value
            Select Case True
                Case TypeOf value Is Integer
                    Return DirectCast(value, Integer) = 0
                Case TypeOf value Is Long
                    Return DirectCast(value, Long) = 0L
                Case Else
                    Return False
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
