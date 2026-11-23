#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " UtilParameterNames "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides the choice of a new name for a parameter that is renamed by a code fix
    ''' (rules <see cref="DiagnosticIds.MisusedSenderParameter"/> and <see cref="DiagnosticIds.CollectionNameOnEnumerableParameter"/>).
    ''' </summary>
    Friend NotInheritable Class UtilParameterNames

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilParameterNames"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the first candidate name that a parameter can take without changing what another identifier of its member refers to.
        ''' </summary>
        '''
        ''' <param name="parameter">
        ''' The parameter to rename.
        ''' </param>
        '''
        ''' <param name="candidates">
        ''' The candidate names, in order of preference.
        ''' </param>
        '''
        ''' <returns>
        ''' The first candidate that is not used in the member that declares the parameter,
        ''' or <see langword="Nothing"/> if every candidate is used.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="parameter"/> or <paramref name="candidates"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function ChooseName(parameter As ParameterSyntax, candidates As IEnumerable(Of String)) As String

            If parameter Is Nothing Then
                Throw New ArgumentNullException(NameOf(parameter))
            End If
            If candidates Is Nothing Then
                Throw New ArgumentNullException(NameOf(candidates))
            End If

            Dim used As HashSet(Of String) = UtilParameterNames.GetUsedNames(parameter)
            Return candidates.FirstOrDefault(Function(candidate As String) Not used.Contains(candidate))
        End Function

        ''' <summary>
        ''' Gets the names that a parameter cannot take: every identifier of the member that declares it (parameters, locals,
        ''' the name of the method, which is also the implicit return variable of a <see langword="Function"/>, type parameters,
        ''' and members or types used without qualification), and <c>value</c> in a property, whose <see langword="Set"/>
        ''' accessor declares an implicit parameter with that name.
        ''' </summary>
        '''
        ''' <param name="parameter">
        ''' The parameter.
        ''' </param>
        '''
        ''' <returns>
        ''' The used names, compared case-insensitively (as Visual Basic compares identifiers).
        ''' </returns>
        '''
        ''' <remarks>
        ''' Names that cannot refer to the parameter are not considered: the member name after a dot (<c>x.Value</c>, <c>.Value</c>
        ''' in a <see langword="With"/> block or an object initializer), the right part of a qualified name (<c>IO.Path</c>) and the
        ''' name of a named argument (<c>value:=1</c>). The member is the outermost declaration that contains the parameter
        ''' (the method of a lambda, for example), because a lambda parameter cannot hide a local variable of its method.
        ''' </remarks>
        Private Shared Function GetUsedNames(parameter As ParameterSyntax) As HashSet(Of String)

            Dim scope As SyntaxNode = parameter.Ancestors().
                TakeWhile(Function(n As SyntaxNode) Not (TypeOf n Is TypeBlockSyntax OrElse TypeOf n Is NamespaceBlockSyntax OrElse
                                                         TypeOf n Is CompilationUnitSyntax OrElse TypeOf n Is EnumBlockSyntax)).
                LastOrDefault()
            Dim used As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            If scope Is Nothing Then
                Return used
            End If

            For Each token As SyntaxToken In scope.DescendantTokens()
                If token.IsKind(SyntaxKind.IdentifierToken) AndAlso Not UtilParameterNames.IsMemberName(token) Then
                    used.Add(token.ValueText)
                End If
            Next token

            If TypeOf scope Is PropertyBlockSyntax OrElse TypeOf scope Is PropertyStatementSyntax Then
                used.Add("value")
            End If
            Return used
        End Function

        ''' <summary>
        ''' Determines whether an identifier is a name that cannot refer to a parameter: the name after a dot, the right part of
        ''' a qualified name, or the name of a named argument.
        ''' </summary>
        '''
        ''' <param name="token">
        ''' The identifier token.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the identifier names a member, a qualified type or namespace, or a parameter of the called member;
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsMemberName(token As SyntaxToken) As Boolean

            Dim name As SimpleNameSyntax = TryCast(token.Parent, SimpleNameSyntax)
            If name Is Nothing Then
                Return False
            End If

            Dim parent As SyntaxNode = name.Parent
            Select Case True
                Case TypeOf parent Is MemberAccessExpressionSyntax
                    Return DirectCast(parent, MemberAccessExpressionSyntax).Name Is name
                Case TypeOf parent Is QualifiedNameSyntax
                    Return DirectCast(parent, QualifiedNameSyntax).Right Is name
                Case TypeOf parent Is NamedFieldInitializerSyntax
                    Return DirectCast(parent, NamedFieldInitializerSyntax).Name Is name
                Case TypeOf parent Is NameColonEqualsSyntax
                    Return True
                Case Else
                    Return False
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
