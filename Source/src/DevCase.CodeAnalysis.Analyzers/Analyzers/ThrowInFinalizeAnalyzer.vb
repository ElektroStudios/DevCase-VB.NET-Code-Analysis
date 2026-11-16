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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ThrowInFinalizeAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="Throw"/> statements inside the <c>Finalize</c> method (rule <see cref="DiagnosticIds.ThrowInFinalize"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The method must be <c>Overrides Sub Finalize()</c> without parameters. Exceptions thrown inside the <see langword="Try"/> section of a 
    ''' <see langword="Try"/> block with <see langword="Catch"/> blocks, and inside lambdas, are not reported.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThrowInFinalizeAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThrowInFinalize)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThrowInFinalizeAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf ThrowInFinalizeAnalyzer.AnalyzeThrow, SyntaxKind.ThrowStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a <see langword="Throw"/> statement that leaves the <c>Finalize</c> method.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeThrow(context As SyntaxNodeAnalysisContext)

            Dim method As MethodBlockSyntax = TryCast(UtilAnalyzerSyntax.GetExecutableBoundary(context.Node), MethodBlockSyntax)
            If method Is Nothing OrElse Not method.IsKind(SyntaxKind.SubBlock) Then
                Exit Sub
            End If

            Dim statement As MethodStatementSyntax = method.SubOrFunctionStatement
            Dim isFinalize As Boolean = statement.Identifier.ValueText.Equals("Finalize", StringComparison.OrdinalIgnoreCase) AndAlso
                                        statement.Modifiers.Any(Function(t As SyntaxToken) t.IsKind(SyntaxKind.OverridesKeyword)) AndAlso
                                        (statement.ParameterList Is Nothing OrElse statement.ParameterList.Parameters.Count = 0)
            If isFinalize AndAlso Not UtilAnalyzerSyntax.IsCaughtLocally(context.Node, method) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThrowInFinalize, context.Node.GetLocation()))
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
