#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Linq
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

#Region " CollectionMethodCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.LinqInsteadOfCollectionMethod"/>: replaces 
    ''' <c>list.FirstOrDefault(predicate)</c> with <c>list.Find(predicate)</c> (and <c>Any</c>/<c>All</c> with <c>Exists</c>/<c>TrueForAll</c>), 
    ''' or <c>array.Any(predicate)</c> with <c>Array.Exists(array, predicate)</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(CollectionMethodCodeFixProvider)), [Shared]>
    Public NotInheritable Class CollectionMethodCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.LinqInsteadOfCollectionMethod)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CollectionMethodCodeFixProvider"/> class.
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
                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Use the collection-specific method",
                                      Function(ct As CancellationToken) CollectionMethodCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(CollectionMethodCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
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
        ''' The diagnostic to fix.
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
        ''' Thrown when the diagnostic no longer points to a LINQ call with a predicate.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim invocation As InvocationExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of InvocationExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim access As MemberAccessExpressionSyntax = If(invocation Is Nothing, Nothing, TryCast(invocation.Expression, MemberAccessExpressionSyntax))
            If access Is Nothing OrElse access.Expression Is Nothing OrElse invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 Then
                Throw New InvalidOperationException("The diagnostic does not point to a LINQ call with a predicate.")
            End If

            Dim replacement As String = diagnostic.Properties(CollectionMethodAnalyzer.ReplacementPropertyKey)
            Dim newExpression As ExpressionSyntax
            If String.Equals(diagnostic.Properties(CollectionMethodAnalyzer.IsArrayPropertyKey), Boolean.TrueString, StringComparison.OrdinalIgnoreCase) Then
                Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
                Dim arrayName As String = model.Compilation.GetSpecialType(SpecialType.System_Array).ToMinimalDisplayString(model, invocation.SpanStart)
                Dim receiver As String = access.Expression.WithoutTrivia().ToFullString()
                Dim predicate As String = invocation.ArgumentList.Arguments(0).GetExpression().WithoutTrivia().ToFullString()
                newExpression = SyntaxFactory.ParseExpression($"{arrayName}.{replacement}({receiver}, {predicate})")
            Else
                ' The predicate is written positionally: List(Of T).Exists and TrueForAll name their parameter "match", 
                ' while Enumerable.Any and All name it "predicate" (Any(predicate:=f) → Exists(f)).
                Dim argument As ArgumentSyntax = invocation.ArgumentList.Arguments(0)
                Dim positional As ArgumentSyntax = SyntaxFactory.SimpleArgument(argument.GetExpression()).WithTriviaFrom(argument)
                newExpression = invocation.WithExpression(access.WithName(SyntaxFactory.IdentifierName(replacement).WithTriviaFrom(access.Name))).
                    WithArgumentList(invocation.ArgumentList.WithArguments(invocation.ArgumentList.Arguments.Replace(argument, positional)))
            End If

            Return document.WithSyntaxRoot(root.ReplaceNode(invocation, newExpression.WithTriviaFrom(invocation)))
        End Function

#End Region

    End Class

End Namespace

#End Region
