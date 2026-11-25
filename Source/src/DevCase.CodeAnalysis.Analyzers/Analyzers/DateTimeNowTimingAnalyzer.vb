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

#Region " DateTimeNowTimingAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports elapsed-time measurements made with the system clock (rule <see cref="DiagnosticIds.DateTimeNowForTiming"/>): 
    ''' <c>DateTime.Now - start</c> or <c>DateTime.Now.Subtract(start)</c>, where <c>start</c> is a local variable that is assigned 
    ''' <c>DateTime.Now</c> in the same method.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>DateTime.Now</c>, <c>DateTime.UtcNow</c> and the Visual Basic <c>Now</c> property are recognized. 
    ''' Subtractions with other dates (a deadline, a file date) are not reported: they are not elapsed-time measurements.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DateTimeNowTimingAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DateTimeNowForTiming)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DateTimeNowTimingAnalyzer"/> class.
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
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf DateTimeNowTimingAnalyzer.AnalyzeBinary), OperationKind.Binary)
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf DateTimeNowTimingAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a <c>Now - start</c> subtraction.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeBinary(context As OperationAnalysisContext)

            Dim binary As IBinaryOperation = DirectCast(context.Operation, IBinaryOperation)
            If binary.OperatorKind = BinaryOperatorKind.Subtract Then
                DateTimeNowTimingAnalyzer.Analyze(context, binary.LeftOperand, binary.RightOperand)
            End If
        End Sub

        ''' <summary>
        ''' Analyzes a <c>Now.Subtract(start)</c> invocation.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            If invocation.TargetMethod.Name.Equals("Subtract", StringComparison.Ordinal) AndAlso invocation.Instance IsNot Nothing AndAlso
               invocation.TargetMethod.ContainingType.SpecialType = SpecialType.System_DateTime AndAlso invocation.Arguments.Length = 1 Then
                DateTimeNowTimingAnalyzer.Analyze(context, invocation.Instance, invocation.Arguments(0).Value)
            End If
        End Sub

        ''' <summary>
        ''' Reports the operation when the left operand is the current time and the right operand is a local variable assigned the current time.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        ''' 
        ''' <param name="left">
        ''' The left operand (the end of the measurement).
        ''' </param>
        ''' 
        ''' <param name="right">
        ''' The right operand (the start of the measurement).
        ''' </param>
        Private Shared Sub Analyze(context As OperationAnalysisContext, left As IOperation, right As IOperation)

            Dim now As IPropertyReferenceOperation = TryCast(DateTimeNowTimingAnalyzer.SkipConversions(left), IPropertyReferenceOperation)
            Dim start As ILocalReferenceOperation = TryCast(DateTimeNowTimingAnalyzer.SkipConversions(right), ILocalReferenceOperation)
            If now Is Nothing OrElse start Is Nothing OrElse Not DateTimeNowTimingAnalyzer.IsNowProperty(now.Property) Then
                Exit Sub
            End If

            Dim boundary As SyntaxNode = UtilAnalyzerSyntax.GetExecutableBoundary(context.Operation.Syntax)
            If boundary IsNot Nothing AndAlso DateTimeNowTimingAnalyzer.IsAssignedNow(boundary, start.Local, context.Operation.SemanticModel, context) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DateTimeNowForTiming, context.Operation.Syntax.GetLocation(), now.Syntax.ToString()))
            End If
        End Sub

        ''' <summary>
        ''' Determines whether a local variable is initialized or assigned with the current time inside an executable boundary.
        ''' </summary>
        ''' 
        ''' <param name="boundary">
        ''' The executable boundary.
        ''' </param>
        ''' 
        ''' <param name="local">
        ''' The local variable.
        ''' </param>
        ''' 
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context (for cancellation).
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the variable receives the current time; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAssignedNow(boundary As SyntaxNode, local As ILocalSymbol, model As SemanticModel, context As OperationAnalysisContext) As Boolean

            For Each node As SyntaxNode In boundary.DescendantNodes()
                Dim value As ExpressionSyntax = Nothing
                Dim declarator As VariableDeclaratorSyntax = TryCast(node, VariableDeclaratorSyntax)
                If declarator IsNot Nothing AndAlso declarator.Initializer IsNot Nothing AndAlso
                   declarator.Names.Any(Function(n As ModifiedIdentifierSyntax) SymbolEqualityComparer.Default.Equals(model.GetDeclaredSymbol(n, context.CancellationToken), local)) Then
                    value = UtilAnalyzerSyntax.SkipParentheses(declarator.Initializer.Value)
                End If

                Dim assignment As AssignmentStatementSyntax = TryCast(node, AssignmentStatementSyntax)
                If assignment IsNot Nothing AndAlso assignment.IsKind(SyntaxKind.SimpleAssignmentStatement) AndAlso
                   SymbolEqualityComparer.Default.Equals(model.GetSymbolInfo(assignment.Left, context.CancellationToken).Symbol, local) Then
                    value = UtilAnalyzerSyntax.SkipParentheses(assignment.Right)
                End If

                Dim [property] As IPropertySymbol = If(value Is Nothing, Nothing, TryCast(model.GetSymbolInfo(value, context.CancellationToken).Symbol, IPropertySymbol))
                If [property] IsNot Nothing AndAlso DateTimeNowTimingAnalyzer.IsNowProperty([property]) Then
                    Return True
                End If
            Next node
            Return False
        End Function

        ''' <summary>
        ''' Determines whether a property returns the current time: <c>DateTime.Now</c>, <c>DateTime.UtcNow</c> or the Visual Basic <c>Now</c>.
        ''' </summary>
        ''' 
        ''' <param name="property">
        ''' The property.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the property returns the current time; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsNowProperty([property] As IPropertySymbol) As Boolean

            Dim isDateTime As Boolean = [property].ContainingType.SpecialType = SpecialType.System_DateTime
            Dim isVisualBasicNow As Boolean = [property].ContainingType.ToDisplayString().Equals("Microsoft.VisualBasic.DateAndTime", StringComparison.Ordinal)
            Return [property].Name.Equals("Now", StringComparison.Ordinal) AndAlso (isDateTime OrElse isVisualBasicNow) OrElse
                   [property].Name.Equals("UtcNow", StringComparison.Ordinal) AndAlso isDateTime
        End Function

        ''' <summary>
        ''' Removes the implicit conversions and the parentheses that wrap an operation.
        ''' </summary>
        ''' 
        ''' <param name="operation">
        ''' The operation.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The innermost operand.
        ''' </returns>
        Private Shared Function SkipConversions(operation As IOperation) As IOperation

            Dim current As IOperation = operation
            While TypeOf current Is IConversionOperation OrElse TypeOf current Is IParenthesizedOperation
                current = If(TypeOf current Is IConversionOperation,
                             DirectCast(current, IConversionOperation).Operand,
                             DirectCast(current, IParenthesizedOperation).Operand)
            End While
            Return current
        End Function

#End Region

    End Class

End Namespace

#End Region
