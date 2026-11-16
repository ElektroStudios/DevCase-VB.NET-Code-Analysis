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

#Region " NetFrameworkApiMigrationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.NetFrameworkApiMigration"/>: duplicates the lines of the statement 
    ''' into an <c>#If &lt;symbol&gt; Then</c> branch that uses the modern APIs, keeping the original lines in the <c>#Else</c> branch.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Every migratable usage of the same statement is converted at once, and the most restrictive symbol among them is used 
    ''' (e.g. <c>NET5_0_OR_GREATER</c> when the statement also uses <c>Convert.ToHexString</c>).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(NetFrameworkApiMigrationCodeFixProvider)), [Shared]>
    Public NotInheritable Class NetFrameworkApiMigrationCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add '#If' block with the modern .NET API"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.NetFrameworkApiMigration)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="NetFrameworkApiMigrationCodeFixProvider"/> class.
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
                Dim span As TextSpan = diagnostic.Location.SourceSpan
                context.RegisterCodeFix(
                    CodeAction.Create(NetFrameworkApiMigrationCodeFixProvider.Title,
                                      Function(ct As CancellationToken) NetFrameworkApiMigrationCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(NetFrameworkApiMigrationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the statement that contains the usage reported at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span.
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
        ''' Thrown when the reported usage can no longer be migrated.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            ' The reported node is one of the nodes whose span equals the diagnostic span.
            Dim reported As ApiMigration = root.FindNode(span, getInnermostNodeForTie:=True).AncestorsAndSelf().
                TakeWhile(Function(n As SyntaxNode) n.Span = span).
                Select(Function(n As SyntaxNode) UtilApiMigrations.TryMatch(n, semanticModel, cancellationToken)).
                FirstOrDefault(Function(m As ApiMigration) m IsNot Nothing)
            If reported Is Nothing Then
                Throw New InvalidOperationException("The reported usage can no longer be migrated.")
            End If

            Dim host As StatementSyntax = UtilApiMigrations.GetHost(reported)
            If host Is Nothing OrElse Not UtilApiMigrations.CanWrapHost(host) Then
                Throw New InvalidOperationException("The statement cannot be wrapped in a conditional compilation block.")
            End If

            ' All the migrations that belong to the same host statement, without overlaps.
            Dim migrations As New List(Of ApiMigration)()
            For Each node As SyntaxNode In host.DescendantNodesAndSelf()
                Dim migration As ApiMigration = UtilApiMigrations.TryMatch(node, semanticModel, cancellationToken)
                If migration IsNot Nothing AndAlso UtilApiMigrations.GetHost(migration) Is host AndAlso
                   Not migrations.Exists(Function(m As ApiMigration) m.Node.Span.OverlapsWith(migration.Node.Span)) Then
                    migrations.Add(migration)
                End If
            Next node

            Dim condition As String = migrations.Select(Function(m As ApiMigration) m.Condition).Aggregate(Function(a As String, b As String) UtilApiMigrations.MostRestrictive(a, b))

            Dim firstLine As TextLine = text.Lines.GetLineFromPosition(host.SpanStart)
            Dim lastLine As TextLine = text.Lines.GetLineFromPosition(host.Span.End)
            Dim blockStart As Integer = firstLine.Start
            Dim blockEnd As Integer = lastLine.EndIncludingLineBreak
            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)

            Dim original As String = text.ToString(TextSpan.FromBounds(blockStart, blockEnd))
            If lastLine.EndIncludingLineBreak = lastLine.End Then
                original = $"{original}{eol}"
            End If

            Dim modern As New StringBuilder(original)
            For Each migration As ApiMigration In migrations.OrderByDescending(Function(m As ApiMigration) m.Node.SpanStart)
                modern.Remove(migration.Node.SpanStart - blockStart, migration.Node.Span.Length)
                modern.Insert(migration.Node.SpanStart - blockStart, migration.Replacement)
            Next migration

            Dim replacement As String = $"#If {condition} Then{eol}{modern}#Else{eol}{original}#End If{eol}"
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(TextSpan.FromBounds(blockStart, blockEnd), replacement)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
