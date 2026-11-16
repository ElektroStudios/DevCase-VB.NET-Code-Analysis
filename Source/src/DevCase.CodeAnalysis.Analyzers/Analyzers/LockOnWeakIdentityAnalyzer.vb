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

#Region " LockOnWeakIdentityAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <see langword="SyncLock"/> statements on objects that code outside the type can also lock: <see langword="Me"/>, 
    ''' <see langword="MyBase"/>, <see langword="MyClass"/>, and objects with weak identity (rule <see cref="DiagnosticIds.LockOnWeakIdentity"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Weak-identity types: <see cref="String"/> (literals are interned and shared by the whole process), <see cref="Type"/> and 
    ''' <see cref="System.Reflection.MemberInfo"/> (<c>GetType(T)</c> returns the same object everywhere), 
    ''' <see cref="System.Reflection.ParameterInfo"/>, <see cref="System.Threading.Thread"/>, <see cref="MarshalByRefObject"/> 
    ''' (and every derived type, such as forms, controls and streams), <see cref="OutOfMemoryException"/>, 
    ''' <see cref="StackOverflowException"/> and <see cref="ExecutionEngineException"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class LockOnWeakIdentityAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The metadata names of the weak-identity types (derived types are included).
        ''' </summary>
        Private Shared ReadOnly WeakIdentityTypeNames As ImmutableArray(Of String) = ImmutableArray.Create(
            "System.String", "System.Type", "System.Reflection.MemberInfo", "System.Reflection.ParameterInfo", "System.Threading.Thread",
            "System.MarshalByRefObject", "System.OutOfMemoryException", "System.StackOverflowException", "System.ExecutionEngineException")
#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.LockOnWeakIdentity)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="LockOnWeakIdentityAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf LockOnWeakIdentityAnalyzer.AnalyzeSyncLock, SyntaxKind.SyncLockStatement)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports a <see langword="SyncLock"/> statement on a publicly accessible or weak-identity object.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeSyncLock(context As SyntaxNodeAnalysisContext)

            Dim statement As SyncLockStatementSyntax = DirectCast(context.Node, SyncLockStatementSyntax)
            If statement.Expression Is Nothing Then
                Exit Sub ' Incomplete code.
            End If

            Dim expression As ExpressionSyntax = UtilAnalyzerSyntax.SkipParentheses(statement.Expression)

            Dim description As String = Nothing
            If TypeOf expression Is MeExpressionSyntax OrElse TypeOf expression Is MyBaseExpressionSyntax OrElse TypeOf expression Is MyClassExpressionSyntax Then
                description = $"'{expression}'"
            Else
                Dim current As ITypeSymbol = context.SemanticModel.GetTypeInfo(expression, context.CancellationToken).Type
                While current IsNot Nothing AndAlso description Is Nothing
                    ' Special types are displayed with their Visual Basic keyword ("String"), so String is checked through its special type.
                    If current.SpecialType = SpecialType.System_String OrElse
                       LockOnWeakIdentityAnalyzer.WeakIdentityTypeNames.Contains(current.ToDisplayString(), StringComparer.Ordinal) Then
                        description = $"an object of type '{current.Name}'"
                    End If
                    current = current.BaseType
                End While
            End If

            If description IsNot Nothing Then
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.LockOnWeakIdentity, statement.Expression.GetLocation(), description))
            End If
        End Sub

#End Region

    End Class

End Namespace

#End Region
