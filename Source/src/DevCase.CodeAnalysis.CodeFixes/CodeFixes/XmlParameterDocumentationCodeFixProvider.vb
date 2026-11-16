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

#Region " XmlParameterDocumentationCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.XmlParameterMismatch"/>: 
    ''' injects the missing <c>&lt;param name="..."&gt;</c> tags (each one placed according to the parameter order) 
    ''' and fills the empty ones, using a default description derived from the parameter name and type.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Unknown or duplicated tags require a human decision and are only reported, never modified.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(XmlParameterDocumentationCodeFixProvider)), [Shared]>
    Public NotInheritable Class XmlParameterDocumentationCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Add missing <param> descriptions"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.XmlParameterMismatch)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="XmlParameterDocumentationCodeFixProvider"/> class.
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
                Dim missing As String = Nothing
                Dim empty As String = Nothing
                diagnostic.Properties.TryGetValue(XmlParameterDocumentationAnalyzer.MissingParametersPropertyKey, missing)
                diagnostic.Properties.TryGetValue(XmlParameterDocumentationAnalyzer.EmptyParametersPropertyKey, empty)
                If String.IsNullOrEmpty(missing) AndAlso String.IsNullOrEmpty(empty) Then
                    Continue For ' Only unknown/duplicated tags: nothing can be generated automatically.
                End If

                Dim document As Document = context.Document
                Dim span As TextSpan = diagnostic.Location.SourceSpan
                context.RegisterCodeFix(
                    CodeAction.Create(XmlParameterDocumentationCodeFixProvider.Title,
                                      Function(ct As CancellationToken) XmlParameterDocumentationCodeFixProvider.FixAsync(document, span, ct),
                                      equivalenceKey:=NameOf(XmlParameterDocumentationCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
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

            Dim declaration As MethodBaseSyntax = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MethodBaseSyntax)()
            If declaration Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a method-like declaration.")
            End If

            Dim docComment As DocumentationCommentTriviaSyntax = UtilAnalyzerSyntax.GetDocumentationComment(declaration)
            If docComment Is Nothing Then
                Throw New InvalidOperationException("The declaration has no structured XML documentation comment.")
            End If

            ' Map each documented parameter name to its first <param> element.
            Dim elementsByName As New Dictionary(Of String, XmlNodeSyntax)(StringComparer.OrdinalIgnoreCase)
            For Each element As XmlNodeSyntax In XmlParameterDocumentationAnalyzer.GetParamElements(docComment)
                Dim name As String = UtilAnalyzerSyntax.GetXmlNameAttributeValue(element)
                If Not String.IsNullOrEmpty(name) AndAlso Not elementsByName.ContainsKey(name) Then
                    elementsByName.Add(name, element)
                End If
            Next element

            Dim summaryElement As XmlNodeSyntax = docComment.Content.FirstOrDefault(
                Function(n As XmlNodeSyntax) "summary".Equals(UtilAnalyzerSyntax.GetXmlElementName(n), StringComparison.Ordinal))

            Dim parameterNames As List(Of String) = XmlParameterDocumentationAnalyzer.GetParameterNames(declaration.ParameterList)
            Dim descriptions As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            If declaration.ParameterList IsNot Nothing Then
                For Each parameter As ParameterSyntax In declaration.ParameterList.Parameters
                    descriptions(parameter.Identifier.Identifier.ValueText) = UtilCodeFix.GetParameterDescription(parameter, semanticModel, cancellationToken)
                Next parameter
            End If
            Dim indentation As String = UtilAnalyzerSyntax.GetLineIndentation(declaration.GetFirstToken())
            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim separator As String = UtilCodeFix.BuildDocumentationSeparator(indentation, eol)

            ' Insertions grouped by position, preserving the parameter order.
            Dim insertions As New SortedDictionary(Of Integer, StringBuilder)()

            For index As Integer = 0 To parameterNames.Count - 1
                Dim name As String = parameterNames(index)
                If elementsByName.ContainsKey(name) Then
                    Continue For
                End If

                Dim section As String = UtilCodeFix.BuildDocumentationSection(indentation, $"<param name=""{name}"">", "</param>", descriptions(name), eol)
                Dim position As Integer
                Dim block As String

                Dim previousElement As XmlNodeSyntax = Enumerable.Range(0, index).Reverse().
                    Select(Function(i As Integer) If(elementsByName.ContainsKey(parameterNames(i)), elementsByName(parameterNames(i)), Nothing)).
                    FirstOrDefault(Function(e As XmlNodeSyntax) e IsNot Nothing)

                Dim nextElement As XmlNodeSyntax = Enumerable.Range(index + 1, parameterNames.Count - index - 1).
                    Select(Function(i As Integer) If(elementsByName.ContainsKey(parameterNames(i)), elementsByName(parameterNames(i)), Nothing)).
                    FirstOrDefault(Function(e As XmlNodeSyntax) e IsNot Nothing)

                If previousElement IsNot Nothing Then
                    position = text.Lines.GetLineFromPosition(previousElement.Span.End).EndIncludingLineBreak
                    block = $"{separator}{section}"
                ElseIf nextElement IsNot Nothing Then
                    position = text.Lines.GetLineFromPosition(nextElement.SpanStart).Start
                    block = $"{section}{separator}"
                ElseIf summaryElement IsNot Nothing Then
                    position = text.Lines.GetLineFromPosition(summaryElement.Span.End).EndIncludingLineBreak
                    block = $"{separator}{section}"
                Else
                    position = text.Lines.GetLineFromPosition(declaration.GetFirstToken().SpanStart).Start
                    block = $"{separator}{section}"
                End If

                Dim builder As StringBuilder = Nothing
                If Not insertions.TryGetValue(position, builder) Then
                    builder = New StringBuilder()
                    insertions.Add(position, builder)
                End If
                builder.Append(block)
            Next index

            Dim changes As List(Of TextChange) = insertions.Select(
                Function(pair As KeyValuePair(Of Integer, StringBuilder)) New TextChange(New TextSpan(pair.Key, 0), pair.Value.ToString())).ToList()

            ' Empty tags are rewritten in place with the default description.
            Dim prefix As String = $"{indentation}{UtilAnalyzerSyntax.DocumentationCommentPrefix} "
            For Each pair As KeyValuePair(Of String, XmlNodeSyntax) In elementsByName
                If Not descriptions.ContainsKey(pair.Key) OrElse Not UtilAnalyzerSyntax.IsXmlElementEmpty(pair.Value) Then
                    Continue For
                End If
                Dim replacement As String = $"<param name=""{pair.Key}"">{eol}{prefix}{descriptions(pair.Key)}{eol}{prefix}</param>"
                changes.Add(New TextChange(pair.Value.Span, replacement))
            Next pair

            Return Await UtilCodeFix.ApplyTextChangesAsync(document, changes, cancellationToken).ConfigureAwait(False)
        End Function

#End Region

    End Class

End Namespace

#End Region
