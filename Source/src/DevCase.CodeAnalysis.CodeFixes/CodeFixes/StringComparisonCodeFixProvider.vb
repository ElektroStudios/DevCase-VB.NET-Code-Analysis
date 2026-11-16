#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " StringComparisonCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.MissingStringComparison"/>: 
    ''' two lightbulb actions that add either <c>StringComparison.Ordinal</c> or <c>StringComparison.OrdinalIgnoreCase</c> 
    ''' (or the equivalent <c>StringComparer</c> members when the parameter expects a comparer).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Two actions are offered because adding a case-insensitive comparison changes the behavior of a call that 
    ''' was previously case-sensitive (e.g. <c>s.Contains("A")</c>); the developer chooses the intended semantics.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(StringComparisonCodeFixProvider)), [Shared]>
    Public NotInheritable Class StringComparisonCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The member name of the ordinal (case-sensitive) comparison.
        ''' </summary>
        Friend Const OrdinalMemberName As String = "Ordinal"

        ''' <summary>
        ''' The member name of the ordinal case-insensitive comparison.
        ''' </summary>
        Friend Const OrdinalIgnoreCaseMemberName As String = "OrdinalIgnoreCase"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingStringComparison)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StringComparisonCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return WellKnownFixAllProviders.BatchFixer
        End Function

        ''' <inheritdoc/>
        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim kindText As String = Nothing
                If Not diagnostic.Properties.TryGetValue(UtilStringComparison.KindPropertyKey, kindText) Then
                    Throw New InvalidOperationException("The diagnostic does not carry the comparison kind.")
                End If

                Dim kind As StringComparisonArgumentKind = DirectCast([Enum].Parse(GetType(StringComparisonArgumentKind), kindText), StringComparisonArgumentKind)
                Dim typeName As String = If(kind = StringComparisonArgumentKind.Comparison, NameOf(StringComparison), NameOf(StringComparer))
                Dim document As Document = context.Document

                For Each memberName As String In {StringComparisonCodeFixProvider.OrdinalMemberName, StringComparisonCodeFixProvider.OrdinalIgnoreCaseMemberName}
                    Dim capturedMember As String = memberName
                    context.RegisterCodeFix(
                        CodeAction.Create($"Add '{typeName}.{capturedMember}'",
                                          Function(ct As CancellationToken) StringComparisonCodeFixProvider.FixAsync(document, diagnostic, capturedMember, ct),
                                          equivalenceKey:=$"{NameOf(StringComparisonCodeFixProvider)}.{capturedMember}"),
                        diagnostic)
                Next memberName
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Builds the fully qualified comparison expression (e.g. <c>Global.System.StringComparison.Ordinal</c>), 
        ''' annotated so that the simplifier reduces it to the shortest valid form.
        ''' </summary>
        '''
        ''' <param name="kind">
        ''' The kind of comparison argument.
        ''' </param>
        '''
        ''' <param name="memberName">
        ''' <see cref="OrdinalMemberName"/> or <see cref="OrdinalIgnoreCaseMemberName"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The comparison expression.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="memberName"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        Friend Shared Function BuildComparisonExpression(kind As StringComparisonArgumentKind, memberName As String) As ExpressionSyntax

            If String.IsNullOrWhiteSpace(memberName) Then
                Throw New ArgumentNullException(NameOf(memberName))
            End If

            Dim typeName As String = If(kind = StringComparisonArgumentKind.Comparison, NameOf(StringComparison), NameOf(StringComparer))
            Dim dot As SyntaxToken = SyntaxFactory.Token(SyntaxKind.DotToken)

            Dim systemAccess As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.GlobalName(), dot, SyntaxFactory.IdentifierName(NameOf(System)))
            Dim typeAccess As MemberAccessExpressionSyntax =
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, systemAccess, dot, SyntaxFactory.IdentifierName(typeName))

            Return SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, typeAccess, dot, SyntaxFactory.IdentifierName(memberName)).
                                 WithAdditionalAnnotations(Simplifier.Annotation)
        End Function

        ''' <summary>
        ''' Returns a copy of an invocation with the comparison argument added at the right position.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The original invocation.
        ''' </param>
        '''
        ''' <param name="comparison">
        ''' The comparison expression.
        ''' </param>
        '''
        ''' <param name="parameterName">
        ''' The name of the parameter that receives the argument (used for a named argument when a positional one is not possible).
        ''' </param>
        '''
        ''' <param name="position">
        ''' The zero-based argument position.
        ''' </param>
        '''
        ''' <returns>
        ''' The new invocation.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="invocation"/> or <paramref name="comparison"/> is <see langword="Nothing"/>, 
        ''' or <paramref name="parameterName"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentOutOfRangeException">
        ''' Thrown when <paramref name="position"/> is negative.
        ''' </exception>
        Friend Shared Function AddComparisonArgument(invocation As InvocationExpressionSyntax, comparison As ExpressionSyntax,
                                                     parameterName As String, position As Integer) As InvocationExpressionSyntax

            If invocation Is Nothing Then
                Throw New ArgumentNullException(NameOf(invocation))
            End If
            If comparison Is Nothing Then
                Throw New ArgumentNullException(NameOf(comparison))
            End If
            If String.IsNullOrWhiteSpace(parameterName) Then
                Throw New ArgumentNullException(NameOf(parameterName))
            End If
            If position < 0 Then
                Throw New ArgumentOutOfRangeException(NameOf(position))
            End If

            ' "obj.Method" without parentheses: the trailing trivia moves after the new argument list.
            If invocation.ArgumentList Is Nothing Then
                Dim newList As ArgumentListSyntax =
                    SyntaxFactory.ArgumentList(SyntaxFactory.SingletonSeparatedList(Of ArgumentSyntax)(SyntaxFactory.SimpleArgument(comparison))).
                                  WithTrailingTrivia(invocation.GetTrailingTrivia())
                Return invocation.WithExpression(invocation.Expression.WithoutTrailingTrivia()).WithArgumentList(newList)
            End If

            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = invocation.ArgumentList.Arguments
            Dim allPositional As Boolean = arguments.All(Function(a As ArgumentSyntax) TypeOf a Is SimpleArgumentSyntax AndAlso Not DirectCast(a, SimpleArgumentSyntax).IsNamed)

            Dim newArguments As SeparatedSyntaxList(Of ArgumentSyntax)
            If allPositional AndAlso position <= arguments.Count Then
                newArguments = arguments.Insert(position, SyntaxFactory.SimpleArgument(comparison))
            Else
                Dim named As SimpleArgumentSyntax =
                    SyntaxFactory.SimpleArgument(SyntaxFactory.NameColonEquals(SyntaxFactory.IdentifierName(parameterName)), comparison)
                newArguments = arguments.Add(named)
            End If

            Dim newArgumentList As ArgumentListSyntax = invocation.ArgumentList.WithArguments(newArguments).WithAdditionalAnnotations(Formatter.Annotation)
            Return invocation.WithArgumentList(newArgumentList)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the invocation reported by the specified diagnostic.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic, which carries the parameter name, argument position and comparison kind.
        ''' </param>
        '''
        ''' <param name="memberName">
        ''' <see cref="OrdinalMemberName"/> or <see cref="OrdinalIgnoreCaseMemberName"/>.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, memberName As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim invocation As InvocationExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of InvocationExpressionSyntax)(root, diagnostic.Location.SourceSpan)

            Dim parameterName As String = diagnostic.Properties(UtilStringComparison.ParameterNamePropertyKey)
            Dim position As Integer = Integer.Parse(diagnostic.Properties(UtilStringComparison.ArgumentPositionPropertyKey), CultureInfo.InvariantCulture)
            Dim kind As StringComparisonArgumentKind =
                DirectCast([Enum].Parse(GetType(StringComparisonArgumentKind), diagnostic.Properties(UtilStringComparison.KindPropertyKey)), StringComparisonArgumentKind)

            Dim comparison As ExpressionSyntax = StringComparisonCodeFixProvider.BuildComparisonExpression(kind, memberName)
            Dim newInvocation As InvocationExpressionSyntax = StringComparisonCodeFixProvider.AddComparisonArgument(invocation, comparison, parameterName, position)

            Dim newDocument As Document = document.WithSyntaxRoot(root.ReplaceNode(invocation, newInvocation))
            newDocument = Await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Dim formatted As Document = Await Formatter.FormatAsync(newDocument, Formatter.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
            Return Await UtilCodeFix.NormalizeLineEndingsAsync(document, formatted, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
