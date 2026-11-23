#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " NamespaceDeclarationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports top-level classes, modules, structures, interfaces, enumerations and delegates that are not declared 
    ''' inside a <see langword="Namespace"/> block and therefore belong to the global namespace 
    ''' (rule <see cref="DiagnosticIds.TypeOutsideNamespace"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' In Visual Basic, the project <c>RootNamespace</c> is implicitly prepended to every type. When it is set, a type without 
    ''' a <see langword="Namespace"/> block already lives in that namespace, and wrapping it would nest it a second time 
    ''' (<c>MyApp.MyApp.Type</c>); such types are not reported. The rule targets projects whose root namespace is empty.
    ''' <para></para>
    ''' Types marked with <c>DesignerGenerated</c> and partial types with a part in a generated file are excluded: 
    ''' moving only the hand-written part into a namespace would split the partial type in two, 
    ''' and moving a form would break the references emitted by the designer (e.g. <c>Application.Designer.vb</c>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class NamespaceDeclarationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.TypeOutsideNamespace)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="NamespaceDeclarationAnalyzer"/> class.
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
            context.RegisterSymbolAction(UtilAnalyzerFiles.SkipExcludedSymbols(AddressOf NamespaceDeclarationAnalyzer.AnalyzeNamedType), SymbolKind.NamedType)
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
            If type.ContainingType IsNot Nothing OrElse type.IsImplicitlyDeclared Then
                Exit Sub ' Nested types inherit the namespace of their container.
            End If

            If UtilAnalyzerSemantic.IsDesignerGenerated(type) OrElse UtilAnalyzerSemantic.HasGeneratedDeclaration(type) Then
                Exit Sub
            End If

            Dim primary As SyntaxReference = UtilAnalyzerSemantic.GetPrimaryDeclaration(type)
            If primary Is Nothing OrElse Not context.Compilation.ContainsSyntaxTree(primary.SyntaxTree) Then
                Exit Sub
            End If

            Dim declaration As StatementSyntax = UtilTypeDeclarations.GetDeclarationNode(primary.GetSyntax(context.CancellationToken))
            If declaration Is Nothing OrElse TypeOf declaration.Parent IsNot CompilationUnitSyntax Then
                Exit Sub
            End If

            ' A non-empty project RootNamespace already places the type in a namespace.
            If type.ContainingNamespace Is Nothing OrElse Not type.ContainingNamespace.IsGlobalNamespace Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.TypeOutsideNamespace,
                                                       UtilTypeDeclarations.GetIdentifierLocation(declaration),
                                                       type.Name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
