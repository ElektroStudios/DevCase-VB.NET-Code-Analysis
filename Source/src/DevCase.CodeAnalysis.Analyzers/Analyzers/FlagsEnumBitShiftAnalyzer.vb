#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " FlagsEnumBitShiftAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports members of <c>&lt;Flags&gt;</c> enumerations that declare a single-bit value with a direct numeric literal 
    ''' instead of the bit-shift operator (rule <see cref="DiagnosticIds.FlagsEnumWithoutBitShift"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' The rule is <b>disabled by default</b> because it produces noise on legacy P/Invoke flag enumerations. 
    ''' Enable it per project with the <c>.editorconfig</c> option <c>devcase_analyzers.dca010_enabled = true</c>.
    ''' <para></para>
    ''' Only literals whose value is a power of two are reported; zero and composite values (e.g. 3) are left untouched.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class FlagsEnumBitShiftAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property key that stores the shift count.
        ''' </summary>
        Friend Const ShiftCountPropertyKey As String = "ShiftCount"

        ''' <summary>
        ''' The diagnostic property key that stores the typed literal "1" to shift (e.g. <c>1</c>, <c>1L</c>, <c>1UI</c>).
        ''' </summary>
        Friend Const OneLiteralPropertyKey As String = "OneLiteral"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.FlagsEnumWithoutBitShift)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FlagsEnumBitShiftAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf FlagsEnumBitShiftAnalyzer.AnalyzeEnumBlock, SyntaxKind.EnumBlock)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the typed literal "1" that must be shifted for the specified enumeration underlying type.
        ''' </summary>
        '''
        ''' <param name="underlyingType">
        ''' The special type of the enumeration underlying type.
        ''' </param>
        '''
        ''' <returns>
        ''' The literal text (e.g. <c>1L</c> for <see cref="Long"/>), so that the shift is evaluated in the correct width.
        ''' </returns>
        Friend Shared Function GetOneLiteral(underlyingType As SpecialType) As String

            Select Case underlyingType
                Case SpecialType.System_Int64
                    Return "1L"
                Case SpecialType.System_UInt64
                    Return "1UL"
                Case SpecialType.System_UInt32
                    Return "1UI"
                Case SpecialType.System_Int16
                    Return "1S"
                Case SpecialType.System_UInt16
                    Return "1US"
                Case Else
                    Return "1"
            End Select
        End Function

        ''' <summary>
        ''' Converts an enumeration constant to its raw unsigned bit pattern, according to the width of the underlying type.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The boxed constant value.
        ''' </param>
        '''
        ''' <param name="underlyingType">
        ''' The special type of the enumeration underlying type.
        ''' </param>
        '''
        ''' <returns>
        ''' The raw bit pattern.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="value"/> is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentOutOfRangeException">
        ''' Thrown when <paramref name="underlyingType"/> is not an integral type.
        ''' </exception>
        Friend Shared Function GetBitPattern(value As Object, underlyingType As SpecialType) As ULong

            If value Is Nothing Then
                Throw New ArgumentNullException(NameOf(value))
            End If

            Select Case underlyingType
                Case SpecialType.System_Byte
                    Return DirectCast(value, Byte)
                Case SpecialType.System_SByte
                    Return CULng(DirectCast(value, SByte) And &HFF)
                Case SpecialType.System_Int16
                    Return CULng(DirectCast(value, Short) And &HFFFF)
                Case SpecialType.System_UInt16
                    Return DirectCast(value, UShort)
                Case SpecialType.System_Int32
                    Return CULng(DirectCast(value, Integer) And &HFFFFFFFFL)
                Case SpecialType.System_UInt32
                    Return DirectCast(value, UInteger)
                Case SpecialType.System_Int64
                    Return BitConverter.ToUInt64(BitConverter.GetBytes(DirectCast(value, Long)), 0)
                Case SpecialType.System_UInt64
                    Return DirectCast(value, ULong)
                Case Else
                    Throw New ArgumentOutOfRangeException(NameOf(underlyingType), underlyingType, "The type is not an integral enumeration type.")
            End Select
        End Function

        ''' <summary>
        ''' Gets the shift count of a single-bit value.
        ''' </summary>
        '''
        ''' <param name="bits">
        ''' The raw bit pattern.
        ''' </param>
        '''
        ''' <returns>
        ''' The zero-based index of the only set bit, or -1 if <paramref name="bits"/> is zero or has more than one bit set.
        ''' </returns>
        Friend Shared Function GetSingleBitIndex(bits As ULong) As Integer

            If bits = 0UL OrElse (bits And (bits - 1UL)) <> 0UL Then
                Return -1
            End If

            Dim index As Integer = 0
            Dim remaining As ULong = bits
            While remaining > 1UL
                remaining >>= 1
                index += 1
            End While
            Return index
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes an enumeration block.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeEnumBlock(context As SyntaxNodeAnalysisContext)

            If Not DevCaseAnalyzerOptions.GetBoolean(context.Options.AnalyzerConfigOptionsProvider, context.Node.SyntaxTree,
                                                     DevCaseAnalyzerOptions.FlagsEnumBitShiftEnabledKey, defaultValue:=False) Then
                Exit Sub ' Disabled by default; see the class remarks.
            End If

            Dim enumBlock As EnumBlockSyntax = DirectCast(context.Node, EnumBlockSyntax)
            Dim enumSymbol As INamedTypeSymbol = context.SemanticModel.GetDeclaredSymbol(enumBlock.EnumStatement, context.CancellationToken)
            If enumSymbol Is Nothing OrElse enumSymbol.EnumUnderlyingType Is Nothing Then
                Exit Sub
            End If

            Dim hasFlags As Boolean = enumSymbol.GetAttributes().Any(
                Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso
                                             a.AttributeClass.ToDisplayString().Equals("System.FlagsAttribute", StringComparison.Ordinal))
            If Not hasFlags Then
                Exit Sub
            End If

            Dim underlyingType As SpecialType = enumSymbol.EnumUnderlyingType.SpecialType
            Dim oneLiteral As String = FlagsEnumBitShiftAnalyzer.GetOneLiteral(underlyingType)

            For Each memberStatement As StatementSyntax In enumBlock.Members
                Dim member As EnumMemberDeclarationSyntax = TryCast(memberStatement, EnumMemberDeclarationSyntax)
                If member Is Nothing OrElse member.Initializer Is Nothing Then
                    Continue For
                End If

                Dim value As ExpressionSyntax = member.Initializer.Value
                If Not value.IsKind(SyntaxKind.NumericLiteralExpression) Then
                    Continue For ' Already a shift, a combination (A Or B) or another expression.
                End If

                Dim field As IFieldSymbol = context.SemanticModel.GetDeclaredSymbol(member, context.CancellationToken)
                If field Is Nothing OrElse Not field.HasConstantValue OrElse field.ConstantValue Is Nothing Then
                    Continue For
                End If

                Dim bitIndex As Integer = FlagsEnumBitShiftAnalyzer.GetSingleBitIndex(FlagsEnumBitShiftAnalyzer.GetBitPattern(field.ConstantValue, underlyingType))
                If bitIndex < 0 Then
                    Continue For
                End If

                ' "1 << 7" evaluates to 128, which does not fit in SByte.
                If underlyingType = SpecialType.System_SByte AndAlso bitIndex = 7 Then
                    Continue For
                End If

                Dim suggestion As String = $"{oneLiteral} << {bitIndex.ToString(CultureInfo.InvariantCulture)}"
                Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                    Add(FlagsEnumBitShiftAnalyzer.ShiftCountPropertyKey, bitIndex.ToString(CultureInfo.InvariantCulture)).
                    Add(FlagsEnumBitShiftAnalyzer.OneLiteralPropertyKey, oneLiteral)

                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.FlagsEnumWithoutBitShift, value.GetLocation(), properties,
                                                           member.Identifier.ValueText, suggestion))
            Next memberStatement
        End Sub

#End Region

    End Class

End Namespace

#End Region
