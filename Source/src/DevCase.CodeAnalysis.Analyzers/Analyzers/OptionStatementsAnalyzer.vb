#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " OptionStatementsAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports source files that do not explicitly declare <c>Option Strict On</c>, <c>Option Explicit On</c> 
    ''' and <c>Option Infer Off</c> (rule <see cref="DiagnosticIds.MissingOptionStatements"/>).
    ''' </summary>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class OptionStatementsAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property key that stores the comma-separated list of missing statements.
        ''' </summary>
        Friend Const MissingStatementsPropertyKey As String = "MissingStatements"

        ''' <summary>
        ''' The text of the <c>Option Strict On</c> statement.
        ''' </summary>
        Friend Const OptionStrictOn As String = "Option Strict On"

        ''' <summary>
        ''' The text of the <c>Option Explicit On</c> statement.
        ''' </summary>
        Friend Const OptionExplicitOn As String = "Option Explicit On"

        ''' <summary>
        ''' The text of the <c>Option Infer Off</c> statement.
        ''' </summary>
        Friend Const OptionInferOff As String = "Option Infer Off"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingOptionStatements)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="OptionStatementsAnalyzer"/> class.
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
            context.RegisterSyntaxTreeAction(AddressOf OptionStatementsAnalyzer.AnalyzeTree)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether an <see langword="Option"/> statement satisfies one of the required declarations.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The <see langword="Option"/> statement.
        ''' </param>
        '''
        ''' <returns>
        ''' The text of the required declaration satisfied by the statement, or <see langword="Nothing"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetSatisfiedRequirement(statement As OptionStatementSyntax) As String

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If

            ' "Option Strict" and "Option Explicit" without a value mean "On".
            Dim valueKind As SyntaxKind = statement.ValueKeyword.Kind()
            Dim isOn As Boolean = valueKind = SyntaxKind.OnKeyword OrElse valueKind = SyntaxKind.None
            Dim isOff As Boolean = valueKind = SyntaxKind.OffKeyword

            Select Case statement.NameKeyword.Kind()
                Case SyntaxKind.StrictKeyword
                    Return If(isOn, OptionStatementsAnalyzer.OptionStrictOn, Nothing)
                Case SyntaxKind.ExplicitKeyword
                    Return If(isOn, OptionStatementsAnalyzer.OptionExplicitOn, Nothing)
                Case SyntaxKind.InferKeyword
                    Return If(isOff, OptionStatementsAnalyzer.OptionInferOff, Nothing)
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Gets the required <see langword="Option"/> declarations that a compilation unit does not satisfy.
        ''' </summary>
        '''
        ''' <param name="root">
        ''' The compilation unit.
        ''' </param>
        '''
        ''' <returns>
        ''' The missing declarations, in canonical order.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="root"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetMissingStatements(root As CompilationUnitSyntax) As List(Of String)

            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If

            Dim satisfied As New HashSet(Of String)(StringComparer.Ordinal)
            For Each statement As OptionStatementSyntax In root.Options
                Dim requirement As String = OptionStatementsAnalyzer.GetSatisfiedRequirement(statement)
                If requirement IsNot Nothing Then
                    satisfied.Add(requirement)
                End If
            Next statement

            Dim missing As New List(Of String)()
            For Each requirement As String In {OptionStatementsAnalyzer.OptionStrictOn, OptionStatementsAnalyzer.OptionExplicitOn, OptionStatementsAnalyzer.OptionInferOff}
                If Not satisfied.Contains(requirement) Then
                    missing.Add(requirement)
                End If
            Next requirement
            Return missing
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a syntax tree.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax tree analysis context.
        ''' </param>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the syntax tree root is not a Visual Basic compilation unit.
        ''' </exception>
        Private Shared Sub AnalyzeTree(context As SyntaxTreeAnalysisContext)

            Dim root As CompilationUnitSyntax = TryCast(context.Tree.GetRoot(context.CancellationToken), CompilationUnitSyntax)
            If root Is Nothing Then
                Throw New InvalidOperationException("The syntax tree root is not a Visual Basic compilation unit.")
            End If

            Dim missing As List(Of String) = OptionStatementsAnalyzer.GetMissingStatements(root)
            If missing.Count = 0 Then
                Exit Sub
            End If

            Dim location As Location
            If root.Options.Count > 0 Then
                location = Location.Create(context.Tree, TextSpan.FromBounds(root.Options.First().SpanStart, root.Options.Last().Span.End))
            Else
                Dim firstToken As SyntaxToken = root.GetFirstToken(includeZeroWidth:=False)
                location = If(firstToken.IsKind(SyntaxKind.None) OrElse firstToken.IsKind(SyntaxKind.EndOfFileToken),
                              Location.Create(context.Tree, New TextSpan(0, 0)),
                              firstToken.GetLocation())
            End If

            Dim properties As ImmutableDictionary(Of String, String) =
                ImmutableDictionary(Of String, String).Empty.Add(OptionStatementsAnalyzer.MissingStatementsPropertyKey, String.Join(",", missing))

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingOptionStatements, location, properties, String.Join(", ", missing)))
        End Sub

#End Region

    End Class

End Namespace

#End Region
