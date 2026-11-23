#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ExplicitConstructorAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports classes that do not explicitly declare an instance constructor (rule <see cref="DiagnosticIds.MissingExplicitConstructor"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Excluded on purpose:
    ''' <list type="bullet">
    '''   <item><description>Modules (they cannot declare instance constructors).</description></item>
    '''   <item><description>Partial classes whose constructor is declared in another part (resolved through the type symbol).</description></item>
    '''   <item><description>Classes marked with <c>DesignerGenerated</c> (Windows Forms): the compiler synthesizes a constructor that calls 
    '''   <c>InitializeComponent</c>, and an explicit empty constructor would leave the form blank.</description></item>
    ''' </list>
    ''' Partial classes are reported only once, on their first non-generated declaration.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ExplicitConstructorAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingExplicitConstructor)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ExplicitConstructorAnalyzer"/> class.
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
            context.RegisterSymbolAction(UtilAnalyzerFiles.SkipExcludedSymbols(AddressOf ExplicitConstructorAnalyzer.AnalyzeNamedType), SymbolKind.NamedType)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a named type symbol.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The symbol analysis context.
        ''' </param>
        Private Shared Sub AnalyzeNamedType(context As SymbolAnalysisContext)

            Dim type As INamedTypeSymbol = DirectCast(context.Symbol, INamedTypeSymbol)
            If type.TypeKind <> TypeKind.Class OrElse type.IsImplicitlyDeclared Then
                Exit Sub
            End If

            If type.InstanceConstructors.Any(Function(c As IMethodSymbol) Not c.IsImplicitlyDeclared) Then
                Exit Sub
            End If

            ' <ComImport> classes: the Visual Basic compiler discards the body of every member and the runtime creates the COM object, 
            ' so an explicit constructor would be dead code (rule DCA054 reports code in those classes).
            If UtilAnalyzerSemantic.IsDesignerGenerated(type) OrElse UtilAnalyzerSemantic.IsComImport(type) Then
                Exit Sub
            End If

            Dim primary As SyntaxReference = UtilAnalyzerSemantic.GetPrimaryDeclaration(type)
            If primary Is Nothing OrElse Not context.Compilation.ContainsSyntaxTree(primary.SyntaxTree) Then
                Exit Sub
            End If

            Dim declaration As StatementSyntax = UtilTypeDeclarations.GetDeclarationNode(primary.GetSyntax(context.CancellationToken))
            If TypeOf declaration IsNot ClassBlockSyntax Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingExplicitConstructor,
                                                       UtilTypeDeclarations.GetIdentifierLocation(declaration),
                                                       type.Name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
