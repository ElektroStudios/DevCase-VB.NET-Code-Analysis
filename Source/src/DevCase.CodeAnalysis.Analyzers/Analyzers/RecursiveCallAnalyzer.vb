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
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " RecursiveCallAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports direct recursion: methods that invoke themselves and property accessors that access their own property 
    ''' in the same direction (a getter that reads the property, a setter that assigns it) (rule <see cref="DiagnosticIds.RecursiveCall"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Calls made from lambdas are not reported (the lambda is a different method that may run later), 
    ''' nor are calls to a base or overloaded member (different symbols). 
    ''' Inside a <see langword="Function"/>, the function name used without parentheses denotes the return variable and is not reported.
    ''' Indirect recursion (A → B → A) is not detected.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class RecursiveCallAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.RecursiveCall)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="RecursiveCallAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf RecursiveCallAnalyzer.AnalyzeName), SyntaxKind.IdentifierName, SyntaxKind.GenericName)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a simple name (method or property reference).
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeName(context As SyntaxNodeAnalysisContext)

            Dim name As SimpleNameSyntax = DirectCast(context.Node, SimpleNameSyntax)

            ' Ignore the "Name" part of a qualified name/member access when analyzed separately, AddressOf, NameOf and Implements/Handles clauses.
            If name.Ancestors().Any(Function(n As SyntaxNode) n.IsKind(SyntaxKind.NameOfExpression) OrElse
                                                               n.IsKind(SyntaxKind.AddressOfExpression) OrElse
                                                               TypeOf n Is ImplementsClauseSyntax OrElse
                                                               TypeOf n Is HandlesClauseSyntax) Then
                Exit Sub
            End If

            Dim symbol As ISymbol = context.SemanticModel.GetSymbolInfo(name, context.CancellationToken).Symbol
            If symbol Is Nothing Then
                Exit Sub
            End If

            ' The containing symbol is the lambda method when the reference is inside a lambda.
            Dim container As IMethodSymbol = TryCast(context.SemanticModel.GetEnclosingSymbol(name.SpanStart, context.CancellationToken), IMethodSymbol)
            If container Is Nothing Then
                Exit Sub
            End If

            Dim isRecursive As Boolean = False
            Select Case symbol.Kind
                Case SymbolKind.Method
                    isRecursive = SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, container.OriginalDefinition)

                Case SymbolKind.Property
                    If container.AssociatedSymbol IsNot Nothing AndAlso
                       SymbolEqualityComparer.Default.Equals(symbol.OriginalDefinition, container.AssociatedSymbol.OriginalDefinition) Then
                        Dim isWrite As Boolean = RecursiveCallAnalyzer.IsAssignmentTarget(name)
                        isRecursive = (container.MethodKind = MethodKind.PropertyGet AndAlso Not isWrite) OrElse
                                      (container.MethodKind = MethodKind.PropertySet AndAlso isWrite)
                    End If
            End Select

            If isRecursive Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.RecursiveCall, name.GetLocation(), symbol.Name))
            End If
        End Sub

        ''' <summary>
        ''' Determines whether a name is the target of an assignment (<c>Me.Value = x</c>, <c>Value += 1</c>).
        ''' </summary>
        '''
        ''' <param name="name">
        ''' The name.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the name is written; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAssignmentTarget(name As SimpleNameSyntax) As Boolean

            Dim target As SyntaxNode = name
            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(name.Parent, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing AndAlso memberAccess.Name Is name Then
                target = memberAccess
            End If

            Dim assignment As AssignmentStatementSyntax = TryCast(target.Parent, AssignmentStatementSyntax)
            Return assignment IsNot Nothing AndAlso assignment.Left Is target
        End Function

#End Region

    End Class

End Namespace

#End Region
