#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis

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

#End Region

    End Class

End Namespace

#End Region
