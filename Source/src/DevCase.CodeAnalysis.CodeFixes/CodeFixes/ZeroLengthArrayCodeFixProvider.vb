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
Imports Microsoft.CodeAnalysis.Simplification
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " ZeroLengthArrayCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.ZeroLengthArrayAllocation"/>: replaces the zero-length array 
    ''' with <see cref="Array.Empty(Of T)"/>, for example <c>New String() {}</c> → <c>Array.Empty(Of String)()</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ZeroLengthArrayCodeFixProvider)), [Shared]>
    Public NotInheritable Class ZeroLengthArrayCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.ZeroLengthArrayAllocation)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ZeroLengthArrayCodeFixProvider"/> class.
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
                Dim typeName As String = Nothing
                If Not diagnostic.Properties.TryGetValue(ZeroLengthArrayAnalyzer.ElementTypePropertyKey, typeName) OrElse String.IsNullOrEmpty(typeName) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create($"Use 'Array.Empty(Of {typeName})()'",
                                      Function(ct As CancellationToken) ZeroLengthArrayCodeFixProvider.FixAsync(document, diagnostic, typeName, ct),
                                      equivalenceKey:=NameOf(ZeroLengthArrayCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the array creation reported by the specified diagnostic with <see cref="Array.Empty(Of T)"/>.
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
        ''' <param name="typeName">
        ''' The element type of the array, as Visual Basic code.
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
        ''' Thrown when the diagnostic no longer points to an array creation.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, typeName As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim node As SyntaxNode = root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie:=True)
            If Not (TypeOf node Is ArrayCreationExpressionSyntax OrElse TypeOf node Is CollectionInitializerSyntax) Then
                Throw New InvalidOperationException("The diagnostic does not point to an array creation.")
            End If

            ' Global.System.Array, simplified to Array unless a type named Array is in scope. Only the type name is annotated: 
            ' the simplification of the whole invocation runs the extension method reducer, which fails on Array.Empty.
            Dim replacement As InvocationExpressionSyntax = DirectCast(SyntaxFactory.ParseExpression($"Global.System.Array.Empty(Of {typeName})()"), InvocationExpressionSyntax)
            Dim typeAccess As ExpressionSyntax = DirectCast(replacement.Expression, MemberAccessExpressionSyntax).Expression
            replacement = replacement.ReplaceNode(typeAccess, typeAccess.WithAdditionalAnnotations(Simplifier.Annotation)).WithTriviaFrom(node)
            Dim newDocument As Document = document.WithSyntaxRoot(root.ReplaceNode(node, replacement))
            Return Await Simplifier.ReduceAsync(newDocument, Simplifier.Annotation, Nothing, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
