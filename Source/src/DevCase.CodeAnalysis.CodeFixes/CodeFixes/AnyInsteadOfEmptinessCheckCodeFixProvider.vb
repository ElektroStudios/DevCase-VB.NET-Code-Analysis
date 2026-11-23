#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " AnyInsteadOfEmptinessCheckCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.AnyInsteadOfEmptinessCheck"/>: replaces the call to 
    ''' <c>Enumerable.Any()</c> with the property of the collection, for example <c>queue.Any()</c> → <c>queue.Count &lt;&gt; 0</c>, 
    ''' <c>Not queue.Any()</c> → <c>queue.Count = 0</c>, <c>bag.Any()</c> → <c>Not bag.IsEmpty</c> and 
    ''' <c>Not bag.Any()</c> → <c>bag.IsEmpty</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(AnyInsteadOfEmptinessCheckCodeFixProvider)), [Shared]>
    Public NotInheritable Class AnyInsteadOfEmptinessCheckCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.AnyInsteadOfEmptinessCheck)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AnyInsteadOfEmptinessCheckCodeFixProvider"/> class.
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
                Dim propertyName As String = Nothing
                If Not diagnostic.Properties.TryGetValue(AnyInsteadOfEmptinessCheckAnalyzer.PropertyNamePropertyKey, propertyName) OrElse
                   String.IsNullOrEmpty(propertyName) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create($"Use the '{propertyName}' property",
                                      Function(ct As CancellationToken) AnyInsteadOfEmptinessCheckCodeFixProvider.FixAsync(document, diagnostic, propertyName, ct),
                                      equivalenceKey:=NameOf(AnyInsteadOfEmptinessCheckCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the call reported by the specified diagnostic with the property of the collection.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic to fix.
        ''' </param>
        '''
        ''' <param name="propertyName">
        ''' The name of the property (<c>IsEmpty</c>, <c>Length</c> or <c>Count</c>).
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the diagnostic no longer points to a call to <c>Enumerable.Any()</c>.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, propertyName As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim [call] As ExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of ExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim source As ExpressionSyntax = AnyInsteadOfEmptinessCheckCodeFixProvider.GetSource([call], diagnostic.Properties.ContainsKey(AnyInsteadOfEmptinessCheckAnalyzer.StaticFormPropertyKey))
            If source Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a call to Enumerable.Any().")
            End If

            ' Not items.Any() (also written Not (items.Any())) is replaced as a whole with the negated check.
            Dim target As ExpressionSyntax = [call]
            While TypeOf target.Parent Is ParenthesizedExpressionSyntax
                target = DirectCast(target.Parent, ExpressionSyntax)
            End While
            Dim negated As Boolean = target.Parent.IsKind(SyntaxKind.NotExpression)
            target = If(negated, DirectCast(target.Parent, ExpressionSyntax), [call])

            Dim sourceText As String = If(AnyInsteadOfEmptinessCheckCodeFixProvider.IsPrimary(source), source.WithoutTrivia().ToString(), $"({source.WithoutTrivia()})")
            Dim isEmpty As Boolean = propertyName.Equals(AnyInsteadOfEmptinessCheckAnalyzer.IsEmptyName, StringComparison.Ordinal)
            Dim replacement As String = If(isEmpty,
                                           If(negated, $"{sourceText}.{propertyName}", $"Not {sourceText}.{propertyName}"),
                                           If(negated, $"{sourceText}.{propertyName} = 0", $"{sourceText}.{propertyName} <> 0"))

            ' A comparison or a negation needs parentheses where the context binds more tightly (a member access, 
            ' an arithmetic or comparison operator...); only items.IsEmpty is a primary expression.
            If Not (isEmpty AndAlso negated) AndAlso AnyInsteadOfEmptinessCheckCodeFixProvider.NeedsParentheses(target) Then
                replacement = $"({replacement})"
            End If
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(target.Span, replacement)}, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Gets the collection of a call to <c>Enumerable.Any()</c>.
        ''' </summary>
        '''
        ''' <param name="call">
        ''' The call: an invocation (<c>items.Any()</c>, <c>Enumerable.Any(items)</c>) or a member access (<c>items.Any</c>).
        ''' </param>
        '''
        ''' <param name="isStaticForm">
        ''' <see langword="True"/> if the call has the static form.
        ''' </param>
        '''
        ''' <returns>
        ''' The collection expression, or <see langword="Nothing"/> if the call has another form.
        ''' </returns>
        Private Shared Function GetSource([call] As ExpressionSyntax, isStaticForm As Boolean) As ExpressionSyntax

            Dim invocation As InvocationExpressionSyntax = TryCast([call], InvocationExpressionSyntax)
            If isStaticForm Then
                Dim arguments As ArgumentListSyntax = invocation?.ArgumentList
                Return If(arguments IsNot Nothing AndAlso arguments.Arguments.Count = 1, arguments.Arguments(0).GetExpression(), Nothing)
            End If

            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(If(invocation IsNot Nothing, invocation.Expression, [call]), MemberAccessExpressionSyntax)
            Return memberAccess?.Expression
        End Function

        ''' <summary>
        ''' Determines whether an expression can be followed by a member access without parentheses.
        ''' </summary>
        '''
        ''' <param name="expression">
        ''' The expression.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for names, member accesses, invocations, parenthesized expressions, casts, <see langword="Me"/>, 
        ''' string literals and <see langword="If"/> operators; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsPrimary(expression As ExpressionSyntax) As Boolean

            Return TypeOf expression Is NameSyntax OrElse TypeOf expression Is MemberAccessExpressionSyntax OrElse
                   TypeOf expression Is InvocationExpressionSyntax OrElse TypeOf expression Is ParenthesizedExpressionSyntax OrElse
                   TypeOf expression Is CastExpressionSyntax OrElse TypeOf expression Is PredefinedCastExpressionSyntax OrElse
                   TypeOf expression Is InstanceExpressionSyntax OrElse TypeOf expression Is TernaryConditionalExpressionSyntax OrElse
                   TypeOf expression Is BinaryConditionalExpressionSyntax OrElse expression.IsKind(SyntaxKind.StringLiteralExpression)
        End Function

        ''' <summary>
        ''' Determines whether a comparison or a <see langword="Not"/> expression that replaces an expression needs parentheses.
        ''' </summary>
        '''
        ''' <param name="expression">
        ''' The replaced expression.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="False"/> where the expression is a whole expression (an argument, a condition, the right side of 
        ''' an assignment...), the operand of a logical operator (<see langword="AndAlso"/>, <see langword="OrElse"/>, 
        ''' <see langword="And"/>, <see langword="Or"/>, <see langword="Xor"/>), or is already enclosed in parentheses 
        ''' (parenthesized expressions, casts, <see langword="If"/> operators, lambda bodies); otherwise, <see langword="True"/>.
        ''' </returns>
        Private Shared Function NeedsParentheses(expression As ExpressionSyntax) As Boolean

            Dim parent As SyntaxNode = expression.Parent
            If TypeOf parent IsNot ExpressionSyntax Then
                Return False
            End If

            Select Case parent.Kind()
                Case SyntaxKind.AndAlsoExpression, SyntaxKind.OrElseExpression, SyntaxKind.AndExpression,
                     SyntaxKind.OrExpression, SyntaxKind.ExclusiveOrExpression, SyntaxKind.NotExpression
                    Return False
                Case Else
                    Return Not (TypeOf parent Is ParenthesizedExpressionSyntax OrElse TypeOf parent Is CastExpressionSyntax OrElse
                                TypeOf parent Is PredefinedCastExpressionSyntax OrElse TypeOf parent Is TernaryConditionalExpressionSyntax OrElse
                                TypeOf parent Is BinaryConditionalExpressionSyntax OrElse TypeOf parent Is LambdaExpressionSyntax OrElse
                                TypeOf parent Is CollectionInitializerSyntax)
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
