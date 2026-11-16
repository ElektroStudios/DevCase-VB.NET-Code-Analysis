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

#Region " AsyncSubCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.AsyncSubNotEventHandler"/>: 
    ''' converts <c>Async Sub X()</c> into <c>Async Function X() As Task</c> 
    ''' (<c>End Sub</c> → <c>End Function</c>, <c>Exit Sub</c> → <c>Exit Function</c>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Call sites are not modified: callers should <see langword="Await"/> the returned task, and a method referenced with 
    ''' <see langword="AddressOf"/> as an <c>Action</c> delegate must be adapted by hand.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(AsyncSubCodeFixProvider)), [Shared]>
    Public NotInheritable Class AsyncSubCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Convert to 'Async Function ... As Task'"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.AsyncSubNotEventHandler)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AsyncSubCodeFixProvider"/> class.
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
                    CodeAction.Create(AsyncSubCodeFixProvider.Title,
                                      Function(ct As CancellationToken) AsyncSubCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(AsyncSubCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the method whose identifier is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the method identifier).
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
        ''' Thrown when the diagnostic does not point to an <see langword="Async"/> <see langword="Sub"/> block.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim statement As MethodStatementSyntax = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MethodStatementSyntax)()
            Dim block As MethodBlockSyntax = If(statement Is Nothing, Nothing, TryCast(statement.Parent, MethodBlockSyntax))
            If block Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to an Async Sub block.")
            End If

            ' "Task" when System.Threading.Tasks is imported (the default in Visual Basic projects), the full name otherwise.
            Dim taskType As INamedTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task")
            Dim visibleTask As Boolean = semanticModel.LookupNamespacesAndTypes(statement.SpanStart, name:="Task").
                Any(Function(s As ISymbol) SymbolEqualityComparer.Default.Equals(s, taskType))
            Dim taskName As String = If(visibleTask, "Task", "System.Threading.Tasks.Task")

            Dim changes As New List(Of TextChange) From {
                New TextChange(statement.SubOrFunctionKeyword.Span, "Function"),
                New TextChange(block.EndBlockStatement.BlockKeyword.Span, "Function")}

            ' " As Task" after the parameter list (or after the name, adding "()", when there is no parameter list).
            If statement.ParameterList IsNot Nothing Then
                changes.Add(New TextChange(New TextSpan(statement.ParameterList.Span.End, 0), $" As {taskName}"))
            Else
                Dim afterName As SyntaxNodeOrToken = If(statement.TypeParameterList IsNot Nothing, CType(statement.TypeParameterList, SyntaxNodeOrToken), statement.Identifier)
                changes.Add(New TextChange(New TextSpan(afterName.Span.End, 0), $"() As {taskName}"))
            End If

            ' "Exit Sub" of this method (not of nested Sub lambdas) becomes "Exit Function".
            For Each exitStatement As ExitStatementSyntax In block.DescendantNodes().OfType(Of ExitStatementSyntax)()
                If exitStatement.IsKind(SyntaxKind.ExitSubStatement) AndAlso
                   Not exitStatement.Ancestors().TakeWhile(Function(a As SyntaxNode) a IsNot block).Any(Function(a As SyntaxNode) TypeOf a Is LambdaExpressionSyntax) Then
                    changes.Add(New TextChange(exitStatement.BlockKeyword.Span, "Function"))
                End If
            Next exitStatement

            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
