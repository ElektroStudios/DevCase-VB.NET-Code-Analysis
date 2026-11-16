#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Globalization
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations

#End Region

#Region " EncodingAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports method calls and object creations that do not pass a <see cref="System.Text.Encoding"/> although an overload accepts one 
    ''' (rule <see cref="DiagnosticIds.EncodingNotSpecified"/>), e.g. <c>File.WriteAllText(path, text)</c> or <c>New StreamReader(path)</c>.
    ''' </summary>
    ''' 
    ''' <remarks>
    ''' The overload is matched through the semantic model: it must have the same parameters plus one <see cref="System.Text.Encoding"/> 
    ''' parameter inserted at any position (e.g. <c>New StreamReader(path, True)</c> maps to <c>New StreamReader(path, encoding, True)</c>),
    ''' or an <c>append As Boolean</c> parameter followed by the encoding (<c>New StreamWriter(path)</c> maps to
    ''' <c>New StreamWriter(path, False, encoding)</c>, which is what the original constructor does). 
    ''' Members of <see cref="System.Text.Encoding"/> itself and extension methods are not analyzed.
    ''' <para></para>
    ''' The diagnostic carries the encoding that the call already uses implicitly, so that the first code action keeps the behavior: 
    ''' UTF-8 with a byte order mark for the <c>Microsoft.VisualBasic</c> file APIs (<c>My.Computer.FileSystem</c>), 
    ''' and UTF-8 without a byte order mark for everything else (<c>System.IO</c>).
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class EncodingAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The diagnostic property that carries the zero-based argument index where the encoding is inserted.
        ''' </summary>
        Public Const InsertIndexPropertyKey As String = "InsertIndex"

        ''' <summary>
        ''' The diagnostic property that carries the name of the encoding parameter.
        ''' </summary>
        Public Const EncodingParameterPropertyKey As String = "EncodingParameter"

        ''' <summary>
        ''' The diagnostic property that carries the name of the <c>append As Boolean</c> parameter inserted before the encoding,
        ''' or an empty string when the overload does not require it.
        ''' </summary>
        Public Const AppendParameterPropertyKey As String = "AppendParameter"

        ''' <summary>
        ''' The diagnostic property that carries the encoding that the call uses implicitly: 
        ''' <see cref="Utf8WithoutBomValue"/> or <see cref="Utf8WithBomValue"/>.
        ''' </summary>
        Public Const ImplicitEncodingPropertyKey As String = "ImplicitEncoding"

        ''' <summary>
        ''' UTF-8 without a byte order mark (the default of the <c>System.IO</c> text APIs).
        ''' </summary>
        Public Const Utf8WithoutBomValue As String = "Utf8WithoutBom"

        ''' <summary>
        ''' UTF-8 with a byte order mark (the default of the <c>Microsoft.VisualBasic.FileIO</c> text APIs).
        ''' </summary>
        Public Const Utf8WithBomValue As String = "Utf8WithBom"

        ''' <summary>
        ''' The metadata name of the <see cref="System.Text.Encoding"/> type.
        ''' </summary>
        Friend Const EncodingTypeName As String = "System.Text.Encoding"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.EncodingNotSpecified)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="EncodingAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf EncodingAnalyzer.OnCompilationStart)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Finds the overload that inserts one <see cref="System.Text.Encoding"/> parameter into the parameter list of a method or constructor.
        ''' </summary>
        ''' 
        ''' <param name="method">
        ''' The invoked method or constructor.
        ''' </param>
        ''' 
        ''' <param name="encodingType">
        ''' The <see cref="System.Text.Encoding"/> type symbol.
        ''' </param>
        ''' 
        ''' <param name="insertIndex">
        ''' When this method returns, the zero-based index of the first inserted parameter in the overload.
        ''' </param>
        ''' 
        ''' <param name="insertsAppend">
        ''' When this method returns, <see langword="True"/> if the overload also requires an <c>append As Boolean</c> parameter
        ''' just before the encoding (e.g. <c>New StreamWriter(path, append, encoding)</c>), which the fix fills with <see langword="False"/>.
        ''' </param>
        ''' 
        ''' <returns>
        ''' The overload, or <see langword="Nothing"/> if the method already takes an encoding or no such overload exists.
        ''' </returns>
        ''' 
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="method"/> or <paramref name="encodingType"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function FindEncodingOverload(method As IMethodSymbol, encodingType As ITypeSymbol, ByRef insertIndex As Integer,
                                                    ByRef insertsAppend As Boolean) As IMethodSymbol

            If method Is Nothing Then
                Throw New ArgumentNullException(NameOf(method))
            End If
            If encodingType Is Nothing Then
                Throw New ArgumentNullException(NameOf(encodingType))
            End If

            insertIndex = -1
            insertsAppend = False
            If method.Parameters.Any(Function(p As IParameterSymbol) SymbolEqualityComparer.Default.Equals(p.Type, encodingType)) Then
                Return Nothing
            End If

            Dim candidates As IEnumerable(Of IMethodSymbol) =
                If(method.MethodKind = MethodKind.Constructor,
                   method.ContainingType.InstanceConstructors.AsEnumerable(),
                   method.ContainingType.GetMembers(method.Name).OfType(Of IMethodSymbol)().Where(Function(m As IMethodSymbol) m.IsStatic = method.IsStatic))

            ' First choice: the same parameters plus the encoding; second choice: plus "append As Boolean" and the encoding.
            Dim count As Integer = method.Parameters.Length
            For extra As Integer = 1 To 2
                For Each candidate As IMethodSymbol In candidates
                    If candidate.DeclaredAccessibility <> Accessibility.Public OrElse candidate.Parameters.Length <> count + extra OrElse
                       candidate.TypeParameters.Length <> method.TypeParameters.Length Then
                        Continue For
                    End If

                    For index As Integer = 0 To count
                        If EncodingAnalyzer.IsInsertion(method, candidate, index, extra, encodingType) Then
                            insertIndex = index
                            insertsAppend = extra = 2
                            Return candidate
                        End If
                    Next index
                Next candidate
            Next extra
            Return Nothing
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Determines whether a candidate overload equals the original method with an encoding parameter inserted at the specified index.
        ''' </summary>
        ''' 
        ''' <param name="method">
        ''' The original method.
        ''' </param>
        ''' 
        ''' <param name="candidate">
        ''' The candidate overload.
        ''' </param>
        ''' 
        ''' <param name="index">
        ''' The insertion index.
        ''' </param>
        ''' 
        ''' <param name="extra">
        ''' The number of inserted parameters: 1 (the encoding) or 2 (<c>append As Boolean</c> and the encoding).
        ''' </param>
        ''' 
        ''' <param name="encodingType">
        ''' The <see cref="System.Text.Encoding"/> type symbol.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the candidate matches; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsInsertion(method As IMethodSymbol, candidate As IMethodSymbol, index As Integer, extra As Integer,
                                            encodingType As ITypeSymbol) As Boolean

            If Not SymbolEqualityComparer.Default.Equals(candidate.Parameters(index + extra - 1).Type, encodingType) Then
                Return False
            End If
            If extra = 2 Then
                Dim append As IParameterSymbol = candidate.Parameters(index)
                If append.Type.SpecialType <> SpecialType.System_Boolean OrElse Not append.Name.Equals("append", StringComparison.Ordinal) Then
                    Return False
                End If
            End If

            For position As Integer = 0 To method.Parameters.Length - 1
                Dim original As IParameterSymbol = method.Parameters(position)
                Dim mapped As IParameterSymbol = candidate.Parameters(If(position < index, position, position + extra))
                If original.RefKind <> mapped.RefKind OrElse Not SymbolEqualityComparer.Default.Equals(original.Type, mapped.Type) Then
                    Return False
                End If
            Next position
            Return True
        End Function

        ''' <summary>
        ''' Resolves the <see cref="System.Text.Encoding"/> type and registers the operation actions.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The compilation start analysis context.
        ''' </param>
        Private Shared Sub OnCompilationStart(context As CompilationStartAnalysisContext)

            Dim encodingType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName(EncodingAnalyzer.EncodingTypeName)
            If encodingType Is Nothing Then
                Exit Sub
            End If

            context.RegisterOperationAction(
                Sub(c As OperationAnalysisContext)
                    Dim invocation As IInvocationOperation = DirectCast(c.Operation, IInvocationOperation)
                    Dim method As IMethodSymbol = invocation.TargetMethod
                    If method.IsExtensionMethod OrElse method.ReducedFrom IsNot Nothing Then
                        Exit Sub
                    End If
                    EncodingAnalyzer.Analyze(c, method, $"{method.ContainingType.Name}.{method.Name}", encodingType)
                End Sub, OperationKind.Invocation)

            context.RegisterOperationAction(
                Sub(c As OperationAnalysisContext)
                    Dim creation As IObjectCreationOperation = DirectCast(c.Operation, IObjectCreationOperation)
                    If creation.Constructor IsNot Nothing Then
                        EncodingAnalyzer.Analyze(c, creation.Constructor, $"New {creation.Constructor.ContainingType.Name}", encodingType)
                    End If
                End Sub, OperationKind.ObjectCreation)
        End Sub

        ''' <summary>
        ''' Reports the operation when its method or constructor has an overload that takes an encoding.
        ''' </summary>
        ''' 
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        ''' 
        ''' <param name="method">
        ''' The invoked method or constructor.
        ''' </param>
        ''' 
        ''' <param name="display">
        ''' The text shown in the diagnostic message.
        ''' </param>
        ''' 
        ''' <param name="encodingType">
        ''' The <see cref="System.Text.Encoding"/> type symbol.
        ''' </param>
        Private Shared Sub Analyze(context As OperationAnalysisContext, method As IMethodSymbol, display As String, encodingType As INamedTypeSymbol)

            ' The members of Encoding (and its derived types) are not text APIs that "use" an encoding.
            Dim current As INamedTypeSymbol = method.ContainingType
            While current IsNot Nothing
                If SymbolEqualityComparer.Default.Equals(current, encodingType) Then
                    Exit Sub
                End If
                current = current.BaseType
            End While

            Dim insertIndex As Integer
            Dim insertsAppend As Boolean
            Dim overload As IMethodSymbol = EncodingAnalyzer.FindEncodingOverload(method, encodingType, insertIndex, insertsAppend)
            If overload Is Nothing Then
                Exit Sub
            End If

            Dim containingNamespace As String = method.ContainingType.ContainingNamespace.ToDisplayString()
            Dim implicitEncoding As String = If(containingNamespace.StartsWith("Microsoft.VisualBasic", StringComparison.Ordinal),
                                                EncodingAnalyzer.Utf8WithBomValue,
                                                EncodingAnalyzer.Utf8WithoutBomValue)

            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary(Of String, String).Empty.
                Add(EncodingAnalyzer.InsertIndexPropertyKey, insertIndex.ToString(CultureInfo.InvariantCulture)).
                Add(EncodingAnalyzer.EncodingParameterPropertyKey, overload.Parameters(insertIndex + If(insertsAppend, 1, 0)).Name).
                Add(EncodingAnalyzer.AppendParameterPropertyKey, If(insertsAppend, overload.Parameters(insertIndex).Name, String.Empty)).
                Add(EncodingAnalyzer.ImplicitEncodingPropertyKey, implicitEncoding)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.EncodingNotSpecified, context.Operation.Syntax.GetLocation(), properties, display))
        End Sub

#End Region

    End Class

End Namespace

#End Region
