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

#Region " RethrowOnlyCatchAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="Catch"/> blocks whose only statement is <c>Throw</c> (rule <see cref="DiagnosticIds.RethrowOnlyCatch"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Only the trailing group of rethrow-only blocks of a <see langword="Try"/> statement is reported (one diagnostic per statement): 
    ''' a rethrow-only block followed by another <see langword="Catch"/> block is meaningful, because it prevents the later block from 
    ''' handling that exception type. Blocks with a <see langword="When"/> filter are not reported. 
    ''' <c>Throw ex</c> (where <c>ex</c> is the caught exception) is reported too: it rethrows the same exception, and also discards its 
    ''' original stack trace, so removing the block is strictly better.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class RethrowOnlyCatchAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.RethrowOnlyCatch)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RethrowOnlyCatchAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf RethrowOnlyCatchAnalyzer.AnalyzeTryBlock, SyntaxKind.TryBlock)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the index of the first <see langword="Catch"/> block of the trailing group of rethrow-only blocks.
        ''' </summary>
        ''' 
        ''' <param name="tryBlock">
        ''' The <see langword="Try"/> block.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The index, or the number of <see langword="Catch"/> blocks if the last one is meaningful.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="tryBlock"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetFirstRethrowOnlyIndex(tryBlock As TryBlockSyntax) As Integer

            If tryBlock Is Nothing Then
                Throw New ArgumentNullException(NameOf(tryBlock))
            End If

            Dim index As Integer = tryBlock.CatchBlocks.Count
            While index > 0 AndAlso RethrowOnlyCatchAnalyzer.IsRethrowOnly(tryBlock.CatchBlocks(index - 1))
                index -= 1
            End While
            Return index
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a <see langword="Try"/> block.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeTryBlock(context As SyntaxNodeAnalysisContext)

            Dim tryBlock As TryBlockSyntax = DirectCast(context.Node, TryBlockSyntax)
            Dim index As Integer = RethrowOnlyCatchAnalyzer.GetFirstRethrowOnlyIndex(tryBlock)
            If index < tryBlock.CatchBlocks.Count Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.RethrowOnlyCatch, tryBlock.CatchBlocks(index).CatchStatement.GetLocation()))
            End If
        End Sub

        ''' <summary>
        ''' Determines whether a <see langword="Catch"/> block only rethrows the exception.
        ''' </summary>
        ''' 
        ''' <param name="catchBlock">
        ''' The <see langword="Catch"/> block.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the block has no filter and its only statement is <c>Throw</c> or <c>Throw ex</c> 
        ''' (the caught exception variable); otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsRethrowOnly(catchBlock As CatchBlockSyntax) As Boolean

            If catchBlock.CatchStatement.WhenClause IsNot Nothing OrElse catchBlock.Statements.Count <> 1 Then
                Return False
            End If
            Dim throwStatement As ThrowStatementSyntax = TryCast(catchBlock.Statements(0), ThrowStatementSyntax)
            If throwStatement Is Nothing Then
                Return False
            End If

            ' "Throw" rethrows the exception.
            If throwStatement.Expression Is Nothing Then
                Return True
            End If

            ' "Throw ex" (the caught variable) rethrows it too, but resets its stack trace.
            Dim thrownName As IdentifierNameSyntax = TryCast(UtilAnalyzerSyntax.SkipParentheses(throwStatement.Expression), IdentifierNameSyntax)
            Dim catchName As IdentifierNameSyntax = catchBlock.CatchStatement.IdentifierName
            Return thrownName IsNot Nothing AndAlso catchName IsNot Nothing AndAlso
                   thrownName.Identifier.ValueText.Equals(catchName.Identifier.ValueText, StringComparison.OrdinalIgnoreCase)
        End Function

#End Region

    End Class

End Namespace

#End Region
