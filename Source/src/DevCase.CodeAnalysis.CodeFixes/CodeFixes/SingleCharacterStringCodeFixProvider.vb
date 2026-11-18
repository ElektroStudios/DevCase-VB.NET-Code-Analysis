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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " SingleCharacterStringCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.SingleCharacterString"/>: replaces the single-character string literal
    ''' with the equivalent character literal, for example <c>String.Join("&amp;", parts)</c> → <c>String.Join("&amp;"c, parts)</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(SingleCharacterStringCodeFixProvider)), [Shared]>
    Public NotInheritable Class SingleCharacterStringCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.SingleCharacterString)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SingleCharacterStringCodeFixProvider"/> class.
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
                    CodeAction.Create("Use a character literal",
                                      Function(ct As CancellationToken) SingleCharacterStringCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(SingleCharacterStringCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the string literals reported by the specified diagnostic.
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
        ''' Thrown when the diagnostic no longer points to a single-character string literal argument.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim literal As LiteralExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of LiteralExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim arguments As ArgumentListSyntax = TryCast(literal.Parent?.Parent, ArgumentListSyntax)
            Dim indexes As String = Nothing
            If arguments Is Nothing OrElse Not diagnostic.Properties.TryGetValue(SingleCharacterStringAnalyzer.ArgumentIndexesPropertyKey, indexes) OrElse indexes Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a single-character string literal argument.")
            End If

            ' Every reported argument of the call is replaced at once (e.g. both arguments of s.Replace("a", "b")).
            Dim changes As New List(Of TextChange)()
            For Each index As Integer In indexes.Split(","c).Select(Function(value As String) Integer.Parse(value, CultureInfo.InvariantCulture))
                Dim argument As SimpleArgumentSyntax = TryCast(If(index < arguments.Arguments.Count, arguments.Arguments(index), Nothing), SimpleArgumentSyntax)
                Dim charLiteral As String = UtilApiMigrations.GetCharLiteral(argument)
                If charLiteral Is Nothing Then
                    Throw New InvalidOperationException("The diagnostic does not point to a single-character string literal argument.")
                End If
                changes.Add(New TextChange(argument.Expression.Span, charLiteral))
            Next index
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
