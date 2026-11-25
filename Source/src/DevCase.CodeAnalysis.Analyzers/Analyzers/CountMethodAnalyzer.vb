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

#Region " CountMethodAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Enumerable.Count()</c> without a predicate on a sequence that exposes its size as a property
    ''' (rule <see cref="DiagnosticIds.CountMethodInsteadOfProperty"/>; equivalent of CA1829 for .NET Framework projects), for example
    ''' <c>values.Count()</c> on an array or a <see cref="String"/>, or <c>Enumerable.Count(list)</c> on a <c>List(Of T)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Visual Basic binds <c>list.Count()</c> to the <c>Count</c> property of a collection, so the extension method is only reached on
    ''' arrays and strings (which have <c>Length</c>) and through calls like <c>Enumerable.Count(list)</c>. On a <see cref="String"/>,
    ''' <c>Count()</c> enumerates every character; on an array it goes through interface checks that the property avoids.
    ''' </para>
    ''' <para>
    ''' The code fix writes <c>Length</c> for one-dimensional arrays and strings, and <c>Count</c> for a type with an accessible
    ''' <see cref="Integer"/> <c>Count</c> property.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class CountMethodAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.CountMethodInsteadOfProperty)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CountMethodAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf CountMethodAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1829 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf CountMethodAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Enumerable.Count()</c> on a sequence with a size property.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            If Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.Linq.Enumerable", NameOf(Enumerable.Count)) OrElse
               UtilAnalyzerSemantic.GetExtensionParameterCount(invocation) <> 0 Then
                Exit Sub
            End If

            Dim source As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(UtilAnalyzerSemantic.GetExtensionSource(invocation))
            If source Is Nothing OrElse source.IsImplicit OrElse TypeOf source Is IConditionalAccessInstanceOperation Then
                Exit Sub
            End If

            Dim model As SemanticModel = invocation.SemanticModel
            Dim position As Integer = invocation.Syntax.SpanStart
            Dim propertyName As String = UtilAnalyzerSemantic.GetSizePropertyName(model, position, source.Type)
            If propertyName Is Nothing Then
                Exit Sub
            End If

            Dim replacement As String = $"{UtilAnalyzerSyntax.GetReceiverText(source.Syntax)}.{propertyName}"
            Dim speculative As IPropertySymbol = TryCast(UtilSpeculativeBinding.GetSymbol(model, position, replacement), IPropertySymbol)
            Dim actions As New List(Of FixReplacement)()
            If speculative IsNot Nothing AndAlso speculative.Name.Equals(propertyName, StringComparison.Ordinal) Then
                actions.Add(New FixReplacement($"Use the {propertyName} property", "Property", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.CountMethodInsteadOfProperty, invocation.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions, propertyName))
        End Sub

#End Region

    End Class

End Namespace

#End Region
