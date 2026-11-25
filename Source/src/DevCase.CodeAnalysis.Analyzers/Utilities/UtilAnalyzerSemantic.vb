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
Imports Microsoft.CodeAnalysis.Operations

#End Region

#Region " UtilAnalyzerSemantic "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides semantic (symbol-based) helper methods shared by the DevCase analyzers and code fix providers.
    ''' </summary>
    Friend NotInheritable Class UtilAnalyzerSemantic

#Region " Constants "

        ''' <summary>
        ''' The metadata name of the attribute that the Windows Forms designer applies to designer-generated classes.
        ''' </summary>
        '''
        ''' <remarks>
        ''' The Visual Basic compiler synthesizes a constructor that calls <c>InitializeComponent</c> for classes
        ''' marked with this attribute; declaring an explicit empty constructor would break the designer.
        ''' </remarks>
        Friend Const DesignerGeneratedAttributeMetadataName As String = "Microsoft.VisualBasic.CompilerServices.DesignerGeneratedAttribute"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilAnalyzerSemantic"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether a type (or any of its base types) exposes an instance <c>ConfigureAwait(Boolean)</c> method.
        ''' </summary>
        '''
        ''' <remarks>
        ''' This prevents false positives (and uncompilable fixes) on awaitables that cannot be configured,
        ''' such as <c>Task.Yield()</c>, <c>ConfiguredTaskAwaitable</c> or custom awaiters.
        ''' </remarks>
        '''
        ''' <param name="type">
        ''' The type of the awaited expression. May be <see langword="Nothing"/> when the expression does not bind.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the method exists; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasBooleanConfigureAwait(type As ITypeSymbol) As Boolean

            If type Is Nothing OrElse type.TypeKind = TypeKind.Error Then
                Return False
            End If

            ' Generic type parameters (e.g. "T As Task") expose the members of their constraint types.
            Dim typeParameter As ITypeParameterSymbol = TryCast(type, ITypeParameterSymbol)
            If typeParameter Is Nothing Then
                Return UtilAnalyzerSemantic.HierarchyHasBooleanConfigureAwait(type)
            End If

            ' A type parameter is awaitable through its constraints (type parameters among them are skipped).
            Dim constraints As IEnumerable(Of ITypeSymbol) = typeParameter.ConstraintTypes.Where(Function(c As ITypeSymbol) TypeOf c IsNot ITypeParameterSymbol)
            Return constraints.Any(AddressOf UtilAnalyzerSemantic.HierarchyHasBooleanConfigureAwait)
        End Function

        ''' <summary>
        ''' Walks a type and its base types (iteratively) looking for an instance <c>ConfigureAwait(Boolean)</c> method.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type to inspect.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the method exists; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function HierarchyHasBooleanConfigureAwait(type As ITypeSymbol) As Boolean

            Dim currentType As ITypeSymbol = type
            While currentType IsNot Nothing
                For Each member As ISymbol In currentType.GetMembers(UtilAnalyzerSyntax.ConfigureAwaitIdentifierName)
                    Dim method As IMethodSymbol = TryCast(member, IMethodSymbol)
                    If method IsNot Nothing AndAlso
                       Not method.IsStatic AndAlso
                       method.Parameters.Length = 1 AndAlso
                       method.Parameters(0).Type.SpecialType = SpecialType.System_Boolean Then
                        Return True
                    End If
                Next member
                currentType = currentType.BaseType
            End While
            Return False
        End Function

        ''' <summary>
        ''' Determines whether a type is marked with <see cref="System.Runtime.InteropServices.ComImportAttribute"/>.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type to evaluate.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type is a COM import (the runtime creates the COM object, and the Visual Basic compiler 
        ''' discards the body of every member); otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsComImport(type As INamedTypeSymbol) As Boolean

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            Return type.GetAttributes().Any(Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso a.AttributeClass.ContainingNamespace IsNot Nothing AndAlso
                                                                       a.AttributeClass.Name.Equals("ComImportAttribute", StringComparison.Ordinal) AndAlso
                                                                       a.AttributeClass.ContainingNamespace.ToDisplayString().Equals("System.Runtime.InteropServices", StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' Determines whether a type is marked with the Windows Forms <c>DesignerGenerated</c> attribute.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type to evaluate.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type is designer-generated; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsDesignerGenerated(type As INamedTypeSymbol) As Boolean

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            Return type.GetAttributes().Any(
                Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso
                                             a.AttributeClass.ToDisplayString().Equals(UtilAnalyzerSemantic.DesignerGeneratedAttributeMetadataName, StringComparison.Ordinal))
        End Function

        ''' <summary>
        ''' Gets the declaration of a (possibly partial) type on which a type-level diagnostic must be reported,
        ''' so that partial types spread across several files are reported only once.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type symbol.
        ''' </param>
        '''
        ''' <returns>
        ''' The first declaration located in a non-generated file, ordered by file path and position;
        ''' or <see langword="Nothing"/> if every declaration is in a generated file.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetPrimaryDeclaration(type As INamedTypeSymbol) As SyntaxReference

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            Dim candidates As ImmutableArray(Of SyntaxReference) = type.DeclaringSyntaxReferences
            Return candidates.
                Where(Function(r As SyntaxReference) Not UtilAnalyzerSyntax.IsGeneratedFilePath(r.SyntaxTree.FilePath)).
                OrderBy(Function(r As SyntaxReference) r.SyntaxTree.FilePath, StringComparer.Ordinal).
                ThenBy(Function(r As SyntaxReference) r.Span.Start).
                FirstOrDefault()
        End Function

        ''' <summary>
        ''' Determines whether any declaration of a type is located in a generated file.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type symbol.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if at least one partial declaration lives in a generated file; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function HasGeneratedDeclaration(type As INamedTypeSymbol) As Boolean

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            Return type.DeclaringSyntaxReferences.Any(
                Function(r As SyntaxReference) UtilAnalyzerSyntax.IsGeneratedFilePath(r.SyntaxTree.FilePath))
        End Function

        ''' <summary>
        ''' Determines whether a compilation targets the .NET Framework, whose core library is <c>mscorlib</c>.
        ''' </summary>
        '''
        ''' <param name="compilation">
        ''' The compilation.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for .NET Framework compilations; <see langword="False"/> for .NET (Core), .NET Standard 
        ''' (<c>netstandard</c>) and compilations without a core library.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="compilation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsNetFramework(compilation As Compilation) As Boolean

            If compilation Is Nothing Then
                Throw New ArgumentNullException(NameOf(compilation))
            End If

            Dim objectType As INamedTypeSymbol = compilation.GetSpecialType(SpecialType.System_Object)
            Return objectType IsNot Nothing AndAlso objectType.ContainingAssembly IsNot Nothing AndAlso
                   objectType.ContainingAssembly.Name.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Gets the parameters that an operation can reference: those of its enclosing lambdas, from the innermost, and then those 
        ''' of the method, accessor or constructor that contains it.
        ''' </summary>
        '''
        ''' <param name="operation">
        ''' The operation.
        ''' </param>
        '''
        ''' <param name="containingSymbol">
        ''' The symbol that contains the operation (<c>OperationAnalysisContext.ContainingSymbol</c>).
        ''' </param>
        '''
        ''' <returns>
        ''' The parameters, innermost first.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="operation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetVisibleParameters(operation As IOperation, containingSymbol As ISymbol) As List(Of IParameterSymbol)

            If operation Is Nothing Then
                Throw New ArgumentNullException(NameOf(operation))
            End If

            Dim parameters As New List(Of IParameterSymbol)()
            Dim current As IOperation = operation.Parent
            While current IsNot Nothing
                Dim lambda As IAnonymousFunctionOperation = TryCast(current, IAnonymousFunctionOperation)
                If lambda IsNot Nothing Then
                    parameters.AddRange(lambda.Symbol.Parameters)
                End If
                current = current.Parent
            End While

            Dim method As IMethodSymbol = TryCast(containingSymbol, IMethodSymbol)
            If method IsNot Nothing Then
                parameters.AddRange(method.Parameters)
            End If
            Return parameters
        End Function

        ''' <summary>
        ''' Gets the source of an extension method invocation: the receiver of a reduced call (<c>x.Count()</c>), or the first 
        ''' argument of a call through the module (<c>Enumerable.Count(x)</c>).
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation of an extension method.
        ''' </param>
        '''
        ''' <returns>
        ''' The source, with its implicit conversions; <see langword="Nothing"/> if the invocation does not have one.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="invocation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetExtensionSource(invocation As IInvocationOperation) As IOperation

            If invocation Is Nothing Then
                Throw New ArgumentNullException(NameOf(invocation))
            End If

            If invocation.TargetMethod.ReducedFrom IsNot Nothing Then
                Return invocation.Instance
            End If
            If Not invocation.TargetMethod.IsExtensionMethod Then
                Return Nothing
            End If

            Dim first As IArgumentOperation = invocation.Arguments.FirstOrDefault(
                Function(a As IArgumentOperation) a.Parameter IsNot Nothing AndAlso a.Parameter.Ordinal = 0)
            Return first?.Value
        End Function

        ''' <summary>
        ''' Gets the number of parameters of an extension method invocation besides its source (see <see cref="GetExtensionSource"/>).
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation of an extension method.
        ''' </param>
        '''
        ''' <returns>
        ''' The number of parameters after the source (0 for <c>Count()</c>, 1 for <c>Count(predicate)</c>).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="invocation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetExtensionParameterCount(invocation As IInvocationOperation) As Integer

            If invocation Is Nothing Then
                Throw New ArgumentNullException(NameOf(invocation))
            End If

            Dim count As Integer = invocation.TargetMethod.Parameters.Length
            Return If(invocation.TargetMethod.ReducedFrom IsNot Nothing, count, Math.Max(0, count - 1))
        End Function

        ''' <summary>
        ''' Determines whether a type argument satisfies the constraints of a type parameter (speculative binding does not check them).
        ''' </summary>
        '''
        ''' <param name="typeParameter">
        ''' The type parameter.
        ''' </param>
        '''
        ''' <param name="typeArgument">
        ''' The type argument.
        ''' </param>
        '''
        ''' <param name="compilation">
        ''' The compilation, to classify the conversions to the constraint types.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the constraints are satisfied; <see langword="False"/> if they are not, or if a constraint 
        ''' depends on another type parameter.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when an argument is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function SatisfiesConstraints(typeParameter As ITypeParameterSymbol, typeArgument As ITypeSymbol, compilation As Compilation) As Boolean

            If typeParameter Is Nothing Then
                Throw New ArgumentNullException(NameOf(typeParameter))
            End If
            If typeArgument Is Nothing Then
                Throw New ArgumentNullException(NameOf(typeArgument))
            End If
            If compilation Is Nothing Then
                Throw New ArgumentNullException(NameOf(compilation))
            End If

            Dim argumentParameter As ITypeParameterSymbol = TryCast(typeArgument, ITypeParameterSymbol)
            Dim isNullable As Boolean = typeArgument.OriginalDefinition.SpecialType = SpecialType.System_Nullable_T
            If typeParameter.HasReferenceTypeConstraint AndAlso Not typeArgument.IsReferenceType Then
                Return False
            End If
            If typeParameter.HasValueTypeConstraint AndAlso (Not typeArgument.IsValueType OrElse isNullable) Then
                Return False
            End If
            If typeParameter.HasConstructorConstraint Then
                Dim named As INamedTypeSymbol = TryCast(typeArgument, INamedTypeSymbol)
                Dim hasConstructor As Boolean
                Select Case True
                    Case argumentParameter IsNot Nothing
                        hasConstructor = argumentParameter.HasConstructorConstraint OrElse argumentParameter.HasValueTypeConstraint
                    Case typeArgument.IsValueType
                        hasConstructor = True
                    Case named IsNot Nothing AndAlso named.TypeKind = TypeKind.Class AndAlso Not named.IsAbstract
                        hasConstructor = named.InstanceConstructors.Any(
                            Function(c As IMethodSymbol) c.Parameters.IsEmpty AndAlso c.DeclaredAccessibility = Accessibility.Public)
                    Case Else
                        hasConstructor = False
                End Select
                If Not hasConstructor Then
                    Return False
                End If
            End If

            For Each constraint As ITypeSymbol In typeParameter.ConstraintTypes
                If UtilAnalyzerSemantic.ReferencesTypeParameter(constraint) Then
                    Return False
                End If
                Dim conversion As Operations.CommonConversion = compilation.ClassifyCommonConversion(typeArgument, constraint)
                If Not conversion.Exists OrElse Not conversion.IsImplicit OrElse conversion.IsUserDefined OrElse conversion.IsNumeric Then
                    Return False
                End If
            Next constraint
            Return True
        End Function

        ''' <summary>
        ''' Determines whether a type is or contains a type parameter (<c>T</c>, <c>IComparable(Of T)</c>, <c>T()</c>...).
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type references a type parameter; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function ReferencesTypeParameter(type As ITypeSymbol) As Boolean

            Dim pending As New Stack(Of ITypeSymbol)()
            pending.Push(type)
            While pending.Count > 0
                Dim current As ITypeSymbol = pending.Pop()
                Select Case True
                    Case current Is Nothing
                        Continue While
                    Case current.TypeKind = TypeKind.TypeParameter
                        Return True
                    Case TypeOf current Is IArrayTypeSymbol
                        pending.Push(DirectCast(current, IArrayTypeSymbol).ElementType)
                    Case TypeOf current Is INamedTypeSymbol
                        For Each argument As ITypeSymbol In DirectCast(current, INamedTypeSymbol).TypeArguments
                            pending.Push(argument)
                        Next argument
                End Select
            End While
            Return False
        End Function

        ''' <summary>
        ''' Gets the argument of an invocation or object creation that corresponds to a parameter.
        ''' </summary>
        '''
        ''' <param name="arguments">
        ''' The arguments, in any order (Visual Basic sorts them by parameter).
        ''' </param>
        '''
        ''' <param name="ordinal">
        ''' The zero-based position of the parameter.
        ''' </param>
        '''
        ''' <returns>
        ''' The argument; <see langword="Nothing"/> if there is none.
        ''' </returns>
        Friend Shared Function GetArgument(arguments As ImmutableArray(Of IArgumentOperation), ordinal As Integer) As IArgumentOperation

            Return If(arguments.IsDefault, Nothing,
                      arguments.FirstOrDefault(Function(a As IArgumentOperation) a.Parameter IsNot Nothing AndAlso a.Parameter.Ordinal = ordinal))
        End Function

        ''' <summary>
        ''' Finds an accessible, non-indexed instance property of a type by name.
        ''' </summary>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the property would be accessed.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The type (its base types and, for interfaces, its base interfaces are searched too).
        ''' </param>
        '''
        ''' <param name="name">
        ''' The name of the property.
        ''' </param>
        '''
        ''' <returns>
        ''' The property; <see langword="Nothing"/> if there is none.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindInstanceProperty(semanticModel As SemanticModel, position As Integer, type As ITypeSymbol, name As String) As IPropertySymbol

            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If
            Return If(type Is Nothing OrElse type.TypeKind = TypeKind.Error, Nothing,
                      semanticModel.LookupSymbols(position, container:=type, name:=name).OfType(Of IPropertySymbol)().FirstOrDefault(
                          Function(p As IPropertySymbol) Not p.IsStatic AndAlso p.Parameters.IsEmpty AndAlso p.GetMethod IsNot Nothing))
        End Function

        ''' <summary>
        ''' Gets the name of the property that returns the number of elements of a sequence without enumerating it.
        ''' </summary>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the property would be accessed.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The type of the sequence.
        ''' </param>
        '''
        ''' <returns>
        ''' <c>Length</c> for one-dimensional arrays and <see cref="String"/>, <c>Count</c> for a type with an accessible 
        ''' <see cref="Integer"/> <c>Count</c> property; otherwise, <see langword="Nothing"/>.
        ''' </returns>
        Friend Shared Function GetSizePropertyName(semanticModel As SemanticModel, position As Integer, type As ITypeSymbol) As String

            Dim array As IArrayTypeSymbol = TryCast(type, IArrayTypeSymbol)
            Select Case True
                Case type Is Nothing
                    Return Nothing
                Case array IsNot Nothing
                    Return If(array.IsSZArray, "Length", Nothing)
                Case type.SpecialType = SpecialType.System_String
                    Return "Length"
            End Select

            Dim count As IPropertySymbol = UtilAnalyzerSemantic.FindInstanceProperty(semanticModel, position, type, "Count")
            Return If(count IsNot Nothing AndAlso count.Type.SpecialType = SpecialType.System_Int32, "Count", Nothing)
        End Function

        ''' <summary>
        ''' Gets the operand of the implicit conversions that wrap an operation (for example the boxing of a value passed to an 
        ''' <see cref="Object"/> parameter).
        ''' </summary>
        '''
        ''' <param name="operation">
        ''' The operation.
        ''' </param>
        '''
        ''' <returns>
        ''' The innermost operation that is not an implicit conversion; <see langword="Nothing"/> if <paramref name="operation"/> 
        ''' is <see langword="Nothing"/>.
        ''' </returns>
        Friend Shared Function SkipImplicitConversions(operation As IOperation) As IOperation

            Dim current As IOperation = operation
            While TypeOf current Is IConversionOperation AndAlso current.IsImplicit
                current = DirectCast(current, IConversionOperation).Operand
            End While
            Return current
        End Function

        ''' <summary>
        ''' Determines whether a symbol is a member with a given name of a type with a given metadata name.
        ''' </summary>
        '''
        ''' <param name="symbol">
        ''' The symbol (a method, property or field; for generic or reduced extension methods, its original definition is used).
        ''' </param>
        '''
        ''' <param name="typeName">
        ''' The fully qualified metadata name of the containing type (for example <c>System.Linq.Enumerable</c> or 
        ''' <c>System.Collections.Generic.Dictionary`2</c>).
        ''' </param>
        '''
        ''' <param name="memberName">
        ''' The name of the member.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the symbol is that member; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsMember(symbol As ISymbol, typeName As String, memberName As String) As Boolean

            If symbol Is Nothing OrElse Not symbol.Name.Equals(memberName, StringComparison.Ordinal) Then
                Return False
            End If

            Dim method As IMethodSymbol = TryCast(symbol, IMethodSymbol)
            Dim definition As ISymbol = If(method IsNot Nothing AndAlso method.ReducedFrom IsNot Nothing, method.ReducedFrom, symbol).OriginalDefinition
            Return UtilAnalyzerSemantic.HasMetadataName(definition.ContainingType, typeName)
        End Function

        ''' <summary>
        ''' Determines whether a type has a given fully qualified metadata name.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type (for a constructed generic type, its original definition is compared).
        ''' </param>
        '''
        ''' <param name="metadataName">
        ''' The fully qualified metadata name (for example <c>System.Collections.Generic.IList`1</c>).
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type has that name; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function HasMetadataName(type As ITypeSymbol, metadataName As String) As Boolean

            If type Is Nothing OrElse String.IsNullOrEmpty(metadataName) Then
                Return False
            End If

            Dim definition As ITypeSymbol = type.OriginalDefinition
            Dim namespaceName As String = If(definition.ContainingNamespace Is Nothing OrElse definition.ContainingNamespace.IsGlobalNamespace,
                                             String.Empty, $"{definition.ContainingNamespace.ToDisplayString()}.")
            Dim containing As String = If(definition.ContainingType Is Nothing, namespaceName, $"{definition.ContainingType.ToDisplayString()}+")
            Return $"{containing}{definition.MetadataName}".Equals(metadataName, StringComparison.Ordinal)
        End Function

        ''' <summary>
        ''' Determines whether a type is, inherits from or implements a type with a given fully qualified metadata name.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <param name="metadataName">
        ''' The fully qualified metadata name of the base type or interface.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if <paramref name="type"/> is or derives from that type; otherwise, <see langword="False"/>.
        ''' </returns>
        Friend Shared Function IsOrDerivesFrom(type As ITypeSymbol, metadataName As String) As Boolean

            If type Is Nothing Then
                Return False
            End If

            Dim current As ITypeSymbol = type
            While current IsNot Nothing
                If UtilAnalyzerSemantic.HasMetadataName(current, metadataName) Then
                    Return True
                End If
                current = current.BaseType
            End While
            Return type.AllInterfaces.Any(Function(i As INamedTypeSymbol) UtilAnalyzerSemantic.HasMetadataName(i, metadataName))
        End Function

#End Region

    End Class

End Namespace

#End Region
