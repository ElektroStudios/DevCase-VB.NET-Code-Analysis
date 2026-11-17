#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " SingleCharacterStringAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports single-character string literals passed to <c>String.Join</c> or <c>StringBuilder.Append</c>,
    ''' where a character literal can be used (rule <see cref="DiagnosticIds.SingleCharacterString"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Analyzed calls: the separator of <c>String.Join</c>, and <c>StringBuilder.Append(String)</c> with a single argument.
    ''' The character literal produces the same result on every target framework:
    ''' <list type="bullet">
    '''   <item><description>On modern .NET, it binds to the <see cref="Char"/> overload (<c>String.Join(Char, ...)</c>, <c>StringBuilder.Append(Char)</c>).</description></item>
    '''   <item><description>On .NET Framework, <c>String.Join</c> has no <see cref="Char"/> overload: the literal is widened to <see cref="String"/>,
    '''   a constant conversion done at compile time, and the same overload is called with the same constant string.</description></item>
    ''' </list>
    ''' To be certain that the replaced call compiles and calls the same method of the same type, it is bound speculatively
    ''' before the diagnostic is reported. Literals of control characters and surrogates are not reported.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class SingleCharacterStringAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The display name of <c>String.Join</c>.
        ''' </summary>
        Private Const JoinApiName As String = "String.Join"

        ''' <summary>
        ''' The display name of <c>StringBuilder.Append</c>.
        ''' </summary>
        Private Const AppendApiName As String = "StringBuilder.Append"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.SingleCharacterString)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SingleCharacterStringAnalyzer"/> class.
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
            context.RegisterOperationAction(AddressOf SingleCharacterStringAnalyzer.AnalyzeInvocation, OperationKind.Invocation)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an analyzed call whose string argument can be a character literal.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            Dim apiName As String = SingleCharacterStringAnalyzer.GetApiName(method)
            If apiName Is Nothing Then
                Exit Sub
            End If

            Dim syntax As InvocationExpressionSyntax = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            If syntax Is Nothing OrElse syntax.ArgumentList Is Nothing OrElse syntax.ArgumentList.Arguments.Count = 0 OrElse
               method.MethodKind <> MethodKind.Ordinary OrElse
               (apiName.Equals(SingleCharacterStringAnalyzer.AppendApiName, StringComparison.Ordinal) AndAlso syntax.ArgumentList.Arguments.Count <> 1) Then
                Exit Sub
            End If

            Dim charLiteral As String = UtilApiMigrations.GetCharLiteral(syntax.ArgumentList.Arguments(0))
            If charLiteral Is Nothing Then
                Exit Sub
            End If

            ' The replaced call must bind to a method with the same name, declared by the same type.
            Dim literal As ExpressionSyntax = DirectCast(syntax.ArgumentList.Arguments(0), SimpleArgumentSyntax).Expression
            Dim replaced As InvocationExpressionSyntax = syntax.ReplaceNode(literal, SyntaxFactory.ParseExpression(charLiteral))
            Dim speculative As IMethodSymbol = TryCast(invocation.SemanticModel.GetSpeculativeSymbolInfo(syntax.SpanStart, replaced,
                                                                                                         SpeculativeBindingOption.BindAsExpression).Symbol, IMethodSymbol)
            If speculative Is Nothing OrElse Not speculative.Name.Equals(method.Name, StringComparison.Ordinal) OrElse
               Not SymbolEqualityComparer.Default.Equals(speculative.ContainingType, method.ContainingType) Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.SingleCharacterString, literal.GetLocation(), charLiteral, apiName))
        End Sub

        ''' <summary>
        ''' Gets the display name of an analyzed method.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The invoked method.
        ''' </param>
        '''
        ''' <returns>
        ''' <c>String.Join</c> or <c>StringBuilder.Append</c>; or <see langword="Nothing"/> if the method is not analyzed.
        ''' </returns>
        Private Shared Function GetApiName(method As IMethodSymbol) As String

            If method.Parameters.Length = 0 OrElse method.Parameters(0).Type.SpecialType <> SpecialType.System_String Then
                Return Nothing
            End If

            Dim isJoin As Boolean = method.IsStatic AndAlso method.ContainingType.SpecialType = SpecialType.System_String AndAlso
                                    method.Name.Equals(NameOf(String.Join), StringComparison.Ordinal)
            Dim isAppend As Boolean = Not method.IsStatic AndAlso method.Parameters.Length = 1 AndAlso
                                      method.Name.Equals(NameOf(System.Text.StringBuilder.Append), StringComparison.Ordinal) AndAlso
                                      method.ContainingType.ToDisplayString().Equals("System.Text.StringBuilder", StringComparison.Ordinal)
            Return If(isJoin, SingleCharacterStringAnalyzer.JoinApiName, If(isAppend, SingleCharacterStringAnalyzer.AppendApiName, Nothing))
        End Function

#End Region

    End Class

End Namespace

#End Region
