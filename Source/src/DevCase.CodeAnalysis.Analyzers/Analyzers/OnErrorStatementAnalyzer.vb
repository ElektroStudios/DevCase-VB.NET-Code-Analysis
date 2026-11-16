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

#Region " OnErrorStatementAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports the unstructured error handling statements <c>On Error GoTo</c> and <c>On Error Resume Next</c> 
    ''' (rule <see cref="DiagnosticIds.OnErrorStatement"/>).
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class OnErrorStatementAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.OnErrorStatement)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="OnErrorStatementAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf OnErrorStatementAnalyzer.AnalyzeOnError,
                                             SyntaxKind.OnErrorGoToLabelStatement, SyntaxKind.OnErrorGoToZeroStatement,
                                             SyntaxKind.OnErrorGoToMinusOneStatement, SyntaxKind.OnErrorResumeNextStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an <c>On Error</c> statement.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeOnError(context As SyntaxNodeAnalysisContext)

            Dim text As String = If(context.Node.IsKind(SyntaxKind.OnErrorResumeNextStatement), "On Error Resume Next", "On Error GoTo")
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.OnErrorStatement, context.Node.GetLocation(), text))
        End Sub

#End Region

    End Class

End Namespace

#End Region
