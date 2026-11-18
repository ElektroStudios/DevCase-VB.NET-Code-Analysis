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

#Region " UnvalidatedParameterCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes for rule <see cref="DiagnosticIds.UnvalidatedParameter"/>: insert a guard clause at the start of the body.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <list type="table">
    '''   <listheader><term>Parameter</term><description>Guard clauses offered (the first one is the default)</description></listheader>
    '''   <item><term>Enumeration</term><description><c>If Not [Enum].IsDefined(GetType(T), p) Then Throw New InvalidEnumArgumentException(NameOf(p), p, GetType(T))</c>
    '''   (<see cref="ArgumentOutOfRangeException"/> when the values do not fit in an <see cref="Integer"/>).</description></item>
    '''   <item><term><see cref="String"/></term><description><c>String.IsNullOrEmpty(p)</c>, <c>String.IsNullOrWhiteSpace(p)</c> or <c>p Is Nothing</c>,
    '''   throwing <see cref="ArgumentNullException"/>.</description></item>
    '''   <item><term>Other reference types</term><description><c>If p Is Nothing Then Throw New ArgumentNullException(NameOf(p))</c>.</description></item>
    ''' </list>
    ''' When every project that compiles the document has the throw helper of the variant (.NET 6 or later; the check covers 
    ''' multi-targeted projects and linked files), it is written instead of the <see langword="If"/> block: 
    ''' <c>ArgumentNullException.ThrowIfNull(p)</c>, <c>ArgumentException.ThrowIfNullOrEmpty(p)</c> (.NET 7) or 
    ''' <c>ArgumentException.ThrowIfNullOrWhiteSpace(p)</c> (.NET 8). The Enum guard clause has no throw helper.
    ''' The guard clause is written after the <c>MyBase.New</c> / <c>Me.New</c> call of a constructor and among the guard clauses
    ''' that already start the body, in parameter order. Fix All inserts the guard clauses of every parameter of a method in parameter order.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(UnvalidatedParameterCodeFixProvider)), [Shared]>
    Public NotInheritable Class UnvalidatedParameterCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The guard clause that throws when the parameter is <see langword="Nothing"/>.
        ''' </summary>
        Friend Const NothingVariant As String = "Nothing"

        ''' <summary>
        ''' The guard clause that throws when the string parameter is <see langword="Nothing"/> or empty.
        ''' </summary>
        Friend Const NullOrEmptyVariant As String = "NullOrEmpty"

        ''' <summary>
        ''' The guard clause that throws when the string parameter is <see langword="Nothing"/>, empty or white space.
        ''' </summary>
        Friend Const NullOrWhiteSpaceVariant As String = "NullOrWhiteSpace"

        ''' <summary>
        ''' The guard clause that throws when the enumeration parameter is not a defined value.
        ''' </summary>
        Friend Const EnumVariant As String = "EnumIsDefined"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The Fix All provider, which inserts the guard clauses of a method in parameter order.
        ''' </summary>
        Private Shared ReadOnly FixAll As FixAllProvider = FixAllProvider.Create(AddressOf UnvalidatedParameterCodeFixProvider.FixAllAsync)

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.UnvalidatedParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="UnvalidatedParameterCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return UnvalidatedParameterCodeFixProvider.FixAll
        End Function

        ''' <inheritdoc/>
        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            Dim text As SourceText = Await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(False)
            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim kind As String = Nothing
                If Not diagnostic.Properties.TryGetValue(UnvalidatedParameterAnalyzer.KindPropertyKey, kind) OrElse kind Is Nothing Then
                    Continue For
                End If

                Dim name As String = text.ToString(diagnostic.Location.SourceSpan)
                For Each [variant] As String In UnvalidatedParameterCodeFixProvider.GetVariants(kind)
                    Dim document As Document = context.Document
                    Dim selected As String = [variant]
                    context.RegisterCodeFix(
                        CodeAction.Create(UnvalidatedParameterCodeFixProvider.GetTitle(selected, name, diagnostic),
                                          Function(ct As CancellationToken) UnvalidatedParameterCodeFixProvider.FixSingleAsync(document, diagnostic, selected, ct),
                                          equivalenceKey:=$"{NameOf(UnvalidatedParameterCodeFixProvider)}.{selected}"),
                        diagnostic)
                Next [variant]
            Next diagnostic
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the guard clauses offered for a kind of parameter, the default one first.
        ''' </summary>
        '''
        ''' <param name="kind">
        ''' The kind of the parameter (see <see cref="UnvalidatedParameterAnalyzer.KindPropertyKey"/>).
        ''' </param>
        '''
        ''' <returns>
        ''' The variants.
        ''' </returns>
        Private Shared Function GetVariants(kind As String) As String()

            Select Case kind
                Case UnvalidatedParameterAnalyzer.EnumKind
                    Return {UnvalidatedParameterCodeFixProvider.EnumVariant}
                Case UnvalidatedParameterAnalyzer.StringKind
                    Return {UnvalidatedParameterCodeFixProvider.NullOrEmptyVariant, UnvalidatedParameterCodeFixProvider.NullOrWhiteSpaceVariant,
                            UnvalidatedParameterCodeFixProvider.NothingVariant}
                Case UnvalidatedParameterAnalyzer.ReferenceKind
                    Return {UnvalidatedParameterCodeFixProvider.NothingVariant}
                Case Else
                    Return Array.Empty(Of String)()
            End Select
        End Function

        ''' <summary>
        ''' Gets the title of a code action.
        ''' </summary>
        '''
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="name">
        ''' The parameter name.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <returns>
        ''' The title.
        ''' </returns>
        Private Shared Function GetTitle([variant] As String, name As String, diagnostic As Diagnostic) As String

            Select Case [variant]
                Case UnvalidatedParameterCodeFixProvider.EnumVariant
                    Dim exception As String = UnvalidatedParameterCodeFixProvider.GetEnumException(diagnostic)
                    Return $"Throw {exception} if '{name}' is not a defined value"
                Case UnvalidatedParameterCodeFixProvider.NullOrEmptyVariant
                    Return $"Throw ArgumentNullException if '{name}' is Nothing or empty"
                Case UnvalidatedParameterCodeFixProvider.NullOrWhiteSpaceVariant
                    Return $"Throw ArgumentNullException if '{name}' is Nothing, empty or white space"
                Case Else
                    Return $"Throw ArgumentNullException if '{name}' is Nothing"
            End Select
        End Function

        ''' <summary>
        ''' Gets the simple name of the exception thrown by the guard clause of an enumeration parameter.
        ''' </summary>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <returns>
        ''' <c>InvalidEnumArgumentException</c> or <c>ArgumentOutOfRangeException</c>.
        ''' </returns>
        Private Shared Function GetEnumException(diagnostic As Diagnostic) As String

            Dim invalidEnum As String = Nothing
            Return If(diagnostic.Properties.TryGetValue(UnvalidatedParameterAnalyzer.InvalidEnumExceptionPropertyKey, invalidEnum) AndAlso
                      Not String.IsNullOrEmpty(invalidEnum),
                      "InvalidEnumArgumentException", "ArgumentOutOfRangeException")
        End Function

        ''' <summary>
        ''' Determines whether a guard clause variant applies to a kind of parameter.
        ''' </summary>
        '''
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the variant is offered for the parameter; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function AppliesTo([variant] As String, diagnostic As Diagnostic) As Boolean

            Dim kind As String = Nothing
            Return diagnostic.Properties.TryGetValue(UnvalidatedParameterAnalyzer.KindPropertyKey, kind) AndAlso kind IsNot Nothing AndAlso
                   UnvalidatedParameterCodeFixProvider.GetVariants(kind).Contains([variant], StringComparer.Ordinal)
        End Function

        ''' <summary>
        ''' Applies a Fix All operation to one document.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The Fix All context.
        ''' </param>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostics">
        ''' The diagnostics of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        '''
        ''' <remarks>
        ''' Methods are fixed from the last one to the first one, and the parameters of a method in parameter order:
        ''' a guard clause is only inserted after the parameters of its method, so the locations of the remaining diagnostics stay valid.
        ''' </remarks>
        Private Shared Async Function FixAllAsync(context As FixAllContext, document As Document, diagnostics As ImmutableArray(Of Diagnostic)) As Task(Of Document)

            Dim key As String = If(context.CodeActionEquivalenceKey, String.Empty)
            Dim prefix As String = $"{NameOf(UnvalidatedParameterCodeFixProvider)}."
            If Not key.StartsWith(prefix, StringComparison.Ordinal) Then
                Return document
            End If

            Dim [variant] As String = key.Substring(prefix.Length)
            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim ordered As List(Of Diagnostic) = diagnostics.
                Where(Function(d As Diagnostic) UnvalidatedParameterCodeFixProvider.AppliesTo([variant], d)).
                OrderByDescending(Function(d As Diagnostic) UnvalidatedParameterCodeFixProvider.GetMethodStart(root, d)).
                ThenBy(Function(d As Diagnostic) d.Location.SourceSpan.Start).
                ToList()

            Dim useHelper As Boolean = Await UnvalidatedParameterCodeFixProvider.IsThrowHelperAvailableAsync(document, [variant], context.CancellationToken).ConfigureAwait(False)
            Dim current As Document = document
            For Each diagnostic As Diagnostic In ordered
                current = Await UnvalidatedParameterCodeFixProvider.FixAsync(current, diagnostic, [variant], useHelper, context.CancellationToken).ConfigureAwait(False)
            Next diagnostic
            Return current
        End Function

        ''' <summary>
        ''' Applies the code fix to one diagnostic, using the throw helper of the variant when every project that compiles 
        ''' the document has it.
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
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function FixSingleAsync(document As Document, diagnostic As Diagnostic, [variant] As String,
                                                     cancellationToken As CancellationToken) As Task(Of Document)

            Dim useHelper As Boolean = Await UnvalidatedParameterCodeFixProvider.IsThrowHelperAvailableAsync(document, [variant], cancellationToken).ConfigureAwait(False)
            Return Await UnvalidatedParameterCodeFixProvider.FixAsync(document, diagnostic, [variant], useHelper, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Gets the name of the throw helper method of a guard clause variant: <c>ArgumentNullException.ThrowIfNull</c> (.NET 6), 
        ''' <c>ArgumentException.ThrowIfNullOrEmpty</c> (.NET 7) or <c>ArgumentException.ThrowIfNullOrWhiteSpace</c> (.NET 8).
        ''' </summary>
        '''
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <returns>
        ''' The method name, or <see langword="Nothing"/> for the Enum variant, which has no throw helper.
        ''' </returns>
        Private Shared Function GetThrowHelperMethod([variant] As String) As String

            Select Case [variant]
                Case UnvalidatedParameterCodeFixProvider.NothingVariant
                    Return "ThrowIfNull"
                Case UnvalidatedParameterCodeFixProvider.NullOrEmptyVariant
                    Return "ThrowIfNullOrEmpty"
                Case UnvalidatedParameterCodeFixProvider.NullOrWhiteSpaceVariant
                    Return "ThrowIfNullOrWhiteSpace"
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Determines whether the throw helper of a guard clause variant exists in every project that compiles the document.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the helper can be used; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <remarks>
        ''' A file can be compiled by several projects: a multi-targeted project (<c>net48;net8.0</c>) or linked files. 
        ''' The helper is only written when it exists in all of them, so the fix never breaks the .NET Framework build.
        ''' </remarks>
        Private Shared Async Function IsThrowHelperAvailableAsync(document As Document, [variant] As String, cancellationToken As CancellationToken) As Task(Of Boolean)

            Dim method As String = UnvalidatedParameterCodeFixProvider.GetThrowHelperMethod([variant])
            If method Is Nothing Then
                Return False
            End If

            Dim typeName As String = If([variant].Equals(UnvalidatedParameterCodeFixProvider.NothingVariant, StringComparison.Ordinal),
                                        "System.ArgumentNullException", "System.ArgumentException")
            Dim solution As Solution = document.Project.Solution
            Dim ids As ImmutableArray(Of DocumentId) = If(String.IsNullOrEmpty(document.FilePath), ImmutableArray(Of DocumentId).Empty,
                                                          solution.GetDocumentIdsWithFilePath(document.FilePath))
            If Not ids.Contains(document.Id) Then
                ids = ids.Add(document.Id)
            End If

            For Each id As DocumentId In ids
                Dim compilation As Compilation = Await solution.GetProject(id.ProjectId).GetCompilationAsync(cancellationToken).ConfigureAwait(False)
                Dim type As INamedTypeSymbol = compilation?.GetTypeByMetadataName(typeName)
                If type Is Nothing OrElse Not type.GetMembers(method).OfType(Of IMethodSymbol)().
                       Any(Function(m As IMethodSymbol) m.IsStatic AndAlso m.DeclaredAccessibility = Accessibility.Public) Then
                    Return False
                End If
            Next id
            Return True
        End Function

        ''' <summary>
        ''' Gets the start position of the method that declares the parameter reported by a diagnostic.
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
        ''' The start position of the method block.
        ''' </returns>
        Private Shared Function GetMethodStart(root As SyntaxNode, diagnostic As Diagnostic) As Integer

            Dim block As MethodBlockBaseSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of MethodBlockBaseSyntax)()
            Return If(block Is Nothing, diagnostic.Location.SourceSpan.Start, block.SpanStart)
        End Function

        ''' <summary>
        ''' Inserts the guard clause of the parameter reported by the specified diagnostic.
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
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="useHelper">
        ''' <see langword="True"/> to write the throw helper of the variant (see <see cref="IsThrowHelperAvailableAsync"/>) 
        ''' instead of an <see langword="If"/> block.
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
        ''' Thrown when the diagnostic no longer points to a parameter of a method with a body.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, [variant] As String, useHelper As Boolean,
                                               cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Dim parameter As ParameterSyntax = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.FirstAncestorOrSelf(Of ParameterSyntax)()
            Dim block As MethodBlockBaseSyntax = parameter?.FirstAncestorOrSelf(Of MethodBlockBaseSyntax)()
            If block Is Nothing OrElse block.BlockStatement.ParameterList Is Nothing Then
                Throw New InvalidOperationException("The diagnostic does not point to a parameter of a method with a body.")
            End If

            ' Ordinals of the parameters, by name (Visual Basic names are case-insensitive).
            Dim parameters As SeparatedSyntaxList(Of ParameterSyntax) = block.BlockStatement.ParameterList.Parameters
            Dim ordinals As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
            For index As Integer = 0 To parameters.Count - 1
                ordinals(parameters(index).Identifier.Identifier.ValueText) = index
            Next index
            Dim ordinal As Integer = parameters.IndexOf(parameter)

            ' The guard clause goes before the first leading guard clause that checks a later parameter,
            ' or after the last leading guard clause, or at the start of the body.
            Dim statements As SyntaxList(Of StatementSyntax) = block.Statements
            Dim first As Integer = If(UnvalidatedParameterAnalyzer.GetConstructorCall(block) Is Nothing, 0, 1)
            Dim target As SyntaxNode = block.EndBlockStatement
            Dim previousGuard As StatementSyntax = Nothing
            For index As Integer = first To statements.Count - 1
                Dim statement As StatementSyntax = statements(index)
                If Not UnvalidatedParameterCodeFixProvider.IsGuard(statement) OrElse
                   UnvalidatedParameterCodeFixProvider.GetReferencedOrdinals(statement, ordinals).Any(Function(o As Integer) o > ordinal) Then
                    target = statement
                    Exit For
                End If
                previousGuard = statement
            Next index

            Dim eol As String = UtilAnalyzerSyntax.GetEndOfLine(root.SyntaxTree)
            Dim indentation As String = If(statements.Count = 0, $"{UtilAnalyzerSyntax.GetLineIndentation(block.EndBlockStatement.GetFirstToken())}    ",
                                           UtilAnalyzerSyntax.GetLineIndentation(statements(0).GetFirstToken()))
            Dim guard As String = String.Join(eol, UnvalidatedParameterCodeFixProvider.BuildGuard([variant], parameter.Identifier.Identifier.Text, diagnostic, indentation, useHelper))

            Dim position As Integer
            Dim insertion As String
            Dim targetStatement As StatementSyntax = TryCast(target, StatementSyntax)
            Dim targetIsCode As Boolean = target IsNot block.EndBlockStatement AndAlso Not UnvalidatedParameterCodeFixProvider.IsGuard(targetStatement)
            If previousGuard IsNot Nothing AndAlso (targetIsCode OrElse target Is block.EndBlockStatement) Then
                ' After the last guard clause: the separation between the guard clauses and the code is kept.
                position = text.Lines.GetLineFromPosition(previousGuard.Span.End).EndIncludingLineBreak
                insertion = $"{guard}{eol}"
            Else
                position = UnvalidatedParameterCodeFixProvider.GetLineStartBeforeComments(target, text)
                insertion = If(targetIsCode, $"{guard}{eol}{eol}", $"{guard}{eol}")
            End If
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {New TextChange(New TextSpan(position, 0), insertion)}, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Builds the lines of a guard clause.
        ''' </summary>
        '''
        ''' <param name="variant">
        ''' The guard clause variant.
        ''' </param>
        '''
        ''' <param name="name">
        ''' The parameter name, as written in the source (escaped names keep their brackets).
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic, whose properties hold the type names.
        ''' </param>
        '''
        ''' <param name="indentation">
        ''' The indentation of the guard clause.
        ''' </param>
        '''
        ''' <param name="useHelper">
        ''' <see langword="True"/> to write the throw helper of the variant (e.g. <c>ArgumentNullException.ThrowIfNull(value)</c>).
        ''' </param>
        '''
        ''' <returns>
        ''' The lines, without line breaks.
        ''' </returns>
        Private Shared Function BuildGuard([variant] As String, name As String, diagnostic As Diagnostic, indentation As String, useHelper As Boolean) As String()

            Dim inner As String = $"{indentation}    "
            Dim argumentNull As String = diagnostic.Properties(UnvalidatedParameterAnalyzer.ArgumentNullExceptionPropertyKey)
            Dim helperMethod As String = UnvalidatedParameterCodeFixProvider.GetThrowHelperMethod([variant])
            If useHelper AndAlso helperMethod IsNot Nothing Then
                ' The Visual Basic compiler fills the paramName parameter ([CallerArgumentExpression]) with the parameter name.
                Dim helperType As String = If([variant].Equals(UnvalidatedParameterCodeFixProvider.NothingVariant, StringComparison.Ordinal), argumentNull,
                                              diagnostic.Properties(UnvalidatedParameterAnalyzer.ArgumentExceptionPropertyKey))
                Return {$"{indentation}{helperType}.{helperMethod}({name})"}
            End If

            Select Case [variant]
                Case UnvalidatedParameterCodeFixProvider.EnumVariant
                    Dim enumType As String = diagnostic.Properties(UnvalidatedParameterAnalyzer.EnumTypePropertyKey)
                    Dim invalidEnum As String = diagnostic.Properties(UnvalidatedParameterAnalyzer.InvalidEnumExceptionPropertyKey)
                    Dim throwLine As String = If(String.IsNullOrEmpty(invalidEnum),
                        $"{inner}Throw New {diagnostic.Properties(UnvalidatedParameterAnalyzer.ArgumentOutOfRangeExceptionPropertyKey)}(NameOf({name}), {name}, $""The value is not defined in the {{NameOf({enumType})}} enumeration."")",
                        $"{inner}Throw New {invalidEnum}(NameOf({name}), {name}, GetType({enumType}))")
                    Return {$"{indentation}If Not [Enum].IsDefined(GetType({enumType}), {name}) Then", throwLine, $"{indentation}End If"}

                Case UnvalidatedParameterCodeFixProvider.NullOrEmptyVariant
                    Return {$"{indentation}If String.IsNullOrEmpty({name}) Then",
                            $"{inner}Throw New {argumentNull}(NameOf({name}), $""The {{NameOf({name})}} cannot be null or empty."")",
                            $"{indentation}End If"}

                Case UnvalidatedParameterCodeFixProvider.NullOrWhiteSpaceVariant
                    Return {$"{indentation}If String.IsNullOrWhiteSpace({name}) Then",
                            $"{inner}Throw New {argumentNull}(NameOf({name}), $""The {{NameOf({name})}} cannot be null, empty or white space."")",
                            $"{indentation}End If"}

                Case Else
                    Return {$"{indentation}If {name} Is Nothing Then",
                            $"{inner}Throw New {argumentNull}(NameOf({name}))",
                            $"{indentation}End If"}
            End Select
        End Function

        ''' <summary>
        ''' Determines whether a statement is a guard clause: an <see langword="If"/> whose only statement is a <see langword="Throw"/>,
        ''' or a call to a method whose name starts with <c>ThrowIf</c> (such as <c>ArgumentNullException.ThrowIfNull</c> or <c>Me.ThrowIfDisposed</c>).
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the statement is a guard clause; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsGuard(statement As StatementSyntax) As Boolean

            Dim multiLine As MultiLineIfBlockSyntax = TryCast(statement, MultiLineIfBlockSyntax)
            If multiLine IsNot Nothing Then
                Return multiLine.ElseIfBlocks.Count = 0 AndAlso multiLine.ElseBlock Is Nothing AndAlso
                       multiLine.Statements.Count = 1 AndAlso TypeOf multiLine.Statements(0) Is ThrowStatementSyntax
            End If

            Dim singleLine As SingleLineIfStatementSyntax = TryCast(statement, SingleLineIfStatementSyntax)
            If singleLine IsNot Nothing Then
                Return singleLine.ElseClause Is Nothing AndAlso singleLine.Statements.Count = 1 AndAlso TypeOf singleLine.Statements(0) Is ThrowStatementSyntax
            End If

            Dim expression As ExpressionSyntax = If(TypeOf statement Is ExpressionStatementSyntax, DirectCast(statement, ExpressionStatementSyntax).Expression,
                                                    If(TypeOf statement Is CallStatementSyntax, DirectCast(statement, CallStatementSyntax).Invocation, Nothing))
            Dim invocation As InvocationExpressionSyntax = TryCast(expression, InvocationExpressionSyntax)
            Return invocation IsNot Nothing AndAlso UnvalidatedParameterAnalyzer.GetInvokedName(invocation).StartsWith("ThrowIf", StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' Gets the ordinals of the parameters referenced by a statement.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The statement.
        ''' </param>
        '''
        ''' <param name="ordinals">
        ''' The ordinals of the parameters, by name.
        ''' </param>
        '''
        ''' <returns>
        ''' The ordinals of the referenced parameters.
        ''' </returns>
        Private Shared Function GetReferencedOrdinals(statement As StatementSyntax, ordinals As Dictionary(Of String, Integer)) As IEnumerable(Of Integer)

            Dim result As New List(Of Integer)()
            For Each identifier As IdentifierNameSyntax In statement.DescendantNodes().OfType(Of IdentifierNameSyntax)()
                Dim ordinal As Integer
                If ordinals.TryGetValue(identifier.Identifier.ValueText, ordinal) Then
                    result.Add(ordinal)
                End If
            Next identifier
            Return result
        End Function

        ''' <summary>
        ''' Gets the start of the line where a node begins, including the comment and directive lines above it.
        ''' </summary>
        '''
        ''' <param name="node">
        ''' The node.
        ''' </param>
        '''
        ''' <param name="text">
        ''' The source text.
        ''' </param>
        '''
        ''' <returns>
        ''' The start position of the line.
        ''' </returns>
        Private Shared Function GetLineStartBeforeComments(node As SyntaxNode, text As SourceText) As Integer

            Dim token As SyntaxToken = node.GetFirstToken()
            Dim position As Integer = token.SpanStart
            For Each trivia As SyntaxTrivia In token.LeadingTrivia
                If trivia.IsKind(SyntaxKind.CommentTrivia) OrElse trivia.IsKind(SyntaxKind.DocumentationCommentTrivia) OrElse trivia.IsDirective Then
                    position = trivia.SpanStart
                    Exit For
                End If
            Next trivia
            Return text.Lines.GetLineFromPosition(position).Start
        End Function

#End Region

    End Class

End Namespace

#End Region
