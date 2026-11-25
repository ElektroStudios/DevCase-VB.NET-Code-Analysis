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

#Region " ArgumentNullExceptionValueTypeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>New ArgumentNullException(NameOf(p))</c> (or <c>"p"</c>) for a parameter <c>p</c> of a non-nullable value type
    ''' (rule <see cref="DiagnosticIds.ArgumentNullExceptionForValueType"/>; similar to CA2264 for the classic guards of .NET Framework
    ''' projects), for example <c>If count = Nothing Then Throw New ArgumentNullException(NameOf(count))</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' A value of a non-nullable value type (<see cref="Integer"/>, <see cref="Date"/>, a <see langword="Structure"/>, a type parameter
    ''' with the <see langword="Structure"/> constraint) can never be <see langword="Nothing"/>: the guard is dead code, or, with
    ''' <c>value = Nothing</c>, a check of the default value that throws the wrong exception. <c>Nullable(Of T)</c> parameters and
    ''' unconstrained type parameters are not reported.
    ''' </para>
    ''' <para>
    ''' The parameter name is the <c>paramName</c> argument: <c>NameOf(p)</c>, or a string literal equal to the name of a parameter of the
    ''' enclosing method, property, accessor or lambda (compared case-sensitively). There is no code fix: whether the guard must be
    ''' removed, or replaced with another check, depends on what the code meant.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ArgumentNullExceptionValueTypeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ArgumentNullExceptionForValueType)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ArgumentNullExceptionValueTypeAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ArgumentNullExceptionValueTypeAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2264 reports the same code.
            End If

            Dim exceptionType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.ArgumentNullException")
            If exceptionType Is Nothing Then
                Exit Sub
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext) ArgumentNullExceptionValueTypeAnalyzer.AnalyzeCreation(c, exceptionType)), OperationKind.ObjectCreation)
        End Sub


        ''' <summary>
        ''' Reports the creation of an <see cref="ArgumentNullException"/> for a parameter of a non-nullable value type.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="exceptionType">
        ''' The <see cref="ArgumentNullException"/> type.
        ''' </param>
        Private Shared Sub AnalyzeCreation(context As OperationAnalysisContext, exceptionType As INamedTypeSymbol)

            Dim creation As IObjectCreationOperation = DirectCast(context.Operation, IObjectCreationOperation)
            If Not SymbolEqualityComparer.Default.Equals(creation.Type, exceptionType) Then
                Exit Sub
            End If

            Dim argument As IArgumentOperation = creation.Arguments.FirstOrDefault(
                Function(a As IArgumentOperation) a.Parameter IsNot Nothing AndAlso a.Parameter.Name.Equals("paramName", StringComparison.Ordinal))
            If argument Is Nothing Then
                Exit Sub
            End If

            Dim parameter As IParameterSymbol = ArgumentNullExceptionValueTypeAnalyzer.GetNamedParameter(argument.Value, context.ContainingSymbol)
            If parameter Is Nothing OrElse Not parameter.Type.IsValueType OrElse
               parameter.Type.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ArgumentNullExceptionForValueType, creation.Syntax.GetLocation(),
                                                       parameter.Name, parameter.Type.ToMinimalDisplayString(creation.SemanticModel, creation.Syntax.SpanStart)))
        End Sub

        ''' <summary>
        ''' Gets the parameter named by the <c>paramName</c> argument of an exception.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The value of the argument.
        ''' </param>
        '''
        ''' <param name="containingSymbol">
        ''' The symbol that contains the operation (a method, accessor or constructor).
        ''' </param>
        '''
        ''' <returns>
        ''' The parameter, for <c>NameOf(p)</c> or a string literal equal to the name of a parameter of the enclosing method or of an 
        ''' enclosing lambda; otherwise, <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function GetNamedParameter(value As IOperation, containingSymbol As ISymbol) As IParameterSymbol

            Dim operand As IOperation = UtilAnalyzerSemantic.SkipImplicitConversions(value)
            Dim nameOfOperation As INameOfOperation = TryCast(operand, INameOfOperation)
            If nameOfOperation IsNot Nothing Then
                Dim reference As IParameterReferenceOperation = TryCast(nameOfOperation.Argument, IParameterReferenceOperation)
                Return reference?.Parameter
            End If

            If Not operand.ConstantValue.HasValue OrElse TypeOf operand.ConstantValue.Value IsNot String Then
                Return Nothing
            End If

            Dim name As String = DirectCast(operand.ConstantValue.Value, String)
            Dim candidates As List(Of IParameterSymbol) = UtilAnalyzerSemantic.GetVisibleParameters(value, containingSymbol)
            Return candidates.Find(Function(p As IParameterSymbol) p.Name.Equals(name, StringComparison.Ordinal))
        End Function

#End Region

    End Class

End Namespace

#End Region
