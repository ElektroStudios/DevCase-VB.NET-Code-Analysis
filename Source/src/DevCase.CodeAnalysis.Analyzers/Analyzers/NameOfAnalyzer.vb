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
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " NameOfAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports string literals passed to a <c>paramName</c> or <c>propertyName</c> parameter that name a parameter or a property
    ''' in scope (rule <see cref="DiagnosticIds.StringLiteralInsteadOfNameOf"/>; equivalent of CA1507 for .NET Framework projects), for
    ''' example <c>New ArgumentNullException("value")</c> or <c>OnPropertyChanged("Text")</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' A <c>paramName</c> literal must equal (case-sensitively) the name of a parameter of the enclosing method, accessor or lambda; a
    ''' <c>propertyName</c> literal, the name of a property that the identifier binds to at that position. A rename refactoring does not
    ''' update a string, so the name goes out of sync; <c>NameOf</c> is checked by the compiler.
    ''' </para>
    ''' <para>
    ''' Projects whose language version is older than Visual Basic 14 (without <c>NameOf</c>) and attribute arguments are not analyzed.
    ''' The code fix writes <c>NameOf(name)</c>, with brackets when the name is a keyword.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class NameOfAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.StringLiteralInsteadOfNameOf)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="NameOfAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf NameOfAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Registers the analysis for .NET Framework compilations.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            If Not UtilAnalyzerSemantic.IsNetFramework(context.Compilation) Then
                Exit Sub ' Modern .NET: the built-in rule CA1507 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf NameOfAnalyzer.AnalyzeArgument), OperationKind.Argument)
        End Sub


        ''' <summary>
        ''' Reports a string literal passed as the name of a parameter or property in scope.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeArgument(context As OperationAnalysisContext)

            Dim argument As IArgumentOperation = DirectCast(context.Operation, IArgumentOperation)
            If argument.Parameter Is Nothing OrElse argument.ArgumentKind <> ArgumentKind.Explicit Then
                Exit Sub
            End If

            Dim isParameterName As Boolean = argument.Parameter.Name.Equals("paramName", StringComparison.Ordinal)
            If Not isParameterName AndAlso Not argument.Parameter.Name.Equals("propertyName", StringComparison.Ordinal) Then
                Exit Sub
            End If

            Dim literal As ILiteralOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(argument.Value), ILiteralOperation)
            Dim options As VisualBasicParseOptions = TryCast(argument.Syntax.SyntaxTree.Options, VisualBasicParseOptions)
            If literal Is Nothing OrElse Not literal.ConstantValue.HasValue OrElse TypeOf literal.ConstantValue.Value IsNot String OrElse
               options Is Nothing OrElse options.LanguageVersion < LanguageVersion.VisualBasic14 OrElse
               argument.Syntax.FirstAncestorOrSelf(Of AttributeSyntax)() IsNot Nothing Then
                Exit Sub
            End If

            Dim name As String = DirectCast(literal.ConstantValue.Value, String)
            If Not SyntaxFacts.IsValidIdentifier(name) Then
                Exit Sub
            End If

            ' The name must bind, with the same case, to the parameter or property: a local variable or another member that 
            ' hides it would make NameOf return another name.
            Dim model As SemanticModel = argument.SemanticModel
            Dim position As Integer = literal.Syntax.SpanStart
            Dim escaped As String = UtilAnalyzerSyntax.EscapeIdentifier(name)
            Dim symbol As ISymbol = UtilSpeculativeBinding.GetSymbol(model, position, escaped)
            If symbol Is Nothing OrElse Not symbol.Name.Equals(name, StringComparison.Ordinal) Then
                Exit Sub
            End If

            If isParameterName Then
                If TypeOf symbol IsNot IParameterSymbol OrElse Not UtilAnalyzerSemantic.GetVisibleParameters(argument, context.ContainingSymbol).Exists(
                    Function(p As IParameterSymbol) p.Name.Equals(name, StringComparison.Ordinal)) Then
                    Exit Sub
                End If
            ElseIf TypeOf symbol IsNot IPropertySymbol Then
                Exit Sub
            End If

            Dim replacement As String = $"NameOf({escaped})"
            Dim actions As New List(Of FixReplacement)()
            Dim replacementType As ITypeSymbol = UtilSpeculativeBinding.GetExpressionType(model, position, replacement)
            If replacementType IsNot Nothing AndAlso replacementType.SpecialType = SpecialType.System_String Then
                actions.Add(New FixReplacement("Use NameOf", "NameOf", replacement))
            End If
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.StringLiteralInsteadOfNameOf, literal.Syntax.GetLocation(),
                                                                        Enumerable.Empty(Of Location)(), actions, name))
        End Sub

#End Region

    End Class

End Namespace

#End Region
