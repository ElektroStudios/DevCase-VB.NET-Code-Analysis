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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " ComImportClassCodeCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.ComImportClassCode"/>: removes a constructor of a
    ''' <c>&lt;ComImport&gt;</c> class when removing it changes nothing.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Offered only for <c>Shared Sub New</c>, and for <c>Public Sub New()</c> without parameters in a class that is not
    ''' <see langword="MustInherit"/>: the compiler then declares the same public constructor without a body. Other members
    ''' (methods, properties, constructors with parameters or another accessibility) are part of the public surface of the class:
    ''' removing them would break the code that uses them, so they are only reported.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ComImportClassCodeCodeFixProvider)), [Shared]>
    Public NotInheritable Class ComImportClassCodeCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.ComImportClassCode)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ComImportClassCodeCodeFixProvider"/> class.
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
            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim constructor As ConstructorBlockSyntax = ComImportClassCodeCodeFixProvider.GetRemovableConstructor(root, diagnostic)
                If constructor Is Nothing Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Remove the constructor (its code never runs)",
                                      Function(ct As CancellationToken) ComImportClassCodeCodeFixProvider.RemoveAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(ComImportClassCodeCodeFixProvider)),
                    diagnostic)
            Next diagnostic
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the constructor reported by a diagnostic, when it can be removed without changing the class.
        ''' </summary>
        '''
        ''' <param name="root">
        ''' The syntax root.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <returns>
        ''' A <c>Shared Sub New</c>, or a <c>Public Sub New()</c> without parameters of a class that is not <see langword="MustInherit"/>;
        ''' otherwise, <see langword="Nothing"/>.
        ''' </returns>
        Private Shared Function GetRemovableConstructor(root As SyntaxNode, diagnostic As Diagnostic) As ConstructorBlockSyntax

            Dim constructor As ConstructorBlockSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent?.FirstAncestorOrSelf(Of ConstructorBlockSyntax)()
            If constructor Is Nothing OrElse constructor.SubNewStatement.NewKeyword.SpanStart <> diagnostic.Location.SourceSpan.Start Then
                Return Nothing
            End If

            Dim statement As SubNewStatementSyntax = constructor.SubNewStatement
            If statement.Modifiers.Any(SyntaxKind.SharedKeyword) Then
                Return constructor
            End If

            Dim owner As ClassBlockSyntax = TryCast(constructor.Parent, ClassBlockSyntax)
            Dim isPublicDefault As Boolean = statement.Modifiers.Any(SyntaxKind.PublicKeyword) AndAlso
                                             (statement.ParameterList Is Nothing OrElse statement.ParameterList.Parameters.Count = 0) AndAlso
                                             owner IsNot Nothing AndAlso Not owner.ClassStatement.Modifiers.Any(SyntaxKind.MustInheritKeyword)
            Return If(isPublicDefault, constructor, Nothing)
        End Function

        ''' <summary>
        ''' Removes the constructor reported by the specified diagnostic, with its comments and XML documentation, 
        ''' but not the preprocessor directives around it (<c>#Region</c>, <c>#End Region</c>, <c>#If</c>...).
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
        ''' Thrown when the diagnostic no longer points to a removable constructor.
        ''' </exception>
        Private Shared Async Function RemoveAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim constructor As ConstructorBlockSyntax = ComImportClassCodeCodeFixProvider.GetRemovableConstructor(root, diagnostic)
            If constructor Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a removable constructor.")
            End If
            ' The #Region / #End Region directives around the constructor stay (an empty region is kept, never broken).
            Return document.WithSyntaxRoot(UtilCodeFix.RemoveNodeKeepingDirectives(root, constructor))
        End Function

#End Region

    End Class

End Namespace

#End Region
