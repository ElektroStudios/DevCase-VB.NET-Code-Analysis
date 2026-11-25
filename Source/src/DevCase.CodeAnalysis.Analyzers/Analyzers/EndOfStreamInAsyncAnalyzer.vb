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

#Region " EndOfStreamInAsyncAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>StreamReader.EndOfStream</c> read in an <see langword="Async"/> method or lambda
    ''' (rule <see cref="DiagnosticIds.EndOfStreamInAsyncMethod"/>; equivalent of CA2024 for .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' When no data is buffered, <c>EndOfStream</c> reads from the underlying stream synchronously, which blocks the thread of the
    ''' <see langword="Async"/> method. <c>ReadLineAsync</c> returns <see langword="Nothing"/> at the end of the stream, so the loop can
    ''' test its result instead: <c>While (Await reader.ReadLineAsync()) IsNot Nothing</c>.
    ''' </para>
    ''' <para>
    ''' The property is reported on <see cref="IO.StreamReader"/> and its derived types, only inside an <see langword="Async"/> method or
    ''' lambda (the nearest one: a non-<see langword="Async"/> lambda inside an <see langword="Async"/> method is not reported). There is
    ''' no code fix: the loop has to be restructured around the result of <c>ReadLineAsync</c>.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class EndOfStreamInAsyncAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.EndOfStreamInAsyncMethod)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="EndOfStreamInAsyncAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf EndOfStreamInAsyncAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2024 reports the same code.
            End If

            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf EndOfStreamInAsyncAnalyzer.AnalyzePropertyReference),
                                            OperationKind.PropertyReference)
        End Sub


        ''' <summary>
        ''' Reports <c>StreamReader.EndOfStream</c> read in an <see langword="Async"/> method or lambda.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzePropertyReference(context As OperationAnalysisContext)

            Dim reference As IPropertyReferenceOperation = DirectCast(context.Operation, IPropertyReferenceOperation)
            If Not UtilAnalyzerSemantic.IsMember(reference.Property, "System.IO.StreamReader", "EndOfStream") Then
                Exit Sub
            End If

            Dim boundary As SyntaxNode = UtilAnalyzerSyntax.GetExecutableBoundary(reference.Syntax)
            If Not UtilAnalyzerSyntax.IsAsyncBoundary(boundary) Then
                Exit Sub
            End If

            Dim access As MemberAccessExpressionSyntax = TryCast(reference.Syntax, MemberAccessExpressionSyntax)
            Dim location As Location = If(access Is Nothing, reference.Syntax.GetLocation(), access.Name.GetLocation())
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.EndOfStreamInAsyncMethod, location))
        End Sub

#End Region

    End Class

End Namespace

#End Region
