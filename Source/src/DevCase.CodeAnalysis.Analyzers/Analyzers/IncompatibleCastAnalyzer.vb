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

#Region " IncompatibleCastAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>Enumerable.Cast(Of T)</c> and <c>Enumerable.OfType(Of T)</c> calls on sequences whose element type can never be
    ''' <c>T</c> at run time (rule <see cref="DiagnosticIds.IncompatibleCastOrOfType"/>; equivalent of CA2021 for .NET Framework
    ''' projects), for example <c>values.Cast(Of String)()</c> on an <c>Integer()</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Both methods use a run-time type check, without widening or user-defined conversions: <c>Cast</c> throws
    ''' <see cref="InvalidCastException"/> for every element, and <c>OfType</c> always returns an empty sequence. A conversion is possible
    ''' when one type derives from the other, when an interface can be implemented by the element (any interface for a class that is not
    ''' <see langword="NotInheritable"/>), when the target is <see cref="Object"/>, <see cref="ValueType"/> or <see cref="[Enum]"/> as
    ''' appropriate, and between an enumeration and its underlying type (or another enumeration with the same underlying type), which
    ''' unboxing accepts. Nullable value types are compared through their underlying type.
    ''' </para>
    ''' <para>
    ''' The element type is the element of an array or the <c>T</c> of the only <c>IEnumerable(Of T)</c> that the source implements;
    ''' non-generic sequences, type parameters, arrays as elements and generic variance are not analyzed. There is no code fix: the
    ''' conversion that the code needs (<c>Select</c> with <c>CStr</c>, <c>ToString</c>...) depends on what it meant.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class IncompatibleCastAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.IncompatibleCastOrOfType)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="IncompatibleCastAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf IncompatibleCastAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2021 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf IncompatibleCastAnalyzer.AnalyzeInvocation),
                                            OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call to <c>Cast(Of T)</c> or <c>OfType(Of T)</c> that can never convert an element.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            Dim isCast As Boolean = UtilAnalyzerSemantic.IsMember(method, "System.Linq.Enumerable", NameOf(Enumerable.Cast))
            If (Not isCast AndAlso Not UtilAnalyzerSemantic.IsMember(method, "System.Linq.Enumerable", NameOf(Enumerable.OfType))) OrElse
               method.TypeArguments.Length <> 1 Then
                Exit Sub
            End If

            Dim source As IOperation = UtilAnalyzerSemantic.GetExtensionSource(invocation)
            If source Is Nothing Then
                Exit Sub
            End If

            Dim sourceType As ITypeSymbol = UtilAnalyzerSemantic.SkipImplicitConversions(source).Type
            Dim elementType As ITypeSymbol = If(sourceType Is Nothing, Nothing, IncompatibleCastAnalyzer.GetElementType(sourceType))
            Dim targetType As ITypeSymbol = method.TypeArguments(0)
            If elementType Is Nothing OrElse IncompatibleCastAnalyzer.CanConvert(elementType, targetType, invocation.SemanticModel.Compilation) Then
                Exit Sub
            End If

            Dim position As Integer = invocation.Syntax.SpanStart
            Dim reason As String = If(isCast, "every element throws InvalidCastException", "the result is always empty")
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.IncompatibleCastOrOfType, invocation.Syntax.GetLocation(), method.Name,
                                                       targetType.ToMinimalDisplayString(invocation.SemanticModel, position),
                                                       elementType.ToMinimalDisplayString(invocation.SemanticModel, position), reason))
        End Sub

        ''' <summary>
        ''' Gets the element type of a sequence.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type of the sequence.
        ''' </param>
        '''
        ''' <returns>
        ''' The element type of an array, or the <c>T</c> of the only <c>IEnumerable(Of T)</c> that the type is or implements; 
        ''' <see langword="Nothing"/> if it is unknown.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Private Shared Function GetElementType(type As ITypeSymbol) As ITypeSymbol

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            Dim array As IArrayTypeSymbol = TryCast(type, IArrayTypeSymbol)
            If array IsNot Nothing Then
                Return array.ElementType
            End If

            Dim candidates As List(Of INamedTypeSymbol) = type.AllInterfaces.ToList()
            Dim named As INamedTypeSymbol = TryCast(type, INamedTypeSymbol)
            If named IsNot Nothing Then
                candidates.Add(named)
            End If
            Dim sequences As List(Of INamedTypeSymbol) = candidates.
                Where(Function(i As INamedTypeSymbol) i.OriginalDefinition.SpecialType = SpecialType.System_Collections_Generic_IEnumerable_T).
                Distinct(SymbolEqualityComparer.Default).Cast(Of INamedTypeSymbol)().ToList()
            Return If(sequences.Count = 1, sequences(0).TypeArguments(0), Nothing)
        End Function

        ''' <summary>
        ''' Determines whether a run-time type check can convert a value of one type to another.
        ''' </summary>
        '''
        ''' <param name="source">
        ''' The type of the elements.
        ''' </param>
        '''
        ''' <param name="target">
        ''' The type argument of <c>Cast</c> or <c>OfType</c>.
        ''' </param>
        '''
        ''' <param name="compilation">
        ''' The compilation, to classify interface conversions.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="False"/> if the conversion always fails; otherwise (also when it cannot be determined), <see langword="True"/>.
        ''' </returns>
        Private Shared Function CanConvert(source As ITypeSymbol, target As ITypeSymbol, compilation As Compilation) As Boolean

            If IncompatibleCastAnalyzer.IsUnknown(source) OrElse IncompatibleCastAnalyzer.IsUnknown(target) Then
                Return True
            End If

            Dim from As ITypeSymbol = IncompatibleCastAnalyzer.WithoutNullable(source)
            Dim [to] As ITypeSymbol = IncompatibleCastAnalyzer.WithoutNullable(target)
            If SymbolEqualityComparer.Default.Equals(from, [to]) OrElse from.SpecialType = SpecialType.System_Object OrElse
               [to].SpecialType = SpecialType.System_Object Then
                Return True
            End If

            If from.IsValueType Then
                ' A boxed value is exactly its type: only its base types, its interfaces and its underlying type can be unboxed.
                Select Case True
                    Case [to].SpecialType = SpecialType.System_ValueType
                        Return True
                    Case [to].SpecialType = SpecialType.System_Enum
                        Return from.TypeKind = TypeKind.Enum
                    Case [to].TypeKind = TypeKind.Interface
                        Return from.AllInterfaces.Contains(DirectCast([to], INamedTypeSymbol), SymbolEqualityComparer.Default)
                    Case [to].IsValueType
                        Return SymbolEqualityComparer.Default.Equals(IncompatibleCastAnalyzer.GetUnderlyingType(from), IncompatibleCastAnalyzer.GetUnderlyingType([to]))
                    Case Else
                        Return False
                End Select
            End If

            If [to].IsValueType Then
                Select Case True
                    Case from.SpecialType = SpecialType.System_ValueType
                        Return True
                    Case from.SpecialType = SpecialType.System_Enum
                        Return [to].TypeKind = TypeKind.Enum
                    Case from.TypeKind = TypeKind.Interface
                        Return [to].AllInterfaces.Contains(DirectCast(from, INamedTypeSymbol), SymbolEqualityComparer.Default)
                    Case Else
                        Return False
                End Select
            End If

            ' Two reference types: a class that is not NotInheritable can have a derived class that implements any interface.
            Select Case True
                Case [to].TypeKind = TypeKind.Interface
                    Return from.TypeKind = TypeKind.Interface OrElse Not from.IsSealed OrElse compilation.ClassifyCommonConversion(from, [to]).IsImplicit
                Case from.TypeKind = TypeKind.Interface
                    Return Not [to].IsSealed OrElse compilation.ClassifyCommonConversion([to], from).IsImplicit
                Case Else
                    Return IncompatibleCastAnalyzer.InheritsFrom(from, [to]) OrElse IncompatibleCastAnalyzer.InheritsFrom([to], from)
            End Select
        End Function

        ''' <summary>
        ''' Determines whether the analysis cannot decide about a type.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for type parameters, arrays, pointers, error types and other special types; otherwise, 
        ''' <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsUnknown(type As ITypeSymbol) As Boolean

            Select Case type.TypeKind
                Case TypeKind.Class, TypeKind.Struct, TypeKind.Enum, TypeKind.Interface, TypeKind.Delegate
                    Return False
                Case Else
                    Return True
            End Select
        End Function

        ''' <summary>
        ''' Gets the underlying type of a nullable value type.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <c>T</c> for <c>Nullable(Of T)</c>; otherwise, <paramref name="type"/>.
        ''' </returns>
        Private Shared Function WithoutNullable(type As ITypeSymbol) As ITypeSymbol

            Dim named As INamedTypeSymbol = TryCast(type, INamedTypeSymbol)
            Return If(named IsNot Nothing AndAlso named.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T, named.TypeArguments(0), type)
        End Function

        ''' <summary>
        ''' Gets the type that a boxed value of a value type has for unboxing: the underlying type of an enumeration.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The value type.
        ''' </param>
        '''
        ''' <returns>
        ''' The underlying type of an enumeration; otherwise, <paramref name="type"/>.
        ''' </returns>
        Private Shared Function GetUnderlyingType(type As ITypeSymbol) As ITypeSymbol

            Dim named As INamedTypeSymbol = TryCast(type, INamedTypeSymbol)
            Return If(named IsNot Nothing AndAlso named.TypeKind = TypeKind.Enum AndAlso named.EnumUnderlyingType IsNot Nothing, named.EnumUnderlyingType, type)
        End Function

        ''' <summary>
        ''' Determines whether a class is or inherits from another class.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The class.
        ''' </param>
        '''
        ''' <param name="baseType">
        ''' The possible base class.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if <paramref name="type"/> is or inherits from <paramref name="baseType"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function InheritsFrom(type As ITypeSymbol, baseType As ITypeSymbol) As Boolean

            Dim current As ITypeSymbol = type
            While current IsNot Nothing
                If SymbolEqualityComparer.Default.Equals(current.OriginalDefinition, baseType.OriginalDefinition) Then
                    Return True
                End If
                current = current.BaseType
            End While
            Return False
        End Function

#End Region

    End Class

End Namespace

#End Region
