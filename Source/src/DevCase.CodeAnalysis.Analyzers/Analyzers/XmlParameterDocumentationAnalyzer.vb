#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " XmlParameterDocumentationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports XML documentation comments whose <c>&lt;param&gt;</c> tags do not exactly match the parameter list 
    ''' of a method, constructor, operator, delegate, event or indexed property (rule <see cref="DiagnosticIds.XmlParameterMismatch"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Reported issues: undocumented parameters, empty <c>&lt;param&gt;</c> tags, <c>&lt;param&gt;</c> tags that refer to 
    ''' a non-existent parameter, and duplicated <c>&lt;param&gt;</c> tags. 
    ''' Comments delegating through <c>&lt;inheritdoc/&gt;</c> or <c>&lt;include/&gt;</c> are skipped.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class XmlParameterDocumentationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property key that stores the comma-separated names of the undocumented parameters.
        ''' </summary>
        Friend Const MissingParametersPropertyKey As String = "MissingParameters"

        ''' <summary>
        ''' The diagnostic property key that stores the comma-separated names of the parameters documented by an empty tag.
        ''' </summary>
        Friend Const EmptyParametersPropertyKey As String = "EmptyParameters"

        ''' <summary>
        ''' The local name of the <c>&lt;param&gt;</c> XML element.
        ''' </summary>
        Friend Const ParamElementName As String = "param"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.XmlParameterMismatch)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="XmlParameterDocumentationAnalyzer"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Sub Initialize(context As AnalysisContext)

            If context Is Nothing Then
                Throw New ArgumentNullException(NameOf(context))
            End If

            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None)
            context.EnableConcurrentExecution()
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf XmlParameterDocumentationAnalyzer.AnalyzeDeclaration),
                                             SyntaxKind.SubStatement, SyntaxKind.FunctionStatement, SyntaxKind.SubNewStatement,
                                             SyntaxKind.OperatorStatement, SyntaxKind.PropertyStatement, SyntaxKind.EventStatement,
                                             SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement,
                                             SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the top-level <c>&lt;param&gt;</c> elements of a documentation comment.
        ''' </summary>
        '''
        ''' <param name="docComment">
        ''' The documentation comment.
        ''' </param>
        '''
        ''' <returns>
        ''' The <c>&lt;param&gt;</c> element nodes (<see cref="XmlElementSyntax"/> or <see cref="XmlEmptyElementSyntax"/>), in source order.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="docComment"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetParamElements(docComment As DocumentationCommentTriviaSyntax) As List(Of XmlNodeSyntax)

            If docComment Is Nothing Then
                Throw New ArgumentNullException(NameOf(docComment))
            End If

            Dim result As New List(Of XmlNodeSyntax)()
            For Each node As XmlNodeSyntax In docComment.Content
                Dim elementName As String = UtilAnalyzerSyntax.GetXmlElementName(node)
                If elementName IsNot Nothing AndAlso elementName.Equals(XmlParameterDocumentationAnalyzer.ParamElementName, StringComparison.Ordinal) Then
                    result.Add(node)
                End If
            Next node
            Return result
        End Function

        ''' <summary>
        ''' Gets the parameter names declared by a parameter list.
        ''' </summary>
        '''
        ''' <param name="parameterList">
        ''' The parameter list. May be <see langword="Nothing"/> when the declaration has no parentheses.
        ''' </param>
        '''
        ''' <returns>
        ''' The parameter names, in declaration order.
        ''' </returns>
        Friend Shared Function GetParameterNames(parameterList As ParameterListSyntax) As List(Of String)

            Dim result As New List(Of String)()
            If parameterList Is Nothing Then
                Return result ' A declaration without parentheses legitimately has no parameters.
            End If

            For Each parameter As ParameterSyntax In parameterList.Parameters
                result.Add(parameter.Identifier.Identifier.ValueText)
            Next parameter
            Return result
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a method-like declaration statement.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeDeclaration(context As SyntaxNodeAnalysisContext)

            Dim declaration As SyntaxNode = context.Node
            Dim docComment As DocumentationCommentTriviaSyntax = UtilAnalyzerSyntax.GetDocumentationComment(declaration)
            If docComment Is Nothing OrElse UtilAnalyzerSyntax.IsInheritedOrIncludedDocumentation(docComment) Then
                Exit Sub ' Undocumented members are reported by DCA007.
            End If

            Dim parameterNames As List(Of String) = XmlParameterDocumentationAnalyzer.GetParameterNames(UtilAnalyzerSyntax.GetParameterList(declaration))
            Dim parameterSet As New HashSet(Of String)(parameterNames, StringComparer.OrdinalIgnoreCase)
            Dim documented As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim emptyTags As New List(Of String)()
            Dim unknownTags As New List(Of String)()
            Dim duplicateTags As New List(Of String)()

            For Each element As XmlNodeSyntax In XmlParameterDocumentationAnalyzer.GetParamElements(docComment)
                Dim name As String = UtilAnalyzerSyntax.GetXmlNameAttributeValue(element)
                If String.IsNullOrEmpty(name) Then
                    unknownTags.Add("(unnamed)")
                    Continue For
                End If
                If Not parameterSet.Contains(name) Then
                    unknownTags.Add(name)
                    Continue For
                End If
                If Not documented.Add(name) Then
                    duplicateTags.Add(name)
                    Continue For
                End If
                If UtilAnalyzerSyntax.IsXmlElementEmpty(element) Then
                    emptyTags.Add(name)
                End If
            Next element

            Dim missing As List(Of String) = parameterNames.Where(Function(n As String) Not documented.Contains(n)).ToList()
            Dim issues As New List(Of String)()
            If missing.Count > 0 Then
                issues.Add($"missing <param> for '{String.Join("', '", missing)}'")
            End If
            If emptyTags.Count > 0 Then
                issues.Add($"empty <param> for '{String.Join("', '", emptyTags)}'")
            End If
            If unknownTags.Count > 0 Then
                issues.Add($"unknown <param> '{String.Join("', '", unknownTags)}'")
            End If
            If duplicateTags.Count > 0 Then
                issues.Add($"duplicated <param> '{String.Join("', '", duplicateTags)}'")
            End If
            If issues.Count = 0 Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(XmlParameterDocumentationAnalyzer.MissingParametersPropertyKey, String.Join(","c, missing)).
                Add(XmlParameterDocumentationAnalyzer.EmptyParametersPropertyKey, String.Join(","c, emptyTags))

            Dim identifier As SyntaxToken = UtilAnalyzerSyntax.GetIdentifierToken(declaration)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.XmlParameterMismatch, identifier.GetLocation(), properties,
                                                       identifier.ValueText, String.Join("; ", issues)))
        End Sub

#End Region

    End Class

End Namespace

#End Region
