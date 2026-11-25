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

#Region " TaskCompletionSourceOptionsAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>TaskContinuationOptions</c> values passed to the constructor of <c>TaskCompletionSource(Of T)</c>
    ''' (rule <see cref="DiagnosticIds.TaskContinuationOptionsAsState"/>; equivalent of CA2247 for .NET Framework projects), for example
    ''' <c>New TaskCompletionSource(Of Integer)(TaskContinuationOptions.RunContinuationsAsynchronously)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>TaskCompletionSource(Of T)</c> has no constructor that takes <c>TaskContinuationOptions</c>: the value binds to
    ''' <c>New(state As Object)</c>, is stored as the <c>AsyncState</c> of the task, and the option is ignored. The intended constructor
    ''' takes <c>TaskCreationOptions</c>.
    ''' </para>
    ''' <para>
    ''' The code fix is offered when the argument only combines <c>TaskContinuationOptions</c> members with <see langword="Or"/> and
    ''' every member also exists in <c>TaskCreationOptions</c> (<c>ExecuteSynchronously</c>, for example, does not): it writes
    ''' <c>TaskCreationOptions.RunContinuationsAsynchronously</c>.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class TaskCompletionSourceOptionsAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.TaskContinuationOptionsAsState)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="TaskCompletionSourceOptionsAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf TaskCompletionSourceOptionsAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2247 reports the same code.
            End If

            Dim sourceType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskCompletionSource`1")
            Dim continuationType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskContinuationOptions")
            Dim creationType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.Tasks.TaskCreationOptions")
            If sourceType Is Nothing OrElse continuationType Is Nothing OrElse creationType Is Nothing Then
                Exit Sub
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext) TaskCompletionSourceOptionsAnalyzer.AnalyzeCreation(c, sourceType, continuationType, creationType)),
                OperationKind.ObjectCreation)
        End Sub


        ''' <summary>
        ''' Reports a <c>TaskContinuationOptions</c> value passed as the state of a <c>TaskCompletionSource(Of T)</c>.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="sourceType">
        ''' The <c>TaskCompletionSource(Of T)</c> type definition.
        ''' </param>
        '''
        ''' <param name="continuationType">
        ''' The <c>TaskContinuationOptions</c> type.
        ''' </param>
        '''
        ''' <param name="creationType">
        ''' The <c>TaskCreationOptions</c> type.
        ''' </param>
        Private Shared Sub AnalyzeCreation(context As OperationAnalysisContext, sourceType As INamedTypeSymbol, continuationType As INamedTypeSymbol,
                                           creationType As INamedTypeSymbol)

            Dim creation As IObjectCreationOperation = DirectCast(context.Operation, IObjectCreationOperation)
            If creation.Constructor Is Nothing OrElse Not SymbolEqualityComparer.Default.Equals(creation.Constructor.ContainingType.OriginalDefinition, sourceType) OrElse
               creation.Constructor.Parameters.Length <> 1 OrElse creation.Constructor.Parameters(0).Type.SpecialType <> SpecialType.System_Object OrElse
               creation.Arguments.Length <> 1 Then
                Exit Sub
            End If

            Dim options As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(creation.Arguments(0).Value)
            If Not SymbolEqualityComparer.Default.Equals(options?.Type, continuationType) Then
                Exit Sub
            End If

            Dim actions As New List(Of FixReplacement)()
            Dim replacement As String = TaskCompletionSourceOptionsAnalyzer.GetCreationOptionsText(options, continuationType, creationType)
            If replacement IsNot Nothing AndAlso
               SymbolEqualityComparer.Default.Equals(UtilSpeculativeBinding.GetExpressionType(creation.SemanticModel, options.Syntax.SpanStart, replacement), creationType) AndAlso
               sourceType.InstanceConstructors.Any(Function(c As IMethodSymbol) c.Parameters.Length = 1 AndAlso SymbolEqualityComparer.Default.Equals(c.Parameters(0).Type, creationType)) Then
                actions.Add(New FixReplacement("Use TaskCreationOptions", "TaskCreationOptions", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.TaskContinuationOptionsAsState, options.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions))
        End Sub

        ''' <summary>
        ''' Gets the text of a <c>TaskCreationOptions</c> value equivalent to a <c>TaskContinuationOptions</c> value.
        ''' </summary>
        '''
        ''' <param name="options">
        ''' The <c>TaskContinuationOptions</c> value.
        ''' </param>
        '''
        ''' <param name="continuationType">
        ''' The <c>TaskContinuationOptions</c> type.
        ''' </param>
        '''
        ''' <param name="creationType">
        ''' The <c>TaskCreationOptions</c> type.
        ''' </param>
        '''
        ''' <returns>
        ''' The text, where each member is replaced with the <c>TaskCreationOptions</c> member of the same name; 
        ''' <see langword="Nothing"/> if the value is not a combination of members with <see langword="Or"/>, or if a member does 
        ''' not exist in <c>TaskCreationOptions</c>.
        ''' </returns>
        Private Shared Function GetCreationOptionsText(options As IOperation, continuationType As INamedTypeSymbol, creationType As INamedTypeSymbol) As String

            Dim model As SemanticModel = options.SemanticModel
            Dim typeName As String = UtilSpeculativeBinding.GetTypeName(model, options.Syntax.SpanStart, creationType)
            If typeName Is Nothing Then
                Return Nothing
            End If

            Dim members As New List(Of IFieldReferenceOperation)()
            Dim pending As New Stack(Of IOperation)()
            pending.Push(options)
            While pending.Count > 0
                Dim current As IOperation = pending.Pop()
                Dim binary As IBinaryOperation = TryCast(current, IBinaryOperation)
                Dim field As IFieldReferenceOperation = TryCast(current, IFieldReferenceOperation)
                Select Case True
                    Case binary IsNot Nothing AndAlso binary.OperatorKind = BinaryOperatorKind.Or
                        pending.Push(binary.LeftOperand)
                        pending.Push(binary.RightOperand)
                    Case TypeOf current Is IParenthesizedOperation
                        pending.Push(DirectCast(current, IParenthesizedOperation).Operand)
                    Case field IsNot Nothing AndAlso field.Instance Is Nothing AndAlso SymbolEqualityComparer.Default.Equals(field.Field.ContainingType, continuationType) AndAlso
                         creationType.GetMembers(field.Field.Name).OfType(Of IFieldSymbol)().Any()
                        members.Add(field)
                    Case Else
                        Return Nothing
                End Select
            End While

            Dim text As String = options.Syntax.ToString()
            Dim offset As Integer = options.Syntax.SpanStart
            For Each member As IFieldReferenceOperation In members.OrderByDescending(Function(m As IFieldReferenceOperation) m.Syntax.SpanStart)
                Dim start As Integer = member.Syntax.SpanStart - offset
                text = $"{text.Substring(0, start)}{typeName}.{member.Field.Name}{text.Substring(start + member.Syntax.Span.Length)}"
            Next member
            Return text
        End Function

#End Region

    End Class

End Namespace

#End Region
