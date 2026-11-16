#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports Microsoft.CodeAnalysis.Rename

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " FlagsEnumNoneCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.FlagsEnumWithoutNone"/>: adds a <c>None = 0</c> member, 
    ''' or renames the existing zero member to <c>None</c> in the whole solution.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The new member is inserted first. When the first existing member has a documentation comment, the new member gets one too 
    ''' (<c>No flags are set.</c>), and the blank-line layout of the members is preserved.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(FlagsEnumNoneCodeFixProvider)), [Shared]>
    Public NotInheritable Class FlagsEnumNoneCodeFixProvider : Inherits CodeFixProvider

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.FlagsEnumWithoutNone)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FlagsEnumNoneCodeFixProvider"/> class.
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
                Dim kind As String = diagnostic.Properties(FlagsEnumNoneAnalyzer.KindPropertyKey)
                If kind.Equals(FlagsEnumNoneAnalyzer.AddKind, StringComparison.Ordinal) Then
                    context.RegisterCodeFix(
                        CodeAction.Create("Add 'None = 0'",
                                          Function(ct As CancellationToken) FlagsEnumNoneCodeFixProvider.AddNoneAsync(document, diagnostic, ct),
                                          equivalenceKey:=$"{NameOf(FlagsEnumNoneCodeFixProvider)}.Add"),
                        diagnostic)
                ElseIf kind.Equals(FlagsEnumNoneAnalyzer.RenameKind, StringComparison.Ordinal) Then
                    context.RegisterCodeFix(
                        CodeAction.Create("Rename the zero member to 'None'",
                                          Function(ct As CancellationToken) FlagsEnumNoneCodeFixProvider.RenameAsync(document, diagnostic, ct),
                                          equivalenceKey:=$"{NameOf(FlagsEnumNoneCodeFixProvider)}.Rename"),
                        diagnostic)
                End If
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Adds a <c>None = 0</c> member as the first member of the reported enumeration.
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
        ''' Thrown when the diagnostic no longer points to an enumeration.
        ''' </exception>
        Private Shared Async Function AddNoneAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim block As EnumBlockSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType(Of EnumBlockSyntax)().FirstOrDefault()
            If block Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to an enumeration.")
            End If

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim memberName As String = FlagsEnumNoneAnalyzer.NoneName
            Dim insertion As String
            Dim position As Integer

            If block.Members.Count = 0 Then
                Dim indentation As String = $"{UtilAnalyzerSyntax.GetLineIndentation(block.EnumStatement.GetFirstToken())}    "
                position = text.Lines.GetLineFromPosition(block.EndEnumStatement.SpanStart).Start
                insertion = $"{indentation}{memberName} = 0{eol}"
            Else
                Dim first As StatementSyntax = block.Members(0)
                Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(first.GetFirstToken())
                Dim firstContent As SyntaxTrivia = first.GetLeadingTrivia().FirstOrDefault(
                    Function(t As SyntaxTrivia) Not t.IsKind(SyntaxKind.WhitespaceTrivia) AndAlso Not t.IsKind(SyntaxKind.EndOfLineTrivia))
                position = text.Lines.GetLineFromPosition(If(firstContent.IsKind(SyntaxKind.None), first.SpanStart, firstContent.SpanStart)).Start

                Dim builder As New System.Text.StringBuilder()
                If UtilAnalyzerSyntax.HasDocumentationComment(first) Then
                    Dim prefix As String = $"{indentation}{UtilAnalyzerSyntax.DocumentationCommentPrefix}"
                    builder.Append($"{prefix} <summary>{eol}{prefix} No flags are set.{eol}{prefix} </summary>{eol}")
                End If
                builder.Append($"{indentation}{memberName} = 0{eol}")

                Dim separatedByBlankLines As Boolean = block.Members.Count > 1 AndAlso
                                                       block.Members(1).GetLeadingTrivia().Any(Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.EndOfLineTrivia))
                If separatedByBlankLines Then
                    builder.Append(eol)
                End If
                insertion = builder.ToString()
            End If

            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), insertion)}, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Renames the reported zero member to <c>None</c> in the whole solution.
        ''' </summary>
        ''' 
        ''' <param name="document">
        ''' The document that contains the member.
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
        ''' The changed solution.
        ''' </returns>
        ''' 
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the diagnostic no longer points to an enumeration member.
        ''' </exception>
        Private Shared Async Function RenameAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Solution)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim member As EnumMemberDeclarationSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().
                                                             OfType(Of EnumMemberDeclarationSyntax)().FirstOrDefault()
            Dim symbol As ISymbol = If(member Is Nothing, Nothing, model.GetDeclaredSymbol(member, cancellationToken))
            If symbol Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to an enumeration member.")
            End If

            Return Await Renamer.RenameSymbolAsync(document.Project.Solution, symbol, New SymbolRenameOptions(), FlagsEnumNoneAnalyzer.NoneName,
                                                   cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
