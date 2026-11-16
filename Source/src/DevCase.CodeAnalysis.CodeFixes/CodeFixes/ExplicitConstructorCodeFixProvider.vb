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

#Region " ExplicitConstructorCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.MissingExplicitConstructor"/>: 
    ''' injects a <c>#Region " Constructors "</c> block containing a documented, empty constructor.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The accessibility of the generated constructor preserves the intent of the class:
    ''' <list type="bullet">
    '''   <item><description><see langword="MustInherit"/> classes get a <see langword="Protected"/> constructor.</description></item>
    '''   <item><description>Classes whose members are all <see langword="Shared"/> (utility classes) get a <see langword="Private"/> 
    '''   constructor that prevents instantiation, as in the original <c>UtilRoslynRewriter</c>.</description></item>
    '''   <item><description>Any other class gets a <see langword="Public"/> constructor (identical to the implicit one).</description></item>
    ''' </list>
    ''' The region is placed after the fields, properties and events, and before the first method; 
    ''' when the anchor member is preceded by an <c>#End Region</c>, the new region is placed after it.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ExplicitConstructorCodeFixProvider)), [Shared]>
    Public NotInheritable Class ExplicitConstructorCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add explicit constructor"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.MissingExplicitConstructor)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ExplicitConstructorCodeFixProvider"/> class.
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
                    CodeAction.Create(ExplicitConstructorCodeFixProvider.Title,
                                      Function(ct As CancellationToken) ExplicitConstructorCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(ExplicitConstructorCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines the accessibility keyword of the generated constructor.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The class symbol.
        ''' </param>
        '''
        ''' <returns>
        ''' <c>"Protected"</c>, <c>"Private"</c> or <c>"Public"</c>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetConstructorAccessibility(type As INamedTypeSymbol) As String

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            If type.IsAbstract Then
                Return "Protected"
            End If

            Dim explicitMembers As ImmutableArray(Of ISymbol) = type.GetMembers().
                Where(Function(m As ISymbol) Not m.IsImplicitlyDeclared AndAlso
                                             Not (TypeOf m Is IMethodSymbol AndAlso DirectCast(m, IMethodSymbol).MethodKind <> MethodKind.Ordinary AndAlso
                                                  DirectCast(m, IMethodSymbol).MethodKind <> MethodKind.UserDefinedOperator AndAlso
                                                  DirectCast(m, IMethodSymbol).MethodKind <> MethodKind.Conversion)).
                ToImmutableArray()

            Dim hasInstanceMembers As Boolean = explicitMembers.Any(Function(m As ISymbol) Not m.IsStatic AndAlso TypeOf m IsNot INamedTypeSymbol)
            Dim hasSharedMembers As Boolean = explicitMembers.Any(Function(m As ISymbol) m.IsStatic)

            Return If(type.IsSealed AndAlso hasSharedMembers AndAlso Not hasInstanceMembers, "Private", "Public")
        End Function

        ''' <summary>
        ''' Gets the member before which the constructor region must be inserted.
        ''' </summary>
        '''
        ''' <param name="classBlock">
        ''' The class block.
        ''' </param>
        '''
        ''' <returns>
        ''' The first member that is not a field, property or event; or the <c>End Class</c> statement.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="classBlock"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetAnchorNode(classBlock As ClassBlockSyntax) As SyntaxNode

            If classBlock Is Nothing Then
                Throw New ArgumentNullException(NameOf(classBlock))
            End If

            For Each member As StatementSyntax In classBlock.Members
                Select Case member.Kind()
                    Case SyntaxKind.FieldDeclaration, SyntaxKind.PropertyStatement, SyntaxKind.PropertyBlock,
                         SyntaxKind.EventStatement, SyntaxKind.EventBlock
                        Continue For
                    Case Else
                        Return member
                End Select
            Next member
            Return classBlock.EndClassStatement
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the class whose identifier is located at the specified span.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="span">
        ''' The diagnostic span (the class identifier).
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
            Dim semanticModel As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)

            Dim classBlock As ClassBlockSyntax = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of ClassBlockSyntax)()
            If classBlock Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a class declaration.")
            End If

            Dim type As INamedTypeSymbol = semanticModel.GetDeclaredSymbol(classBlock, cancellationToken)
            If type Is Nothing Then
                Throw New InvalidOperationException("The class symbol cannot be resolved.")
            End If

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim accessibility As String = ExplicitConstructorCodeFixProvider.GetConstructorAccessibility(type)

            ' Indentation: the one of the existing members, or the class indentation plus four spaces.
            Dim indentation As String = If(classBlock.Members.Count > 0,
                                           UtilAnalyzerSyntax.GetLineIndentation(classBlock.Members(0).GetFirstToken()),
                                           $"{UtilAnalyzerSyntax.GetLineIndentation(classBlock.ClassStatement.GetFirstToken())}    ")

            Dim summaryText As String = UtilDocumentationText.GetConstructorSummary(type, isPrivate:=accessibility.Equals("Private", StringComparison.Ordinal))

            Dim regionBuilder As New StringBuilder()
            regionBuilder.Append("#Region "" Constructors """).Append(eol).Append(eol)
            regionBuilder.Append(UtilCodeFix.BuildDocumentationSection(indentation, "<summary>", "</summary>", summaryText, eol))
            regionBuilder.Append(indentation).Append(accessibility).Append(" Sub New()").Append(eol)
            regionBuilder.Append(indentation).Append("End Sub").Append(eol)
            regionBuilder.Append(eol)
            regionBuilder.Append("#End Region").Append(eol)

            ' Insertion point: right after the last "#End Region" that precedes the anchor, otherwise before the anchor trivia.
            Dim anchor As SyntaxNode = ExplicitConstructorCodeFixProvider.GetAnchorNode(classBlock)
            Dim lastEndRegion As SyntaxTrivia = anchor.GetLeadingTrivia().LastOrDefault(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.EndRegionDirectiveTrivia))

            Dim insertionPosition As Integer = If(lastEndRegion.IsKind(SyntaxKind.EndRegionDirectiveTrivia),
                                                  text.Lines.GetLineFromPosition(lastEndRegion.SpanStart).EndIncludingLineBreak,
                                                  anchor.FullSpan.Start)

            ' Keep a blank line between the new region and the code that follows it.
            If insertionPosition < text.Length AndAlso
               Not String.IsNullOrWhiteSpace(text.ToString(text.Lines.GetLineFromPosition(insertionPosition).Span)) Then
                regionBuilder.Append(eol)
            End If

            Dim insertedText As String = $"{eol}{regionBuilder}"
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(insertionPosition, 0), insertedText)}, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
