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

#Region " ThreadSleepCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.ThreadSleepInAsyncMethod"/>: two lightbulb actions that replace 
    ''' <c>Thread.Sleep(x)</c> with <c>Await Task.Delay(x).ConfigureAwait(False)</c> or <c>Await Task.Delay(x).ConfigureAwait(True)</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>ConfigureAwait(False)</c> is offered first (library code, background work). In UI code (an event handler of a form), 
    ''' the code after <c>Thread.Sleep</c> runs on the UI thread, so <c>ConfigureAwait(True)</c> keeps that behavior: with 
    ''' <c>ConfigureAwait(False)</c> the code after the delay would run on a thread-pool thread and could not access the controls. 
    ''' No fix is offered where <see langword="Await"/> is not allowed (see <see cref="ThreadSleepAnalyzer.CanAwaitPropertyKey"/>).
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ThreadSleepCodeFixProvider)), [Shared]>
    Public NotInheritable Class ThreadSleepCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.ThreadSleepInAsyncMethod)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThreadSleepCodeFixProvider"/> class.
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
                Dim canAwait As String = Nothing
                If Not diagnostic.Properties.TryGetValue(ThreadSleepAnalyzer.CanAwaitPropertyKey, canAwait) OrElse
                   Not Boolean.TrueString.Equals(canAwait, StringComparison.Ordinal) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Replace with 'Await Task.Delay(...).ConfigureAwait(False)'",
                                      Function(ct As CancellationToken) ThreadSleepCodeFixProvider.FixAsync(document, diagnostic, False, ct),
                                      equivalenceKey:=$"{NameOf(ThreadSleepCodeFixProvider)}.False"),
                    diagnostic)
                context.RegisterCodeFix(
                    CodeAction.Create("Replace with 'Await Task.Delay(...).ConfigureAwait(True)': continues on the UI thread",
                                      Function(ct As CancellationToken) ThreadSleepCodeFixProvider.FixAsync(document, diagnostic, True, ct),
                                      equivalenceKey:=$"{NameOf(ThreadSleepCodeFixProvider)}.True"),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the <c>Thread.Sleep</c> statement reported by the specified diagnostic with an <c>Await Task.Delay</c> statement.
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
        ''' <param name="continueOnCapturedContext">
        ''' The argument of <c>ConfigureAwait</c>.
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
        ''' Thrown when the diagnostic no longer points to a <c>Thread.Sleep</c> statement, or the <c>Task</c> type cannot be resolved.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, continueOnCapturedContext As Boolean,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim invocation As InvocationExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of InvocationExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim statement As SyntaxNode = invocation.Parent
            Dim taskType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")
            If invocation.ArgumentList Is Nothing OrElse invocation.ArgumentList.Arguments.Count <> 1 OrElse taskType Is Nothing OrElse
               (TypeOf statement IsNot ExpressionStatementSyntax AndAlso TypeOf statement IsNot CallStatementSyntax) Then
                Throw New InvalidOperationException("The diagnostic does not point to a Thread.Sleep statement.")
            End If

            Dim delay As String = invocation.ArgumentList.Arguments(0).GetExpression().ToString()
            Dim taskName As String = taskType.ToMinimalDisplayString(model, statement.SpanStart)
            Dim replacement As String = $"Await {taskName}.Delay({delay}).ConfigureAwait({If(continueOnCapturedContext, "True", "False")})"
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(statement.Span, replacement)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
