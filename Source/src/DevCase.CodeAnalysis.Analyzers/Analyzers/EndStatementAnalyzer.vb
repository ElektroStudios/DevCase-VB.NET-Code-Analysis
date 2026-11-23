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

#Region " EndStatementAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports the <see langword="End"/> statement (rule <see cref="DiagnosticIds.EndStatement"/>), which terminates the process 
    ''' without running <see langword="Finally"/> blocks, <c>Dispose</c> methods or <c>FormClosing</c> events.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Only the standalone <see langword="End"/> statement is reported; block terminators such as <c>End If</c> or <c>End Sub</c> are different statements.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class EndStatementAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.EndStatement)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="EndStatementAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf EndStatementAnalyzer.AnalyzeEndStatement), SyntaxKind.EndStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an <see langword="End"/> statement.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeEndStatement(context As SyntaxNodeAnalysisContext)

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.EndStatement, context.Node.GetLocation()))
        End Sub

#End Region

    End Class

End Namespace

#End Region
