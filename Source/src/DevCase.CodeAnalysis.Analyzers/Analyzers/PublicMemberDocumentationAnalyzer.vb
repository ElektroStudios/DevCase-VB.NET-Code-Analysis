#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic

#End Region

#Region " PublicMemberDocumentationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports declarations with the <see langword="Public"/> modifier that have no XML documentation comment 
    ''' (rule <see cref="DiagnosticIds.MissingXmlDocumentation"/>). An <c>''' &lt;inheritdoc/&gt;</c> comment is accepted.
    ''' </summary>
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

            Dim identifier As SyntaxToken = UtilAnalyzerSyntax.GetIdentifierToken(declaration)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingXmlDocumentation, identifier.GetLocation(), identifier.ValueText))
        End Sub

#End Region

    End Class

End Namespace

#End Region
