#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " UnvalidatedParameterAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports parameters of externally visible methods and constructors that are used without being validated
    ''' (rule <see cref="DiagnosticIds.UnvalidatedParameter"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Analyzed parameters, by type:
    ''' <list type="bullet">
    '''   <item><description>Enumerations without <see cref="FlagsAttribute"/>: an undefined value can be passed (e.g. <c>CType(99, MyEnum)</c>).</description></item>
    '''   <item><description><see cref="String"/>: <see langword="Nothing"/>, and possibly an empty string, can be passed.</description></item>
    '''   <item><description>Other reference types (classes, interfaces, delegates, arrays, type parameters constrained to <see langword="Class"/>).</description></item>
    ''' </list>
    ''' Other value types and <c>Nullable(Of T)</c> are not analyzed: a value type cannot be <see langword="Nothing"/>
    ''' (<c>value = Nothing</c> compares it with its default value, which is usually valid), and a nullable parameter accepts
    ''' <see langword="Nothing"/> on purpose. Flags enumerations are not analyzed because <c>[Enum].IsDefined</c> rejects combined values.
    ''' </para>
    ''' <para>
    ''' A parameter is not reported when it is <see langword="Optional"/>, <see langword="ByRef"/> or a <see langword="ParamArray"/>;
    ''' when it has the <c>AllowNull</c> attribute; when its type derives from <see cref="EventArgs"/>; when it is never used; 
    ''' when a <see cref="String"/> or reference parameter is never consumed (it is only stored, returned, compared or concatenated, 
    ''' where <see langword="Nothing"/> does not fail; see <see cref="IsConsumed"/>); 
    ''' when it is passed to the <c>MyBase.New</c> / <c>Me.New</c> call of a constructor (the called constructor validates it), 
    ''' or to a method of the same compilation that validates it (see <see cref="IsValidatedByCallee"/>); 
    ''' and when any use of it looks like a validation: a condition of <see langword="If"/>, <see langword="ElseIf"/>,
    ''' <see langword="Select Case"/>, <see langword="While"/> or <see langword="Do"/>; an operand of <see langword="Is"/>,
    ''' <see langword="IsNot"/> or <see langword="TypeOf"/>; the first operand of <c>If(x, y)</c> or the condition of <c>If(c, x, y)</c>;
    ''' the target of <c>?.</c>; the argument of <see langword="NameOf"/>; or an argument of a method whose name starts with
    ''' <c>ThrowIf</c>, <c>Validate</c>, <c>Ensure</c>, <c>Check</c>, <c>Verify</c>, <c>Guard</c>, <c>Assert</c> or <c>Require</c>.
    ''' </para>
    ''' <para>
    ''' Analyzed methods: methods and constructors with a body that are callable from other assemblies (<see langword="Public"/>,
    ''' <see langword="Protected"/> or <see langword="Protected Friend"/>, in types that are also visible). Event handlers,
    ''' <c>Shared Sub Main</c>, <c>Equals</c>, <c>ReferenceEquals</c>, <c>CompareTo</c> and <c>Compare</c> (which accept <see langword="Nothing"/> by contract),
    ''' overrides and implementations of members declared in referenced assemblies (their contract is defined there, and may accept 
    ''' <see langword="Nothing"/>), and methods whose body only throws an exception are not analyzed.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class UnvalidatedParameterAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the kind of the parameter:
        ''' <see cref="EnumKind"/>, <see cref="StringKind"/> or <see cref="ReferenceKind"/>.
        ''' </summary>
        Friend Const KindPropertyKey As String = "Kind"

        ''' <summary>
        ''' The parameter is an enumeration.
        ''' </summary>
        Friend Const EnumKind As String = "Enum"

        ''' <summary>
        ''' The parameter is a <see cref="String"/>.
        ''' </summary>
        Friend Const StringKind As String = "String"

        ''' <summary>
        ''' The parameter is of another reference type.
        ''' </summary>
        Friend Const ReferenceKind As String = "Reference"

        ''' <summary>
        ''' The key of the diagnostic property that holds the shortest valid name of the enumeration type.
        ''' </summary>
        Friend Const EnumTypePropertyKey As String = "EnumType"

        ''' <summary>
        ''' The key of the diagnostic property that holds the shortest valid name of <c>System.ComponentModel.InvalidEnumArgumentException</c>,
        ''' or an empty string when that exception cannot be used (the type is not referenced, or the enumeration values do not fit in an <see cref="Integer"/>).
        ''' </summary>
        Friend Const InvalidEnumExceptionPropertyKey As String = "InvalidEnumArgumentException"

        ''' <summary>
        ''' The key of the diagnostic property that holds the shortest valid name of <see cref="ArgumentNullException"/>.
        ''' </summary>
        Friend Const ArgumentNullExceptionPropertyKey As String = "ArgumentNullException"

        ''' <summary>
        ''' The key of the diagnostic property that holds the shortest valid name of <see cref="ArgumentOutOfRangeException"/>.
        ''' </summary>
        Friend Const ArgumentOutOfRangeExceptionPropertyKey As String = "ArgumentOutOfRangeException"

        ''' <summary>
        ''' The key of the diagnostic property that holds the shortest valid name of <see cref="ArgumentException"/>.
        ''' </summary>
        Friend Const ArgumentExceptionPropertyKey As String = "ArgumentException"

#End Region

#Region " Fields "

        ''' <summary>
        ''' The name prefixes of methods that validate their arguments (e.g. <c>ArgumentNullException.ThrowIfNull</c>, <c>Debug.Assert</c>).
        ''' </summary>
        Friend Shared ReadOnly ValidationMethodPrefixes As ImmutableArray(Of String) =
            ImmutableArray.Create("ThrowIf", "Validate", "Ensure", "Check", "Verify", "Guard", "Assert", "Require")

        ''' <summary>
        ''' The names of methods that accept <see langword="Nothing"/> arguments by contract.
        ''' </summary>
        Private Shared ReadOnly IgnoredMethodNames As ImmutableHashSet(Of String) =
            ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Equals", "ReferenceEquals", "CompareTo", "Compare")

        ''' <summary>
        ''' Framework methods (<c>Type.Method</c>, matched on the called method and the methods it overrides) whose arguments accept <see langword="Nothing"/> by contract, 
        ''' as a valid input: output and concatenation methods, and object identity and equality. String comparisons such as 
        ''' <c>dir.Equals(directory, comparison)</c> are deliberately not listed: there, <see langword="Nothing"/> silently returns 
        ''' <see langword="False"/> and hides the missing argument.
        ''' </summary>
        Private Shared ReadOnly NullAcceptingMethods As ImmutableHashSet(Of String) = ImmutableHashSet.Create(StringComparer.Ordinal,
            "System.Object.ReferenceEquals", "System.Object.Equals", "System.String.Concat", "System.String.IsNullOrEmpty",
            "System.String.IsNullOrWhiteSpace", "System.Convert.ToString",
            "System.Console.Write", "System.Console.WriteLine", "System.IO.TextWriter.Write", "System.IO.TextWriter.WriteLine",
            "System.Text.StringBuilder.Append", "System.Text.StringBuilder.AppendLine", "System.Text.StringBuilder.Insert",
            "System.Diagnostics.Debug.Write", "System.Diagnostics.Debug.WriteLine", "System.Diagnostics.Debug.WriteIf", "System.Diagnostics.Debug.WriteLineIf",
            "System.Diagnostics.Debug.Print", "System.Diagnostics.Trace.Write", "System.Diagnostics.Trace.WriteLine", "System.Diagnostics.Trace.WriteIf",
            "System.Diagnostics.Trace.WriteLineIf", "Microsoft.VisualBasic.Information.IsNothing")

        ''' <summary>
        ''' Framework types whose methods all accept <see langword="Nothing"/> arguments (Visual Basic string functions treat it as an empty string).
        ''' </summary>
        Private Shared ReadOnly NullAcceptingTypes As ImmutableHashSet(Of String) = ImmutableHashSet.Create(StringComparer.Ordinal,
            "Microsoft.VisualBasic.Strings")

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.UnvalidatedParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="UnvalidatedParameterAnalyzer"/> class.
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
            context.RegisterSyntaxNodeAction(AddressOf UnvalidatedParameterAnalyzer.AnalyzeMethod,
                                             SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock)
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the constructor call (<c>MyBase.New</c>, <c>MyClass.New</c> or <c>Me.New</c>) that starts a constructor body.
        ''' </summary>
        '''
        ''' <param name="block">
        ''' The method block.
        ''' </param>
        '''
        ''' <returns>
        ''' The first statement when it is a constructor call; otherwise, <see langword="Nothing"/>.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="block"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetConstructorCall(block As MethodBlockBaseSyntax) As StatementSyntax

            If block Is Nothing Then
                Throw New ArgumentNullException(NameOf(block))
            End If

            If Not block.IsKind(SyntaxKind.ConstructorBlock) OrElse block.Statements.Count = 0 Then
                Return Nothing
            End If

            Dim first As StatementSyntax = block.Statements(0)
            Dim expression As ExpressionSyntax = If(TypeOf first Is ExpressionStatementSyntax, DirectCast(first, ExpressionStatementSyntax).Expression,
                                                    If(TypeOf first Is CallStatementSyntax, DirectCast(first, CallStatementSyntax).Invocation, Nothing))
            Dim invocation As InvocationExpressionSyntax = TryCast(expression, InvocationExpressionSyntax)
            Dim memberAccess As MemberAccessExpressionSyntax = If(invocation Is Nothing, Nothing, TryCast(invocation.Expression, MemberAccessExpressionSyntax))
            Dim isConstructorCall As Boolean =
                memberAccess IsNot Nothing AndAlso memberAccess.Name.Identifier.ValueText.Equals("New", StringComparison.OrdinalIgnoreCase) AndAlso
                (memberAccess.Expression.IsKind(SyntaxKind.MyBaseExpression) OrElse memberAccess.Expression.IsKind(SyntaxKind.MyClassExpression) OrElse
                 memberAccess.Expression.IsKind(SyntaxKind.MeExpression))
            Return If(isConstructorCall, first, Nothing)
        End Function

        ''' <summary>
        ''' Gets the simple name of an invoked method (<c>ThrowIfNull</c> for <c>ArgumentNullException.ThrowIfNull(x)</c>).
        ''' </summary>
        '''
        ''' <param name="invocation">
        ''' The invocation.
        ''' </param>
        '''
        ''' <returns>
        ''' The method name, or an empty string if the invoked expression is not a name.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="invocation"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetInvokedName(invocation As InvocationExpressionSyntax) As String

            If invocation Is Nothing Then
                Throw New ArgumentNullException(NameOf(invocation))
            End If

            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(invocation.Expression, MemberAccessExpressionSyntax)
            If memberAccess IsNot Nothing Then
                Return memberAccess.Name.Identifier.ValueText
            End If
            Dim name As SimpleNameSyntax = TryCast(invocation.Expression, SimpleNameSyntax)
            Return If(name Is Nothing, String.Empty, name.Identifier.ValueText)
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Analyzes the parameters of a method or constructor.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        Private Shared Sub AnalyzeMethod(context As SyntaxNodeAnalysisContext)

            Dim block As MethodBlockBaseSyntax = DirectCast(context.Node, MethodBlockBaseSyntax)
            Dim parameterList As ParameterListSyntax = block.BlockStatement.ParameterList
            If parameterList Is Nothing OrElse parameterList.Parameters.Count = 0 OrElse block.Statements.Count = 0 OrElse
               (block.Statements.Count = 1 AndAlso TypeOf block.Statements(0) Is ThrowStatementSyntax) Then
                Exit Sub
            End If

            Dim method As IMethodSymbol = context.SemanticModel.GetDeclaredSymbol(block, context.CancellationToken)
            If method Is Nothing OrElse method.Parameters.Length <> parameterList.Parameters.Count OrElse
               Not UnvalidatedParameterAnalyzer.IsAnalyzedMethod(method, block) Then
                Exit Sub
            End If

            Dim references As Dictionary(Of IParameterSymbol, List(Of IdentifierNameSyntax)) =
                UnvalidatedParameterAnalyzer.GetReferences(block, method, context.SemanticModel, context)
            Dim constructorCall As StatementSyntax = UnvalidatedParameterAnalyzer.GetConstructorCall(block)
            Dim position As Integer = block.Statements(0).SpanStart

            For Each parameter As IParameterSymbol In method.Parameters
                Dim kind As String = UnvalidatedParameterAnalyzer.GetKind(parameter)
                Dim uses As List(Of IdentifierNameSyntax) = Nothing
                If kind Is Nothing OrElse Not references.TryGetValue(parameter, uses) OrElse
                   uses.Exists(Function(use As IdentifierNameSyntax) UnvalidatedParameterAnalyzer.IsValidation(use)) OrElse
                   (constructorCall IsNot Nothing AndAlso uses.Exists(Function(use As IdentifierNameSyntax) constructorCall.Span.Contains(use.Span))) OrElse
                   uses.Exists(Function(use As IdentifierNameSyntax) UnvalidatedParameterAnalyzer.IsValidatedByCallee(use, context)) Then
                    Continue For
                End If

                ' Nothing only fails when a reference is consumed: stored, returned or concatenated, it is not an error by itself.
                If Not kind.Equals(UnvalidatedParameterAnalyzer.EnumKind, StringComparison.Ordinal) AndAlso
                   Not uses.Exists(Function(use As IdentifierNameSyntax) UnvalidatedParameterAnalyzer.IsConsumed(use, context)) Then
                    Continue For
                End If

                Dim properties As ImmutableDictionary(Of String, String) =
                    UnvalidatedParameterAnalyzer.GetProperties(parameter, kind, context.SemanticModel, position)
                Dim identifier As SyntaxToken = parameterList.Parameters(parameter.Ordinal).Identifier.Identifier
                Dim methodName As String = If(method.MethodKind = MethodKind.Constructor, "New", method.Name)
                context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.UnvalidatedParameter, identifier.GetLocation(), properties,
                                                           parameter.Name, $"{method.ContainingType.Name}.{methodName}"))
            Next parameter
        End Sub

        ''' <summary>
        ''' Determines whether the parameters of a method are analyzed.
        ''' </summary>
        '''
        ''' <param name="method">
        ''' The method symbol.
        ''' </param>
        '''
        ''' <param name="block">
        ''' The method block.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for externally visible methods and constructors that are not event handlers, entry points
        ''' or comparison methods; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsAnalyzedMethod(method As IMethodSymbol, block As MethodBlockBaseSyntax) As Boolean

            If (method.MethodKind <> MethodKind.Ordinary AndAlso method.MethodKind <> MethodKind.Constructor) OrElse
               UnvalidatedParameterAnalyzer.IgnoredMethodNames.Contains(method.Name) OrElse
               (method.IsStatic AndAlso method.Name.Equals("Main", StringComparison.OrdinalIgnoreCase)) Then
                Return False
            End If

            ' Event handlers: a Handles clause, or the (sender As Object, e As EventArgs) signature.
            Dim statement As MethodStatementSyntax = TryCast(block.BlockStatement, MethodStatementSyntax)
            If (statement IsNot Nothing AndAlso statement.HandlesClause IsNot Nothing) OrElse
               (method.Parameters.Length = 2 AndAlso method.Parameters(0).Type.SpecialType = SpecialType.System_Object AndAlso
                UnvalidatedParameterAnalyzer.IsEventArgs(method.Parameters(1).Type)) Then
                Return False
            End If

            ' Overrides and implementations of members of referenced assemblies follow a contract defined elsewhere,
            ' which may accept Nothing (e.g. the "context" parameter of TypeConverter.ConvertTo).
            If (method.OverriddenMethod IsNot Nothing AndAlso method.OverriddenMethod.Locations.All(Function(l As Location) l.IsInMetadata)) OrElse
               method.ExplicitInterfaceImplementations.Any(Function(m As IMethodSymbol) m.Locations.All(Function(l As Location) l.IsInMetadata)) Then
                Return False
            End If

            If Not UnvalidatedParameterAnalyzer.IsVisibleAccessibility(method.DeclaredAccessibility) OrElse
               (method.DeclaredAccessibility <> Accessibility.Public AndAlso method.ContainingType.IsSealed) Then
                Return False
            End If

            Dim type As INamedTypeSymbol = method.ContainingType
            While type IsNot Nothing
                If Not UnvalidatedParameterAnalyzer.IsVisibleAccessibility(type.DeclaredAccessibility) Then
                    Return False
                End If
                type = type.ContainingType
            End While
            Return True
        End Function

        ''' <summary>
        ''' Determines whether an accessibility makes a member callable from other assemblies.
        ''' </summary>
        '''
        ''' <param name="accessibility">
        ''' The declared accessibility.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> for <see langword="Public"/>, <see langword="Protected"/> and <see langword="Protected Friend"/>;
        ''' otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsVisibleAccessibility(accessibility As Accessibility) As Boolean

            Return accessibility = Accessibility.Public OrElse accessibility = Accessibility.Protected OrElse
                   accessibility = Accessibility.ProtectedOrInternal
        End Function

        ''' <summary>
        ''' Determines whether a type is <see cref="EventArgs"/> or derives from it.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the type is an event arguments type; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsEventArgs(type As ITypeSymbol) As Boolean

            Dim current As ITypeSymbol = type
            While current IsNot Nothing
                If current.ToDisplayString().Equals("System.EventArgs", StringComparison.Ordinal) Then
                    Return True
                End If
                current = current.BaseType
            End While
            Return False
        End Function

        ''' <summary>
        ''' Gets the kind of validation that applies to a parameter.
        ''' </summary>
        '''
        ''' <param name="parameter">
        ''' The parameter.
        ''' </param>
        '''
        ''' <returns>
        ''' <see cref="EnumKind"/>, <see cref="StringKind"/> or <see cref="ReferenceKind"/>;
        ''' or <see langword="Nothing"/> if the parameter is not analyzed.
        ''' </returns>
        Private Shared Function GetKind(parameter As IParameterSymbol) As String

            If parameter.IsOptional OrElse parameter.IsParams OrElse parameter.RefKind <> RefKind.None OrElse
               parameter.GetAttributes().Any(Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso
                                                                       a.AttributeClass.Name.Equals("AllowNullAttribute", StringComparison.Ordinal)) Then
                Return Nothing
            End If

            Dim type As ITypeSymbol = parameter.Type
            If type.TypeKind = TypeKind.Error OrElse UnvalidatedParameterAnalyzer.IsEventArgs(type) Then
                Return Nothing
            End If

            If type.TypeKind = TypeKind.Enum Then
                Dim isFlags As Boolean = type.GetAttributes().Any(
                    Function(a As AttributeData) a.AttributeClass IsNot Nothing AndAlso a.AttributeClass.ToDisplayString().Equals("System.FlagsAttribute", StringComparison.Ordinal))
                Return If(isFlags, Nothing, UnvalidatedParameterAnalyzer.EnumKind)
            End If

            If type.SpecialType = SpecialType.System_String Then
                Return UnvalidatedParameterAnalyzer.StringKind
            End If

            ' A type parameter is analyzed only when it is constrained to reference types ("Of T As Class").
            Dim typeParameter As ITypeParameterSymbol = TryCast(type, ITypeParameterSymbol)
            Dim isReference As Boolean = If(typeParameter Is Nothing, type.IsReferenceType, typeParameter.HasReferenceTypeConstraint)
            Return If(isReference, UnvalidatedParameterAnalyzer.ReferenceKind, Nothing)
        End Function

        ''' <summary>
        ''' Gets the uses of each parameter of a method, inside its body.
        ''' </summary>
        '''
        ''' <param name="block">
        ''' The method block.
        ''' </param>
        '''
        ''' <param name="method">
        ''' The method symbol.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context (for cancellation).
        ''' </param>
        '''
        ''' <returns>
        ''' The identifiers that refer to each used parameter.
        ''' </returns>
        Private Shared Function GetReferences(block As MethodBlockBaseSyntax, method As IMethodSymbol, model As SemanticModel,
                                              context As SyntaxNodeAnalysisContext) As Dictionary(Of IParameterSymbol, List(Of IdentifierNameSyntax))

            Dim names As New HashSet(Of String)(method.Parameters.Select(Function(p As IParameterSymbol) p.Name), StringComparer.OrdinalIgnoreCase)
            Dim references As New Dictionary(Of IParameterSymbol, List(Of IdentifierNameSyntax))(SymbolEqualityComparer.Default)
            For Each statement As StatementSyntax In block.Statements
                For Each identifier As IdentifierNameSyntax In statement.DescendantNodesAndSelf().OfType(Of IdentifierNameSyntax)()
                    If Not names.Contains(identifier.Identifier.ValueText) Then
                        Continue For
                    End If

                    Dim parameter As IParameterSymbol = TryCast(model.GetSymbolInfo(identifier, context.CancellationToken).Symbol, IParameterSymbol)
                    If parameter Is Nothing OrElse Not SymbolEqualityComparer.Default.Equals(parameter.ContainingSymbol, method) Then
                        Continue For
                    End If

                    Dim uses As List(Of IdentifierNameSyntax) = Nothing
                    If Not references.TryGetValue(parameter, uses) Then
                        uses = New List(Of IdentifierNameSyntax)()
                        references.Add(parameter, uses)
                    End If
                    uses.Add(identifier)
                Next identifier
            Next statement
            Return references
        End Function

        ''' <summary>
        ''' Determines whether a use of a parameter looks like a validation of it.
        ''' </summary>
        '''
        ''' <param name="use">
        ''' The identifier that refers to the parameter.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the use is a condition, a null test, a <see langword="NameOf"/> argument, or an argument of a
        ''' validation method; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function IsValidation(use As IdentifierNameSyntax) As Boolean

            Dim node As SyntaxNode = use
            Dim parent As SyntaxNode = node.Parent
            While parent IsNot Nothing
                Select Case True
                    Case TypeOf parent Is NameOfExpressionSyntax, TypeOf parent Is TypeOfExpressionSyntax,
                         parent.IsKind(SyntaxKind.IsExpression), parent.IsKind(SyntaxKind.IsNotExpression),
                         TypeOf parent Is WhileOrUntilClauseSyntax
                        Return True

                    Case TypeOf parent Is ConditionalAccessExpressionSyntax
                        If DirectCast(parent, ConditionalAccessExpressionSyntax).Expression Is node Then
                            Return True
                        End If

                    Case TypeOf parent Is TernaryConditionalExpressionSyntax
                        If DirectCast(parent, TernaryConditionalExpressionSyntax).Condition Is node Then
                            Return True
                        End If

                    Case TypeOf parent Is BinaryConditionalExpressionSyntax
                        If DirectCast(parent, BinaryConditionalExpressionSyntax).FirstExpression Is node Then
                            Return True
                        End If

                    Case TypeOf parent Is InvocationExpressionSyntax
                        Dim invocation As InvocationExpressionSyntax = DirectCast(parent, InvocationExpressionSyntax)
                        Dim name As String = UnvalidatedParameterAnalyzer.GetInvokedName(invocation)
                        If invocation.ArgumentList Is node AndAlso
                           UnvalidatedParameterAnalyzer.ValidationMethodPrefixes.Any(Function(prefix As String) name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) Then
                            Return True
                        End If

                    Case TypeOf parent Is IfStatementSyntax
                        Return DirectCast(parent, IfStatementSyntax).Condition Is node

                    Case TypeOf parent Is ElseIfStatementSyntax
                        Return DirectCast(parent, ElseIfStatementSyntax).Condition Is node

                    Case TypeOf parent Is SingleLineIfStatementSyntax
                        Return DirectCast(parent, SingleLineIfStatementSyntax).Condition Is node

                    Case TypeOf parent Is SelectStatementSyntax
                        Return DirectCast(parent, SelectStatementSyntax).Expression Is node

                    Case TypeOf parent Is WhileStatementSyntax
                        Return DirectCast(parent, WhileStatementSyntax).Condition Is node

                    Case TypeOf parent Is StatementSyntax, TypeOf parent Is LambdaExpressionSyntax
                        Return False
                End Select

                node = parent
                parent = node.Parent
            End While
            Return False
        End Function

        ''' <summary>
        ''' Determines whether a use of a reference parameter consumes it, so that <see langword="Nothing"/> makes it fail: 
        ''' a member access or invocation on it, an argument of a method or constructor, the collection of a <see langword="For Each"/>, 
        ''' the lock of a <see langword="SyncLock"/>, or the operand of <see langword="Await"/>.
        ''' </summary>
        ''' 
        ''' <param name="use">
        ''' The identifier that refers to the parameter.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the use consumes the parameter; otherwise (e.g. the parameter is returned, assigned, 
        ''' concatenated, or passed to a parameter that accepts <see langword="Nothing"/>), <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <remarks>
        ''' An argument does not consume the parameter when the called method accepts <see langword="Nothing"/>: 
        ''' its parameter type is a type parameter (<c>List(Of T).Add(item As T)</c>), or the method is listed in 
        ''' <see cref="NullAcceptingMethods"/> or <see cref="NullAcceptingTypes"/>. (Nullable annotations such as 
        ''' <c>object? objA</c> cannot be used: Visual Basic symbols do not expose them.)
        ''' </remarks>
        Private Shared Function IsConsumed(use As IdentifierNameSyntax, context As SyntaxNodeAnalysisContext) As Boolean

            Dim parent As SyntaxNode = use.Parent
            Select Case True
                Case TypeOf parent Is MemberAccessExpressionSyntax
                    Return DirectCast(parent, MemberAccessExpressionSyntax).Expression Is use
                Case TypeOf parent Is InvocationExpressionSyntax
                    Return DirectCast(parent, InvocationExpressionSyntax).Expression Is use
                Case TypeOf parent Is SimpleArgumentSyntax
                    Dim owner As SyntaxNode = parent.Parent?.Parent
                    Dim isCallArgument As Boolean = TypeOf owner Is InvocationExpressionSyntax OrElse TypeOf owner Is ObjectCreationExpressionSyntax
                    Return isCallArgument AndAlso Not UnvalidatedParameterAnalyzer.AcceptsNothing(parent, context)
                Case TypeOf parent Is ForEachStatementSyntax
                    Return DirectCast(parent, ForEachStatementSyntax).Expression Is use
                Case Else
                    Return TypeOf parent Is SyncLockStatementSyntax OrElse TypeOf parent Is AwaitExpressionSyntax
            End Select
        End Function

        ''' <summary>
        ''' Determines whether the parameter that receives an argument accepts <see langword="Nothing"/> (see <see cref="IsConsumed"/>).
        ''' </summary>
        ''' 
        ''' <param name="argument">
        ''' The argument syntax.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the receiving parameter accepts <see langword="Nothing"/>; otherwise, <see langword="False"/>.
        ''' </returns>
        Private Shared Function AcceptsNothing(argument As SyntaxNode, context As SyntaxNodeAnalysisContext) As Boolean

            Dim operation As IArgumentOperation = TryCast(context.SemanticModel.GetOperation(argument, context.CancellationToken), IArgumentOperation)
            Dim parameter As IParameterSymbol = operation?.Parameter
            If parameter Is Nothing Then
                Return False
            End If
            If TypeOf parameter.OriginalDefinition.Type Is ITypeParameterSymbol Then
                Return True
            End If

            ' The method, or the method it overrides (StreamWriter.Write overrides TextWriter.Write).
            Dim method As IMethodSymbol = TryCast(parameter.ContainingSymbol, IMethodSymbol)
            While method IsNot Nothing
                ' Built from the namespace: ToDisplayString would write "Object" or "String" (the Visual Basic keywords) for special types.
                Dim type As INamedTypeSymbol = method.ContainingType.OriginalDefinition
                Dim typeName As String = $"{type.ContainingNamespace.ToDisplayString()}.{type.MetadataName}"
                If UnvalidatedParameterAnalyzer.NullAcceptingTypes.Contains(typeName) OrElse
                   UnvalidatedParameterAnalyzer.NullAcceptingMethods.Contains($"{typeName}.{method.Name}") Then
                    Return True
                End If
                method = method.OverriddenMethod
            End While
            Return False
        End Function

        ''' <summary>
        ''' Determines whether a parameter is passed, as a positional argument, to a method declared in the same compilation 
        ''' that validates the corresponding parameter (for example, a private helper that throws <see cref="ArgumentNullException"/>).
        ''' </summary>
        ''' 
        ''' <param name="use">
        ''' The identifier that refers to the parameter.
        ''' </param>
        ''' 
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        ''' 
        ''' <returns>
        ''' <see langword="True"/> if the called method validates the argument; otherwise, <see langword="False"/>.
        ''' </returns>
        ''' 
        ''' <remarks>
        ''' Only one level of calls is followed. The called method is inspected syntactically: in Visual Basic, no local variable 
        ''' or lambda parameter can have the name of a parameter of its method, so the name identifies the parameter.
        ''' </remarks>
        Private Shared Function IsValidatedByCallee(use As IdentifierNameSyntax, context As SyntaxNodeAnalysisContext) As Boolean

            Dim argument As SimpleArgumentSyntax = TryCast(use.Parent, SimpleArgumentSyntax)
            Dim list As ArgumentListSyntax = If(argument Is Nothing OrElse argument.IsNamed, Nothing, TryCast(argument.Parent, ArgumentListSyntax))
            Dim invocation As InvocationExpressionSyntax = If(list Is Nothing, Nothing, TryCast(list.Parent, InvocationExpressionSyntax))
            Dim callee As IMethodSymbol = If(invocation Is Nothing, Nothing,
                                             TryCast(context.SemanticModel.GetSymbolInfo(invocation, context.CancellationToken).Symbol, IMethodSymbol))
            Dim index As Integer = If(list Is Nothing, -1, list.Arguments.IndexOf(argument))
            If callee Is Nothing OrElse index < 0 OrElse index >= callee.Parameters.Length OrElse callee.Parameters(index).IsParams Then
                Return False
            End If

            Dim name As String = callee.Parameters(index).Name
            Dim definition As IMethodSymbol = If(callee.ReducedFrom, callee).OriginalDefinition
            For Each reference As SyntaxReference In definition.DeclaringSyntaxReferences
                Dim block As MethodBlockBaseSyntax = reference.GetSyntax(context.CancellationToken).FirstAncestorOrSelf(Of MethodBlockBaseSyntax)()
                If block IsNot Nothing AndAlso
                   block.Statements.Any(Function(statement As StatementSyntax) statement.DescendantNodesAndSelf().OfType(Of IdentifierNameSyntax)().Any(
                       Function(identifier As IdentifierNameSyntax) identifier.Identifier.ValueText.Equals(name, StringComparison.OrdinalIgnoreCase) AndAlso
                                                                    UnvalidatedParameterAnalyzer.IsValidation(identifier))) Then
                    Return True
                End If
            Next reference
            Return False
        End Function

        ''' <summary>
        ''' Gets the diagnostic properties used by the code fix.
        ''' </summary>
        '''
        ''' <param name="parameter">
        ''' The parameter.
        ''' </param>
        '''
        ''' <param name="kind">
        ''' The kind of the parameter (see <see cref="GetKind"/>).
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position of the first statement of the body, where the guard clause is written.
        ''' </param>
        '''
        ''' <returns>
        ''' The diagnostic properties.
        ''' </returns>
        Private Shared Function GetProperties(parameter As IParameterSymbol, kind As String, model As SemanticModel, position As Integer) As ImmutableDictionary(Of String, String)

            Dim properties As ImmutableDictionary(Of String, String).Builder = ImmutableDictionary.CreateBuilder(Of String, String)(StringComparer.Ordinal)
            properties.Add(UnvalidatedParameterAnalyzer.KindPropertyKey, kind)
            properties.Add(UnvalidatedParameterAnalyzer.ArgumentNullExceptionPropertyKey, UnvalidatedParameterAnalyzer.GetTypeName("System.ArgumentNullException", model, position))
            properties.Add(UnvalidatedParameterAnalyzer.ArgumentOutOfRangeExceptionPropertyKey, UnvalidatedParameterAnalyzer.GetTypeName("System.ArgumentOutOfRangeException", model, position))
            properties.Add(UnvalidatedParameterAnalyzer.ArgumentExceptionPropertyKey, UnvalidatedParameterAnalyzer.GetTypeName("System.ArgumentException", model, position))

            If kind.Equals(UnvalidatedParameterAnalyzer.EnumKind, StringComparison.Ordinal) Then
                Dim enumType As INamedTypeSymbol = DirectCast(parameter.Type, INamedTypeSymbol)
                properties.Add(UnvalidatedParameterAnalyzer.EnumTypePropertyKey, enumType.ToMinimalDisplayString(model, position))

                ' InvalidEnumArgumentException stores the value as an Integer: it is used only when every value fits in one.
                Dim underlying As SpecialType = If(enumType.EnumUnderlyingType Is Nothing, SpecialType.None, enumType.EnumUnderlyingType.SpecialType)
                Dim fitsInteger As Boolean = underlying = SpecialType.System_Int32 OrElse underlying = SpecialType.System_Int16 OrElse
                                             underlying = SpecialType.System_UInt16 OrElse underlying = SpecialType.System_Byte OrElse
                                             underlying = SpecialType.System_SByte
                Dim exceptionType As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName("System.ComponentModel.InvalidEnumArgumentException")
                properties.Add(UnvalidatedParameterAnalyzer.InvalidEnumExceptionPropertyKey,
                               If(fitsInteger AndAlso exceptionType IsNot Nothing, exceptionType.ToMinimalDisplayString(model, position), String.Empty))
            End If
            Return properties.ToImmutable()
        End Function

        ''' <summary>
        ''' Gets the shortest valid name of a type at a position.
        ''' </summary>
        '''
        ''' <param name="metadataName">
        ''' The metadata name of the type.
        ''' </param>
        '''
        ''' <param name="model">
        ''' The semantic model.
        ''' </param>
        '''
        ''' <param name="position">
        ''' The position where the name is written.
        ''' </param>
        '''
        ''' <returns>
        ''' The shortest valid name, or the fully qualified name if the type cannot be resolved.
        ''' </returns>
        Private Shared Function GetTypeName(metadataName As String, model As SemanticModel, position As Integer) As String

            Dim type As INamedTypeSymbol = model.Compilation.GetTypeByMetadataName(metadataName)
            Return If(type Is Nothing, metadataName, type.ToMinimalDisplayString(model, position))
        End Function

#End Region

    End Class

End Namespace

#End Region
