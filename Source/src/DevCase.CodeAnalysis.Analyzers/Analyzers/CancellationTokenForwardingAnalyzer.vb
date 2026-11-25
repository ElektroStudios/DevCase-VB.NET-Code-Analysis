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

#Region " CancellationTokenForwardingAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls that can take a <c>CancellationToken</c> but do not receive the one of the enclosing method
    ''' (rule <see cref="DiagnosticIds.CancellationTokenNotForwarded"/>; equivalent of CA2016 for .NET Framework projects), for example
    ''' <c>Await Task.Delay(500)</c> in a method with a <c>cancellationToken</c> parameter.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The enclosing method, or the innermost enclosing lambda, must have exactly one <c>CancellationToken</c> parameter (a lambda
    ''' without one is not analyzed, even inside a method that has one). The call is reported when it omits an optional
    ''' <c>CancellationToken</c> parameter, or when the type has an accessible overload with the same parameters and a
    ''' <c>CancellationToken</c> at the end. Calls that already pass a token, or that expand a <see langword="ParamArray"/>, are not
    ''' reported.
    ''' </para>
    ''' <para>
    ''' The code fix appends the token, as a named argument (<c>cancellationToken:=token</c>) when the call uses named or omitted
    ''' arguments, or when a positional argument would not reach the parameter. The rule is only reported when that call binds to the
    ''' expected method.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class CancellationTokenForwardingAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.CancellationTokenNotForwarded)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="CancellationTokenForwardingAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf CancellationTokenForwardingAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2016 reports the same code.
            End If

            Dim tokenType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.Threading.CancellationToken")
            If tokenType Is Nothing Then
                Exit Sub
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext) CancellationTokenForwardingAnalyzer.AnalyzeInvocation(c, tokenType)), OperationKind.Invocation)
        End Sub


        ''' <summary>
        ''' Reports a call that does not receive the cancellation token of the enclosing method.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="tokenType">
        ''' The <c>CancellationToken</c> type.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext, tokenType As INamedTypeSymbol)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim syntax As SyntaxNode = invocation.Syntax
            Dim invocationSyntax As InvocationExpressionSyntax = TryCast(syntax, InvocationExpressionSyntax)
            Dim expression As ExpressionSyntax = If(invocationSyntax IsNot Nothing, invocationSyntax.Expression, TryCast(syntax, ExpressionSyntax))
            If invocation.IsImplicit OrElse (TypeOf expression IsNot MemberAccessExpressionSyntax AndAlso TypeOf expression IsNot SimpleNameSyntax) OrElse
               invocation.Arguments.Any(
                Function(a As IArgumentOperation) a.ArgumentKind = ArgumentKind.ParamArray OrElse
                                                  (a.ArgumentKind = ArgumentKind.Explicit AndAlso SymbolEqualityComparer.Default.Equals(a.Parameter?.Type, tokenType))) Then
                Exit Sub
            End If

            Dim token As IParameterSymbol = CancellationTokenForwardingAnalyzer.GetToken(invocation, context.ContainingSymbol, tokenType)
            If token Is Nothing Then
                Exit Sub
            End If

            ' The parameter that receives the token: an optional parameter that the call omits, or the last one of an overload.
            Dim model As SemanticModel = invocation.SemanticModel
            Dim expected As IMethodSymbol = invocation.TargetMethod
            Dim parameter As IParameterSymbol = invocation.Arguments.Where(
                Function(a As IArgumentOperation) a.ArgumentKind = ArgumentKind.DefaultValue AndAlso SymbolEqualityComparer.Default.Equals(a.Parameter?.Type, tokenType)).
                Select(Function(a As IArgumentOperation) a.Parameter).FirstOrDefault()
            If parameter Is Nothing Then
                expected = CancellationTokenForwardingAnalyzer.FindOverload(invocation.TargetMethod, tokenType, model, syntax.SpanStart)
                If expected Is Nothing Then
                    Exit Sub
                End If
                parameter = expected.Parameters(expected.Parameters.Length - 1)
            End If

            Dim argumentList As ArgumentListSyntax = invocationSyntax?.ArgumentList
            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = If(argumentList Is Nothing, Nothing, argumentList.Arguments)
            Dim isNamed As Boolean = parameter.Ordinal <> arguments.Count OrElse arguments.Any(Function(a As ArgumentSyntax) a.IsNamed OrElse a.IsOmitted)
            Dim tokenName As String = UtilAnalyzerSyntax.EscapeIdentifier(token.Name)
            Dim argument As String = If(isNamed, $"{UtilAnalyzerSyntax.EscapeIdentifier(parameter.Name)}:={tokenName}", tokenName)
            Dim position As Integer
            Dim insertion As String
            Select Case True
                Case argumentList Is Nothing
                    position = syntax.Span.End ' A call without parentheses: "gate.Wait" becomes "gate.Wait(token)".
                    insertion = $"({argument})"
                Case arguments.Count = 0
                    position = argumentList.CloseParenToken.SpanStart
                    insertion = argument
                Case Else
                    position = arguments(arguments.Count - 1).Span.End
                    insertion = $", {argument}"
            End Select

            Dim newCall As String = syntax.ToString().Insert(position - syntax.SpanStart, insertion)
            Dim speculative As IMethodSymbol = TryCast(UtilSpeculativeBinding.GetSymbol(model, syntax.SpanStart, newCall), IMethodSymbol)
            If speculative Is Nothing OrElse Not SymbolEqualityComparer.Default.Equals(CancellationTokenForwardingAnalyzer.GetDefinition(speculative),
                                                                                    CancellationTokenForwardingAnalyzer.GetDefinition(expected)) Then
                Exit Sub
            End If

            Dim access As MemberAccessExpressionSyntax = TryCast(expression, MemberAccessExpressionSyntax)
            Dim location As Location = If(access Is Nothing, expression.GetLocation(), access.Name.GetLocation())
            Dim actions As FixReplacement() = {New FixReplacement($"Forward '{token.Name}'", "Forward", insertion)}
            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.CancellationTokenNotForwarded, location,
                                                                        {Location.Create(syntax.SyntaxTree, New Text.TextSpan(position, 0))},
                                                                        actions, token.Name, invocation.TargetMethod.Name))
        End Sub

        ''' <summary>
        ''' Gets the <c>CancellationToken</c> parameter of the innermost lambda, or of the method, that contains an invocation.
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <param name="containingSymbol">
        ''' The symbol that contains the invocation.
        ''' </param>
        '''
        ''' <param name="tokenType">
        ''' The <c>CancellationToken</c> type.
        ''' </param>
        '''
        ''' <returns>
        ''' The parameter; <see langword="Nothing"/> if that lambda or method does not have exactly one.
        ''' </returns>
        Private Shared Function GetToken(invocation As IInvocationOperation, containingSymbol As ISymbol, tokenType As INamedTypeSymbol) As IParameterSymbol

            Dim parameters As ImmutableArray(Of IParameterSymbol) = ImmutableArray(Of IParameterSymbol).Empty
            Dim current As IOperation = invocation.Parent
            While current IsNot Nothing AndAlso TypeOf current IsNot IAnonymousFunctionOperation
                current = current.Parent
            End While

            If current IsNot Nothing Then
                parameters = DirectCast(current, IAnonymousFunctionOperation).Symbol.Parameters
            ElseIf TypeOf containingSymbol Is IMethodSymbol Then
                parameters = DirectCast(containingSymbol, IMethodSymbol).Parameters
            End If

            Dim tokens As List(Of IParameterSymbol) = parameters.Where(
                Function(p As IParameterSymbol) p.RefKind = RefKind.None AndAlso SymbolEqualityComparer.Default.Equals(p.Type, tokenType)).ToList()
            Return If(tokens.Count = 1, tokens(0), Nothing)
        End Function

        ''' <summary>
        ''' Finds the overload of a method with the same parameters and a <c>CancellationToken</c> at the end.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The invoked method.
        ''' </param>
        '''
        ''' <param name="tokenType">
        ''' The <c>CancellationToken</c> type.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position of the invocation, to check accessibility.
        ''' </param>
        '''
        ''' <returns>
        ''' The overload; <see langword="Nothing"/> if there is none (extension methods are not searched).
        ''' </returns>
        Private Shared Function FindOverload(method As IMethodSymbol, tokenType As INamedTypeSymbol, model As SemanticModel, position As Integer) As IMethodSymbol

            If method.ReducedFrom IsNot Nothing OrElse method.IsExtensionMethod Then
                Return Nothing
            End If

            Dim definition As IMethodSymbol = method.OriginalDefinition
            For Each candidate As IMethodSymbol In method.ContainingType.GetMembers(method.Name).OfType(Of IMethodSymbol)()
                If candidate.IsStatic <> method.IsStatic OrElse candidate.Arity <> method.Arity OrElse
                   candidate.Parameters.Length <> definition.Parameters.Length + 1 OrElse
                   Not SymbolEqualityComparer.Default.Equals(candidate.Parameters(candidate.Parameters.Length - 1).Type, tokenType) OrElse
                   Not model.IsAccessible(position, candidate) Then
                    Continue For
                End If

                ' Type parameters of different methods are different symbols: the types are compared by their display.
                Dim matches As Boolean = Enumerable.Range(0, definition.Parameters.Length).All(
                    Function(i As Integer) candidate.Parameters(i).RefKind = definition.Parameters(i).RefKind AndAlso
                                           candidate.Parameters(i).Type.ToDisplayString().Equals(definition.Parameters(i).Type.ToDisplayString(), StringComparison.Ordinal))
                If matches Then
                    Return candidate
                End If
            Next candidate
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets the original definition of a method, through its reduced form for extension methods.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The method.
        ''' </param>
        '''
        ''' <returns>
        ''' The definition.
        ''' </returns>
        Private Shared Function GetDefinition(method As IMethodSymbol) As IMethodSymbol

            Return If(method.ReducedFrom, method).OriginalDefinition
        End Function

#End Region

    End Class

End Namespace

#End Region
