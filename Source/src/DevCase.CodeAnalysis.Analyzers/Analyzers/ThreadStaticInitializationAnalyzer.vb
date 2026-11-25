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
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ThreadStaticInitializationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>&lt;ThreadStatic&gt;</c> fields initialized inline (<c>As New T()</c>, <c>= value</c>) or assigned in the
    ''' <see langword="Shared"/> constructor (rule <see cref="DiagnosticIds.ThreadStaticFieldInitialization"/>; equivalent of CA2019 for
    ''' .NET Framework projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' A thread-static field has one value per thread, but its initializer and the <see langword="Shared"/> constructor run once, on
    ''' the thread that initializes the type: every other thread sees <see langword="Nothing"/> or the default value. Only
    ''' <see langword="Shared"/> fields are analyzed (fields of a <see langword="Module"/> are <see langword="Shared"/>); on an instance
    ''' field the attribute has no effect. There is no code fix: the field has to be initialized lazily where it is read
    ''' (<c>If state Is Nothing Then state = New StringBuilder()</c>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ThreadStaticInitializationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ThreadStaticFieldInitialization)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ThreadStaticInitializationAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ThreadStaticInitializationAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA2019 reports the same code.
            End If

            context.RegisterSymbolAction(UtilAnalyzerFiles.SkipExcludedSymbols(AddressOf ThreadStaticInitializationAnalyzer.AnalyzeField), SymbolKind.Field)
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(AddressOf ThreadStaticInitializationAnalyzer.AnalyzeAssignment),
                                            OperationKind.SimpleAssignment)
        End Sub


        ''' <summary>
        ''' Reports a thread-static field declared with an initializer.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The symbol analysis context.
        ''' </param>
        Private Shared Sub AnalyzeField(context As SymbolAnalysisContext)

            Dim field As IFieldSymbol = DirectCast(context.Symbol, IFieldSymbol)
            If Not ThreadStaticInitializationAnalyzer.IsThreadStatic(field) Then
                Exit Sub
            End If

            For Each reference As SyntaxReference In field.DeclaringSyntaxReferences
                Dim identifier As ModifiedIdentifierSyntax = TryCast(reference.GetSyntax(context.CancellationToken), ModifiedIdentifierSyntax)
                Dim declarator As VariableDeclaratorSyntax = If(identifier Is Nothing, Nothing, TryCast(identifier.Parent, VariableDeclaratorSyntax))
                If declarator IsNot Nothing AndAlso (declarator.Initializer IsNot Nothing OrElse TypeOf declarator.AsClause Is AsNewClauseSyntax) Then
                    context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThreadStaticFieldInitialization, identifier.Identifier.GetLocation(), field.Name))
                End If
            Next reference
        End Sub

        ''' <summary>
        ''' Reports the assignment of a thread-static field in the <see langword="Shared"/> constructor of its type.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeAssignment(context As OperationAnalysisContext)

            Dim assignment As ISimpleAssignmentOperation = DirectCast(context.Operation, ISimpleAssignmentOperation)
            Dim target As IFieldReferenceOperation = TryCast(assignment.Target, IFieldReferenceOperation)
            Dim constructor As IMethodSymbol = TryCast(context.ContainingSymbol, IMethodSymbol)
            If target Is Nothing OrElse constructor Is Nothing OrElse constructor.MethodKind <> MethodKind.SharedConstructor OrElse
               Not SymbolEqualityComparer.Default.Equals(target.Field.ContainingType, constructor.ContainingType) OrElse
               Not ThreadStaticInitializationAnalyzer.IsThreadStatic(target.Field) Then
                Exit Sub
            End If

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.ThreadStaticFieldInitialization, assignment.Syntax.GetLocation(), target.Field.Name))
        End Sub

        ''' <summary>
        ''' Determines whether a field is <see langword="Shared"/> and marked with <see cref="ThreadStaticAttribute"/>.
        ''' </summary>
        '''
        ''' <param name="field">
        ''' The field.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the field is thread-static; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsThreadStatic(field As IFieldSymbol) As Boolean

            Return field.IsStatic AndAlso field.GetAttributes().Any(
                Function(a As AttributeData) UtilAnalyzerSemantic.HasMetadataName(a.AttributeClass, "System.ThreadStaticAttribute"))
        End Function

#End Region

    End Class

End Namespace

#End Region
