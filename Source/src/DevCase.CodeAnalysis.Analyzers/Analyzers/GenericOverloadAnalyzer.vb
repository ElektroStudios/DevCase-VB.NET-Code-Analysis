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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " GenericOverloadAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls that pass <c>GetType(T)</c> to a method that has a generic overload taking <c>T</c> as a type argument
    ''' (rule <see cref="DiagnosticIds.GenericOverloadInsteadOfTypeArgument"/>; equivalent of CA2263 for .NET Framework projects), for
    ''' example <c>DirectCast(Activator.CreateInstance(GetType(T)), T)</c> or <c>Marshal.SizeOf(GetType(NativePoint))</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The generic overload must be declared in the same type, have one type parameter whose constraints the type satisfies, and take
    ''' the other parameters in the same order (<c>Marshal.PtrToStructure(pointer, GetType(T))</c> has
    ''' <c>Marshal.PtrToStructure(Of T)(pointer)</c>). Speculative binding does not check constraints, so they are checked explicitly
    ''' (<see langword="Structure"/>, <see langword="Class"/>, <c>New</c> and constraint types). In .NET Framework,
    ''' <c>Activator.CreateInstance(Of T)()</c> has no constraint: like <c>Activator.CreateInstance(GetType(T))</c>, it throws
    ''' <see cref="MissingMethodException"/> at run time when the type has no public parameterless constructor.
    ''' </para>
    ''' <para>
    ''' The generic overload returns the type itself instead of <see cref="Object"/>, so the code fix also removes a
    ''' <see langword="DirectCast"/>, <see langword="CType"/> or <see langword="TryCast"/> to the same type around the call:
    ''' <c>Activator.CreateInstance(Of T)()</c>.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class GenericOverloadAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.GenericOverloadInsteadOfTypeArgument)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="GenericOverloadAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf GenericOverloadAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2263 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf GenericOverloadAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call that passes <c>GetType(T)</c> to a method with a generic overload.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            Dim syntax As InvocationExpressionSyntax = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            If method.IsGenericMethod OrElse method.IsExtensionMethod OrElse method.ReducedFrom IsNot Nothing OrElse syntax?.ArgumentList Is Nothing OrElse
               (TypeOf syntax.Expression IsNot MemberAccessExpressionSyntax AndAlso TypeOf syntax.Expression IsNot IdentifierNameSyntax) OrElse
               syntax.ArgumentList.Arguments.Any(Function(a As ArgumentSyntax) a.IsOmitted) OrElse
               invocation.Arguments.Any(Function(a As IArgumentOperation) a.ArgumentKind <> ArgumentKind.Explicit) Then
                Exit Sub
            End If

            Dim typeArguments As List(Of IArgumentOperation) = invocation.Arguments.Where(
                Function(a As IArgumentOperation) UtilAnalyzerSemantic.HasMetadataName(a.Parameter?.Type, "System.Type") AndAlso
                                                  TypeOf UtilAnalyzerSemantic.SkipImplicitConversions(a.Value) Is ITypeOfOperation).ToList()
            If typeArguments.Count <> 1 Then
                Exit Sub
            End If

            Dim typeArgument As IArgumentOperation = typeArguments(0)
            Dim typeOfOperation As ITypeOfOperation = DirectCast(UtilAnalyzerSemantic.SkipImplicitConversions(typeArgument.Value), ITypeOfOperation)
            Dim getTypeSyntax As GetTypeExpressionSyntax = TryCast(typeOfOperation.Syntax, GetTypeExpressionSyntax)
            Dim target As ITypeSymbol = typeOfOperation.TypeOperand
            Dim namedTarget As INamedTypeSymbol = TryCast(target, INamedTypeSymbol)
            If getTypeSyntax Is Nothing OrElse target Is Nothing OrElse target.TypeKind = TypeKind.Error OrElse
               (namedTarget IsNot Nothing AndAlso namedTarget.IsUnboundGenericType) OrElse target.SpecialType = SpecialType.System_Void Then
                Exit Sub
            End If

            Dim model As SemanticModel = invocation.SemanticModel
            Dim overload As IMethodSymbol = GenericOverloadAnalyzer.FindOverload(method, typeArgument.Parameter, target, model, syntax.SpanStart)
            If overload Is Nothing Then
                Exit Sub
            End If

            Dim typeText As String = getTypeSyntax.Type.ToString()
            Dim otherArguments As IEnumerable(Of String) = syntax.ArgumentList.Arguments.Where(Function(a As ArgumentSyntax) a IsNot typeArgument.Syntax).
                                                                                          Select(Function(a As ArgumentSyntax) a.ToString())
            Dim genericCall As String = $"{syntax.Expression}(Of {typeText})({String.Join(", ", otherArguments)})"

            ' A cast to the same type becomes redundant: the generic overload returns the type itself.
            Dim replaced As SyntaxNode = syntax
            Dim outer As SyntaxNode = syntax.Parent
            While TypeOf outer Is ParenthesizedExpressionSyntax
                outer = outer.Parent
            End While
            Dim cast As CastExpressionSyntax = TryCast(outer, CastExpressionSyntax)
            If cast IsNot Nothing AndAlso SymbolEqualityComparer.Default.Equals(overload.ReturnType, overload.TypeParameters(0)) AndAlso
               SymbolEqualityComparer.Default.Equals(model.GetTypeInfo(cast.Type).Type, target) Then
                replaced = cast
            End If

            Dim speculative As IMethodSymbol = TryCast(UtilSpeculativeBinding.GetSymbol(model, replaced.SpanStart, genericCall), IMethodSymbol)
            If speculative Is Nothing OrElse Not SymbolEqualityComparer.Default.Equals(speculative.OriginalDefinition, overload) OrElse
               Not SymbolEqualityComparer.Default.Equals(speculative.TypeArguments(0), target) Then
                Exit Sub
            End If

            Dim actions As FixReplacement() = {New FixReplacement("Use the generic overload", "Generic", genericCall)}
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.GenericOverloadInsteadOfTypeArgument, syntax.GetLocation(),
                                                                        {replaced.GetLocation()}, actions,
                                                                        $"{method.ContainingType.Name}.{method.Name}(Of {typeText})", typeText))
        End Sub

        ''' <summary>
        ''' Finds the generic overload of a method that takes the type as a type argument instead of a <see cref="Type"/> parameter.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The invoked method.
        ''' </param>
        '''
        ''' <param name="typeParameter">
        ''' The <see cref="Type"/> parameter that receives <c>GetType(T)</c>.
        ''' </param>
        '''
        ''' <param name="target">
        ''' The type passed to <c>GetType</c>.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position of the invocation, to check accessibility.
        ''' </param>
        '''
        ''' <returns>
        ''' The original definition of the overload; <see langword="Nothing"/> if there is none.
        ''' </returns>
        Private Shared Function FindOverload(method As IMethodSymbol, typeParameter As IParameterSymbol, target As ITypeSymbol, model As SemanticModel,
                                             position As Integer) As IMethodSymbol

            Dim remaining As List(Of IParameterSymbol) = method.Parameters.Where(Function(p As IParameterSymbol) p.Ordinal <> typeParameter.Ordinal).ToList()
            For Each candidate As IMethodSymbol In method.ContainingType.GetMembers(method.Name).OfType(Of IMethodSymbol)()
                If candidate.Arity <> 1 OrElse candidate.IsStatic <> method.IsStatic OrElse candidate.Parameters.Length <> remaining.Count OrElse
                   Not model.IsAccessible(position, candidate) OrElse
                   Not UtilAnalyzerSemantic.SatisfiesConstraints(candidate.TypeParameters(0), target, model.Compilation) Then
                    Continue For
                End If

                Dim constructed As IMethodSymbol = candidate.Construct(target)
                Dim matches As Boolean = Enumerable.Range(0, remaining.Count).All(
                    Function(i As Integer) constructed.Parameters(i).RefKind = remaining(i).RefKind AndAlso
                                           SymbolEqualityComparer.Default.Equals(constructed.Parameters(i).Type, remaining(i).Type))
                If matches Then
                    Return candidate.OriginalDefinition
                End If
            Next candidate
            Return Nothing
        End Function

#End Region

    End Class

End Namespace

#End Region
