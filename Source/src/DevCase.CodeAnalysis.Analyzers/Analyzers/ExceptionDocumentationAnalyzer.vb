#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ExceptionDocumentationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports documented members that explicitly throw exceptions not listed with an <c>&lt;exception cref="..."&gt;</c> tag
    ''' (rule <see cref="DiagnosticIds.MissingExceptionDocumentation"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Only deterministic sources are considered (nothing is inferred):
    ''' <list type="bullet">
    '''   <item><description><c>Throw New SomeException(...)</c> statements in the member body (lambdas excluded).</description></item>
    '''   <item><description>Calls to the .NET throw helpers, whose thrown types are documented by Microsoft:
    '''   <c>ArgumentNullException.ThrowIfNull</c>, <c>ArgumentException.ThrowIfNullOrEmpty</c> / <c>ThrowIfNullOrWhiteSpace</c>
    '''   (<c>ArgumentNullException</c> and <c>ArgumentException</c>), <c>ArgumentOutOfRangeException.ThrowIf*</c> and
    '''   <c>ObjectDisposedException.ThrowIf</c>.</description></item>
    ''' </list>
    ''' Exceptions caught by an enclosing <see langword="Try"/> of the same member (catch-all or a base type, without <see langword="When"/> filter),
    ''' re-throws (<c>Throw</c>, <c>Throw ex</c>) and <see cref="NotImplementedException"/> are ignored.
    ''' Undocumented members (DCA007) and <c>&lt;inheritdoc/&gt;</c> / <c>&lt;include/&gt;</c> comments are skipped.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ExceptionDocumentationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property key that stores the comma-separated undocumented exception type names (as they must be written in a cref).
        ''' </summary>
        Friend Const MissingExceptionsPropertyKey As String = "MissingExceptions"

        ''' <summary>
        ''' The local name of the <c>&lt;exception&gt;</c> XML element.
        ''' </summary>
        Friend Const ExceptionElementName As String = "exception"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingExceptionDocumentation)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ExceptionDocumentationAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ExceptionDocumentationAnalyzer.AnalyzeMember),
                                             SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock,
                                             SyntaxKind.OperatorBlock, SyntaxKind.PropertyBlock, SyntaxKind.EventBlock)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the declaration statement that carries the XML documentation of a member block.
        ''' </summary>
        '''
        ''' <param name="memberBlock">
        ''' A method, constructor, operator, property or event block.
        ''' </param>
        '''
        ''' <returns>
        ''' The declaration statement.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="memberBlock"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="memberBlock"/> is not a supported member block.
        ''' </exception>
        Friend Shared Function GetDeclarationStatement(memberBlock As SyntaxNode) As StatementSyntax

            If memberBlock Is Nothing Then
                Throw New ArgumentNullException(NameOf(memberBlock))
            End If

            If TypeOf memberBlock Is MethodBlockBaseSyntax Then
                Return DirectCast(memberBlock, MethodBlockBaseSyntax).BlockStatement
            End If
            If TypeOf memberBlock Is PropertyBlockSyntax Then
                Return DirectCast(memberBlock, PropertyBlockSyntax).PropertyStatement
            End If
            If TypeOf memberBlock Is EventBlockSyntax Then
                Return DirectCast(memberBlock, EventBlockSyntax).EventStatement
            End If
            Throw New ArgumentException($"Unsupported member kind: {memberBlock.Kind()}.", NameOf(memberBlock))
        End Function

        ''' <summary>
        ''' Gets the exception types explicitly thrown (and not caught) by a member, in order of first appearance.
        ''' </summary>
        '''
        ''' <param name="memberBlock">
        ''' The member block.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The thrown exception types, without duplicates.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="memberBlock"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetThrownExceptionTypes(memberBlock As SyntaxNode, semanticModel As SemanticModel,
                                                       cancellationToken As CancellationToken) As List(Of INamedTypeSymbol)

            If memberBlock Is Nothing Then
                Throw New ArgumentNullException(NameOf(memberBlock))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            Dim result As New List(Of INamedTypeSymbol)()
            Dim notImplemented As INamedTypeSymbol = semanticModel.Compilation.GetTypeByMetadataName("System.NotImplementedException")

            For Each node As SyntaxNode In memberBlock.DescendantNodes()
                If ExceptionDocumentationAnalyzer.IsInsideLambda(node, memberBlock) Then
                    Continue For
                End If

                Dim thrownTypes As New List(Of INamedTypeSymbol)()

                Dim throwStatement As ThrowStatementSyntax = TryCast(node, ThrowStatementSyntax)
                If throwStatement IsNot Nothing Then
                    Dim creation As ObjectCreationExpressionSyntax = TryCast(throwStatement.Expression, ObjectCreationExpressionSyntax)
                    If creation IsNot Nothing Then
                        Dim type As INamedTypeSymbol = TryCast(semanticModel.GetTypeInfo(creation, cancellationToken).Type, INamedTypeSymbol)
                        If type IsNot Nothing AndAlso type.TypeKind <> TypeKind.Error Then
                            thrownTypes.Add(type)
                        End If
                    End If
                End If

                Dim invocation As InvocationExpressionSyntax = TryCast(node, InvocationExpressionSyntax)
                If invocation IsNot Nothing Then
                    Dim method As IMethodSymbol = TryCast(semanticModel.GetSymbolInfo(invocation, cancellationToken).Symbol, IMethodSymbol)
                    If method IsNot Nothing Then
                        thrownTypes.AddRange(ExceptionDocumentationAnalyzer.GetThrowHelperExceptions(method, semanticModel.Compilation))
                    End If
                End If

                For Each type As INamedTypeSymbol In thrownTypes
                    If SymbolEqualityComparer.Default.Equals(type, notImplemented) OrElse
                       result.Contains(type, SymbolEqualityComparer.Default) OrElse
                       ExceptionDocumentationAnalyzer.IsCaughtWithinMember(node, memberBlock, type, semanticModel, cancellationToken) Then
                        Continue For
                    End If
                    result.Add(type)
                Next type
            Next node
            Return result
        End Function

        ''' <summary>
        ''' Gets the type names referenced by the <c>&lt;exception cref="..."&gt;</c> tags of a documentation comment.
        ''' </summary>
        '''
        ''' <param name="docComment">
        ''' The documentation comment.
        ''' </param>
        '''
        ''' <returns>
        ''' The simple type names (without namespace or <c>T:</c> prefix).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="docComment"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetDocumentedExceptionNames(docComment As DocumentationCommentTriviaSyntax) As HashSet(Of String)

            If docComment Is Nothing Then
                Throw New ArgumentNullException(NameOf(docComment))
            End If

            Dim result As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each element As XmlNodeSyntax In docComment.Content
                If Not ExceptionDocumentationAnalyzer.ExceptionElementName.Equals(UtilAnalyzerSyntax.GetXmlElementName(element), StringComparison.Ordinal) Then
                    Continue For
                End If

                Dim attributes As SyntaxList(Of XmlNodeSyntax) =
                    If(element.IsKind(SyntaxKind.XmlElement), DirectCast(element, XmlElementSyntax).StartTag.Attributes, DirectCast(element, XmlEmptyElementSyntax).Attributes)

                For Each attribute As XmlNodeSyntax In attributes
                    Dim crefText As String = Nothing
                    If attribute.IsKind(SyntaxKind.XmlCrefAttribute) Then
                        crefText = DirectCast(attribute, XmlCrefAttributeSyntax).Reference.ToString()
                    ElseIf attribute.IsKind(SyntaxKind.XmlAttribute) Then
                        Dim xmlAttribute As XmlAttributeSyntax = DirectCast(attribute, XmlAttributeSyntax)
                        Dim attributeName As XmlNameSyntax = TryCast(xmlAttribute.Name, XmlNameSyntax)
                        Dim value As XmlStringSyntax = TryCast(xmlAttribute.Value, XmlStringSyntax)
                        If attributeName IsNot Nothing AndAlso value IsNot Nothing AndAlso
                           attributeName.LocalName.ValueText.Equals("cref", StringComparison.OrdinalIgnoreCase) Then
                            crefText = String.Concat(value.TextTokens.Select(Function(t As SyntaxToken) t.ValueText))
                        End If
                    End If

                    If Not String.IsNullOrWhiteSpace(crefText) Then
                        result.Add(ExceptionDocumentationAnalyzer.GetSimpleCrefName(crefText))
                    End If
                Next attribute
            Next element
            Return result
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a member block.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeMember(context As SyntaxNodeAnalysisContext)

            Dim declaration As StatementSyntax = ExceptionDocumentationAnalyzer.GetDeclarationStatement(context.Node)
            Dim docComment As DocumentationCommentTriviaSyntax = UtilAnalyzerSyntax.GetDocumentationComment(declaration)
            If docComment Is Nothing OrElse UtilAnalyzerSyntax.IsInheritedOrIncludedDocumentation(docComment) Then
                Exit Sub ' Undocumented members are reported by DCA007.
            End If

            Dim documented As HashSet(Of String) = ExceptionDocumentationAnalyzer.GetDocumentedExceptionNames(docComment)
            Dim missing As List(Of String) = ExceptionDocumentationAnalyzer.GetThrownExceptionTypes(context.Node, context.SemanticModel, context.CancellationToken).
                Where(Function(t As INamedTypeSymbol) Not documented.Contains(t.Name)).
                Select(Function(t As INamedTypeSymbol) t.ToMinimalDisplayString(context.SemanticModel, declaration.SpanStart)).
                ToList()

            If missing.Count = 0 Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) =
                ImmutableDictionary(Of String, String).Empty.Add(ExceptionDocumentationAnalyzer.MissingExceptionsPropertyKey, String.Join(","c, missing))
            Dim identifier As SyntaxToken = UtilAnalyzerSyntax.GetIdentifierToken(declaration)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingExceptionDocumentation, identifier.GetLocation(), properties,
                                                       identifier.ValueText, String.Join(", ", missing)))
        End Sub

        ''' <summary>
        ''' Gets the exceptions documented by Microsoft for a .NET throw-helper method.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The invoked method.
        ''' </param>
        '''
        ''' <param name="compilation">
        ''' The compilation, used to resolve the exception types.
        ''' </param>
        '''
        ''' <returns>
        ''' The thrown exception types, or an empty sequence if the method is not a known throw helper.
        ''' </returns>
        Private Shared Function GetThrowHelperExceptions(method As IMethodSymbol, compilation As Compilation) As IEnumerable(Of INamedTypeSymbol)

            Dim containingType As String = method.ContainingType.ToDisplayString()
            Dim names As String()
            Select Case containingType
                Case "System.ArgumentNullException"
                    names = If(method.Name.Equals("ThrowIfNull", StringComparison.Ordinal), {"System.ArgumentNullException"}, Array.Empty(Of String)())
                Case "System.ArgumentException"
                    names = If(method.Name.StartsWith("ThrowIfNullOr", StringComparison.Ordinal),
                               {"System.ArgumentNullException", "System.ArgumentException"}, Array.Empty(Of String)())
                Case "System.ArgumentOutOfRangeException"
                    names = If(method.Name.StartsWith("ThrowIf", StringComparison.Ordinal), {"System.ArgumentOutOfRangeException"}, Array.Empty(Of String)())
                Case "System.ObjectDisposedException"
                    names = If(method.Name.Equals("ThrowIf", StringComparison.Ordinal), {"System.ObjectDisposedException"}, Array.Empty(Of String)())
                Case Else
                    names = Array.Empty(Of String)()
            End Select

            Return names.Select(Function(n As String) compilation.GetTypeByMetadataName(n)).Where(Function(t As INamedTypeSymbol) t IsNot Nothing)
        End Function

        ''' <summary>
        ''' Determines whether a node is located inside a lambda expression of the member (whose throws do not belong to the member).
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node.
        ''' </param>
        '''
        ''' <param name="memberBlock">
        ''' The member block.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the node is inside a lambda; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsInsideLambda(node As SyntaxNode, memberBlock As SyntaxNode) As Boolean

            For Each ancestor As SyntaxNode In node.Ancestors()
                If ancestor Is memberBlock Then
                    Return False
                End If
                If TypeOf ancestor Is LambdaExpressionSyntax Then
                    Return True
                End If
            Next ancestor
            Return False
        End Function

        ''' <summary>
        ''' Determines whether an exception thrown at a node is caught by an enclosing <see langword="Try"/> block of the same member.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The throwing node.
        ''' </param>
        '''
        ''' <param name="memberBlock">
        ''' The member block.
        ''' </param>
        '''
        ''' <param name="exceptionType">
        ''' The thrown exception type.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if a catch clause (without filter) handles the exception; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsCaughtWithinMember(node As SyntaxNode, memberBlock As SyntaxNode, exceptionType As INamedTypeSymbol,
                                                     semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            Dim child As SyntaxNode = node
            For Each ancestor As SyntaxNode In node.Ancestors()
                If ancestor Is memberBlock Then
                    Return False
                End If

                Dim tryBlock As TryBlockSyntax = TryCast(ancestor, TryBlockSyntax)
                ' Only throws located in the Try part (not in Catch/Finally blocks) can be caught by its handlers.
                If tryBlock IsNot Nothing AndAlso tryBlock.Statements.Contains(TryCast(child, StatementSyntax)) Then
                    For Each catchBlock As CatchBlockSyntax In tryBlock.CatchBlocks
                        If catchBlock.CatchStatement.WhenClause IsNot Nothing Then
                            Continue For
                        End If
                        If catchBlock.CatchStatement.AsClause Is Nothing Then
                            Return True
                        End If

                        Dim caughtType As ITypeSymbol = semanticModel.GetTypeInfo(catchBlock.CatchStatement.AsClause.Type, cancellationToken).Type
                        Dim current As INamedTypeSymbol = exceptionType
                        While current IsNot Nothing
                            If SymbolEqualityComparer.Default.Equals(current, caughtType) Then
                                Return True
                            End If
                            current = current.BaseType
                        End While
                    Next catchBlock
                End If
                child = ancestor
            Next ancestor
            Return False
        End Function

        ''' <summary>
        ''' Gets the simple type name of a cref (e.g. <c>"T:System.ArgumentException"</c> → <c>"ArgumentException"</c>).
        ''' </summary>
        '''
        ''' <param name="cref">
        ''' The cref text.
        ''' </param>
        '''
        ''' <returns>
        ''' The simple name.
        ''' </returns>
        Private Shared Function GetSimpleCrefName(cref As String) As String

            Dim text As String = cref.Trim()
            Dim colonIndex As Integer = text.IndexOf(":", StringComparison.Ordinal)
            If colonIndex >= 0 Then
                text = text.Substring(colonIndex + 1)
            End If

            Dim genericIndex As Integer = text.IndexOf("(", StringComparison.Ordinal)
            If genericIndex >= 0 Then
                text = text.Substring(0, genericIndex)
            End If

            Dim dotIndex As Integer = text.LastIndexOf(".", StringComparison.Ordinal)
            Return If(dotIndex >= 0, text.Substring(dotIndex + 1), text).Trim()
        End Function

#End Region

    End Class

End Namespace

#End Region
