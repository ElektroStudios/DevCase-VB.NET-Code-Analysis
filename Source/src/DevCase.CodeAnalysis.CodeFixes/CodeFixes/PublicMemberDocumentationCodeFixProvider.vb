#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

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

#Region " PublicMemberDocumentationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MissingXmlDocumentation"/>: 
    ''' generates a <c>&lt;summary&gt;</c> block (empty, or with the conventional text for constructors), 
    ''' <c>&lt;param&gt;</c> sections with a default description derived from each parameter name and type, 
    ''' and an empty <c>&lt;returns&gt;</c> section when the member returns a value.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(PublicMemberDocumentationCodeFixProvider)), [Shared]>
    Public NotInheritable Class PublicMemberDocumentationCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add XML documentation comment"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingXmlDocumentation)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="PublicMemberDocumentationCodeFixProvider"/> class.
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
                    CodeAction.Create(PublicMemberDocumentationCodeFixProvider.Title,
                                      Function(ct As CancellationToken) PublicMemberDocumentationCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(PublicMemberDocumentationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Builds the XML documentation comment text for a declaration.
        ''' </summary>
        '''
        ''' <param name="declaration">
        ''' The declaration statement.
        ''' </param>
        '''
        ''' <param name="indentation">
        ''' The indentation of the declaration.
        ''' </param>
        '''
        ''' <param name="endOfLine">
        ''' The end-of-line sequence.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model, used to resolve the constructor type and the parameter types.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The documentation comment text, terminated by <paramref name="endOfLine"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="declaration"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function BuildDocumentation(declaration As SyntaxNode, indentation As String, endOfLine As String,
                                                  semanticModel As SemanticModel, cancellationToken As CancellationToken) As String

            If declaration Is Nothing Then
                Throw New ArgumentNullException(NameOf(declaration))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            ' Constructors get the conventional summary text; other members get an empty summary to be written by hand.
            Dim summaryText As String = String.Empty
            If declaration.IsKind(SyntaxKind.SubNewStatement) Then
                Dim constructor As IMethodSymbol = TryCast(semanticModel.GetDeclaredSymbol(declaration, cancellationToken), IMethodSymbol)
                If constructor IsNot Nothing AndAlso Not constructor.IsStatic Then
                    summaryText = UtilDocumentationText.GetConstructorSummary(constructor.ContainingType,
                                                                             isPrivate:=constructor.DeclaredAccessibility = Accessibility.Private)
                End If
            End If

            Dim builder As New StringBuilder()
            builder.Append(UtilCodeFix.BuildDocumentationSection(indentation, "<summary>", "</summary>", summaryText, endOfLine))

            Dim parameterList As ParameterListSyntax = UtilAnalyzerSyntax.GetParameterList(declaration)
            If parameterList IsNot Nothing Then
                For Each parameter As ParameterSyntax In parameterList.Parameters
                    Dim parameterName As String = parameter.Identifier.Identifier.ValueText
                    builder.Append(UtilCodeFix.BuildDocumentationSeparator(indentation, endOfLine))
                    builder.Append(UtilCodeFix.BuildDocumentationSection(indentation, $"<param name=""{parameterName}"">", "</param>",
                                                                         UtilCodeFix.GetParameterDescription(parameter, semanticModel, cancellationToken),
                                                                         endOfLine))
                Next parameter
            End If

            Select Case declaration.Kind()
                Case SyntaxKind.FunctionStatement, SyntaxKind.DelegateFunctionStatement, SyntaxKind.DeclareFunctionStatement, SyntaxKind.OperatorStatement
                    builder.Append(UtilCodeFix.BuildDocumentationSeparator(indentation, endOfLine))
                    builder.Append(UtilCodeFix.BuildDocumentationSection(indentation, "<returns>", "</returns>", String.Empty, endOfLine))
            End Select

            Return builder.ToString()
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the declaration whose identifier is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the declaration identifier).
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
        Private Shared Async Function FixAsync(document As Document, span As TextSpan, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)

            Dim declaration As SyntaxNode = root.FindToken(span.Start).Parent.AncestorsAndSelf().
                FirstOrDefault(Function(n As SyntaxNode) PublicMemberDocumentationAnalyzer.AnalyzedKinds.Contains(n.Kind()))
            If declaration Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a supported declaration.")
            End If

            If UtilAnalyzerSyntax.HasDocumentationComment(declaration) Then
                Return document ' Already documented (e.g. by a previous Fix All pass).
            End If

            Dim firstToken As SyntaxToken = declaration.GetFirstToken()
            Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(firstToken)
            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim documentation As String = PublicMemberDocumentationCodeFixProvider.BuildDocumentation(declaration, indentation, eol, semanticModel, cancellationToken)

            Dim insertionPosition As Integer = text.Lines.GetLineFromPosition(firstToken.SpanStart).Start
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(insertionPosition, 0), documentation)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
