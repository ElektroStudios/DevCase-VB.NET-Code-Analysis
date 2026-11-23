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

#Region " RegexTimeoutAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports regular expressions that are evaluated without a match timeout (rule <see cref="DiagnosticIds.RegexWithoutTimeout"/>):
    ''' <c>New Regex(...)</c> and calls to the static <c>Regex</c> methods (<c>IsMatch</c>, <c>Match</c>, <c>Matches</c>,
    ''' <c>Replace</c>, <c>Split</c>, <c>Count</c>, <c>EnumerateMatches</c>, ...) that use an overload without a <see cref="TimeSpan"/> parameter.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Overloads are matched through the semantic model: a call is reported only when an overload with the same parameters plus
    ''' a trailing <c>matchTimeout</c> parameter (optionally preceded by <c>options</c>) exists, so the rule adapts to every target framework.
    ''' <para></para>
    ''' Not reported:
    ''' <list type="bullet">
    '''   <item><description>Instance methods (<c>regex.Match(input)</c>): they use the timeout given to the constructor, which is where the rule applies.</description></item>
    '''   <item><description>Calls whose options are a constant that includes <c>RegexOptions.NonBacktracking</c> (.NET 7+),
    '''   which guarantees linear-time matching.</description></item>
    '''   <item><description>Calls that already pass a timeout, including an explicit <c>Regex.InfiniteMatchTimeout</c> (a deliberate decision).</description></item>
    ''' </list>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class RegexTimeoutAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that indicates whether the fix must also add an <c>options</c> argument (<c>"True"</c> or <c>"False"</c>).
        ''' </summary>
        Public Const AddsOptionsPropertyKey As String = "AddsOptions"

        ''' <summary>
        ''' The diagnostic property that carries the name of the <c>options</c> parameter of the timeout overload.
        ''' </summary>
        Public Const OptionsParameterPropertyKey As String = "OptionsParameter"

        ''' <summary>
        ''' The diagnostic property that carries the name of the timeout parameter of the timeout overload.
        ''' </summary>
        Public Const TimeoutParameterPropertyKey As String = "TimeoutParameter"

        ''' <summary>
        ''' The metadata name of the <c>Regex</c> type.
        ''' </summary>
        Friend Const RegexMetadataName As String = "System.Text.RegularExpressions.Regex"

        ''' <summary>
        ''' The metadata name of the <c>RegexOptions</c> type.
        ''' </summary>
        Friend Const RegexOptionsMetadataName As String = "System.Text.RegularExpressions.RegexOptions"

        ''' <summary>
        ''' The value of <c>RegexOptions.NonBacktracking</c>.
        ''' </summary>
        Private Const NonBacktrackingValue As Long = &H400

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.RegexWithoutTimeout)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RegexTimeoutAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf RegexTimeoutAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Finds the overload that accepts a match timeout for the specified <c>Regex</c> method or constructor.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The invoked method or constructor.
        ''' </param>
        '''
        ''' <param name="timeSpanType">
        ''' The <see cref="TimeSpan"/> type symbol.
        ''' </param>
        '''
        ''' <param name="optionsType">
        ''' The <c>RegexOptions</c> type symbol.
        ''' </param>
        '''
        ''' <param name="addsOptions">
        ''' When this method returns, <see langword="True"/> if the overload also requires an <c>options</c> argument.
        ''' </param>
        '''
        ''' <returns>
        ''' The overload, or <see langword="Nothing"/> if the method already has a timeout or no such overload exists.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="method"/>, <paramref name="timeSpanType"/> or <paramref name="optionsType"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindTimeoutOverload(method As IMethodSymbol, timeSpanType As ITypeSymbol, optionsType As ITypeSymbol,
                                                   ByRef addsOptions As Boolean) As IMethodSymbol

            If method Is Nothing Then
                Throw New ArgumentNullException(NameOf(method))
            End If
            If timeSpanType Is Nothing Then
                Throw New ArgumentNullException(NameOf(timeSpanType))
            End If
            If optionsType Is Nothing Then
                Throw New ArgumentNullException(NameOf(optionsType))
            End If

            addsOptions = False
            If method.Parameters.Any(Function(p As IParameterSymbol) SymbolEqualityComparer.Default.Equals(p.Type, timeSpanType)) Then
                Return Nothing
            End If

            Dim candidates As IEnumerable(Of IMethodSymbol) =
                If(method.MethodKind = MethodKind.Constructor,
                   method.ContainingType.InstanceConstructors.AsEnumerable(),
                   method.ContainingType.GetMembers(method.Name).OfType(Of IMethodSymbol)().Where(Function(m As IMethodSymbol) m.IsStatic = method.IsStatic))

            Dim count As Integer = method.Parameters.Length
            For Each candidate As IMethodSymbol In candidates
                If candidate.DeclaredAccessibility <> Accessibility.Public OrElse
                   (candidate.Parameters.Length <> count + 1 AndAlso candidate.Parameters.Length <> count + 2) OrElse
                   Not SymbolEqualityComparer.Default.Equals(candidate.Parameters.Last().Type, timeSpanType) Then
                    Continue For
                End If

                Dim samePrefix As Boolean = True
                For index As Integer = 0 To count - 1
                    If Not SymbolEqualityComparer.Default.Equals(candidate.Parameters(index).Type, method.Parameters(index).Type) Then
                        samePrefix = False
                        Exit For
                    End If
                Next index
                If Not samePrefix Then
                    Continue For
                End If

                If candidate.Parameters.Length = count + 2 Then
                    If Not SymbolEqualityComparer.Default.Equals(candidate.Parameters(count).Type, optionsType) Then
                        Continue For
                    End If
                    addsOptions = True
                End If
                Return candidate
            Next candidate

            Return Nothing
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Resolves the regular expression types and registers the operation actions when they are available.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            Dim regexType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(RegexTimeoutAnalyzer.RegexMetadataName)
            Dim optionsType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(RegexTimeoutAnalyzer.RegexOptionsMetadataName)
            Dim timeSpanType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(GetType(TimeSpan).FullName)
            If regexType Is Nothing OrElse optionsType Is Nothing OrElse timeSpanType Is Nothing Then
                Exit Sub ' The project does not reference System.Text.RegularExpressions.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext)
                    Dim invocation As IInvocationOperation = DirectCast(c.Operation, IInvocationOperation)
                    If invocation.TargetMethod.IsStatic AndAlso SymbolEqualityComparer.Default.Equals(invocation.TargetMethod.ContainingType, regexType) Then
                        RegexTimeoutAnalyzer.Analyze(c, invocation.TargetMethod, invocation.Arguments, $"Regex.{invocation.TargetMethod.Name}", timeSpanType, optionsType)
                    End If
                End Sub), OperationKind.Invocation)

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext)
                    Dim creation As IObjectCreationOperation = DirectCast(c.Operation, IObjectCreationOperation)
                    If creation.Constructor IsNot Nothing AndAlso SymbolEqualityComparer.Default.Equals(creation.Constructor.ContainingType, regexType) Then
                        RegexTimeoutAnalyzer.Analyze(c, creation.Constructor, creation.Arguments, "New Regex", timeSpanType, optionsType)
                    End If
                End Sub), OperationKind.ObjectCreation)
        End Sub

        ''' <summary>
        ''' Reports the operation when it evaluates a regular expression without a match timeout.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The invoked method or constructor.
        ''' </param>
        '''
        ''' <param name="arguments">
        ''' The arguments of the operation.
        ''' </param>
        '''
        ''' <param name="display">
        ''' The text shown in the diagnostic message.
        ''' </param>
        '''
        ''' <param name="timeSpanType">
        ''' The <see cref="TimeSpan"/> type symbol.
        ''' </param>
        '''
        ''' <param name="optionsType">
        ''' The <c>RegexOptions</c> type symbol.
        ''' </param>
        Private Shared Sub Analyze(context As OperationAnalysisContext, method As IMethodSymbol, arguments As ImmutableArray(Of IArgumentOperation),
                                   display As String, timeSpanType As ITypeSymbol, optionsType As ITypeSymbol)

            ' RegexOptions.NonBacktracking guarantees linear-time matching: a timeout is not needed.
            For Each argument As IArgumentOperation In arguments
                If argument.Parameter IsNot Nothing AndAlso SymbolEqualityComparer.Default.Equals(argument.Parameter.Type, optionsType) AndAlso
                   argument.Value.ConstantValue.HasValue AndAlso argument.Value.ConstantValue.Value IsNot Nothing AndAlso
                   (Convert.ToInt64(argument.Value.ConstantValue.Value, CultureInfo.InvariantCulture) And RegexTimeoutAnalyzer.NonBacktrackingValue) <> 0 Then
                    Exit Sub
                End If
            Next argument

            Dim addsOptions As Boolean
            Dim overload As IMethodSymbol = RegexTimeoutAnalyzer.FindTimeoutOverload(method, timeSpanType, optionsType, addsOptions)
            If overload Is Nothing Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(RegexTimeoutAnalyzer.AddsOptionsPropertyKey, addsOptions.ToString(CultureInfo.InvariantCulture)).
                Add(RegexTimeoutAnalyzer.OptionsParameterPropertyKey, If(addsOptions, overload.Parameters(overload.Parameters.Length - 2).Name, String.Empty)).
                Add(RegexTimeoutAnalyzer.TimeoutParameterPropertyKey, overload.Parameters.Last().Name)

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.RegexWithoutTimeout, context.Operation.Syntax.GetLocation(), properties, display))
        End Sub

#End Region

    End Class

End Namespace

#End Region
