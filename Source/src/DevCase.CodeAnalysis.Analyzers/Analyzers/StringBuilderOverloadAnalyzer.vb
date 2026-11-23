#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " StringBuilderOverloadAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports arguments of <see cref="StringBuilder.Append(String)"/> and <see cref="StringBuilder.Insert(Integer, String)"/>
    ''' that are converted to a string although a strongly typed overload exists (rule
    ''' <see cref="DiagnosticIds.StringBuilderArgumentConvertedToString"/>; equivalent of CA1830 for .NET Framework projects),
    ''' for example <c>sb.Append(count.ToString())</c> → <c>sb.Append(count)</c> and
    ''' <c>sb.Append(New String(c, 2))</c> → <c>sb.Append(c, 2)</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' <c>ToString()</c> is reported only without arguments, and only on the primitive types for which <see cref="StringBuilder"/>
    ''' has an overload (<see cref="Boolean"/>, <see cref="Char"/>, <see cref="SByte"/>, <see cref="Byte"/>, <see cref="Short"/>,
    ''' <see cref="UShort"/>, <see cref="Integer"/>, <see cref="UInteger"/>, <see cref="Long"/>, <see cref="ULong"/>,
    ''' <see cref="Single"/>, <see cref="Double"/> and <see cref="Decimal"/>): the overload formats the value with the current
    ''' culture, exactly as <c>ToString()</c> does. <c>New String(Char, Integer)</c> is reported only in <c>Append</c>,
    ''' replaced with <see cref="StringBuilder.Append(Char, Integer)"/>.
    ''' </para>
    ''' <para>
    ''' The rule runs only for .NET Framework compilations: modern .NET projects get the same suggestion from the built-in rule CA1830.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class StringBuilderOverloadAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that is present when the argument is <c>New String(Char, Integer)</c>;
        ''' otherwise, the argument is a call to <c>ToString()</c>.
        ''' </summary>
        Friend Const RepeatedCharacterPropertyKey As String = "RepeatedCharacter"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The primitive types that have an <c>Append</c> and an <c>Insert</c> overload in <see cref="StringBuilder"/>.
        ''' </summary>
        Private Shared ReadOnly OverloadTypes As ImmutableHashSet(Of SpecialType) = ImmutableHashSet.Create(
            SpecialType.System_Boolean, SpecialType.System_Char, SpecialType.System_SByte, SpecialType.System_Byte,
            SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32,
            SpecialType.System_Int64, SpecialType.System_UInt64, SpecialType.System_Single, SpecialType.System_Double,
            SpecialType.System_Decimal)

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.StringBuilderArgumentConvertedToString)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StringBuilderOverloadAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf StringBuilderOverloadAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1830 already reports these arguments.
            End If

            Dim builderType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Text.StringBuilder")
            If builderType Is Nothing Then
                Exit Sub
            End If

            context.RegisterOperationAction(
                UtilAnalyzerFiles.SkipExcludedOperations(Sub(c As OperationAnalysisContext) StringBuilderOverloadAnalyzer.AnalyzeInvocation(c, builderType)),
                OperationKind.Invocation)
        End Sub

        ''' <summary>
        ''' Reports the string argument of a call to <c>Append(String)</c> or <c>Insert(Integer, String)</c> that is converted
        ''' to a string although a strongly typed overload exists.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="builderType">
        ''' The <see cref="StringBuilder"/> type.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext, builderType As INamedTypeSymbol)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            If method.IsStatic OrElse Not SymbolEqualityComparer.Default.Equals(method.ContainingType, builderType) Then
                Exit Sub
            End If

            Dim isAppend As Boolean = method.Name.Equals(NameOf(StringBuilder.Append), StringComparison.Ordinal) AndAlso
                                      method.Parameters.Length = 1
            Dim isInsert As Boolean = method.Name.Equals(NameOf(StringBuilder.Insert), StringComparison.Ordinal) AndAlso
                                      method.Parameters.Length = 2 AndAlso method.Parameters(0).Type.SpecialType = SpecialType.System_Int32
            If Not (isAppend OrElse isInsert) OrElse method.Parameters.Last().Type.SpecialType <> SpecialType.System_String Then
                Exit Sub
            End If

            Dim argument As IArgumentOperation = invocation.Arguments.FirstOrDefault(
                Function(a As IArgumentOperation) a.Parameter IsNot Nothing AndAlso a.Parameter.Ordinal = method.Parameters.Length - 1)
            If argument Is Nothing OrElse argument.IsImplicit OrElse argument.Value Is Nothing Then
                Exit Sub
            End If

            Dim value As IOperation = argument.Value
            Dim model As SemanticModel = invocation.SemanticModel
            Dim position As Integer = value.Syntax.SpanStart
            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty
            Dim overloadArguments As String

            Dim toStringCall As IInvocationOperation = TryCast(value, IInvocationOperation)
            Dim creation As IObjectCreationOperation = TryCast(value, IObjectCreationOperation)
            If toStringCall IsNot Nothing Then
                Dim instanceType As ITypeSymbol = toStringCall.Instance?.Type
                If instanceType Is Nothing OrElse toStringCall.Instance.IsImplicit OrElse
                   Not toStringCall.TargetMethod.Name.Equals(NameOf(Object.ToString), StringComparison.Ordinal) OrElse
                   Not toStringCall.Arguments.IsEmpty OrElse
                   Not StringBuilderOverloadAnalyzer.OverloadTypes.Contains(instanceType.SpecialType) OrElse
                   toStringCall.TargetMethod.ContainingType.SpecialType <> instanceType.SpecialType OrElse
                   Not StringBuilderOverloadAnalyzer.HasOverload(builderType, method.Name, If(isInsert, {SpecialType.System_Int32, instanceType.SpecialType}, {instanceType.SpecialType})) Then
                    Exit Sub
                End If
                Dim typeName As String = instanceType.ToMinimalDisplayString(model, position)
                overloadArguments = If(isInsert, $"Integer, {typeName}", typeName)

            ElseIf creation IsNot Nothing AndAlso isAppend AndAlso TypeOf value.Syntax Is ObjectCreationExpressionSyntax AndAlso
                   creation.Constructor IsNot Nothing AndAlso creation.Constructor.ContainingType.SpecialType = SpecialType.System_String AndAlso
                   creation.Constructor.Parameters.Length = 2 AndAlso
                   creation.Constructor.Parameters(0).Type.SpecialType = SpecialType.System_Char AndAlso
                   creation.Constructor.Parameters(1).Type.SpecialType = SpecialType.System_Int32 AndAlso
                   StringBuilderOverloadAnalyzer.HasOverload(builderType, method.Name, {SpecialType.System_Char, SpecialType.System_Int32}) Then
                properties = properties.Add(StringBuilderOverloadAnalyzer.RepeatedCharacterPropertyKey, Boolean.TrueString)
                overloadArguments = "Char, Integer"

            Else
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.StringBuilderArgumentConvertedToString, value.Syntax.GetLocation(),
                                                       properties, method.Name, overloadArguments))
        End Sub

        ''' <summary>
        ''' Determines whether <see cref="StringBuilder"/> has a public instance overload with the specified parameter types.
        ''' </summary>
        '''
        ''' <param name="builderType">
        ''' The <see cref="StringBuilder"/> type.
        ''' </param>
        '''
        ''' <param name="name">
        ''' The method name (<c>Append</c> or <c>Insert</c>).
        ''' </param>
        '''
        ''' <param name="parameterTypes">
        ''' The parameter types of the overload.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the overload exists; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function HasOverload(builderType As INamedTypeSymbol, name As String, parameterTypes As SpecialType()) As Boolean

            Return builderType.GetMembers(name).OfType(Of IMethodSymbol)().Any(
                Function(m As IMethodSymbol) Not m.IsStatic AndAlso m.DeclaredAccessibility = Accessibility.Public AndAlso
                                             m.Parameters.Select(Function(p As IParameterSymbol) p.Type.SpecialType).SequenceEqual(parameterTypes))
        End Function

#End Region

    End Class

End Namespace

#End Region
