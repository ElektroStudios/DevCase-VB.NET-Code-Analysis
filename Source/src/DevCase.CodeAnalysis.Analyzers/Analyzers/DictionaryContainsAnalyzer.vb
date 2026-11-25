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

#End Region

#Region " DictionaryContainsAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Contains</c> called on the <c>Keys</c> or <c>Values</c> of a dictionary that has <c>ContainsKey</c> or
    ''' <c>ContainsValue</c> (rule <see cref="DiagnosticIds.KeysOrValuesContains"/>; equivalent of CA1841 for .NET Framework projects),
    ''' for example <c>settings.Keys.Contains(name)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The call goes through the key or value collection (the <c>Enumerable.Contains</c> extension, or an <c>ICollection(Of T)</c> that
    ''' some dictionaries implement as a linear search) instead of the lookup of the dictionary. Calls with a comparer are not reported,
    ''' because <c>ContainsKey</c> and <c>ContainsValue</c> use the comparer of the dictionary.
    ''' </para>
    ''' <para>
    ''' The rule is only reported when <c>dictionary.ContainsKey(key)</c> or <c>dictionary.ContainsValue(value)</c> binds at that position,
    ''' which the code fix writes.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DictionaryContainsAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.KeysOrValuesContains)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DictionaryContainsAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf DictionaryContainsAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1841 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf DictionaryContainsAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Contains</c> on the keys or values of a dictionary.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            If Not method.Name.Equals("Contains", StringComparison.Ordinal) Then
                Exit Sub
            End If

            Dim collection As IOperation
            If UtilAnalyzerSemantic.IsMember(method, "System.Linq.Enumerable", NameOf(Enumerable.Contains)) Then
                If UtilAnalyzerSemantic.GetExtensionParameterCount(invocation) <> 1 Then
                    Exit Sub ' The overload with a comparer.
                End If
                collection = UtilAnalyzerSemantic.GetExtensionSource(invocation)
            ElseIf Not method.IsStatic AndAlso Not method.IsExtensionMethod AndAlso method.Parameters.Length = 1 Then
                collection = invocation.Instance
            Else
                Exit Sub
            End If

            Dim keys As IPropertyReferenceOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(collection), IPropertyReferenceOperation)
            If keys Is Nothing OrElse keys.Instance Is Nothing OrElse Not keys.Property.Parameters.IsEmpty OrElse invocation.Arguments.IsEmpty Then
                Exit Sub
            End If

            Dim lookupName As String
            Select Case keys.Property.Name
                Case "Keys"
                    lookupName = "ContainsKey"
                Case "Values"
                    lookupName = "ContainsValue"
                Case Else
                    Exit Sub
            End Select

            Dim item As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(invocation.Arguments(invocation.Arguments.Length - 1).Value)
            Dim receiver As String = If(keys.Instance.IsImplicit, String.Empty, $"{UtilAnalyzerSyntax.GetReceiverText(keys.Instance.Syntax)}.")
            Dim replacement As String = $"{receiver}{lookupName}({item.Syntax})"
            Dim speculative As IMethodSymbol = TryCast(UtilSpeculativeBinding.GetSymbol(invocation.SemanticModel, invocation.Syntax.SpanStart, replacement), IMethodSymbol)
            If speculative Is Nothing OrElse Not speculative.Name.Equals(lookupName, StringComparison.Ordinal) OrElse
               speculative.ReturnType.SpecialType <> SpecialType.System_Boolean OrElse speculative.Parameters.Length <> 1 Then
                Exit Sub
            End If

            Dim actions As FixReplacement() = {New FixReplacement($"Use {lookupName}", "DirectLookup", replacement)}
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.KeysOrValuesContains, invocation.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions, lookupName, keys.Property.Name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
