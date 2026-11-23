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
    ''' Reports <c>&lt;Flags&gt;</c> enumerations whose zero value is not a member named <c>None</c> 
    ''' (rule <see cref="DiagnosticIds.FlagsEnumWithoutNone"/>).
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' Two cases are reported, each with its own code fix:
    ''' <list type="bullet">
    '''   <item><description>No member has the value zero: the fix adds <c>None = 0</c> as the first member.</description></item>
    '''   <item><description>A member has the value zero but another name (e.g. <c>Empty = 0</c>): the fix renames it to <c>None</c> 
    '''   in the whole solution.</description></item>
    ''' </list>
    ''' When a member named <c>None</c> already exists with a non-zero value, the diagnostic is reported without a fix.
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class FlagsEnumNoneAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that carries the kind of problem: <see cref="AddKind"/>, <see cref="RenameKind"/> or <see cref="ConflictKind"/>.
        ''' </summary>
        Public Const KindPropertyKey As String = "Kind"

        ''' <summary>
        ''' No member has the value zero.
        ''' </summary>
        Public Const AddKind As String = "Add"

        ''' <summary>
        ''' The zero member has a name other than <c>None</c>.
        ''' </summary>
        Public Const RenameKind As String = "Rename"

        ''' <summary>
        ''' A member named <c>None</c> already exists with a non-zero value; no fix is possible.
        ''' </summary>
        Public Const ConflictKind As String = "Conflict"

        ''' <summary>
        ''' The expected name of the zero member.
        ''' </summary>
        Public Const NoneName As String = "None"

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
        ''' Analyzes a named type.
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

            Dim members As List(Of IFieldSymbol) = type.GetMembers().OfType(Of IFieldSymbol)().Where(Function(f As IFieldSymbol) f.HasConstantValue).ToList()
            Dim zeroMembers As List(Of IFieldSymbol) = members.Where(Function(f As IFieldSymbol) FlagsEnumNoneAnalyzer.IsZero(f.ConstantValue)).ToList()
            If zeroMembers.Exists(Function(f As IFieldSymbol) f.Name.Equals(FlagsEnumNoneAnalyzer.NoneName, StringComparison.Ordinal)) Then
                Exit Sub
            End If

            Dim noneConflict As Boolean = members.Exists(Function(f As IFieldSymbol) f.Name.Equals(FlagsEnumNoneAnalyzer.NoneName, StringComparison.OrdinalIgnoreCase))
            Dim kind As String
            Dim location As Location
            If noneConflict Then
                kind = FlagsEnumNoneAnalyzer.ConflictKind
                location = type.Locations.FirstOrDefault(Function(l As Location) l.IsInSource)
            ElseIf zeroMembers.Count = 0 Then
                kind = FlagsEnumNoneAnalyzer.AddKind
                location = type.Locations.FirstOrDefault(Function(l As Location) l.IsInSource)
            Else
                kind = FlagsEnumNoneAnalyzer.RenameKind
                location = zeroMembers(0).Locations.FirstOrDefault(Function(l As Location) l.IsInSource)
            End If
            If location Is Nothing Then
                Exit Sub
            End If

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.Add(FlagsEnumNoneAnalyzer.KindPropertyKey, kind)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.FlagsEnumWithoutNone, location, properties, type.Name))
        End Sub

        ''' <summary>
        ''' Determines whether an enumeration constant value is zero.
        ''' </summary>
        ''' 
        ''' <param name="value">
        ''' The boxed constant value.
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
