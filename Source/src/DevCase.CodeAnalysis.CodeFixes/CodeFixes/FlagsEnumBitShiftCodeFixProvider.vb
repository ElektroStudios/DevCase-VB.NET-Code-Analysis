#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " FlagsEnumBitShiftCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.FlagsEnumWithoutBitShift"/>: 
    ''' converts a single-bit numeric literal into bit-shift syntax (e.g. <c>8</c> → <c>1 &lt;&lt; 3</c>).
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(FlagsEnumBitShiftCodeFixProvider)), [Shared]>
    Public NotInheritable Class FlagsEnumBitShiftCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The title of the code action.
        ''' </summary>
        Private Const Title As String = "Convert to bit-shift syntax"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.FlagsEnumWithoutBitShift)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FlagsEnumBitShiftCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return WellKnownFixAllProviders.BatchFixer
        End Function

        ''' <inheritdoc/>
        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create(FlagsEnumBitShiftCodeFixProvider.Title,
                                      Function(ct As CancellationToken) FlagsEnumBitShiftCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(FlagsEnumBitShiftCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Builds a bit-shift expression such as <c>1L &lt;&lt; 40</c>.
        ''' </summary>
        '''
        ''' <param name="oneLiteral">
        ''' The typed literal "1" to shift (e.g. <c>1</c>, <c>1L</c>, <c>1UI</c>).
        ''' </param>
        '''
        ''' <param name="shiftCount">
        ''' The shift count.
        ''' </param>
        '''
        ''' <returns>
        ''' The bit-shift expression.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="oneLiteral"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentOutOfRangeException">
        ''' Thrown when <paramref name="shiftCount"/> is outside the range 0 to 63.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when <paramref name="oneLiteral"/> is not an integer literal.
        ''' </exception>
        Friend Shared Function BuildShiftExpression(oneLiteral As String, shiftCount As Integer) As BinaryExpressionSyntax

            If String.IsNullOrWhiteSpace(oneLiteral) Then
                Throw New ArgumentNullException(NameOf(oneLiteral))
            End If
            If shiftCount < 0 OrElse shiftCount > 63 Then
                Throw New ArgumentOutOfRangeException(NameOf(shiftCount), shiftCount, "The shift count must be between 0 and 63.")
            End If

            Dim leftToken As SyntaxToken = SyntaxFactory.ParseToken(oneLiteral)
            If Not leftToken.IsKind(SyntaxKind.IntegerLiteralToken) Then
                Throw New ArgumentException($"'{oneLiteral}' is not an integer literal.", NameOf(oneLiteral))
            End If

            Dim left As LiteralExpressionSyntax = SyntaxFactory.NumericLiteralExpression(leftToken.WithTrailingTrivia(SyntaxFactory.Space))
            Dim right As LiteralExpressionSyntax = SyntaxFactory.NumericLiteralExpression(SyntaxFactory.Literal(shiftCount))
            Dim shiftOperator As SyntaxToken = SyntaxFactory.Token(SyntaxKind.LessThanLessThanToken).WithTrailingTrivia(SyntaxFactory.Space)

            Return SyntaxFactory.LeftShiftExpression(left, shiftOperator, right)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Applies the fix to the literal reported by the specified diagnostic.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic, which carries the shift count and the typed literal.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim literal As LiteralExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of LiteralExpressionSyntax)(root, diagnostic.Location.SourceSpan)

            Dim shiftCount As Integer = Integer.Parse(diagnostic.Properties(FlagsEnumBitShiftAnalyzer.ShiftCountPropertyKey), Globalization.CultureInfo.InvariantCulture)
            Dim oneLiteral As String = diagnostic.Properties(FlagsEnumBitShiftAnalyzer.OneLiteralPropertyKey)

            Dim shift As BinaryExpressionSyntax = FlagsEnumBitShiftCodeFixProvider.BuildShiftExpression(oneLiteral, shiftCount).WithTriviaFrom(literal)
            Return document.WithSyntaxRoot(root.ReplaceNode(literal, shift))
        End Function

#End Region

    End Class

End Namespace

#End Region
