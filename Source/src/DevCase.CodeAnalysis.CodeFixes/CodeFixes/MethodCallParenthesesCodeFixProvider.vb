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

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " MethodCallParenthesesCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MethodCallWithoutParentheses"/>: adds <c>()</c> after the method name,
    ''' for example <c>query.ToArray</c> → <c>query.ToArray()</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(MethodCallParenthesesCodeFixProvider)), [Shared]>
    Public NotInheritable Class MethodCallParenthesesCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MethodCallWithoutParentheses)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="MethodCallParenthesesCodeFixProvider"/> class.
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
                    CodeAction.Create("Add parentheses to the method call",
                                      Function(ct As CancellationToken) UtilCodeFix.ApplyTextChangesAsync(
                                          document, {New TextChange(New TextSpan(diagnostic.Location.SourceSpan.End, 0), "()")}, ct),
                                      equivalenceKey:=NameOf(MethodCallParenthesesCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

    End Class

End Namespace

#End Region
