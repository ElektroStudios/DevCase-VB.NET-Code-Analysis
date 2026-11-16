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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " StaThreadCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MissingStaThread"/>: adds the <c>&lt;STAThread&gt;</c> attribute to the entry point.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The attribute is written on its own line, before the existing attributes and modifiers, with the shortest type name that resolves correctly.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(StaThreadCodeFixProvider)), [Shared]>
    Public NotInheritable Class StaThreadCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The conventional suffix of attribute type names, omitted when the attribute is applied.
        ''' </summary>
        Private Const AttributeSuffix As String = "Attribute"
#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingStaThread)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StaThreadCodeFixProvider"/> class.
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
                    CodeAction.Create("Add <STAThread>",
                                      Function(ct As CancellationToken) StaThreadCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(StaThreadCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Adds <c>&lt;STAThread&gt;</c> to the method reported by the specified diagnostic.
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
        ''' Thrown when the diagnostic no longer points to a method, or the attribute type cannot be resolved.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim statement As MethodStatementSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.FirstAncestorOrSelf(Of MethodStatementSyntax)()
            Dim attributeType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName("System.STAThreadAttribute")
            If statement Is Nothing OrElse attributeType Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a method.")
            End If

            Dim firstToken As SyntaxToken = statement.GetFirstToken()
            Dim name As String = attributeType.ToMinimalDisplayString(model, statement.SpanStart)
            If name.EndsWith(StaThreadCodeFixProvider.AttributeSuffix, StringComparison.Ordinal) Then
                name = name.Substring(0, name.Length - StaThreadCodeFixProvider.AttributeSuffix.Length)
            End If

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim insertion As String = $"<{name}>{eol}{UtilAnalyzerSyntax.GetLineIndentation(firstToken)}"
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(firstToken.SpanStart, 0), insertion)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
