#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers
Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

Imports Xunit

#End Region

#Region " UtilityTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Unit tests of the shared helper methods.
    ''' </summary>
    Public NotInheritable Class UtilityTests

#Region " Fields "

        ''' <summary>
        ''' The atoms expected from flattening <c>a OrElse b OrElse c OrElse (d AndAlso e)</c>, in source order.
        ''' </summary>
        Private Shared ReadOnly ExpectedFlattenedAtoms As String() = {"a", "b", "c", "d AndAlso e"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="UtilityTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies the index returned for single-bit and non-single-bit values.
        ''' </summary>
        '''
        ''' <param name="bits">
        ''' The value to inspect.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' The expected bit index, or -1.
        ''' </param>
        <Theory>
        <InlineData(0UL, -1)>
        <InlineData(1UL, 0)>
        <InlineData(2UL, 1)>
        <InlineData(3UL, -1)>
        <InlineData(1024UL, 10)>
        <InlineData(&H8000000000000000UL, 63)>
        <InlineData(&HFFFFFFFFFFFFFFFFUL, -1)>
        Public Sub GetSingleBitIndex(bits As ULong, expected As Integer)
            Assert.Equal(expected, FlagsEnumBitShiftAnalyzer.GetSingleBitIndex(bits))
        End Sub

        ''' <summary>
        ''' Verifies that negative values are reinterpreted as unsigned bit patterns of their own width.
        ''' </summary>
        <Fact>
        Public Sub GetBitPattern_ReinterpretsSignedValuesByWidth()
            Assert.Equal(&H80UL, FlagsEnumBitShiftAnalyzer.GetBitPattern(CSByte(-128), SpecialType.System_SByte))
            Assert.Equal(&H8000UL, FlagsEnumBitShiftAnalyzer.GetBitPattern(Short.MinValue, SpecialType.System_Int16))
            Assert.Equal(&H80000000UL, FlagsEnumBitShiftAnalyzer.GetBitPattern(Integer.MinValue, SpecialType.System_Int32))
            Assert.Equal(&H8000000000000000UL, FlagsEnumBitShiftAnalyzer.GetBitPattern(Long.MinValue, SpecialType.System_Int64))
            Assert.Equal(255UL, FlagsEnumBitShiftAnalyzer.GetBitPattern(CByte(255), SpecialType.System_Byte))
            Assert.Equal(ULong.MaxValue, FlagsEnumBitShiftAnalyzer.GetBitPattern(ULong.MaxValue, SpecialType.System_UInt64))
            Assert.Throws(Of ArgumentNullException)(Function() FlagsEnumBitShiftAnalyzer.GetBitPattern(Nothing, SpecialType.System_Int32))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() FlagsEnumBitShiftAnalyzer.GetBitPattern(1.0R, SpecialType.System_Double))
        End Sub

        ''' <summary>
        ''' Verifies the typed literal <c>1</c> used by the bit-shift expressions of each underlying enumeration type.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The underlying type of the enumeration.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' The expected literal text.
        ''' </param>
        <Theory>
        <InlineData(SpecialType.System_Int32, "1")>
        <InlineData(SpecialType.System_Byte, "1")>
        <InlineData(SpecialType.System_Int64, "1L")>
        <InlineData(SpecialType.System_UInt64, "1UL")>
        <InlineData(SpecialType.System_UInt32, "1UI")>
        <InlineData(SpecialType.System_Int16, "1S")>
        <InlineData(SpecialType.System_UInt16, "1US")>
        Public Sub GetOneLiteral(type As SpecialType, expected As String)
            Assert.Equal(expected, FlagsEnumBitShiftAnalyzer.GetOneLiteral(type))
        End Sub

        ''' <summary>
        ''' Verifies that building a shift expression rejects invalid arguments.
        ''' </summary>
        <Fact>
        Public Sub BuildShiftExpression_ValidatesArguments()
            Assert.Equal("1L << 40", FlagsEnumBitShiftCodeFixProvider.BuildShiftExpression("1L", 40).ToFullString())
            Assert.Throws(Of ArgumentNullException)(Function() FlagsEnumBitShiftCodeFixProvider.BuildShiftExpression(" ", 1))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() FlagsEnumBitShiftCodeFixProvider.BuildShiftExpression("1", 64))
            Assert.Throws(Of ArgumentException)(Function() FlagsEnumBitShiftCodeFixProvider.BuildShiftExpression("x", 1))
        End Sub

        ''' <summary>
        ''' Verifies that a long <c>OrElse</c> chain is flattened in source order without recursion.
        ''' </summary>
        <Fact>
        Public Sub FlattenOrElseChain_KeepsSourceOrder_WithoutRecursion()
            Dim condition As ExpressionSyntax = SyntaxFactory.ParseExpression("(a OrElse (b Or c)) OrElse d AndAlso e")
            Dim atoms As List(Of String) = UtilAnalyzerSyntax.FlattenOrElseChain(condition).Select(Function(x As ExpressionSyntax) x.ToString()).ToList()
            Assert.Equal(UtilityTests.ExpectedFlattenedAtoms, atoms.ToArray())

            ' A very long chain must not overflow the stack.
            Dim longChain As String = String.Join(" OrElse ", Enumerable.Range(0, 3000).Select(Function(i As Integer) $"x{i}"))
            Assert.Equal(3000, UtilAnalyzerSyntax.FlattenOrElseChain(SyntaxFactory.ParseExpression(longChain)).Count)
        End Sub

        ''' <summary>
        ''' Verifies which expressions must be parenthesized before a member access is appended to them.
        ''' </summary>
        '''
        ''' <param name="expression">
        ''' The expression text.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' <see langword="True"/> if the expression must be parenthesized; otherwise, <see langword="False"/>.
        ''' </param>
        <Theory>
        <InlineData("x", False)>
        <InlineData("x.y", False)>
        <InlineData("x.y()", False)>
        <InlineData("(x)", False)>
        <InlineData("If(x, y)", False)>
        <InlineData("CType(x, Task)", False)>
        <InlineData("Me", False)>
        <InlineData("x?.y()", True)>
        <InlineData("New Task(Nothing)", True)>
        <InlineData("a + b", True)>
        <InlineData("Function() x", True)>
        Public Sub RequiresParenthesesForMemberAccess(expression As String, expected As Boolean)
            Assert.Equal(expected, UtilAnalyzerSyntax.RequiresParenthesesForMemberAccess(SyntaxFactory.ParseExpression(expression)))
        End Sub

        ''' <summary>
        ''' Verifies that an <c>Await</c> expression must be parenthesized before a member access is appended to it.
        ''' </summary>
        <Fact>
        Public Sub RequiresParenthesesForMemberAccess_AwaitExpression()
            ' "Await x" is only parsed as an Await expression inside an Async method, so it is built explicitly.
            Dim awaitExpression As ExpressionSyntax = SyntaxFactory.AwaitExpression(SyntaxFactory.IdentifierName("x"))
            Assert.True(UtilAnalyzerSyntax.RequiresParenthesesForMemberAccess(awaitExpression))
        End Sub

        ''' <summary>
        ''' Verifies which file paths are recognized as generated code.
        ''' </summary>
        '''
        ''' <param name="path">
        ''' The file path.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' <see langword="True"/> if the path is generated code; otherwise, <see langword="False"/>.
        ''' </param>
        <Theory>
        <InlineData("C:\src\Form1.Designer.vb", True)>
        <InlineData("C:\src\Settings.designer.vb", True)>
        <InlineData("/src/Api.g.vb", True)>
        <InlineData("/src/Api.g.i.vb", True)>
        <InlineData("Resources.Generated.vb", True)>
        <InlineData("C:\src\Designer.vb", False)>
        <InlineData("C:\src\Form1.vb", False)>
        <InlineData("", False)>
        <InlineData(Nothing, False)>
        Public Sub IsGeneratedFilePath(path As String, expected As Boolean)
            Assert.Equal(expected, UtilAnalyzerSyntax.IsGeneratedFilePath(path))
        End Sub

        ''' <summary>
        ''' Verifies that the end-of-line sequence of a document is detected, with CRLF as the default.
        ''' </summary>
        <Fact>
        Public Sub GetEndOfLine_DetectsLineEndings()
            Assert.Equal(vbCrLf, UtilAnalyzerSyntax.GetEndOfLine(VisualBasicSyntaxTree.ParseText("A" & vbCrLf & "B")))
            Assert.Equal(vbLf, UtilAnalyzerSyntax.GetEndOfLine(VisualBasicSyntaxTree.ParseText("A" & vbLf & "B")))
            Assert.Equal(vbCrLf, UtilAnalyzerSyntax.GetEndOfLine(VisualBasicSyntaxTree.ParseText("A")))
            Assert.Throws(Of ArgumentNullException)(Function() UtilAnalyzerSyntax.GetEndOfLine(Nothing))
        End Sub

        ''' <summary>
        ''' Verifies that every form of a configured awaitable is recognized.
        ''' </summary>
        <Fact>
        Public Sub IsAlreadyConfigured_RecognizesConfiguredForms()
            Assert.True(UtilAnalyzerSyntax.IsAlreadyConfigured(SyntaxFactory.ParseExpression("t.ConfigureAwait(False)")))
            Assert.True(UtilAnalyzerSyntax.IsAlreadyConfigured(SyntaxFactory.ParseExpression("((t.configureawait(True)))")))
            Assert.True(UtilAnalyzerSyntax.IsAlreadyConfigured(SyntaxFactory.ParseExpression("a?.b().ConfigureAwait(False)")))
            Assert.False(UtilAnalyzerSyntax.IsAlreadyConfigured(SyntaxFactory.ParseExpression("ConfigureAwait(False)")))
            Assert.False(UtilAnalyzerSyntax.IsAlreadyConfigured(SyntaxFactory.ParseExpression("t.ConfigureAwait")))
            Assert.Throws(Of ArgumentNullException)(Function() UtilAnalyzerSyntax.IsAlreadyConfigured(Nothing))
        End Sub

        ''' <summary>
        ''' Verifies that splitting leading trivia keeps the preprocessor directives before the declaration trivia.
        ''' </summary>
        <Fact>
        Public Sub SplitLeadingTrivia_KeepsDirectivesInHead()
            Dim tree As SyntaxTree = VisualBasicSyntaxTree.ParseText("#Region "" X """ & vbLf & vbLf & "''' <summary>S</summary>" & vbLf & "Public Class C" & vbLf & "End Class" & vbLf,
                                                                      New VisualBasicParseOptions(documentationMode:=DocumentationMode.Diagnose))
            Dim classBlock As ClassBlockSyntax = tree.GetRoot().DescendantNodes().OfType(Of ClassBlockSyntax)().Single()
            Dim head As SyntaxTriviaList = Nothing
            Dim tail As SyntaxTriviaList = Nothing
            UtilCodeFix.SplitLeadingTrivia(classBlock.GetLeadingTrivia(), head, tail)
            Assert.Contains(head, Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.RegionDirectiveTrivia))
            Assert.Contains(tail, Function(t As SyntaxTrivia) t.IsKind(SyntaxKind.DocumentationCommentTrivia))
            Assert.DoesNotContain(tail, Function(t As SyntaxTrivia) t.IsDirective)
        End Sub

        ''' <summary>
        ''' Verifies that the guard condition and string comparison opportunity types reject invalid arguments.
        ''' </summary>
        <Fact>
        Public Sub GuardCondition_And_Opportunity_ValidateArguments()
            Dim expr As ExpressionSyntax = SyntaxFactory.ParseExpression("x Is Nothing")
            Assert.Throws(Of ArgumentNullException)(Function() New GuardCondition(Nothing, "x", GuardExceptionKind.ArgumentNull, Nothing))
            Assert.Throws(Of ArgumentNullException)(Function() New GuardCondition(expr, " ", GuardExceptionKind.ArgumentNull, Nothing))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() New StringComparisonOpportunity("p", -1, StringComparisonArgumentKind.Comparison, False))
            Assert.Throws(Of ArgumentOutOfRangeException)(Function() New StringComparisonOpportunity("p", 0, StringComparisonArgumentKind.None, False))
        End Sub

        ''' <summary>
        ''' Verifies how identifiers are split into lowercase words for the generated parameter descriptions.
        ''' </summary>
        '''
        ''' <param name="identifier">
        ''' The identifier.
        ''' </param>
        '''
        ''' <param name="expected">
        ''' The expected lowercase words.
        ''' </param>
        <Theory>
        <InlineData("filePath", "file path")>
        <InlineData("maxURLLength", "max URL length")>
        <InlineData("HTMLParser", "HTML parser")>
        <InlineData("_field_name", "field name")>
        <InlineData("item2Count", "item 2 count")>
        <InlineData("x", "x")>
        Public Sub SplitIdentifier(identifier As String, expected As String)
            Assert.Equal(expected, String.Join(" ", UtilDocumentationText.SplitIdentifier(identifier)))
        End Sub

        ''' <summary>
        ''' Verifies that a parameter description is generated from the name alone when the type is unknown.
        ''' </summary>
        <Fact>
        Public Sub GetParameterDescription_WithoutType_UsesTheName()
            Assert.Equal("The source directory.", UtilDocumentationText.GetParameterDescription("sourceDirectory", Nothing))
            Assert.Throws(Of ArgumentNullException)(Function() UtilDocumentationText.GetParameterDescription(" ", Nothing))
        End Sub

#End Region

    End Class

End Namespace

#End Region
