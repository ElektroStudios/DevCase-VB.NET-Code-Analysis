#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " AsyncSubAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="Async"/> <see langword="Sub"/> methods that are not event handlers 
    ''' (rule <see cref="DiagnosticIds.AsyncSubNotEventHandler"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' Not reported (their signature cannot or must not change): methods with a <see langword="Handles"/> clause, 
    ''' methods with the event-handler signature <c>(sender As Object, e As EventArgs-derived)</c>, 
    ''' <see langword="Overrides"/> and <see langword="Implements"/> members. Async lambdas are not analyzed.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class AsyncSubAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.AsyncSubNotEventHandler)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="AsyncSubAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf AsyncSubAnalyzer.AnalyzeSub, SyntaxKind.SubStatement)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Determines whether a <see langword="Sub"/> statement is an <see langword="Async"/> method that must be converted to a task-returning function.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The <see langword="Sub"/> statement.
        ''' </param>
        '''
        ''' <param name="semanticModel">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the method is a non-event-handler <see langword="Async"/> <see langword="Sub"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="statement"/> or <paramref name="semanticModel"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function IsOffendingAsyncSub(statement As MethodStatementSyntax, semanticModel As SemanticModel, cancellationToken As CancellationToken) As Boolean

            If statement Is Nothing Then
                Throw New ArgumentNullException(NameOf(statement))
            End If
            If semanticModel Is Nothing Then
                Throw New ArgumentNullException(NameOf(semanticModel))
            End If

            If Not statement.IsKind(SyntaxKind.SubStatement) OrElse
               Not statement.Modifiers.Any(SyntaxKind.AsyncKeyword) OrElse
               statement.Modifiers.Any(SyntaxKind.OverridesKeyword) OrElse
               statement.HandlesClause IsNot Nothing OrElse
               statement.ImplementsClause IsNot Nothing OrElse
               TypeOf statement.Parent IsNot MethodBlockSyntax Then
                Return False
            End If

            Dim method As IMethodSymbol = semanticModel.GetDeclaredSymbol(statement, cancellationToken)
            Return method IsNot Nothing AndAlso Not AsyncSubAnalyzer.HasEventHandlerSignature(method, semanticModel.Compilation)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a <see langword="Sub"/> statement.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeSub(context As SyntaxNodeAnalysisContext)

            Dim statement As MethodStatementSyntax = DirectCast(context.Node, MethodStatementSyntax)
            If AsyncSubAnalyzer.IsOffendingAsyncSub(statement, context.SemanticModel, context.CancellationToken) Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.AsyncSubNotEventHandler, statement.Identifier.GetLocation(), statement.Identifier.ValueText))
            End If
        End Sub

        ''' <summary>
        ''' Determines whether a method has the event-handler signature <c>(sender As Object, e As EventArgs-derived)</c>.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The method.
        ''' </param>
        '''
        ''' <param name="compilation">
        ''' The compilation, used to resolve <see cref="EventArgs"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for an event-handler signature; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function HasEventHandlerSignature(method As IMethodSymbol, compilation As Compilation) As Boolean

            If method.Parameters.Length <> 2 OrElse method.Parameters(0).Type.SpecialType <> SpecialType.System_Object Then
                Return False
            End If

            Dim eventArgs As INamedTypeSymbol = compilation.GetTypeByMetadataName("System.EventArgs")
            Dim current As ITypeSymbol = method.Parameters(1).Type
            While current IsNot Nothing
                If SymbolEqualityComparer.Default.Equals(current, eventArgs) Then
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
