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

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " EnumerableParameterNameCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.CollectionNameOnEnumerableParameter"/>: renames the 
    ''' <see cref="System.Collections.IEnumerable"/> parameter with the name suggested by the analyzer (<c>sequence</c>, 
    ''' <c>seq</c>, or the next free name), in the whole solution (named arguments and XML documentation included).
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(EnumerableParameterNameCodeFixProvider)), [Shared]>
    Public NotInheritable Class EnumerableParameterNameCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.CollectionNameOnEnumerableParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="EnumerableParameterNameCodeFixProvider"/> class.
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
                If Not diagnostic.Properties.TryGetValue(EnumerableParameterNameAnalyzer.NewNamePropertyKey, newName) OrElse String.IsNullOrEmpty(newName) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create($"Rename the parameter to '{newName}'",
                                      Function(ct As CancellationToken) UtilCodeFix.RenameParameterAsync(document, diagnostic, newName, ct),
                                      equivalenceKey:=NameOf(EnumerableParameterNameCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

    End Class

End Namespace

#End Region
