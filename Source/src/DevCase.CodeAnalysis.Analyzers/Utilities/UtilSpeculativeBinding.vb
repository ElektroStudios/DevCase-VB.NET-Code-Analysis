#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " UtilSpeculativeBinding "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Binds replacement expressions speculatively, to validate a code fix before the analyzer reports it.
    ''' </summary>
    Friend NotInheritable Class UtilSpeculativeBinding

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilSpeculativeBinding"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Parses the text of an expression.
        ''' </summary>
        '''
        ''' <param name="text">
        ''' The text of the expression.
        ''' </param>
        '''
        ''' <returns>
        ''' The expression; <see langword="Nothing"/> if the text is not exactly one valid expression.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="text"/> is <see langword="Nothing"/>, empty or white space.
        ''' </exception>
        Friend Shared Function ParseExpression(text As String) As ExpressionSyntax

            If String.IsNullOrWhiteSpace(text) Then
                Throw New ArgumentNullException(NameOf(text))
            End If

            Dim expression As ExpressionSyntax = SyntaxFactory.ParseExpression(text, consumeFullText:=True)
            Return If(expression.ContainsDiagnostics, Nothing, expression)
        End Function

        ''' <summary>
        ''' Gets the symbol that an expression would bind to at a position.
        ''' </summary>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model of the document.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the expression would be placed.
        ''' </param>
        '''
        ''' <param name="text">
        ''' The text of the expression.
        ''' </param>
        '''
        ''' <returns>
        ''' The symbol; <see langword="Nothing"/> if the text is not valid or does not bind to exactly one symbol.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetSymbol(semanticModel As SemanticModel, position As Integer, text As String) As ISymbol

            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim expression As ExpressionSyntax = UtilSpeculativeBinding.ParseExpression(text)
            Return If(expression Is Nothing, Nothing,
                      semanticModel.GetSpeculativeSymbolInfo(position, expression, SpeculativeBindingOption.BindAsExpression).Symbol)
        End Function

        ''' <summary>
        ''' Gets the type that an expression would have at a position.
        ''' </summary>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model of the document.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the expression would be placed.
        ''' </param>
        '''
        ''' <param name="text">
        ''' The text of the expression.
        ''' </param>
        '''
        ''' <returns>
        ''' The type; <see langword="Nothing"/> if the text is not valid or has no type.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetExpressionType(semanticModel As SemanticModel, position As Integer, text As String) As ITypeSymbol

            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim expression As ExpressionSyntax = UtilSpeculativeBinding.ParseExpression(text)
            If expression Is Nothing Then
                Return Nothing
            End If

            Dim type As ITypeSymbol = semanticModel.GetSpeculativeTypeInfo(position, expression, SpeculativeBindingOption.BindAsExpression).Type
            Return If(type Is Nothing OrElse type.TypeKind = TypeKind.Error, Nothing, type)
        End Function

        ''' <summary>
        ''' Gets the shortest name of a type that binds to it at a position (the name that the code would write, according to its 
        ''' <see langword="Imports"/> statements).
        ''' </summary>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model of the document.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the name would be placed.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' The name; <see langword="Nothing"/> if no name binds to the type at that position.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="semanticModel"/> or <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetTypeName(semanticModel As SemanticModel, position As Integer, type As ITypeSymbol) As String

            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If
            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            For Each candidate As String In {type.ToMinimalDisplayString(semanticModel, position), $"Global.{type.ToDisplayString()}"}
                Dim parsed As TypeSyntax = SyntaxFactory.ParseTypeName(candidate)
                If parsed.ContainsDiagnostics Then
                    Continue For
                End If
                Dim bound As ITypeSymbol = semanticModel.GetSpeculativeTypeInfo(position, parsed, SpeculativeBindingOption.BindAsTypeOrNamespace).Type
                If SymbolEqualityComparer.Default.Equals(bound, type) Then
                    Return candidate
                End If
            Next candidate
            Return Nothing
        End Function

#End Region

    End Class

End Namespace

#End Region
