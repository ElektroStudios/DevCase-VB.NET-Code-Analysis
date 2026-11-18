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
Imports Microsoft.CodeAnalysis.Rename

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " SenderParameterCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MisusedSenderParameter"/>: renames the <c>sender</c> parameter
    ''' with the name suggested by the analyzer, in the whole solution (named arguments and XML documentation included).
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(SenderParameterCodeFixProvider)), [Shared]>
    Public NotInheritable Class SenderParameterCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MisusedSenderParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SenderParameterCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        ''' <remarks>
        ''' The batch fixer merges the renames: each one changes distinct spans (a parameter declaration and its references).
        ''' </remarks>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return WellKnownFixAllProviders.BatchFixer
        End Function

        ''' <inheritdoc/>
        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim newName As String = Nothing
                If Not diagnostic.Properties.TryGetValue(SenderParameterAnalyzer.NewNamePropertyKey, newName) OrElse String.IsNullOrEmpty(newName) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create($"Rename 'sender' to '{newName}'",
                                      Function(ct As CancellationToken) SenderParameterCodeFixProvider.RenameAsync(document, diagnostic, newName, ct),
                                      equivalenceKey:=NameOf(SenderParameterCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Renames the parameter reported by the specified diagnostic.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document that declares the parameter.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic to fix.
        ''' </param>
        '''
        ''' <param name="newName">
        ''' The new parameter name.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The solution with the renamed parameter.
        ''' </returns>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the diagnostic no longer points to a parameter.
        ''' </exception>
        Private Shared Async Function RenameAsync(document As Document, diagnostic As Diagnostic, newName As String,
                                                  cancellationToken As CancellationToken) As Task(Of Solution)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim node As SyntaxNode = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent
            Dim parameter As IParameterSymbol = TryCast(model.GetDeclaredSymbol(node, cancellationToken), IParameterSymbol)
            If parameter Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a parameter.")
            End If

            Return Await Renamer.RenameSymbolAsync(document.Project.Solution, parameter, New SymbolRenameOptions(), newName,
                                                   cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
