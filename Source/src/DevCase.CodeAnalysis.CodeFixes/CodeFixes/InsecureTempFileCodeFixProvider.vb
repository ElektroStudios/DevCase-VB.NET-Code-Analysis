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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " InsecureTempFileCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.InsecureTempFile"/>: replaces <c>Path.GetTempFileName()</c> with a path 
    ''' built from <c>Path.GetRandomFileName()</c>, and creates the file atomically with <c>FileMode.CreateNew</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The result keeps the behavior of <c>GetTempFileName</c>: the path points to a new, empty file in the temporary folder, 
    ''' with the <c>.tmp</c> extension. For example:
    ''' <code>
    ''' Dim tempPath As String = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), "tmp"))
    ''' Using New FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None) ' Creates the file atomically...
    ''' End Using
    ''' </code>
    ''' The fix is offered when the call initializes a local variable, is assigned to a variable, field or property, 
    ''' or is returned (a new local variable is declared). Type names are written in their shortest valid form.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(InsecureTempFileCodeFixProvider)), [Shared]>
    Public NotInheritable Class InsecureTempFileCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The base name of the local variable declared when the call is returned.
        ''' </summary>
        Friend Const ReturnVariableName As String = "tempFilePath"

        ''' <summary>
        ''' The comment written after the <see langword="Using"/> statement that creates the file.
        ''' </summary>
        Friend Const ExplanatoryComment As String = "' Creates the file atomically: fails instead of opening a file that already exists."
#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.InsecureTempFile)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="InsecureTempFileCodeFixProvider"/> class.
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
                Dim usage As String = Nothing
                If Not diagnostic.Properties.TryGetValue(InsecureTempFileAnalyzer.UsagePropertyKey, usage) OrElse String.IsNullOrEmpty(usage) Then
                    Continue For
                End If

                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Create the file with a random name and FileMode.CreateNew",
                                      Function(ct As CancellationToken) InsecureTempFileCodeFixProvider.FixAsync(document, diagnostic, usage, ct),
                                      equivalenceKey:=NameOf(InsecureTempFileCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the call reported by the specified diagnostic.
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
        ''' <param name="usage">
        ''' How the returned path is used (see <see cref="InsecureTempFileAnalyzer.UsagePropertyKey"/>).
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
        ''' Thrown when the diagnostic no longer points to a supported usage, or the <c>System.IO</c> types cannot be resolved.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, usage As String,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim model As SemanticModel = Await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim invocation As InvocationExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of InvocationExpressionSyntax)(root, diagnostic.Location.SourceSpan)

            Dim statement As StatementSyntax
            Dim variable As String
            Select Case usage
                Case InsecureTempFileAnalyzer.DeclarationUsage
                    Dim declarator As VariableDeclaratorSyntax = DirectCast(invocation.Parent.Parent, VariableDeclaratorSyntax)
                    statement = DirectCast(declarator.Parent, StatementSyntax)
                    variable = declarator.Names(0).Identifier.ToString()
                Case InsecureTempFileAnalyzer.AssignmentUsage
                    Dim assignment As AssignmentStatementSyntax = DirectCast(invocation.Parent, AssignmentStatementSyntax)
                    statement = assignment
                    variable = assignment.Left.ToString()
                Case InsecureTempFileAnalyzer.ReturnUsage
                    statement = DirectCast(invocation.Parent, StatementSyntax)
                    variable = InsecureTempFileCodeFixProvider.GetUniqueName(model, statement.SpanStart)
                Case Else
                    Throw New InvalidOperationException($"Unsupported usage '{usage}'.")
            End Select

            ' Type names are resolved after the statement, where a declared variable named "path" would hide the Path type.
            Dim position As Integer = statement.Span.End
            Dim names As Dictionary(Of String, String) = InsecureTempFileCodeFixProvider.GetTypeNames(model, position)
            Dim pathName As String = names("System.IO.Path")
            Dim newPath As String = $"{pathName}.Combine({pathName}.GetTempPath(), {pathName}.ChangeExtension({pathName}.GetRandomFileName(), ""tmp""))"

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(statement.GetFirstToken())
            Dim arguments As String = $"{variable}, {names("System.IO.FileMode")}.CreateNew, {names("System.IO.FileAccess")}.Write, {names("System.IO.FileShare")}.None"
            Dim usingStatement As String = $"{indentation}Using New {names("System.IO.FileStream")}({arguments}) {InsecureTempFileCodeFixProvider.ExplanatoryComment}"
            Dim usingBlock As String = $"{usingStatement}{eol}{indentation}End Using"

            Dim changes As New List(Of TextChange)()
            If usage.Equals(InsecureTempFileAnalyzer.ReturnUsage, StringComparison.Ordinal) Then
                Dim replacement As String = $"Dim {variable} As String = {newPath}{eol}{usingBlock}{eol}{indentation}Return {variable}"
                changes.Add(New TextChange(statement.Span, replacement))
            Else
                Dim line As TextLine = text.Lines.GetLineFromPosition(statement.Span.End)
                Dim lineBreak As String = If(line.EndIncludingLineBreak = line.End, eol, String.Empty)
                changes.Add(New TextChange(invocation.Span, newPath))
                changes.Add(New TextChange(New TextSpan(line.EndIncludingLineBreak, 0), $"{lineBreak}{usingBlock}{eol}"))
            End If
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Gets the shortest valid names of the <c>System.IO</c> types used by the fix, at the specified position.
        ''' </summary>
        ''' 
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        ''' 
        ''' <param name="position">
        ''' The position where the names are written.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The names, keyed by metadata name.
        ''' </returns>
        ''' 
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when a type cannot be resolved.
        ''' </exception>
        Private Shared Function GetTypeNames(model As SemanticModel, position As Integer) As Dictionary(Of String, String)

            Dim names As New Dictionary(Of String, String)(StringComparer.Ordinal)
            For Each metadataName As String In {"System.IO.Path", "System.IO.FileStream", "System.IO.FileMode", "System.IO.FileAccess", "System.IO.FileShare"}
                Dim type As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(metadataName)
                If type Is Nothing Then
                    Throw New InvalidOperationException($"The {metadataName} type cannot be resolved.")
                End If
                names.Add(metadataName, type.ToMinimalDisplayString(model, position))
            Next metadataName
            Return names
        End Function

        ''' <summary>
        ''' Gets a local variable name, based on <see cref="ReturnVariableName"/>, that does not conflict with any symbol in scope.
        ''' </summary>
        ''' 
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        ''' 
        ''' <param name="position">
        ''' The position where the variable is declared.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The variable name.
        ''' </returns>
        Private Shared Function GetUniqueName(model As SemanticModel, position As Integer) As String

            Dim name As String = InsecureTempFileCodeFixProvider.ReturnVariableName
            Dim suffix As Integer = 1
            While model.LookupSymbols(position, name:=name).Any()
                suffix += 1
                name = $"{InsecureTempFileCodeFixProvider.ReturnVariableName}{suffix}"
            End While
            Return name
        End Function

#End Region

    End Class

End Namespace

#End Region
