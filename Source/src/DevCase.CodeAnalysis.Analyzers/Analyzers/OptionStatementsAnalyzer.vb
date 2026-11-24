#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " OptionStatementsAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports source files that are not compiled with <c>Option Strict On</c>, <c>Option Explicit On</c> and 
    ''' <c>Option Infer Off</c> (rule <see cref="DiagnosticIds.MissingOptionStatements"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Each setting is the one of the <see langword="Option"/> statement of the file or, when the file does not declare it, 
    ''' the one of the project: a file without <see langword="Option"/> statements in a project that already uses the 
    ''' required settings is not reported.
    ''' </remarks>
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
            context.RegisterCompilationStartAction(AddressOf OptionStatementsAnalyzer.OnCompilationStart)
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
        ''' The requirement that the statement satisfies (for example <see cref="OptionStrictOn"/>), or 
        ''' <see langword="Nothing"/> if it satisfies none.
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
        ''' Gets the required settings (<c>Option Strict On</c>, <c>Option Explicit On</c>, <c>Option Infer Off</c>) that a 
        ''' source file does not use, with the setting that it uses instead.
        ''' </summary>
        '''
        ''' <remarks>
        ''' Each setting is the one of the <see langword="Option"/> statement of the file or, when the file does not declare it, 
        ''' the one of the project (<c>&lt;OptionStrict&gt;</c>, <c>&lt;OptionExplicit&gt;</c> and <c>&lt;OptionInfer&gt;</c>, 
        ''' or the <i>Compile</i> page of the project properties).
        ''' </remarks>
        '''
        ''' <param name="root">
        ''' The root of the source file.
        ''' </param>
        '''
        ''' <param name="options">
        ''' The compilation options of the project.
        ''' </param>
        '''
        ''' <returns>
        ''' The unsatisfied requirements (for example <see cref="OptionStrictOn"/>), in canonical order, each with the setting 
        ''' that the file uses (for example <c>'Option Strict Off' (project)</c>); an empty list if every requirement is satisfied.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="root"/> or <paramref name="options"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetMismatches(root As CompilationUnitSyntax, options As VisualBasicCompilationOptions) As List(Of KeyValuePair(Of String, String))

            If root Is Nothing Then
                Throw New ArgumentNullException(NameOf(root))
            End If
            If options Is Nothing Then
                Throw New ArgumentNullException(NameOf(options))
            End If

            Dim declared As New Dictionary(Of SyntaxKind, OptionStatementSyntax)()
            For Each statement As OptionStatementSyntax In root.Options
                declared(statement.NameKeyword.Kind()) = statement
            Next statement

            Dim mismatches As New List(Of KeyValuePair(Of String, String))()
            OptionStatementsAnalyzer.AddMismatch(mismatches, declared, SyntaxKind.StrictKeyword, OptionStatementsAnalyzer.OptionStrictOn,
                                                 options.OptionStrict = OptionStrict.On, $"Option Strict {options.OptionStrict}")
            OptionStatementsAnalyzer.AddMismatch(mismatches, declared, SyntaxKind.ExplicitKeyword, OptionStatementsAnalyzer.OptionExplicitOn,
                                                 options.OptionExplicit, $"Option Explicit {If(options.OptionExplicit, "On", "Off")}")
            OptionStatementsAnalyzer.AddMismatch(mismatches, declared, SyntaxKind.InferKeyword, OptionStatementsAnalyzer.OptionInferOff,
                                                 Not options.OptionInfer, $"Option Infer {If(options.OptionInfer, "On", "Off")}")
            Return mismatches
        End Function

        ''' <summary>
        ''' Gets the required <see langword="Option"/> statements whose setting a source file does not use 
        ''' (see <see cref="GetMismatches"/>).
        ''' </summary>
        '''
        ''' <param name="root">
        ''' The root of the source file.
        ''' </param>
        '''
        ''' <param name="options">
        ''' The compilation options of the project.
        ''' </param>
        '''
        ''' <returns>
        ''' The unsatisfied requirements, in canonical order; an empty list if every requirement is satisfied.
        ''' </returns>
        Friend Shared Function GetMissingStatements(root As CompilationUnitSyntax, options As VisualBasicCompilationOptions) As List(Of String)

            Return OptionStatementsAnalyzer.GetMismatches(root, options).Select(Function(m As KeyValuePair(Of String, String)) m.Key).ToList()
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis of each source file with the compilation options of the project.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the compilation is not a Visual Basic compilation.
        ''' </exception>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            Dim options As VisualBasicCompilationOptions = TryCast(context.Compilation.Options, VisualBasicCompilationOptions)
            If options Is Nothing Then
                Throw New InvalidOperationException("The compilation is not a Visual Basic compilation.")
            End If

            context.RegisterSyntaxTreeAction(UtilAnalyzerFiles.SkipExcludedTrees(Sub(treeContext As SyntaxTreeAnalysisContext) OptionStatementsAnalyzer.AnalyzeTree(treeContext, options)))
        End Sub

        ''' <summary>
        ''' Adds a mismatch when a setting does not satisfy its requirement.
        ''' </summary>
        '''
        ''' <param name="mismatches">
        ''' The list of mismatches.
        ''' </param>
        '''
        ''' <param name="declared">
        ''' The <see langword="Option"/> statements of the file, by option name keyword.
        ''' </param>
        '''
        ''' <param name="name">
        ''' The option name keyword (<see cref="SyntaxKind.StrictKeyword"/>, <see cref="SyntaxKind.ExplicitKeyword"/> or 
        ''' <see cref="SyntaxKind.InferKeyword"/>).
        ''' </param>
        '''
        ''' <param name="requirement">
        ''' The required statement (for example <see cref="OptionStrictOn"/>).
        ''' </param>
        '''
        ''' <param name="projectSatisfies">
        ''' <see langword="True"/> if the setting of the project satisfies the requirement.
        ''' </param>
        '''
        ''' <param name="projectSetting">
        ''' The setting of the project (for example <c>Option Strict Off</c>).
        ''' </param>
        Private Shared Sub AddMismatch(mismatches As List(Of KeyValuePair(Of String, String)), declared As Dictionary(Of SyntaxKind, OptionStatementSyntax),
                                       name As SyntaxKind, requirement As String, projectSatisfies As Boolean, projectSetting As String)

            Dim statement As OptionStatementSyntax = Nothing
            If declared.TryGetValue(name, statement) Then
                If OptionStatementsAnalyzer.GetSatisfiedRequirement(statement) Is Nothing Then
                    ' "Option Strict" without a value means "On".
                    Dim value As String = If(statement.ValueKeyword.IsKind(SyntaxKind.OffKeyword), "Off", "On")
                    mismatches.Add(New KeyValuePair(Of String, String)(requirement, $"'Option {statement.NameKeyword.ValueText} {value}' (file)"))
                End If
            ElseIf Not projectSatisfies Then
                mismatches.Add(New KeyValuePair(Of String, String)(requirement, $"'{projectSetting}' (project)"))
            End If
        End Sub

        ''' <summary>
        ''' Analyzes a source file.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax tree analysis context.
        ''' </param>
        '''
        ''' <param name="options">
        ''' The compilation options of the project.
        ''' </param>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the root of the syntax tree is not a Visual Basic compilation unit.
        ''' </exception>
        Private Shared Sub AnalyzeTree(context As SyntaxTreeAnalysisContext, options As VisualBasicCompilationOptions)

            Dim root As CompilationUnitSyntax = TryCast(context.Tree.GetRoot(context.CancellationToken), CompilationUnitSyntax)
            If root Is Nothing Then
                Throw New InvalidOperationException("The syntax tree root is not a Visual Basic compilation unit.")
            End If

            Dim mismatches As List(Of KeyValuePair(Of String, String)) = OptionStatementsAnalyzer.GetMismatches(root, options)
            If mismatches.Count = 0 Then
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
                ImmutableDictionary(Of String, String).Empty.Add(OptionStatementsAnalyzer.MissingStatementsPropertyKey,
                                                                 String.Join(","c, mismatches.Select(Function(m As KeyValuePair(Of String, String)) m.Key)))

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MissingOptionStatements, location, properties,
                                                       String.Join(", ", mismatches.Select(Function(m As KeyValuePair(Of String, String)) $"'{m.Key}'")),
                                                       String.Join(", ", mismatches.Select(Function(m As KeyValuePair(Of String, String)) m.Value))))
        End Sub

#End Region

    End Class

End Namespace

#End Region
