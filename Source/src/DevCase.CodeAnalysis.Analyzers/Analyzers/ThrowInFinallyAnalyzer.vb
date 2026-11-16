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

#Region " ThrowInFinallyAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="Throw"/> statements inside a <see langword="Finally"/> block (rule <see cref="DiagnosticIds.ThrowInFinally"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Not reported: a <see langword="Throw"/> inside the <see langword="Try"/> section of a nested <see langword="Try"/> block with 
    ''' <see langword="Catch"/> blocks (it is handled inside the <see langword="Finally"/> block), and a <see langword="Throw"/> 
    ''' inside a lambda declared in the <see langword="Finally"/> block (it runs later, if ever).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThrowInFinallyAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThrowInFinally)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThrowInFinallyAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf ThrowInFinallyAnalyzer.AnalyzeThrow, SyntaxKind.ThrowStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a <see langword="Throw"/> statement that leaves a <see langword="Finally"/> block.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeThrow(context As SyntaxNodeAnalysisContext)

            Dim current As SyntaxNode = context.Node.Parent
            While current IsNot Nothing AndAlso TypeOf current IsNot LambdaExpressionSyntax AndAlso TypeOf current IsNot MethodBlockBaseSyntax
                If TypeOf current Is FinallyBlockSyntax Then
                    If Not UtilAnalyzerSyntax.IsCaughtLocally(context.Node, current) Then
                        context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThrowInFinally, context.Node.GetLocation()))
                    End If
                    Exit Sub
                End If
                current = current.Parent
            End While
        End Sub

#End Region

    End Class

End Namespace

#End Region
