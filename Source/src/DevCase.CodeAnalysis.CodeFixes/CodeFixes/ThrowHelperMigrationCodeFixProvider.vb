#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Text
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

#Region " ThrowHelperMigrationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.ThrowHelperMigration"/>: wraps a run of classic argument guards 
    ''' in an <c>#If NETCOREAPP Then</c> ... <c>#Else</c> ... <c>#End If</c> block whose first branch uses the modern throw helpers.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The original guards are kept verbatim in the <c>#Else</c> branch, so the .NET Framework build is unchanged. 
    ''' The symbol can be changed with the <c>devcase_analyzers.dca011_condition</c> option 
    ''' (e.g. <c>NET8_0_OR_GREATER</c> when the project also targets .NET versions older than 8, which lack some helpers).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ThrowHelperMigrationCodeFixProvider)), [Shared]>
    Public NotInheritable Class ThrowHelperMigrationCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.ThrowHelperMigration)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThrowHelperMigrationCodeFixProvider"/> class.
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
        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            Dim root As SyntaxNode = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim condition As String = ThrowHelperMigrationCodeFixProvider.GetCondition(context.Document, root.SyntaxTree)

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim document As Document = context.Document
                Dim span As TextSpan = diagnostic.Location.SourceSpan
                context.RegisterCodeFix(
                    CodeAction.Create($"Add '#If {condition}' block with throw helpers",
                                      Function(ct As CancellationToken) ThrowHelperMigrationCodeFixProvider.FixAsync(document, span, condition, ct),
                                      equivalenceKey:=NameOf(ThrowHelperMigrationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the conditional compilation expression configured for a document.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document.
        ''' </param>
        '''
        ''' <param name="tree">
        ''' The syntax tree of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The condition (default: <see cref="DevCaseAnalyzerOptions.ThrowHelperConditionValue"/>).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="document"/> or <paramref name="tree"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetCondition(document As Document, tree As SyntaxTree) As String

            If document Is Nothing Then
                Throw New ArgumentNullException(NameOf(document))
            End If
            If tree Is Nothing Then
                Throw New ArgumentNullException(NameOf(tree))
            End If

            Return DevCaseAnalyzerOptions.GetString(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, tree,
                                                   DevCaseAnalyzerOptions.ThrowHelperConditionKey, DevCaseAnalyzerOptions.ThrowHelperConditionValue)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the run of guards that starts at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the whole run of guards).
        ''' </param>
        '''
        ''' <param name="condition">
        ''' The conditional compilation expression.
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
        ''' Thrown when the syntax at the diagnostic location no longer matches the reported issue.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, condition As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            Dim first As StatementSyntax = root.FindToken(span.Start).Parent.AncestorsAndSelf().OfType(Of StatementSyntax)().
                FirstOrDefault(Function(s As StatementSyntax) s.SpanStart = span.Start AndAlso
                                                              (s.IsKind(SyntaxKind.MultiLineIfBlock) OrElse s.IsKind(SyntaxKind.SingleLineIfStatement)))
            If first Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to an argument guard.")
            End If

            Dim run As List(Of ThrowHelperGuard) = UtilThrowHelperGuards.GetRun(first, semanticModel, cancellationToken)
            If run.Count = 0 Then
                Throw New InvalidOperationException("The statement is no longer a convertible argument guard.")
            End If

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(first.GetFirstToken())

            Dim header As New StringBuilder()
            header.Append("#If ").Append(condition).Append(" Then").Append(eol)
            For Each guard As ThrowHelperGuard In run
                header.Append(indentation).Append(guard.Replacement).Append(eol)
            Next guard
            header.Append("#Else").Append(eol)

            Dim startPosition As Integer = text.Lines.GetLineFromPosition(first.SpanStart).Start
            Dim endPosition As Integer = text.Lines.GetLineFromPosition(run(run.Count - 1).Statement.Span.End).EndIncludingLineBreak

            Dim changes As TextChange() = {
                New TextChange(New TextSpan(startPosition, 0), header.ToString()),
                New TextChange(New TextSpan(endPosition, 0), $"#End If{eol}")}
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
