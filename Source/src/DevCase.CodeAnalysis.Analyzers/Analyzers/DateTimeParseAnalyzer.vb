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
Imports Microsoft.CodeAnalysis.Operations

#End Region

#Region " DateTimeParseAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Parse</c>, <c>TryParse</c>, <c>ParseExact</c> and <c>TryParseExact</c> calls on <see cref="DateTime"/>, 
    ''' <see cref="DateTimeOffset"/>, <see cref="TimeSpan"/>, <c>DateOnly</c> and <c>TimeOnly</c> that do not pass an 
    ''' <see cref="IFormatProvider"/> although an overload accepts one (rule <see cref="DiagnosticIds.DateTimeParseWithoutProvider"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The overload is matched through the semantic model: it must have the same parameters plus an <see cref="IFormatProvider"/> 
    ''' (optionally followed by a <see cref="Globalization.DateTimeStyles"/> or <see cref="Globalization.TimeSpanStyles"/> parameter, 
    ''' filled with <c>None</c> by the code fix) inserted at one position. 
    ''' For example, <c>DateTime.TryParse(s, result)</c> maps to <c>DateTime.TryParse(s, provider, DateTimeStyles.None, result)</c> on 
    ''' .NET Framework, and to <c>DateTime.TryParse(s, provider, result)</c> on .NET 7+.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DateTimeParseAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that carries the zero-based argument index where the provider is inserted.
        ''' </summary>
        Public Const InsertIndexPropertyKey As String = "InsertIndex"

        ''' <summary>
        ''' The diagnostic property that carries the name of the provider parameter.
        ''' </summary>
        Public Const ProviderParameterPropertyKey As String = "ProviderParameter"

        ''' <summary>
        ''' The diagnostic property that carries the name of the styles parameter, or an empty string when the overload has none.
        ''' </summary>
        Public Const StylesParameterPropertyKey As String = "StylesParameter"

        ''' <summary>
        ''' The diagnostic property that carries the full name of the styles type, or an empty string when the overload has none.
        ''' </summary>
        Public Const StylesTypePropertyKey As String = "StylesType"

        ''' <summary>
        ''' The metadata names of the types whose parsing methods are analyzed.
        ''' </summary>
        Private Shared ReadOnly ParsingTypeNames As String() = {
            "System.DateTime", "System.DateTimeOffset", "System.TimeSpan", "System.DateOnly", "System.TimeOnly"}

        ''' <summary>
        ''' The metadata names of the styles enumerations that may follow the provider parameter.
        ''' </summary>
        Private Shared ReadOnly StylesTypeNames As String() = {"System.Globalization.DateTimeStyles", "System.Globalization.TimeSpanStyles"}

        ''' <summary>
        ''' The names of the analyzed parsing methods.
        ''' </summary>
        Private Shared ReadOnly MethodNames As String() = {"Parse", "TryParse", "ParseExact", "TryParseExact"}

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DateTimeParseWithoutProvider)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DateTimeParseAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf DateTimeParseAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Finds the overload that inserts an <see cref="IFormatProvider"/> parameter (optionally followed by a styles parameter) 
        ''' into the parameter list of the specified parsing method.
        ''' </summary>
        ''' 
        ''' <param name="method">
        ''' The invoked parsing method.
        ''' </param>
        ''' 
        ''' <param name="providerType">
        ''' The <see cref="IFormatProvider"/> type symbol.
        ''' </param>
        ''' 
        ''' <param name="stylesTypes">
        ''' The styles enumeration type symbols (<see cref="DateTimeStyles"/>, <see cref="TimeSpanStyles"/>).
        ''' </param>
        ''' 
        ''' <param name="insertIndex">
        ''' When this method returns, the zero-based index where the new arguments are inserted.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The overload, or <see langword="Nothing"/> if the method already has a provider or no such overload exists.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="method"/>, <paramref name="providerType"/> or <paramref name="stylesTypes"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindProviderOverload(method As IMethodSymbol, providerType As ITypeSymbol, stylesTypes As IEnumerable(Of ITypeSymbol),
                                                    ByRef insertIndex As Integer) As IMethodSymbol

            If method Is Nothing Then
                Throw New ArgumentNullException(NameOf(method))
            End If
            If providerType Is Nothing Then
                Throw New ArgumentNullException(NameOf(providerType))
            End If
            If stylesTypes Is Nothing Then
                Throw New ArgumentNullException(NameOf(stylesTypes))
            End If

            insertIndex = -1
            If method.Parameters.Any(Function(p As IParameterSymbol) SymbolEqualityComparer.Default.Equals(p.Type, providerType)) Then
                Return Nothing
            End If

            Dim count As Integer = method.Parameters.Length
            Dim best As IMethodSymbol = Nothing
            For Each candidate As IMethodSymbol In method.ContainingType.GetMembers(method.Name).OfType(Of IMethodSymbol)()
                Dim extra As Integer = candidate.Parameters.Length - count
                If Not candidate.IsStatic OrElse candidate.DeclaredAccessibility <> Accessibility.Public OrElse extra < 1 OrElse extra > 2 Then
                    Continue For
                End If

                For index As Integer = 0 To count
                    If DateTimeParseAnalyzer.IsInsertion(method, candidate, index, extra, providerType, stylesTypes) AndAlso
                       (best Is Nothing OrElse candidate.Parameters.Length < best.Parameters.Length) Then
                        best = candidate
                        insertIndex = index
                    End If
                Next index
            Next candidate
            Return best
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Determines whether a candidate overload equals the original method with a provider block inserted at the specified index.
        ''' </summary>
        ''' 
        ''' <param name="method">
        ''' The original method.
        ''' </param>
        ''' 
        ''' <param name="candidate">
        ''' The candidate overload.
        ''' </param>
        ''' 
        ''' <param name="index">
        ''' The insertion index.
        ''' </param>
        ''' 
        ''' <param name="extra">
        ''' The number of inserted parameters (1: provider; 2: provider and styles).
        ''' </param>
        ''' 
        ''' <param name="providerType">
        ''' The <see cref="IFormatProvider"/> type symbol.
        ''' </param>
        ''' 
        ''' <param name="stylesTypes">
        ''' The styles enumeration type symbols.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the candidate matches; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsInsertion(method As IMethodSymbol, candidate As IMethodSymbol, index As Integer, extra As Integer,
                                            providerType As ITypeSymbol, stylesTypes As IEnumerable(Of ITypeSymbol)) As Boolean

            If Not SymbolEqualityComparer.Default.Equals(candidate.Parameters(index).Type, providerType) Then
                Return False
            End If
            If extra = 2 AndAlso Not stylesTypes.Contains(candidate.Parameters(index + 1).Type, SymbolEqualityComparer.Default) Then
                Return False
            End If

            For position As Integer = 0 To method.Parameters.Length - 1
                Dim original As IParameterSymbol = method.Parameters(position)
                Dim mapped As IParameterSymbol = candidate.Parameters(If(position < index, position, position + extra))
                If original.RefKind <> mapped.RefKind OrElse Not SymbolEqualityComparer.Default.Equals(original.Type, mapped.Type) Then
                    Return False
                End If
            Next position
            Return True
        End Function

        ''' <summary>
        ''' Resolves the parsing types and registers the invocation action.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            Dim providerType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(GetType(IFormatProvider).FullName)
            If providerType Is Nothing Then
                Exit Sub
            End If

            Dim parsingTypes As New HashSet(Of ITypeSymbol)(SymbolEqualityComparer.Default)
            For Each metadataName As String In DateTimeParseAnalyzer.ParsingTypeNames
                Dim type As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(metadataName)
                If type IsNot Nothing Then
                    parsingTypes.Add(type)
                End If
            Next metadataName

            Dim stylesTypes As New List(Of ITypeSymbol)()
            For Each metadataName As String In DateTimeParseAnalyzer.StylesTypeNames
                Dim type As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(metadataName)
                If type IsNot Nothing Then
                    stylesTypes.Add(type)
                End If
            Next metadataName

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext)
                    Dim invocation As IInvocationOperation = DirectCast(c.Operation, IInvocationOperation)
                    Dim method As IMethodSymbol = invocation.TargetMethod
                    If Not method.IsStatic OrElse Not parsingTypes.Contains(method.ContainingType) OrElse
                       Not DateTimeParseAnalyzer.MethodNames.Contains(method.Name, StringComparer.Ordinal) Then
                        Exit Sub
                    End If

                    Dim insertIndex As Integer
                    Dim overload As IMethodSymbol = DateTimeParseAnalyzer.FindProviderOverload(method, providerType, stylesTypes, insertIndex)
                    If overload Is Nothing Then
                        Exit Sub
                    End If

                    Dim hasStyles As Boolean = overload.Parameters.Length - method.Parameters.Length = 2
                    Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                        Add(DateTimeParseAnalyzer.InsertIndexPropertyKey, insertIndex.ToString(CultureInfo.InvariantCulture)).
                        Add(DateTimeParseAnalyzer.ProviderParameterPropertyKey, overload.Parameters(insertIndex).Name).
                        Add(DateTimeParseAnalyzer.StylesParameterPropertyKey, If(hasStyles, overload.Parameters(insertIndex + 1).Name, String.Empty)).
                        Add(DateTimeParseAnalyzer.StylesTypePropertyKey, If(hasStyles, overload.Parameters(insertIndex + 1).Type.ToDisplayString(), String.Empty))

                    c.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DateTimeParseWithoutProvider, invocation.Syntax.GetLocation(), properties,
                                                         $"{method.ContainingType.Name}.{method.Name}"))
                End Sub), OperationKind.Invocation)
        End Sub

#End Region

    End Class

End Namespace

#End Region
