#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics

#End Region

#Region " FlagsEnumNoneAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports <c>&lt;Flags&gt;</c> enumerations without a member whose value is zero 
    ''' (rule <see cref="DiagnosticIds.FlagsEnumWithoutNone"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' A zero member already declared is never reported, whatever its name (<c>None</c>, <c>Null</c>, <c>Empty</c>...): 
    ''' renaming it would break the code that uses it. The code fix adds a zero member named <c>None</c>; when that name is 
    ''' already used by a member with another value, the first free name of <see cref="CandidateNames"/>.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class FlagsEnumNoneAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that carries the name of the member to add, as written in source 
        ''' (for example <c>None</c> or <c>[Nothing]</c>); absent when every candidate name is already used.
        ''' </summary>
        Public Const NamePropertyKey As String = "Name"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The names for the added zero member, in order of preference, as written in source (<c>Nothing</c> is a keyword, 
        ''' so it is written between brackets).
        ''' </summary>
        Friend Shared ReadOnly CandidateNames As ImmutableArray(Of String) = ImmutableArray.Create("None", "[Nothing]", "Null", "Zero", "Empty")

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.FlagsEnumWithoutNone)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FlagsEnumNoneAnalyzer"/> class.
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
            context.RegisterSymbolAction(UtilAnalyzerFiles.SkipExcludedSymbols(AddressOf FlagsEnumNoneAnalyzer.AnalyzeNamedType), SymbolKind.NamedType)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes a named type symbol.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The symbol analysis context.
        ''' </param>
        Private Shared Sub AnalyzeNamedType(context As SymbolAnalysisContext)

            Dim type As INamedTypeSymbol = DirectCast(context.Symbol, INamedTypeSymbol)
            If type.TypeKind <> TypeKind.Enum OrElse
               Not type.GetAttributes().Any(Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso
                                                                          a.AttributeClass.ToDisplayString().Equals(GetType(FlagsAttribute).FullName, StringComparison.Ordinal)) Then
                Exit Sub
            End If

            ' The value of each member is its constant value, also when it is implicit (the first member is 0).
            Dim members As List(Of IFieldSymbol) = type.GetMembers().OfType(Of IFieldSymbol)().Where(Function(f As IFieldSymbol) f.HasConstantValue).ToList()
            If members.Exists(Function(f As IFieldSymbol) FlagsEnumNoneAnalyzer.IsZero(f.ConstantValue)) Then
                Exit Sub
            End If

            Dim location As Location = type.Locations.FirstOrDefault(Function(l As Location) l.IsInSource)
            If location Is Nothing Then
                Exit Sub
            End If

            ' Visual Basic names are case-insensitive: "none" and "NONE" are the same name as "None".
            Dim name As String = FlagsEnumNoneAnalyzer.CandidateNames.FirstOrDefault(
                Function(candidate As String) Not members.Exists(Function(f As IFieldSymbol) f.Name.Equals(candidate.Trim("["c, "]"c), StringComparison.OrdinalIgnoreCase)))
            Dim properties As ImmutableDictionary(Of String, String) = If(name Is Nothing,
                                                                          ImmutableDictionary(Of String, String).Empty,
                                                                          ImmutableDictionary(Of String, String).Empty.Add(FlagsEnumNoneAnalyzer.NamePropertyKey, name))
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.FlagsEnumWithoutNone, location, properties, type.Name))
        End Sub

        ''' <summary>
        ''' Determines whether a constant value is zero.
        ''' </summary>
        ''' 
        ''' <param name="value">
        ''' The constant value of an enumeration member.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the value is zero; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsZero(value As Object) As Boolean

            Return value IsNot Nothing AndAlso Convert.ToDecimal(value, CultureInfo.InvariantCulture) = Decimal.Zero
        End Function

#End Region

    End Class

End Namespace

#End Region
