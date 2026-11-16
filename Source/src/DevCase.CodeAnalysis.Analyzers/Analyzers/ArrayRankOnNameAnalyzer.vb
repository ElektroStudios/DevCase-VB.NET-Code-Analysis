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
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ArrayRankOnNameAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports array designators written on the name of a variable, field or parameter (<c>Dim values() As Integer</c>) instead of 
    ''' on its type (<c>Dim values As Integer()</c>) (rule <see cref="DiagnosticIds.ArrayRankOnVariable"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Both forms declare exactly the same type. Not reported, because they cannot be written on the type: declarations with bounds 
    ''' (<c>Dim values(9) As Integer</c>), names without an <c>As</c> clause, nullable names (<c>value?</c>), declarations whose type is 
    ''' already an array, and declarations that share one <c>As</c> clause between several names (<c>Dim a(), b As Integer</c>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ArrayRankOnNameAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ArrayRankOnVariable)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ArrayRankOnNameAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf ArrayRankOnNameAnalyzer.AnalyzeIdentifier, SyntaxKind.ModifiedIdentifier)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the type of the <c>As</c> clause that applies to a modified identifier, when the array designator can be moved to it.
        ''' </summary>
        ''' 
        ''' <param name="identifier">
        ''' The modified identifier.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The type syntax, or <see langword="Nothing"/> if the designator cannot be moved.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="identifier"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetMovableTarget(identifier As ModifiedIdentifierSyntax) As TypeSyntax

            If identifier Is Nothing Then
                Throw New ArgumentNullException(NameOf(identifier))
            End If

            If identifier.ArrayRankSpecifiers.Count = 0 OrElse identifier.ArrayBounds IsNot Nothing OrElse
               Not identifier.Nullable.IsKind(SyntaxKind.None) Then
                Return Nothing
            End If

            Dim asClause As SimpleAsClauseSyntax
            Dim declarator As VariableDeclaratorSyntax = TryCast(identifier.Parent, VariableDeclaratorSyntax)
            If declarator IsNot Nothing Then
                If declarator.Names.Count <> 1 Then
                    Return Nothing
                End If
                asClause = TryCast(declarator.AsClause, SimpleAsClauseSyntax)
            ElseIf TypeOf identifier.Parent Is ParameterSyntax Then
                asClause = DirectCast(identifier.Parent, ParameterSyntax).AsClause
            Else
                Return Nothing
            End If

            Dim movable As Boolean = asClause IsNot Nothing AndAlso TypeOf asClause.Type IsNot ArrayTypeSyntax AndAlso TypeOf asClause.Type IsNot NullableTypeSyntax
            Return If(movable, asClause.Type, Nothing)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a modified identifier whose array designator can be moved to the type.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeIdentifier(context As SyntaxNodeAnalysisContext)

            Dim identifier As ModifiedIdentifierSyntax = DirectCast(context.Node, ModifiedIdentifierSyntax)
            If ArrayRankOnNameAnalyzer.GetMovableTarget(identifier) Is Nothing Then
                Exit Sub
            End If

            Dim designator As String = String.Concat(identifier.ArrayRankSpecifiers.Select(Function(r As ArrayRankSpecifierSyntax) r.ToString()))
            Dim span As TextSpan = TextSpan.FromBounds(identifier.Identifier.SpanStart, identifier.ArrayRankSpecifiers.Last().Span.End)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ArrayRankOnVariable, Location.Create(identifier.SyntaxTree, span),
                                                       designator, identifier.Identifier.ValueText))
        End Sub

#End Region

    End Class

End Namespace

#End Region
