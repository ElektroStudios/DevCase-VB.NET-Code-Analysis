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

#Region " PublicMemberDocumentationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports declarations with the <see langword="Public"/> modifier that have no XML documentation comment 
    ''' (rule <see cref="DiagnosticIds.MissingXmlDocumentation"/>). An <c>''' &lt;inheritdoc/&gt;</c> comment is accepted.
    ''' </summary>
    '''
    ''' <remarks>
    ''' A partial type is reported on one declaration only, and not at all when any of its declarations is documented
    ''' (see <see cref="IsDocumentationOwner"/>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class PublicMemberDocumentationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Fields "

        ''' <summary>
        ''' The declaration statement kinds analyzed by this rule.
        ''' </summary>
        Friend Shared ReadOnly AnalyzedKinds As ImmutableArray(Of SyntaxKind) = ImmutableArray.Create(
            SyntaxKind.ClassStatement, SyntaxKind.StructureStatement, SyntaxKind.ModuleStatement,
            SyntaxKind.InterfaceStatement, SyntaxKind.EnumStatement,
            SyntaxKind.SubStatement, SyntaxKind.FunctionStatement, SyntaxKind.SubNewStatement,
            SyntaxKind.PropertyStatement, SyntaxKind.EventStatement, SyntaxKind.OperatorStatement,
            SyntaxKind.DelegateSubStatement, SyntaxKind.DelegateFunctionStatement,
            SyntaxKind.DeclareSubStatement, SyntaxKind.DeclareFunctionStatement,
            SyntaxKind.FieldDeclaration)

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingXmlDocumentation)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="PublicMemberDocumentationAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf PublicMemberDocumentationAnalyzer.AnalyzeDeclaration, PublicMemberDocumentationAnalyzer.AnalyzedKinds)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a declaration statement.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeDeclaration(context As SyntaxNodeAnalysisContext)

            Dim declaration As SyntaxNode = context.Node
            If Not UtilAnalyzerSyntax.GetModifiers(declaration).Any(SyntaxKind.PublicKeyword) Then
                Exit Sub
            End If

            If UtilAnalyzerSyntax.HasDocumentationComment(declaration) Then
                Exit Sub
            End If

            If TypeOf declaration Is TypeStatementSyntax AndAlso
               Not PublicMemberDocumentationAnalyzer.IsDocumentationOwner(DirectCast(declaration, TypeStatementSyntax), context) Then
                Exit Sub
            End If

            Dim identifier As SyntaxToken = UtilAnalyzerSyntax.GetIdentifierToken(declaration)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingXmlDocumentation, identifier.GetLocation(), identifier.ValueText))
        End Sub

        ''' <summary>
        ''' Determines whether a type declaration is the one that must hold the XML documentation of its type.
        ''' </summary>
        '''
        ''' <remarks>
        ''' A partial type is documented once: documenting more than one of its declarations produces the compiler warning 
        ''' BC42314 (XML comment cannot be applied more than once on a partial class), and the comments are ignored. 
        ''' So no declaration is reported when any declaration of the type is documented, and otherwise only one declaration 
        ''' is reported: the first <see langword="Public"/> declaration without the <see langword="Partial"/> modifier 
        ''' (the main declaration), or else the first <see langword="Public"/> declaration ordered by file path and position. 
        ''' Declarations in generated files are never reported.
        ''' </remarks>
        '''
        ''' <param name="declaration">
        ''' The type declaration statement, which has the <see langword="Public"/> modifier and no documentation.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the declaration must be reported; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsDocumentationOwner(declaration As TypeStatementSyntax, context As SyntaxNodeAnalysisContext) As Boolean

            Dim type As INamedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(declaration, context.CancellationToken)
            If type Is Nothing OrElse type.DeclaringSyntaxReferences.Length <= 1 Then
                Return True
            End If

            Dim headers As New List(Of TypeStatementSyntax)()
            For Each reference As SyntaxReference In type.DeclaringSyntaxReferences
                Dim node As SyntaxNode = reference.GetSyntax(context.CancellationToken)
                Dim header As TypeStatementSyntax = If(TypeOf node Is TypeBlockSyntax, DirectCast(node, TypeBlockSyntax).BlockStatement, TryCast(node, TypeStatementSyntax))
                If header Is Nothing Then
                    Continue For
                End If
                If UtilAnalyzerSyntax.HasDocumentationComment(header) Then
                    Return False ' Another declaration already documents the type.
                End If
                If header.Modifiers.Any(SyntaxKind.PublicKeyword) AndAlso Not UtilAnalyzerSyntax.IsGeneratedFilePath(header.SyntaxTree.FilePath) Then
                    headers.Add(header)
                End If
            Next reference

            Dim owner As TypeStatementSyntax = headers.
                OrderBy(Function(h As TypeStatementSyntax) If(h.Modifiers.Any(SyntaxKind.PartialKeyword), 1, 0)).
                ThenBy(Function(h As TypeStatementSyntax) h.SyntaxTree.FilePath, StringComparer.Ordinal).
                ThenBy(Function(h As TypeStatementSyntax) h.SpanStart).
                FirstOrDefault()
            Return owner IsNot Nothing AndAlso owner.SyntaxTree Is declaration.SyntaxTree AndAlso owner.Span = declaration.Span
        End Function

#End Region

    End Class

End Namespace

#End Region
