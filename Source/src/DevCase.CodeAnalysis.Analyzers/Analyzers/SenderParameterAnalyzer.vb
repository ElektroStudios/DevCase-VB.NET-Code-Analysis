#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Linq
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " SenderParameterAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports parameters named <c>sender</c> in methods that are not event handlers (rule <see cref="DiagnosticIds.MisusedSenderParameter"/>),
    ''' for example <c>Public Function ToPen(sender As SolidBrush) As Pen</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' By convention, <c>sender</c> is the object that raises an event, and it is typed <see cref="Object"/>. A parameter named
    ''' <c>sender</c> with a more specific type, in a method that does not handle an event, hides what the argument really is.
    ''' </para>
    ''' <para>
    ''' The suggested name is the first of <c>source</c>, <c>value</c>, <c>input</c>, <c>instance</c> and <c>item</c> that is not 
    ''' already used in the method (another parameter, a local variable, the name of the method or a member used without 
    ''' qualification); when all of them are used, the parameter is reported without a code fix.
    ''' </para>
    ''' <para>
    ''' Not reported, because the method can be an event handler or a callback whose signature is defined elsewhere:
    ''' <c>sender</c> parameters typed <see cref="Object"/> (any delegate convention) or with a type parameter; methods with a
    ''' <see langword="Handles"/> clause; methods with another parameter whose type derives from <see cref="EventArgs"/>;
    ''' <see langword="Overrides"/> and <see langword="Implements"/> methods; and methods whose name appears after
    ''' <see langword="AddressOf"/> anywhere in the project (handlers of custom events with a typed sender, attached with
    ''' <see langword="AddHandler"/>).
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class SenderParameterAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the suggested parameter name.
        ''' </summary>
        Friend Const NewNamePropertyKey As String = "NewName"

        ''' <summary>
        ''' The reported parameter name.
        ''' </summary>
        Private Const SenderName As String = "sender"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The names suggested for the parameter, in order of preference: the first one that is not used in the method is chosen.
        ''' </summary>
        Private Shared ReadOnly CandidateNames As ImmutableArray(Of String) = ImmutableArray.Create("source", "value", "input", "instance", "item")

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MisusedSenderParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SenderParameterAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(
                Sub(start As CompilationStartAnalysisContext)
                    ' The names used after AddressOf in the project, computed once, on the first "sender" parameter found.
                    Dim delegateTargets As New Lazy(Of ImmutableHashSet(Of String))(
                        Function() SenderParameterAnalyzer.GetAddressOfNames(start.Compilation, start.CancellationToken),
                        LazyThreadSafetyMode.ExecutionAndPublication)
                    start.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(Sub(c As SyntaxNodeAnalysisContext) SenderParameterAnalyzer.AnalyzeMethod(c, delegateTargets)),
                                                   SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock)
                End Sub)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports the <c>sender</c> parameter of a method that is not an event handler.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <param name="delegateTargets">
        ''' The names used after <see langword="AddressOf"/> in the project.
        ''' </param>
        Private Shared Sub AnalyzeMethod(context As SyntaxNodeAnalysisContext, delegateTargets As Lazy(Of ImmutableHashSet(Of String)))

            Dim block As MethodBlockBaseSyntax = DirectCast(context.Node, MethodBlockBaseSyntax)
            Dim parameterList As ParameterListSyntax = block.BlockStatement.ParameterList
            Dim senderSyntax As ParameterSyntax = parameterList?.Parameters.FirstOrDefault(
                Function(p As ParameterSyntax) p.Identifier.Identifier.ValueText.Equals(SenderParameterAnalyzer.SenderName, StringComparison.OrdinalIgnoreCase))
            Dim statement As MethodStatementSyntax = TryCast(block.BlockStatement, MethodStatementSyntax)
            If senderSyntax Is Nothing OrElse (statement IsNot Nothing AndAlso statement.HandlesClause IsNot Nothing) Then
                Exit Sub
            End If

            Dim method As IMethodSymbol = context.SemanticModel.GetDeclaredSymbol(block, context.CancellationToken)
            If method Is Nothing OrElse method.IsOverride OrElse Not method.ExplicitInterfaceImplementations.IsEmpty OrElse
               method.Parameters.Length <> parameterList.Parameters.Count Then
                Exit Sub
            End If

            Dim sender As IParameterSymbol = method.Parameters(parameterList.Parameters.IndexOf(senderSyntax))
            If sender.Type.SpecialType = SpecialType.System_Object OrElse sender.Type.TypeKind = TypeKind.TypeParameter OrElse
               sender.Type.TypeKind = TypeKind.Error OrElse
               method.Parameters.Any(Function(p As IParameterSymbol) SenderParameterAnalyzer.IsEventArgs(p.Type)) OrElse
               delegateTargets.Value.Contains(method.Name) Then
                Exit Sub
            End If

            ' When every candidate is already used in the method, the rule still reports the parameter, without a code fix.
            Dim newName As String = UtilParameterNames.ChooseName(senderSyntax, SenderParameterAnalyzer.CandidateNames)
            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty
            Dim suggestion As String = String.Empty
            If newName IsNot Nothing Then
                properties = properties.Add(SenderParameterAnalyzer.NewNamePropertyKey, newName)
                suggestion = $" to '{newName}'"
            End If

            Dim methodName As String = If(method.MethodKind = MethodKind.Constructor, "New", method.Name)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MisusedSenderParameter, senderSyntax.Identifier.Identifier.GetLocation(),
                                                       properties, $"{method.ContainingType.Name}.{methodName}", suggestion))
        End Sub

        ''' <summary>
        ''' Gets the names used after <see langword="AddressOf"/> in every syntax tree of the compilation
        ''' (the rightmost name: <c>Handler</c> for <c>AddressOf Me.Handler</c>).
        ''' </summary>
        '''
        ''' <param name="compilation">
        ''' The compilation.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The names, compared case-insensitively.
        ''' </returns>
        Private Shared Function GetAddressOfNames(compilation As Compilation, cancellationToken As CancellationToken) As ImmutableHashSet(Of String)

            Dim names As ImmutableHashSet(Of String).Builder = ImmutableHashSet.CreateBuilder(StringComparer.OrdinalIgnoreCase)
            For Each tree As SyntaxTree In compilation.SyntaxTrees
                For Each node As UnaryExpressionSyntax In tree.GetRoot(cancellationToken).DescendantNodes().OfType(Of UnaryExpressionSyntax)()
                    If Not node.IsKind(SyntaxKind.AddressOfExpression) Then
                        Continue For
                    End If
                    Dim operand As ExpressionSyntax = node.Operand
                    Dim name As SimpleNameSyntax = If(TypeOf operand Is MemberAccessExpressionSyntax, DirectCast(operand, MemberAccessExpressionSyntax).Name,
                                                      TryCast(operand, SimpleNameSyntax))
                    If name IsNot Nothing Then
                        names.Add(name.Identifier.ValueText)
                    End If
                Next node
            Next tree
            Return names.ToImmutable()
        End Function

        ''' <summary>
        ''' Determines whether a type is <see cref="EventArgs"/> or derives from it.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type is an event arguments type; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsEventArgs(type As ITypeSymbol) As Boolean

            Dim current As ITypeSymbol = type
            While current IsNot Nothing
                If current.Name.Equals(NameOf(EventArgs), StringComparison.Ordinal) AndAlso
                   current.ContainingNamespace?.ToDisplayString().Equals("System", StringComparison.Ordinal) = True Then
                    Return True
                End If
                current = current.BaseType
            End While
            Return False
        End Function

#End Region

    End Class

End Namespace

#End Region
