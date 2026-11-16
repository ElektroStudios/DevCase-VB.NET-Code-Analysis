#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " StringComparisonArgumentKind "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Specifies the kind of string comparison argument accepted by a parameter.
    ''' </summary>
    Friend Enum StringComparisonArgumentKind As Integer

        ''' <summary>
        ''' The parameter does not accept a string comparison argument.
        ''' </summary>
        None = 0

        ''' <summary>
        ''' The parameter is of type <see cref="System.StringComparison"/>.
        ''' </summary>
        Comparison = 1

        ''' <summary>
        ''' The parameter accepts a <see cref="System.StringComparer"/>
        ''' (<c>StringComparer</c>, <c>IComparer(Of String)</c> or <c>IEqualityComparer(Of String)</c>).
        ''' </summary>
        Comparer = 2

    End Enum

End Namespace

#End Region

#Region " StringComparisonOpportunity "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Describes a string comparison argument that can be added to an invocation.
    ''' </summary>
    Friend NotInheritable Class StringComparisonOpportunity

#Region " Properties "

        ''' <summary>
        ''' Gets the name of the parameter that receives the comparison argument.
        ''' </summary>
        Friend ReadOnly Property ParameterName As String

        ''' <summary>
        ''' Gets the zero-based position of the comparison argument in the invocation argument list
        ''' (the receiver of a reduced extension method is not counted).
        ''' </summary>
        Friend ReadOnly Property ArgumentPosition As Integer

        ''' <summary>
        ''' Gets the kind of comparison argument.
        ''' </summary>
        Friend ReadOnly Property Kind As StringComparisonArgumentKind

        ''' <summary>
        ''' Gets a value indicating whether the comparison parameter is an omitted optional parameter of the invoked method itself
        ''' (as opposed to a parameter of another overload).
        ''' </summary>
        Friend ReadOnly Property IsOmittedOptionalParameter As Boolean

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StringComparisonOpportunity"/> class.
        ''' </summary>
        '''
        ''' <param name="parameterName">
        ''' The name of the parameter that receives the comparison argument.
        ''' </param>
        '''
        ''' <param name="argumentPosition">
        ''' The zero-based position of the comparison argument in the invocation argument list.
        ''' </param>
        '''
        ''' <param name="kind">
        ''' The kind of comparison argument.
        ''' </param>
        '''
        ''' <param name="isOmittedOptionalParameter">
        ''' <see langword="True"/> if the parameter is an omitted optional parameter of the invoked method.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="parameterName"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentOutOfRangeException">
        ''' Thrown when <paramref name="argumentPosition"/> is negative or <paramref name="kind"/> is <see cref="StringComparisonArgumentKind.None"/>.
        ''' </exception>
        Friend Sub New(parameterName As String, argumentPosition As Integer, kind As StringComparisonArgumentKind, isOmittedOptionalParameter As Boolean)

            If String.IsNullOrWhiteSpace(parameterName) Then
                Throw New ArgumentNullException(NameOf(parameterName))
            End If
            If argumentPosition < 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(argumentPosition))
            End If
            If kind = StringComparisonArgumentKind.None Then
                Throw New ArgumentOutOfRangeException(NameOf(kind))
            End If

            Me.ParameterName = parameterName
            Me.ArgumentPosition = argumentPosition
            Me.Kind = kind
            Me.IsOmittedOptionalParameter = isOmittedOptionalParameter
        End Sub

#End Region

    End Class

End Namespace

#End Region

#Region " UtilStringComparison "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the semantic detection logic of omitted string comparison arguments
    ''' (rule <see cref="DiagnosticIds.MissingStringComparison"/>).
    ''' </summary>
    Friend NotInheritable Class UtilStringComparison

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property key that stores the comparison parameter name.
        ''' </summary>
        Friend Const ParameterNamePropertyKey As String = "ParameterName"

        ''' <summary>
        ''' The diagnostic property key that stores the comparison argument position.
        ''' </summary>
        Friend Const ArgumentPositionPropertyKey As String = "ArgumentPosition"

        ''' <summary>
        ''' The diagnostic property key that stores the <see cref="StringComparisonArgumentKind"/>.
        ''' </summary>
        Friend Const KindPropertyKey As String = "Kind"

        ''' <summary>
        ''' The diagnostic property key that indicates an omitted optional parameter.
        ''' </summary>
        Friend Const OmittedOptionalPropertyKey As String = "OmittedOptional"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilStringComparison"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines the kind of string comparison argument accepted by a parameter type.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The parameter type.
        ''' </param>
        '''
        ''' <returns>
        ''' The <see cref="StringComparisonArgumentKind"/> accepted by the type.
        ''' </returns>
        Friend Shared Function GetArgumentKind(type As ITypeSymbol) As StringComparisonArgumentKind

            Dim namedType As INamedTypeSymbol = TryCast(type, INamedTypeSymbol)
            If namedType Is Nothing Then
                Return StringComparisonArgumentKind.None
            End If

            Dim namespaceName As String = If(namedType.ContainingNamespace Is Nothing, String.Empty, namedType.ContainingNamespace.ToDisplayString())

            Dim kind As StringComparisonArgumentKind = StringComparisonArgumentKind.None
            If namespaceName.Equals("System", StringComparison.Ordinal) Then
                If namedType.MetadataName.Equals("StringComparison", StringComparison.Ordinal) Then
                    kind = StringComparisonArgumentKind.Comparison
                ElseIf namedType.MetadataName.Equals("StringComparer", StringComparison.Ordinal) Then
                    kind = StringComparisonArgumentKind.Comparer
                End If

            ElseIf namespaceName.Equals("System.Collections.Generic", StringComparison.Ordinal) AndAlso
                   namedType.TypeArguments.Length = 1 AndAlso
                   namedType.TypeArguments(0).SpecialType = SpecialType.System_String AndAlso
                   (namedType.MetadataName.Equals("IEqualityComparer`1", StringComparison.Ordinal) OrElse
                    namedType.MetadataName.Equals("IComparer`1", StringComparison.Ordinal)) Then
                kind = StringComparisonArgumentKind.Comparer
            End If
            Return kind
        End Function

        ''' <summary>
        ''' Analyzes an invocation and determines whether a string comparison argument is available but omitted.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation expression.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <param name="method">
        ''' When this method returns, receives the invoked method symbol, or <see langword="Nothing"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The detected opportunity, or <see langword="Nothing"/> if the invocation is compliant.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="invocation"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindOpportunity(invocation As InvocationExpressionSyntax, semanticModel As SemanticModel,
                                               cancellationToken As CancellationToken, ByRef method As IMethodSymbol) As StringComparisonOpportunity

            If invocation Is Nothing Then
                Throw New ArgumentNullException(NameOf(invocation))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            method = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
            If method Is Nothing Then
                Return Nothing
            End If

            Dim isReduced As Boolean = method.ReducedFrom IsNot Nothing
            Dim unreduced As IMethodSymbol = If(isReduced, method.GetConstructedReducedFrom(), method)
            Dim receiverOffset As Integer = If(isReduced, 1, 0)

            ' Case A: the invoked method already declares a comparison parameter.
            For index As Integer = 0 To unreduced.Parameters.Length - 1
                Dim parameter As IParameterSymbol = unreduced.Parameters(index)
                Dim kind As StringComparisonArgumentKind = UtilStringComparison.GetArgumentKind(parameter.Type)
                If kind = StringComparisonArgumentKind.None Then
                    Continue For
                End If

                Return If(parameter.IsOptional AndAlso index >= receiverOffset AndAlso
                   Not UtilStringComparison.IsArgumentSupplied(invocation, semanticModel, cancellationToken, parameter),
                    New StringComparisonOpportunity(parameter.Name, index - receiverOffset, kind, isOmittedOptionalParameter:=True),
                    Nothing)
            Next index

            ' Case B: another accessible overload accepts the same arguments plus one comparison argument.
            Dim best As StringComparisonOpportunity = Nothing
            Dim currentType As INamedTypeSymbol = unreduced.ContainingType
            While currentType IsNot Nothing
                For Each member As ISymbol In currentType.GetMembers(unreduced.Name)
                    Dim candidate As IMethodSymbol = TryCast(member, IMethodSymbol)
                    If candidate Is Nothing Then
                        Continue For
                    End If

                    Dim opportunity As StringComparisonOpportunity =
                        UtilStringComparison.MatchCandidate(unreduced, candidate, receiverOffset, semanticModel, invocation.SpanStart)

                    If opportunity IsNot Nothing AndAlso
                       (best Is Nothing OrElse
                        (best.Kind = StringComparisonArgumentKind.Comparer AndAlso opportunity.Kind = StringComparisonArgumentKind.Comparison)) Then
                        best = opportunity
                    End If
                Next member
                currentType = currentType.BaseType
            End While
            Return best
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Determines whether a candidate overload accepts exactly the parameters of the invoked method plus one comparison parameter.
        ''' </summary>
        '''
        ''' <param name="invoked">
        ''' The (unreduced) invoked method.
        ''' </param>
        '''
        ''' <param name="candidate">
        ''' The candidate overload.
        ''' </param>
        '''
        ''' <param name="receiverOffset">
        ''' 1 when the invocation uses reduced extension method syntax; otherwise 0.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model used for accessibility checks.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The source position used for accessibility checks.
        ''' </param>
        '''
        ''' <returns>
        ''' The resulting opportunity, or <see langword="Nothing"/> if the candidate does not match.
        ''' </returns>
        Private Shared Function MatchCandidate(invoked As IMethodSymbol, candidate As IMethodSymbol, receiverOffset As Integer,
                                               semanticModel As SemanticModel, position As Integer) As StringComparisonOpportunity

            If SymbolEqualityComparer.Default.Equals(candidate.OriginalDefinition, invoked.OriginalDefinition) OrElse
               candidate.IsStatic <> invoked.IsStatic OrElse
               candidate.Parameters.Length <> invoked.Parameters.Length + 1 OrElse
               candidate.IsGenericMethod <> invoked.IsGenericMethod Then
                Return Nothing
            End If

            Dim constructed As IMethodSymbol = candidate
            If candidate.IsGenericMethod Then
                If candidate.Arity <> invoked.Arity Then
                    Return Nothing
                End If
                constructed = candidate.OriginalDefinition.Construct(invoked.TypeArguments.ToArray())
            End If

            If Not semanticModel.IsAccessible(position, candidate) OrElse
               candidate.GetAttributes().Any(Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso
                                                                          a.AttributeClass.Name.Equals(NameOf(ObsoleteAttribute), StringComparison.Ordinal)) Then
                Return Nothing
            End If

            For comparisonIndex As Integer = receiverOffset To constructed.Parameters.Length - 1
                Dim comparisonParameter As IParameterSymbol = constructed.Parameters(comparisonIndex)
                Dim kind As StringComparisonArgumentKind = UtilStringComparison.GetArgumentKind(comparisonParameter.Type)
                If kind = StringComparisonArgumentKind.None Then
                    Continue For
                End If

                Dim matches As Boolean = True
                Dim invokedIndex As Integer = 0
                For candidateIndex As Integer = 0 To constructed.Parameters.Length - 1
                    If candidateIndex = comparisonIndex Then
                        Continue For
                    End If
                    Dim candidateParameter As IParameterSymbol = constructed.Parameters(candidateIndex)
                    Dim invokedParameter As IParameterSymbol = invoked.Parameters(invokedIndex)
                    If candidateParameter.RefKind <> invokedParameter.RefKind OrElse
                       Not SymbolEqualityComparer.Default.Equals(candidateParameter.Type, invokedParameter.Type) Then
                        matches = False
                        Exit For
                    End If
                    invokedIndex += 1
                Next candidateIndex

                If matches Then
                    Return New StringComparisonOpportunity(comparisonParameter.Name, comparisonIndex - receiverOffset, kind, isOmittedOptionalParameter:=False)
                End If
            Next comparisonIndex
            Return Nothing
        End Function

        ''' <summary>
        ''' Determines whether the invocation explicitly supplies an argument for the specified parameter.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation expression.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <param name="parameter">
        ''' The parameter to look for.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the argument is explicit (or cannot be determined); otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsArgumentSupplied(invocation As InvocationExpressionSyntax, semanticModel As SemanticModel,
                                                   cancellationToken As CancellationToken, parameter As IParameterSymbol) As Boolean

            Dim operation As IInvocationOperation = TryCast(semanticModel.GetOperation(invocation, cancellationToken), IInvocationOperation)
            If operation Is Nothing Then
                Return True
            End If

            For Each argument As IArgumentOperation In operation.Arguments
                If argument.Parameter IsNot Nothing AndAlso
                   argument.Parameter.Ordinal = parameter.Ordinal AndAlso
                   argument.Parameter.Name.Equals(parameter.Name, StringComparison.Ordinal) Then
                    Return argument.ArgumentKind <> ArgumentKind.DefaultValue
                End If
            Next argument
            Return True
        End Function

#End Region

    End Class

End Namespace

#End Region
