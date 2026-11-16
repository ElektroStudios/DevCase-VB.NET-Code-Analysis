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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " GoToStatementAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="GoTo"/> statements (rule <see cref="DiagnosticIds.GoToStatement"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' <c>On Error GoTo</c> is a different statement, reported by rule <see cref="DiagnosticIds.OnErrorStatement"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class GoToStatementAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.GoToStatement)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="GoToStatementAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf GoToStatementAnalyzer.AnalyzeGoTo, SyntaxKind.GoToStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a <see langword="GoTo"/> statement.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeGoTo(context As SyntaxNodeAnalysisContext)

            Dim statement As GoToStatementSyntax = DirectCast(context.Node, GoToStatementSyntax)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.GoToStatement, statement.GetLocation(), statement.Label.ToString()))
        End Sub

#End Region

    End Class

End Namespace

#End Region
