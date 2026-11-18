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

#Region " SingleCharacterStringAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports single-character string literals passed to framework methods that have an equivalent <see cref="Char"/> overload
    ''' (rule <see cref="DiagnosticIds.SingleCharacterString"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Analyzed methods (<see cref="AnalyzedMethods"/>), whose <see cref="Char"/> overload produces the same result:
    ''' <c>String.Join</c>, <c>String.Split</c>, <c>String.Replace</c>, <c>String.Contains</c>,
    ''' <c>StringBuilder.Append</c>, <c>StringBuilder.Insert</c>, <c>StringBuilder.Replace</c>, <c>StringBuilder.AppendJoin</c>,
    ''' <c>TextWriter.Write</c> / <c>WriteLine</c> (also <c>StreamWriter</c>, <c>StringWriter</c>...) and <c>Console.Write</c> / <c>WriteLine</c>.
    ''' Methods whose <see cref="Char"/> overload behaves differently are not analyzed: <c>IndexOf</c>, <c>LastIndexOf</c>,
    ''' <c>StartsWith</c> and <c>EndsWith</c> compare a <see cref="String"/> with the current culture, but a <see cref="Char"/> ordinally.
    ''' </para>
    ''' <para>
    ''' Every single-character string literal of the call is replaced at once (<c>s.Replace("a", "b")</c> → <c>s.Replace("a"c, "b"c)</c>),
    ''' and the replaced call is bound speculatively before the diagnostic is reported: it must call a method with the same name on
    ''' the same type (or a base type) whose parameters are <see cref="Char"/> (or <see cref="Char"/> arrays) at those positions.
    ''' <c>String.Join</c> is the exception: on .NET Framework it has no <see cref="Char"/> overload, but the literal is widened to
    ''' <see cref="String"/>, a constant conversion done at compile time, so the same overload receives the same constant string.
    ''' Literals of control characters and surrogates are not reported.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class SingleCharacterStringAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the indexes of the replaced arguments, separated by commas.
        ''' </summary>
        Friend Const ArgumentIndexesPropertyKey As String = "ArgumentIndexes"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The analyzed methods (<c>Namespace.Type.Method</c>), matched on the called method and the methods it overrides.
        ''' </summary>
        Private Shared ReadOnly AnalyzedMethods As ImmutableHashSet(Of String) = ImmutableHashSet.Create(StringComparer.Ordinal,
            "System.String.Join", "System.String.Split", "System.String.Replace", "System.String.Contains",
            "System.Text.StringBuilder.Append", "System.Text.StringBuilder.Insert", "System.Text.StringBuilder.Replace", "System.Text.StringBuilder.AppendJoin",
            "System.IO.TextWriter.Write", "System.IO.TextWriter.WriteLine", "System.Console.Write", "System.Console.WriteLine")

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

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the name of a method as <c>Namespace.Type.Method</c>, built from the namespace
        ''' (<c>ToDisplayString</c> would write the Visual Basic keyword <c>String</c> for <c>System.String</c>).
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The method.
        ''' </param>
        '''
        ''' <returns>
        ''' The qualified name.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="method"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetQualifiedName(method As IMethodSymbol) As String

            If method Is Nothing Then
                Throw New ArgumentNullException(NameOf(method))
            End If

            Dim type As INamedTypeSymbol = method.ContainingType.OriginalDefinition
            Return $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}.{method.Name}"
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an analyzed call whose single-character string arguments can be character literals.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim invocation As IInvocationOperation = DirectCast(context.Operation, IInvocationOperation)
            Dim method As IMethodSymbol = invocation.TargetMethod
            Dim syntax As InvocationExpressionSyntax = TryCast(invocation.Syntax, InvocationExpressionSyntax)
            If syntax Is Nothing OrElse syntax.ArgumentList Is Nothing OrElse method.MethodKind <> MethodKind.Ordinary OrElse
               Not SingleCharacterStringAnalyzer.IsAnalyzedMethod(method) Then
                Exit Sub
            End If

            ' Positional arguments that are single-character string literals passed to String parameters.
            Dim indexes As New List(Of Integer)()
            Dim replaced As InvocationExpressionSyntax = syntax
            Dim literals As New List(Of String)()
            For Each argument As IArgumentOperation In invocation.Arguments
                Dim argumentSyntax As SimpleArgumentSyntax = TryCast(argument.Syntax, SimpleArgumentSyntax)
                Dim charLiteral As String = If(argumentSyntax Is Nothing OrElse argument.Parameter Is Nothing OrElse
                                               argument.Parameter.Type.SpecialType <> SpecialType.System_String, Nothing,
                                               UtilApiMigrations.GetCharLiteral(argumentSyntax))
                Dim index As Integer = syntax.ArgumentList.Arguments.IndexOf(argumentSyntax)
                If charLiteral IsNot Nothing AndAlso index >= 0 Then
                    indexes.Add(index)
                    literals.Add(charLiteral)
                End If
            Next argument
            If indexes.Count = 0 Then
                Exit Sub
            End If

            For Each index As Integer In indexes
                Dim literal As ExpressionSyntax = DirectCast(syntax.ArgumentList.Arguments(index), SimpleArgumentSyntax).Expression
                Dim current As ArgumentSyntax = replaced.ArgumentList.Arguments(index)
                replaced = replaced.ReplaceNode(DirectCast(current, SimpleArgumentSyntax).Expression,
                                                SyntaxFactory.ParseExpression(UtilApiMigrations.GetCharLiteral(syntax.ArgumentList.Arguments(index))).WithTriviaFrom(literal))
            Next index

            Dim speculative As IMethodSymbol = TryCast(invocation.SemanticModel.GetSpeculativeSymbolInfo(syntax.SpanStart, replaced,
                                                                                                         SpeculativeBindingOption.BindAsExpression).Symbol, IMethodSymbol)
            If Not SingleCharacterStringAnalyzer.IsEquivalentCharOverload(method, speculative, indexes) Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary.CreateRange(StringComparer.Ordinal,
                {New KeyValuePair(Of String, String)(SingleCharacterStringAnalyzer.ArgumentIndexesPropertyKey, String.Join(","c, indexes))})
            Dim location As Location = DirectCast(syntax.ArgumentList.Arguments(indexes(0)), SimpleArgumentSyntax).Expression.GetLocation()
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.SingleCharacterString, location, properties,
                                                       String.Join(" / ", literals), $"{method.ContainingType.Name}.{method.Name}"))
        End Sub

        ''' <summary>
        ''' Determines whether a method, or a method it overrides, is in <see cref="AnalyzedMethods"/>.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The invoked method.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the method is analyzed; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAnalyzedMethod(method As IMethodSymbol) As Boolean

            Dim current As IMethodSymbol = method
            While current IsNot Nothing
                If SingleCharacterStringAnalyzer.AnalyzedMethods.Contains(SingleCharacterStringAnalyzer.GetQualifiedName(current)) Then
                    Return True
                End If
                current = current.OverriddenMethod
            End While
            Return False
        End Function

        ''' <summary>
        ''' Determines whether the method bound with character literals is the equivalent <see cref="Char"/> overload of the original method.
        ''' </summary>
        '''
        ''' <param name="original">
        ''' The method called with string literals.
        ''' </param>
        '''
        ''' <param name="speculative">
        ''' The method called with character literals, or <see langword="Nothing"/> if the call does not bind.
        ''' </param>
        '''
        ''' <param name="indexes">
        ''' The indexes of the replaced arguments.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> when the speculative method has the same name, is declared by the same type or a base type,
        ''' and receives <see cref="Char"/> values at the replaced positions (or, for <c>String.Join</c> on .NET Framework,
        ''' is the same method); otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsEquivalentCharOverload(original As IMethodSymbol, speculative As IMethodSymbol, indexes As List(Of Integer)) As Boolean

            If speculative Is Nothing OrElse Not speculative.Name.Equals(original.Name, StringComparison.Ordinal) Then
                Return False
            End If

            Dim type As INamedTypeSymbol = original.ContainingType
            While type IsNot Nothing AndAlso Not SymbolEqualityComparer.Default.Equals(type, speculative.ContainingType)
                type = type.BaseType
            End While
            If type Is Nothing Then
                Return False
            End If

            ' String.Join on .NET Framework: the literal is widened to the same constant string, for the same overload.
            Return If(SymbolEqualityComparer.Default.Equals(speculative.OriginalDefinition, original.OriginalDefinition),
                      SingleCharacterStringAnalyzer.GetQualifiedName(original).Equals("System.String.Join", StringComparison.Ordinal),
                      indexes.TrueForAll(
                Function(index As Integer)
                    Dim parameter As IParameterSymbol = If(index < speculative.Parameters.Length, speculative.Parameters(index), speculative.Parameters.LastOrDefault())
                    Dim arrayType As IArrayTypeSymbol = TryCast(parameter?.Type, IArrayTypeSymbol)
                    Return parameter IsNot Nothing AndAlso
                           (parameter.Type.SpecialType = SpecialType.System_Char OrElse
                            (parameter.IsParams AndAlso arrayType IsNot Nothing AndAlso arrayType.ElementType.SpecialType = SpecialType.System_Char))
                End Function))
        End Function

#End Region

    End Class

End Namespace

#End Region
