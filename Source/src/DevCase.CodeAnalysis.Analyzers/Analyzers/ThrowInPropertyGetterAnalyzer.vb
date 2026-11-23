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

#Region " ThrowInPropertyGetterAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="Throw"/> statements inside property getters that throw an exception type other than 
    ''' <see cref="InvalidOperationException"/>, <see cref="NotSupportedException"/> or <see cref="NotImplementedException"/> 
    ''' (or a type derived from them, such as <see cref="ObjectDisposedException"/>) (rule <see cref="DiagnosticIds.ThrowInPropertyGetter"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Not reported: parameterized (indexed) properties, where argument validation is expected; the rethrow statement <c>Throw</c> 
    ''' without an expression; exceptions thrown inside the <see langword="Try"/> section of a <see langword="Try"/> block with 
    ''' <see langword="Catch"/> blocks; and exceptions thrown inside lambdas.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThrowInPropertyGetterAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The metadata names of the exception types that a getter may throw (derived types are included).
        ''' </summary>
        Private Shared ReadOnly AllowedExceptionNames As ImmutableArray(Of String) = ImmutableArray.Create(
            "System.InvalidOperationException", "System.NotSupportedException", "System.NotImplementedException")
#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThrowInPropertyGetter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThrowInPropertyGetterAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(AddressOf ThrowInPropertyGetterAnalyzer.AnalyzeThrow), SyntaxKind.ThrowStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a <see langword="Throw"/> statement that leaves a property getter with an unexpected exception type.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeThrow(context As SyntaxNodeAnalysisContext)

            Dim statement As ThrowStatementSyntax = DirectCast(context.Node, ThrowStatementSyntax)
            Dim accessor As AccessorBlockSyntax = TryCast(UtilAnalyzerSyntax.GetExecutableBoundary(statement), AccessorBlockSyntax)
            If statement.Expression Is Nothing OrElse accessor Is Nothing OrElse Not accessor.IsKind(SyntaxKind.GetAccessorBlock) Then
                Exit Sub
            End If

            Dim propertyBlock As PropertyBlockSyntax = TryCast(accessor.Parent, PropertyBlockSyntax)
            If propertyBlock Is Nothing OrElse
               (propertyBlock.PropertyStatement.ParameterList IsNot Nothing AndAlso propertyBlock.PropertyStatement.ParameterList.Parameters.Count > 0) OrElse
               UtilAnalyzerSyntax.IsCaughtLocally(statement, accessor) Then
                Exit Sub
            End If

            Dim exceptionType As ITypeSymbol = context.SemanticModel.GetTypeInfo(statement.Expression, context.CancellationToken).Type
            Dim current As ITypeSymbol = exceptionType
            While current IsNot Nothing
                If ThrowInPropertyGetterAnalyzer.AllowedExceptionNames.Contains(current.ToDisplayString(), StringComparer.Ordinal) Then
                    Exit Sub
                End If
                current = current.BaseType
            End While

            If exceptionType IsNot Nothing Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThrowInPropertyGetter, statement.GetLocation(),
                                                           propertyBlock.PropertyStatement.Identifier.ValueText, exceptionType.Name))
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
