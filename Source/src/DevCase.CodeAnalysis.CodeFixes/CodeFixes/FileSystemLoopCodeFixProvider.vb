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

#Region " FileSystemLoopCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.FileSystemLoopWithoutMessagePumping"/>: 
    ''' inserts <c>Thread.CurrentThread.Join(0)</c> (with an explanatory comment) as the first statement of the loop body, 
    ''' inside a <c>#If DEBUG Then</c> block.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The <c>ContextSwitchDeadlock</c> message is raised by a Managed Debugging Assistant, which only runs under the debugger, 
    ''' so the call is only compiled in the Debug configuration (Visual Basic projects define <c>DEBUG</c> there by default). 
    ''' The directives are written at column 0, and the thread type in its shortest valid form 
    ''' (<c>Thread</c>, or <c>Threading.Thread</c> when <c>System.Threading</c> is not imported).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(FileSystemLoopCodeFixProvider)), [Shared]>
    Public NotInheritable Class FileSystemLoopCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add 'Thread.CurrentThread.Join(0)'"

        ''' <summary>
        ''' The directive that opens the conditional block around the inserted statement.
        ''' </summary>
        Friend Const DebugCondition As String = "#If DEBUG Then"

        ''' <summary>
        ''' The comment appended to the inserted statement.
        ''' </summary>
        Friend Const ExplanatoryComment As String = "' Pumps pending messages to prevent ContextSwitchDeadlock during long-running file system enumerations."

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.FileSystemLoopWithoutMessagePumping)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FileSystemLoopCodeFixProvider"/> class.
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
                    CodeAction.Create(FileSystemLoopCodeFixProvider.Title,
                                      Function(ct As CancellationToken) FileSystemLoopCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(FileSystemLoopCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the forEachBlock whose <see langword="For Each"/> statement is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the <see langword="For Each"/> statement).
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
        ''' Thrown when the diagnostic does not point to a <see langword="For Each"/> forEachBlock.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim forEachStatement As ForEachStatementSyntax = UtilCodeFix.FindNodeBySpan(Of ForEachStatementSyntax)(root, span)
            Dim forEachBlock As ForEachBlockSyntax = TryCast(forEachStatement?.Parent, ForEachBlockSyntax)
            If forEachBlock Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a For Each loop.")
            End If

            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim threadType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName("System.Threading.Thread")
            If threadType Is Nothing Then
                Throw New InvalidOperationException("The System.Threading.Thread type cannot be resolved.")
            End If

            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim indentation As String = If(forEachBlock.Statements.Count > 0,
                                           UtilAnalyzerSyntax.GetLineIndentation(forEachBlock.Statements(0).GetFirstToken()),
                                           $"{UtilAnalyzerSyntax.GetLineIndentation(forEachStatement.GetFirstToken())}    ")
            Dim position As Integer = text.Lines.GetLineFromPosition(forEachStatement.Span.End).EndIncludingLineBreak
            Dim threadName As String = threadType.ToMinimalDisplayString(model, position)

            Dim statement As String = $"{indentation}{threadName}.CurrentThread.Join(0) {FileSystemLoopCodeFixProvider.ExplanatoryComment}"
            Dim block As String = $"{FileSystemLoopCodeFixProvider.DebugCondition}{eol}{statement}{eol}#End If{eol}"
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), block)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
