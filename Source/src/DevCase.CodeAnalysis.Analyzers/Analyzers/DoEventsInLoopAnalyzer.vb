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
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " DoEventsInLoopAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports calls to <c>Application.DoEvents</c> (Windows Forms) and <c>My.Application.DoEvents</c> inside a loop of the same method 
    ''' (rule <see cref="DiagnosticIds.DoEventsInLoop"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Loops are <see langword="For"/>, <see langword="For Each"/>, <see langword="While"/> and <see langword="Do"/> blocks. 
    ''' The search stops at the executable boundary: a call inside a lambda is reported only if the loop is inside that lambda.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DoEventsInLoopAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DoEventsInLoop)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DoEventsInLoopAnalyzer"/> class.
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
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf DoEventsInLoopAnalyzer.AnalyzeInvocation), OperationKind.Invocation)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports an invocation of <c>DoEvents</c> inside a loop.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeInvocation(context As OperationAnalysisContext)

            Dim method As IMethodSymbol = DirectCast(context.Operation, IInvocationOperation).TargetMethod
            If Not method.Name.Equals("DoEvents", StringComparison.Ordinal) OrElse Not DoEventsInLoopAnalyzer.IsDoEventsType(method.ContainingType) Then
                Exit Sub
            End If

            Dim boundary As SyntaxNode = UtilAnalyzerSyntax.GetExecutableBoundary(context.Operation.Syntax)
            Dim current As SyntaxNode = context.Operation.Syntax.Parent
            While current IsNot Nothing AndAlso current IsNot boundary
                If TypeOf current Is ForOrForEachBlockSyntax OrElse TypeOf current Is WhileBlockSyntax OrElse TypeOf current Is DoLoopBlockSyntax Then
                    context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DoEventsInLoop, context.Operation.Syntax.GetLocation(),
                                                               $"{method.ContainingType.Name}.DoEvents"))
                    Exit Sub
                End If
                current = current.Parent
            End While
        End Sub

        ''' <summary>
        ''' Determines whether a type is <c>System.Windows.Forms.Application</c> or derives from 
        ''' <c>Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase</c> (<c>My.Application</c>).
        ''' </summary>
        ''' 
        ''' <param name="type">
        ''' The type. May be <see langword="Nothing"/>.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the type exposes the Windows Forms <c>DoEvents</c> method; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsDoEventsType(type As INamedTypeSymbol) As Boolean

            Dim current As INamedTypeSymbol = type
            While current IsNot Nothing
                Dim name As String = current.ToDisplayString()
                If name.Equals("System.Windows.Forms.Application", StringComparison.Ordinal) OrElse
                   name.Equals("Microsoft.VisualBasic.ApplicationServices.WindowsFormsApplicationBase", StringComparison.Ordinal) Then
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
