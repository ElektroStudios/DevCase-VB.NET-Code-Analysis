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

#Region " BlockCopyCountAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Buffer.BlockCopy</c> calls whose <c>count</c> argument is the <c>Length</c> of the source or destination array,
    ''' when its elements are larger than a byte (rule <see cref="DiagnosticIds.BlockCopyLengthAsByteCount"/>; equivalent of CA2018 for
    ''' .NET Framework projects), for example <c>Buffer.BlockCopy(source, 0, target, 0, source.Length)</c> with two <c>Integer()</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <c>count</c> is a number of bytes, so <c>Length</c> copies only a part of an array of <see cref="Integer"/>, <see cref="Char"/>,
    ''' <see cref="Double"/>... Arrays of <see cref="Byte"/>, <see cref="SByte"/> and <see cref="Boolean"/> are not reported, because
    ''' their length is their size in bytes. The code fix writes <c>Buffer.ByteLength(source)</c>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class BlockCopyCountAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.BlockCopyLengthAsByteCount)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="BlockCopyCountAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf BlockCopyCountAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2018 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf BlockCopyCountAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Buffer.BlockCopy</c> that passes the length of an array as the number of bytes.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            If Not UtilAnalyzerSemantic.IsMember(invocation.TargetMethod, "System.Buffer", NameOf(Buffer.BlockCopy)) OrElse
               invocation.TargetMethod.Parameters.Length <> 5 Then
                Exit Sub
            End If

            Dim count As IArgumentOperation = UtilAnalyzerSemantic.GetArgument(invocation.Arguments, 4)
            Dim length As IPropertyReferenceOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(count?.Value), IPropertyReferenceOperation)
            If length Is Nothing OrElse length.Instance Is Nothing OrElse Not UtilAnalyzerSemantic.IsMember(length.Property, "System.Array", NameOf(Array.Length)) Then
                Exit Sub
            End If

            Dim source As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(length.Instance)
            Dim sourceType As IArrayTypeSymbol = TryCast(source.Type, IArrayTypeSymbol)
            If sourceType Is Nothing OrElse BlockCopyCountAnalyzer.IsByteSized(sourceType.ElementType) Then
                Exit Sub
            End If

            ' Only the length of the copied arrays is the element count that the code meant to copy.
            Dim isCopiedArray As Boolean = {0, 2}.Any(
                Function(ordinal As Integer) UtilAnalyzerSyntax.AreEquivalentExpressions(
                    UtilAnalyzerSemantic.SkipImplicitConversions(UtilAnalyzerSemantic.GetArgument(invocation.Arguments, ordinal)?.Value)?.Syntax, source.Syntax))
            If Not isCopiedArray Then
                Exit Sub
            End If

            Dim model As SemanticModel = invocation.SemanticModel
            Dim position As Integer = length.Syntax.SpanStart
            Dim sourceText As String = source.Syntax.ToString()
            Dim bufferName As String = UtilSpeculativeBinding.GetTypeName(model, position, invocation.TargetMethod.ContainingType)
            Dim replacement As String = $"{bufferName}.ByteLength({sourceText})"
            Dim actions As New List(Of FixReplacement)()
            If bufferName IsNot Nothing AndAlso
               UtilAnalyzerSemantic.IsMember(UtilSpeculativeBinding.GetSymbol(model, position, replacement), "System.Buffer", NameOf(Buffer.ByteLength)) Then
                actions.Add(New FixReplacement("Use Buffer.ByteLength", "ByteLength", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.BlockCopyLengthAsByteCount, length.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions, sourceText))
        End Sub

        ''' <summary>
        ''' Determines whether each element of a type occupies one byte in an array.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The element type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for <see cref="Byte"/>, <see cref="SByte"/> and <see cref="Boolean"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsByteSized(type As ITypeSymbol) As Boolean

            Select Case type.SpecialType
                Case SpecialType.System_Byte, SpecialType.System_SByte, SpecialType.System_Boolean
                    Return True
                Case Else
                    Return False
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
