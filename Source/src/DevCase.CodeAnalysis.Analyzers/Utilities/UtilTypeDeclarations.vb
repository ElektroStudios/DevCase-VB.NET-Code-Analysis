#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " UtilTypeDeclarations "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Provides helper methods to navigate Visual Basic type declarations 
    ''' (classes, modules, structures, interfaces, enumerations and delegates).
    ''' </summary>
    Friend NotInheritable Class UtilTypeDeclarations

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilTypeDeclarations"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the outermost node of a type declaration: the block for block types, or the statement for delegates.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' Any node returned by <see cref="SyntaxReference.GetSyntax"/> for a type symbol.
        ''' </param>
        '''
        ''' <returns>
        ''' The declaration node, or <see langword="Nothing"/> if the node is not a type declaration.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="node"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetDeclarationNode(node As SyntaxNode) As StatementSyntax

            If node Is Nothing Then
                Throw New ArgumentNullException(NameOf(node))
            End If

            Dim declaration As StatementSyntax = Nothing
            If TypeOf node Is TypeBlockSyntax OrElse TypeOf node Is EnumBlockSyntax OrElse TypeOf node Is DelegateStatementSyntax Then
                declaration = DirectCast(node, StatementSyntax)

            ElseIf (TypeOf node Is TypeStatementSyntax OrElse TypeOf node Is EnumStatementSyntax) AndAlso
                   (TypeOf node.Parent Is TypeBlockSyntax OrElse TypeOf node.Parent Is EnumBlockSyntax) Then
                declaration = DirectCast(node.Parent, StatementSyntax)
            End If
            Return declaration
        End Function

        ''' <summary>
        ''' Gets the header statement of a type declaration (e.g. the <see cref="ClassStatementSyntax"/> of a class block).
        ''' </summary>
        '''
        ''' <param name="declaration">
        ''' The declaration node returned by <see cref="UtilTypeDeclarations.GetDeclarationNode"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The header statement.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="declaration"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="declaration"/> is not a type declaration.
        ''' </exception>
        Friend Shared Function GetHeader(declaration As StatementSyntax) As StatementSyntax

            If declaration Is Nothing Then
                Throw New ArgumentNullException(NameOf(declaration))
            End If

            If TypeOf declaration Is TypeBlockSyntax Then
                Return DirectCast(declaration, TypeBlockSyntax).BlockStatement
            End If
            If TypeOf declaration Is EnumBlockSyntax Then
                Return DirectCast(declaration, EnumBlockSyntax).EnumStatement
            End If
            If TypeOf declaration Is DelegateStatementSyntax Then
                Return declaration
            End If
            Throw New ArgumentException($"Unsupported declaration kind: {declaration.Kind()}.", NameOf(declaration))
        End Function

        ''' <summary>
        ''' Gets the location of the identifier of a type declaration.
        ''' </summary>
        '''
        ''' <param name="declaration">
        ''' The declaration node returned by <see cref="UtilTypeDeclarations.GetDeclarationNode"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The identifier location.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="declaration"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetIdentifierLocation(declaration As StatementSyntax) As Location

            Return UtilAnalyzerSyntax.GetIdentifierToken(UtilTypeDeclarations.GetHeader(declaration)).GetLocation()
        End Function

#End Region

    End Class

End Namespace

#End Region
