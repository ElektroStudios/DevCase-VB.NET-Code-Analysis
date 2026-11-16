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

#End Region

#Region " DateTimeKindAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>New DateTime(...)</c> (or <c>New Date(...)</c>) calls that do not specify a <see cref="DateTimeKind"/> 
    ''' although a constructor overload accepts one (rule <see cref="DiagnosticIds.DateTimeWithoutKind"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The overload is matched through the semantic model: it must start with the same parameters, optionally followed by 
    ''' <see cref="Integer"/> time components (filled with <c>0</c> by the code fix), and end with a <see cref="DateTimeKind"/> parameter. 
    ''' For example, <c>New DateTime(year, month, day)</c> maps to <c>New DateTime(year, month, day, 0, 0, 0, kind)</c>, which creates the same value. 
    ''' Constructors without such an overload (e.g. the parameterless one, or those that take a <see cref="Globalization.Calendar"/> before the time) are not reported.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DateTimeKindAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that carries the comma-separated names of the parameters the fix appends 
        ''' (the time components filled with <c>0</c>, then the kind).
        ''' </summary>
        Public Const ParameterNamesPropertyKey As String = "ParameterNames"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.DateTimeWithoutKind)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DateTimeKindAnalyzer"/> class.
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
            context.RegisterOperationAction(AddressOf DateTimeKindAnalyzer.AnalyzeObjectCreation, OperationKind.ObjectCreation)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Finds the shortest constructor overload that extends the specified <see cref="DateTime"/> constructor with 
        ''' optional <see cref="Integer"/> time components and a trailing <see cref="DateTimeKind"/> parameter.
        ''' </summary>
        ''' 
        ''' <param name="constructor">
        ''' The invoked <see cref="DateTime"/> constructor.
        ''' </param>
        ''' 
        ''' <param name="kindType">
        ''' The <see cref="DateTimeKind"/> type symbol.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The overload, or <see langword="Nothing"/> if the constructor already has a kind or no such overload exists.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="constructor"/> or <paramref name="kindType"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindKindOverload(constructor As IMethodSymbol, kindType As ITypeSymbol) As IMethodSymbol

            If constructor Is Nothing Then
                Throw New ArgumentNullException(NameOf(constructor))
            End If
            If kindType Is Nothing Then
                Throw New ArgumentNullException(NameOf(kindType))
            End If

            ' The parameterless constructor (DateTime.MinValue) is excluded: filling the date components with 0 would be invalid.
            If constructor.Parameters.IsEmpty OrElse
               constructor.Parameters.Any(Function(p As IParameterSymbol) SymbolEqualityComparer.Default.Equals(p.Type, kindType)) Then
                Return Nothing
            End If

            Dim count As Integer = constructor.Parameters.Length
            Dim best As IMethodSymbol = Nothing
            For Each candidate As IMethodSymbol In constructor.ContainingType.InstanceConstructors
                If candidate.DeclaredAccessibility <> Accessibility.Public OrElse candidate.Parameters.Length <= count OrElse
                   Not SymbolEqualityComparer.Default.Equals(candidate.Parameters.Last().Type, kindType) Then
                    Continue For
                End If

                Dim matches As Boolean = True
                For index As Integer = 0 To candidate.Parameters.Length - 2
                    Dim candidateType As ITypeSymbol = candidate.Parameters(index).Type
                    If index < count Then
                        matches = SymbolEqualityComparer.Default.Equals(candidateType, constructor.Parameters(index).Type)
                    Else
                        matches = candidateType.SpecialType = SpecialType.System_Int32 ' A time component filled with 0.
                    End If
                    If Not matches Then
                        Exit For
                    End If
                Next index

                If matches AndAlso (best Is Nothing OrElse candidate.Parameters.Length < best.Parameters.Length) Then
                    best = candidate
                End If
            Next candidate
            Return best
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes an object creation operation.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        Private Shared Sub AnalyzeObjectCreation(context As OperationAnalysisContext)

            Dim creation As IObjectCreationOperation = DirectCast(context.Operation, IObjectCreationOperation)
            If creation.Constructor Is Nothing OrElse creation.Constructor.ContainingType.SpecialType <> SpecialType.System_DateTime Then
                Exit Sub
            End If

            Dim kindType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(GetType(DateTimeKind).FullName)
            If kindType Is Nothing Then
                Exit Sub
            End If

            Dim overload As IMethodSymbol = DateTimeKindAnalyzer.FindKindOverload(creation.Constructor, kindType)
            If overload Is Nothing Then
                Exit Sub
            End If

            Dim appended As IEnumerable(Of String) = overload.Parameters.Skip(creation.Constructor.Parameters.Length).Select(Function(p As IParameterSymbol) p.Name)
            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(DateTimeKindAnalyzer.ParameterNamesPropertyKey, String.Join(",", appended))

            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.DateTimeWithoutKind, creation.Syntax.GetLocation(), properties))
        End Sub

#End Region

    End Class

End Namespace

#End Region
