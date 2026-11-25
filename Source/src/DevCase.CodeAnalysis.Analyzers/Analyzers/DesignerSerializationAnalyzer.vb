#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Globalization
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " DesignerSerializationAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports read/write properties of components and controls that do not configure how the Windows Forms designer serializes
    ''' them (rule <see cref="DiagnosticIds.MissingDesignerSerializationConfiguration"/>; equivalent of WFO1000 for .NET Framework
    ''' projects).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The designer writes the value of every public property of a component into the <c>InitializeComponent</c> code (or the
    ''' resources) unless the property says otherwise, so a property that holds runtime data (a connection string, a token, a large
    ''' object) ends up serialized in the generated code. A property is configured by <c>&lt;DesignerSerializationVisibility&gt;</c>,
    ''' by <c>&lt;DefaultValue&gt;</c> (the designer only writes values that differ from it), or by a
    ''' <c>ShouldSerialize{Name}() As Boolean</c> method.
    ''' </para>
    ''' <para>
    ''' The rule analyzes the properties declared in classes that implement <c>System.ComponentModel.IComponent</c> (every
    ''' <c>Component</c>, <c>Control</c>, <c>Form</c> and <c>UserControl</c>): non-<see langword="Shared"/>, non-indexed properties that
    ''' are at least <see langword="Friend"/>, have a setter that is not <see langword="Private"/>, and do not override another property
    ''' (the base property carries its configuration). <see langword="WithEvents"/> variables and the <c>Site</c> implementation of
    ''' <c>IComponent</c> are not reported.
    ''' </para>
    ''' <para>
    ''' The code fix offers up to four attributes: <c>&lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)&gt;</c>,
    ''' which keeps the current behavior; <c>&lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Content)&gt;</c> for
    ''' properties of a class, interface or array type other than <see cref="String"/>, which makes the designer write the content of the
    ''' object (its properties, or the items of a collection) instead of the object itself; <c>&lt;DefaultValue(value)&gt;</c> for auto-implemented properties whose initial value is a constant
    ''' (or the default value of a numeric, <see cref="Boolean"/>, <see cref="Char"/> or enumeration type), which also stops the designer
    ''' from writing that value; and <c>&lt;DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)&gt;</c>, which stops
    ''' the designer from writing the property at all.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class DesignerSerializationAnalyzer : Inherits DiagnosticAnalyzer

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MissingDesignerSerializationConfiguration)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DesignerSerializationAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf DesignerSerializationAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule WFO1000 reports the same code.
            End If

            Dim componentType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.IComponent")
            Dim attributeType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DesignerSerializationVisibilityAttribute")
            Dim visibilityType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DesignerSerializationVisibility")
            If componentType Is Nothing OrElse attributeType Is Nothing OrElse visibilityType Is Nothing Then
                Exit Sub
            End If

            Dim defaultValueType As INamedTypeSymbol = context.Compilation.GetTypeByMetadataName("System.ComponentModel.DefaultValueAttribute")
            context.RegisterSyntaxNodeAction(UtilAnalyzerFiles.SkipExcludedNodes(
                Sub(c As SyntaxNodeAnalysisContext) DesignerSerializationAnalyzer.AnalyzeProperty(c, attributeType, visibilityType, defaultValueType)),
                SyntaxKind.PropertyStatement)
        End Sub


        ''' <summary>
        ''' Reports a property of a component that does not configure its designer serialization.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <param name="attributeType">
        ''' The <c>DesignerSerializationVisibilityAttribute</c> type.
        ''' </param>
        '''
        ''' <param name="visibilityType">
        ''' The <c>DesignerSerializationVisibility</c> enumeration.
        ''' </param>
        '''
        ''' <param name="defaultValueType">
        ''' The <c>DefaultValueAttribute</c> type; <see langword="Nothing"/> if the compilation does not reference it.
        ''' </param>
        Private Shared Sub AnalyzeProperty(context As SyntaxNodeAnalysisContext, attributeType As INamedTypeSymbol, visibilityType As INamedTypeSymbol,
                                           defaultValueType As INamedTypeSymbol)

            Dim statement As PropertyStatementSyntax = DirectCast(context.Node, PropertyStatementSyntax)
            Dim model As SemanticModel = context.SemanticModel
            Dim declared As IPropertySymbol = model.GetDeclaredSymbol(statement, context.CancellationToken)
            If declared Is Nothing OrElse Not DesignerSerializationAnalyzer.IsAnalyzed(declared) OrElse
               DesignerSerializationAnalyzer.IsConfigured(declared, attributeType, defaultValueType) Then
                Exit Sub
            End If

            Dim position As Integer = statement.SpanStart
            Dim attributeName As String = DesignerSerializationAnalyzer.GetAttributeName(model, position, attributeType)
            Dim visibilityName As String = UtilSpeculativeBinding.GetTypeName(model, position, visibilityType)
            Dim tree As SyntaxTree = statement.SyntaxTree
            Dim separator As String = $"{UtilAnalyzerSyntax.GetEndOfLine(tree)}{UtilAnalyzerSyntax.GetLineIndentation(statement.GetFirstToken())}"
            Dim actions As New List(Of FixReplacement)()
            If attributeName IsNot Nothing AndAlso visibilityName IsNot Nothing Then
                actions.Add(New FixReplacement("Add <DesignerSerializationVisibility(Visible)>", "Visible",
                                               $"<{attributeName}({visibilityName}.Visible)>{separator}"))
                If DesignerSerializationAnalyzer.HasSerializableContent(declared.Type) Then
                    actions.Add(New FixReplacement("Add <DesignerSerializationVisibility(Content)>", "Content",
                                                   $"<{attributeName}({visibilityName}.Content)>{separator}"))
                End If
                Dim defaultValue As String = DesignerSerializationAnalyzer.GetDefaultValueArgument(statement, declared, model, context)
                Dim defaultValueName As String = If(defaultValueType Is Nothing, Nothing, DesignerSerializationAnalyzer.GetAttributeName(model, position, defaultValueType))
                If defaultValue IsNot Nothing AndAlso defaultValueName IsNot Nothing Then
                    actions.Add(New FixReplacement($"Add <DefaultValue({defaultValue})>", "DefaultValue", $"<{defaultValueName}({defaultValue})>{separator}"))
                End If
                actions.Add(New FixReplacement("Add <DesignerSerializationVisibility(Hidden)>", "Hidden",
                                               $"<{attributeName}({visibilityName}.Hidden)>{separator}"))
            End If

            context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.MissingDesignerSerializationConfiguration, statement.Identifier.GetLocation(),
                                                                        {Location.Create(tree, New Text.TextSpan(position, 0))}, actions, declared.Name))
        End Sub

        ''' <summary>
        ''' Determines whether the designer serializes a property: a read/write, non-indexed instance property of a component.
        ''' </summary>
        '''
        ''' <param name="declared">
        ''' The property.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the rule analyzes the property; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAnalyzed(declared As IPropertySymbol) As Boolean

            Dim type As INamedTypeSymbol = declared.ContainingType
            Return Not declared.IsStatic AndAlso declared.Parameters.IsEmpty AndAlso Not declared.IsOverride AndAlso Not declared.IsWithEvents AndAlso
                   Not declared.IsImplicitlyDeclared AndAlso declared.SetMethod IsNot Nothing AndAlso
                   declared.SetMethod.DeclaredAccessibility <> Accessibility.Private AndAlso declared.DeclaredAccessibility >= Accessibility.Internal AndAlso
                   type IsNot Nothing AndAlso type.TypeKind = TypeKind.Class AndAlso UtilAnalyzerSemantic.IsOrDerivesFrom(type, "System.ComponentModel.IComponent") AndAlso
                   Not declared.ExplicitInterfaceImplementations.Any(
                Function(i As IPropertySymbol) i.Name.Equals("Site", StringComparison.Ordinal) AndAlso
                                               UtilAnalyzerSemantic.HasMetadataName(i.ContainingType, "System.ComponentModel.IComponent"))
        End Function

        ''' <summary>
        ''' Determines whether a property already configures its designer serialization.
        ''' </summary>
        '''
        ''' <param name="declared">
        ''' The property.
        ''' </param>
        '''
        ''' <param name="attributeType">
        ''' The <c>DesignerSerializationVisibilityAttribute</c> type.
        ''' </param>
        '''
        ''' <param name="defaultValueType">
        ''' The <c>DefaultValueAttribute</c> type; <see langword="Nothing"/> if the compilation does not reference it.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the property has one of the attributes (or a derived one), or if its type or a base type 
        ''' declares <c>ShouldSerialize{Name}() As Boolean</c>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsConfigured(declared As IPropertySymbol, attributeType As INamedTypeSymbol, defaultValueType As INamedTypeSymbol) As Boolean

            Dim hasAttribute As Boolean = declared.GetAttributes().Any(
                Function(a As AttributeData) UtilAnalyzerSemantic.IsOrDerivesFrom(a.AttributeClass, attributeType.ToDisplayString()) OrElse
                                             (defaultValueType IsNot Nothing AndAlso UtilAnalyzerSemantic.IsOrDerivesFrom(a.AttributeClass, defaultValueType.ToDisplayString())))
            If hasAttribute Then
                Return True
            End If

            Dim methodName As String = $"ShouldSerialize{declared.Name}"
            Dim current As INamedTypeSymbol = declared.ContainingType
            While current IsNot Nothing
                If current.GetMembers().OfType(Of IMethodSymbol)().Any(
                    Function(m As IMethodSymbol) m.Name.Equals(methodName, StringComparison.OrdinalIgnoreCase) AndAlso m.Parameters.IsEmpty AndAlso
                                                 m.ReturnType.SpecialType = SpecialType.System_Boolean) Then
                    Return True
                End If
                current = current.BaseType
            End While
            Return False
        End Function

        ''' <summary>
        ''' Determines whether the designer can serialize the content of a property instead of its value 
        ''' (<c>DesignerSerializationVisibility.Content</c>).
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type of the property.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for classes, interfaces and arrays other than <see cref="String"/> (the designer writes their 
        ''' properties or items); <see langword="False"/> for value types, whose content would be assigned to a copy (the generated 
        ''' code does not compile), and for <see cref="String"/>, type parameters and delegates, which have no content to write.
        ''' </returns>
        Private Shared Function HasSerializableContent(type As ITypeSymbol) As Boolean

            Select Case type.TypeKind
                Case TypeKind.Class
                    Return type.SpecialType <> SpecialType.System_String
                Case TypeKind.Interface, TypeKind.Array
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        ''' <summary>
        ''' Gets the name of an attribute as the code would write it at a position (without the <c>Attribute</c> suffix).
        ''' </summary>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position of the attribute.
        ''' </param>
        '''
        ''' <param name="attributeType">
        ''' The attribute type.
        ''' </param>
        '''
        ''' <returns>
        ''' The name; <see langword="Nothing"/> if no name binds to the type at that position.
        ''' </returns>
        Private Shared Function GetAttributeName(model As SemanticModel, position As Integer, attributeType As INamedTypeSymbol) As String

            Dim name As String = UtilSpeculativeBinding.GetTypeName(model, position, attributeType)
            Const Suffix As String = "Attribute"
            Return If(name IsNot Nothing AndAlso name.EndsWith(Suffix, StringComparison.Ordinal), name.Substring(0, name.Length - Suffix.Length), name)
        End Function

        ''' <summary>
        ''' Gets the argument of <c>&lt;DefaultValue&gt;</c> for an auto-implemented property: its constant initial value, or the 
        ''' default value of its type.
        ''' </summary>
        '''
        ''' <param name="statement">
        ''' The declaration of the property.
        ''' </param>
        '''
        ''' <param name="declared">
        ''' The property.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <returns>
        ''' The text of the argument, typed so that it binds to the overload of the property type (<c>5L</c>, <c>CByte(5)</c>, 
        ''' <c>Color.Red</c>...); <see langword="Nothing"/> for expanded properties, non-constant initial values, and types 
        ''' without an exact overload.
        ''' </returns>
        Private Shared Function GetDefaultValueArgument(statement As PropertyStatementSyntax, declared As IPropertySymbol, model As SemanticModel,
                                                        context As SyntaxNodeAnalysisContext) As String

            If TypeOf statement.Parent Is PropertyBlockSyntax OrElse TypeOf statement.AsClause Is AsNewClauseSyntax Then
                Return Nothing ' Expanded property (the initial value is unknown) or "As New".
            End If

            Dim type As ITypeSymbol = declared.Type
            Dim value As Object
            If statement.Initializer IsNot Nothing Then
                Dim constant As [Optional](Of Object) = model.GetConstantValue(statement.Initializer.Value, context.CancellationToken)
                If Not constant.HasValue OrElse constant.Value Is Nothing Then
                    Return Nothing
                End If
                value = constant.Value
            ElseIf type.IsValueType Then
                value = 0 ' The default value of the numeric, Boolean, Char and enumeration types handled below.
            Else
                Return Nothing
            End If

            Dim enumType As INamedTypeSymbol = TryCast(type, INamedTypeSymbol)
            If enumType IsNot Nothing AndAlso enumType.TypeKind = TypeKind.Enum Then
                Return DesignerSerializationAnalyzer.GetEnumMember(enumType, value, model, statement.SpanStart)
            End If

            Try
                Select Case type.SpecialType
                    Case SpecialType.System_Boolean
                        Return If(Convert.ToBoolean(value, CultureInfo.InvariantCulture), "True", "False")
                    Case SpecialType.System_Byte
                        Return $"CByte({Convert.ToByte(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)})"
                    Case SpecialType.System_Int16
                        Return $"{Convert.ToInt16(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}S"
                    Case SpecialType.System_Int32
                        Return Convert.ToInt32(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)
                    Case SpecialType.System_Int64
                        Return $"{Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture)}L"
                    Case SpecialType.System_Single
                        Dim number As Single = Convert.ToSingle(value, CultureInfo.InvariantCulture)
                        Return If(Single.IsNaN(number) OrElse Single.IsInfinity(number), Nothing, $"{number.ToString("R", CultureInfo.InvariantCulture)}F")
                    Case SpecialType.System_Double
                        Dim number As Double = Convert.ToDouble(value, CultureInfo.InvariantCulture)
                        Return If(Double.IsNaN(number) OrElse Double.IsInfinity(number), Nothing, $"{number.ToString("R", CultureInfo.InvariantCulture)}R")
                    Case SpecialType.System_Char
                        Dim character As Char = If(TypeOf value Is Char, DirectCast(value, Char), Convert.ToChar(value, CultureInfo.InvariantCulture))
                        Return If(Char.IsControl(character), Nothing, $"""{If(character = """"c, """""", character.ToString())}""c")
                    Case SpecialType.System_String
                        Dim text As String = DirectCast(value, String)
                        Return If(text.Any(Function(c As Char) Char.IsControl(c)), Nothing, $"""{New System.Text.StringBuilder(text).Replace("""", """""")}""")
                    Case Else
                        Return Nothing ' Decimal and Date cannot be attribute arguments; other types have no exact overload.
                End Select
            Catch ex As Exception When TypeOf ex Is InvalidCastException OrElse TypeOf ex Is OverflowException OrElse TypeOf ex Is FormatException
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Gets the member of an enumeration that has a value.
        ''' </summary>
        '''
        ''' <param name="enumType">
        ''' The enumeration.
        ''' </param>
        '''
        ''' <param name="value">
        ''' The value, of any integral type.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the member would be written.
        ''' </param>
        '''
        ''' <returns>
        ''' The text of the member (<c>Color.Red</c>); <see langword="Nothing"/> if no member has that value.
        ''' </returns>
        Private Shared Function GetEnumMember(enumType As INamedTypeSymbol, value As Object, model As SemanticModel, position As Integer) As String

            Dim typeName As String = UtilSpeculativeBinding.GetTypeName(model, position, enumType)
            Dim number As Decimal
            Try
                number = Convert.ToDecimal(value, CultureInfo.InvariantCulture)
            Catch ex As Exception When TypeOf ex Is InvalidCastException OrElse TypeOf ex Is OverflowException OrElse TypeOf ex Is FormatException
                Return Nothing
            End Try

            Dim member As IFieldSymbol = enumType.GetMembers().OfType(Of IFieldSymbol)().FirstOrDefault(
                Function(f As IFieldSymbol) f.HasConstantValue AndAlso Convert.ToDecimal(f.ConstantValue, CultureInfo.InvariantCulture) = number)
            Return If(member Is Nothing OrElse typeName Is Nothing, Nothing, $"{typeName}.{UtilAnalyzerSyntax.EscapeIdentifier(member.Name)}")
        End Function

#End Region

    End Class

End Namespace

#End Region
