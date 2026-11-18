#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq
Imports System.Text
Imports System.Threading

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " SenderParameterAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports parameters named <c>sender</c> in methods that are not event handlers (rule <see cref="DiagnosticIds.MisusedSenderParameter"/>),
    ''' for example <c>Public Function ToPen(sender As SolidBrush) As Pen</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' By convention, <c>sender</c> is the object that raises an event, and it is typed <see cref="Object"/>. A parameter named
    ''' <c>sender</c> with a more specific type, in a method that does not handle an event, hides what the argument really is.
    ''' </para>
    ''' <para>
    ''' Not reported, because the method can be an event handler or a callback whose signature is defined elsewhere:
    ''' <c>sender</c> parameters typed <see cref="Object"/> (any delegate convention) or with a type parameter; methods with a
    ''' <see langword="Handles"/> clause; methods with another parameter whose type derives from <see cref="EventArgs"/>;
    ''' <see langword="Overrides"/> and <see langword="Implements"/> methods; and methods whose name appears after
    ''' <see langword="AddressOf"/> anywhere in the project (handlers of custom events with a typed sender, attached with
    ''' <see langword="AddHandler"/>).
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class SenderParameterAnalyzer : Inherits DiagnosticAnalyzer

#Region " Constants "

        ''' <summary>
        ''' The key of the diagnostic property that holds the suggested parameter name.
        ''' </summary>
        Friend Const NewNamePropertyKey As String = "NewName"

        ''' <summary>
        ''' The reported parameter name.
        ''' </summary>
        Private Const SenderName As String = "sender"

        ''' <summary>
        ''' The suggested name when the type gives no usable name (a Visual Basic keyword, such as <c>String</c> or <c>Integer</c>).
        ''' </summary>
        Private Const FallbackName As String = "value"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.MisusedSenderParameter)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="SenderParameterAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(
                Sub(start As CompilationStartAnalysisContext)
                    ' The names used after AddressOf in the project, computed once, on the first "sender" parameter found.
                    Dim delegateTargets As New Lazy(Of ImmutableHashSet(Of String))(
                        Function() SenderParameterAnalyzer.GetAddressOfNames(start.Compilation, start.CancellationToken),
                        LazyThreadSafetyMode.ExecutionAndPublication)
                    start.RegisterSyntaxNodeAction(Sub(c As SyntaxNodeAnalysisContext) SenderParameterAnalyzer.AnalyzeMethod(c, delegateTargets),
                                                   SyntaxKind.SubBlock, SyntaxKind.FunctionBlock, SyntaxKind.ConstructorBlock)
                End Sub)
        End Sub

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Reports the <c>sender</c> parameter of a method that is not an event handler.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The syntax node analysis context.
        ''' </param>
        '''
        ''' <param name="delegateTargets">
        ''' The names used after <see langword="AddressOf"/> in the project.
        ''' </param>
        Private Shared Sub AnalyzeMethod(context As SyntaxNodeAnalysisContext, delegateTargets As Lazy(Of ImmutableHashSet(Of String)))

            Dim block As MethodBlockBaseSyntax = DirectCast(context.Node, MethodBlockBaseSyntax)
            Dim parameterList As ParameterListSyntax = block.BlockStatement.ParameterList
            Dim senderSyntax As ParameterSyntax = parameterList?.Parameters.FirstOrDefault(
                Function(p As ParameterSyntax) p.Identifier.Identifier.ValueText.Equals(SenderParameterAnalyzer.SenderName, StringComparison.OrdinalIgnoreCase))
            Dim statement As MethodStatementSyntax = TryCast(block.BlockStatement, MethodStatementSyntax)
            If senderSyntax Is Nothing OrElse (statement IsNot Nothing AndAlso statement.HandlesClause IsNot Nothing) Then
                Exit Sub
            End If

            Dim method As IMethodSymbol = context.SemanticModel.GetDeclaredSymbol(block, context.CancellationToken)
            If method Is Nothing OrElse method.IsOverride OrElse Not method.ExplicitInterfaceImplementations.IsEmpty OrElse
               method.Parameters.Length <> parameterList.Parameters.Count Then
                Exit Sub
            End If

            Dim sender As IParameterSymbol = method.Parameters(parameterList.Parameters.IndexOf(senderSyntax))
            If sender.Type.SpecialType = SpecialType.System_Object OrElse sender.Type.TypeKind = TypeKind.TypeParameter OrElse
               sender.Type.TypeKind = TypeKind.Error OrElse
               method.Parameters.Any(Function(p As IParameterSymbol) SenderParameterAnalyzer.IsEventArgs(p.Type)) OrElse
               delegateTargets.Value.Contains(method.Name) Then
                Exit Sub
            End If

            Dim newName As String = SenderParameterAnalyzer.GetNewName(sender.Type, block)
            Dim properties As ImmutableDictionary(Of String, String) = ImmutableDictionary.CreateRange(StringComparer.Ordinal,
                {New KeyValuePair(Of String, String)(SenderParameterAnalyzer.NewNamePropertyKey, newName)})
            Dim methodName As String = If(method.MethodKind = MethodKind.Constructor, "New", method.Name)
            context.ReportDiagnostic(Diagnostic.Create(DevCaseDescriptors.MisusedSenderParameter, senderSyntax.Identifier.Identifier.GetLocation(),
                                                       properties, $"{method.ContainingType.Name}.{methodName}", newName))
        End Sub

        ''' <summary>
        ''' Gets the names used after <see langword="AddressOf"/> in every syntax tree of the compilation
        ''' (the rightmost name: <c>Handler</c> for <c>AddressOf Me.Handler</c>).
        ''' </summary>
        '''
        ''' <param name="compilation">
        ''' The compilation.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The names, compared case-insensitively.
        ''' </returns>
        Private Shared Function GetAddressOfNames(compilation As Compilation, cancellationToken As CancellationToken) As ImmutableHashSet(Of String)

            Dim names As ImmutableHashSet(Of String).Builder = ImmutableHashSet.CreateBuilder(StringComparer.OrdinalIgnoreCase)
            For Each tree As SyntaxTree In compilation.SyntaxTrees
                For Each node As UnaryExpressionSyntax In tree.GetRoot(cancellationToken).DescendantNodes().OfType(Of UnaryExpressionSyntax)()
                    If Not node.IsKind(SyntaxKind.AddressOfExpression) Then
                        Continue For
                    End If
                    Dim operand As ExpressionSyntax = node.Operand
                    Dim name As SimpleNameSyntax = If(TypeOf operand Is MemberAccessExpressionSyntax, DirectCast(operand, MemberAccessExpressionSyntax).Name,
                                                      TryCast(operand, SimpleNameSyntax))
                    If name IsNot Nothing Then
                        names.Add(name.Identifier.ValueText)
                    End If
                Next node
            Next tree
            Return names.ToImmutable()
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
                If current.Name.Equals(NameOf(EventArgs), StringComparison.Ordinal) AndAlso
                   current.ContainingNamespace?.ToDisplayString().Equals("System", StringComparison.Ordinal) = True Then
                    Return True
                End If
                current = current.BaseType
            End While
            Return False
        End Function

        ''' <summary>
        ''' Gets the suggested name of the parameter, from its type: <c>brush</c> for <c>SolidBrush</c>...
        ''' see <see cref="GetBaseName"/>; a number is appended when the name is already used in the method.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The parameter type.
        ''' </param>
        '''
        ''' <param name="block">
        ''' The method block, whose identifiers must not conflict with the new name.
        ''' </param>
        '''
        ''' <returns>
        ''' The suggested name.
        ''' </returns>
        Private Shared Function GetNewName(type As ITypeSymbol, block As MethodBlockBaseSyntax) As String

            ' Every identifier of the method (parameters, locals, members used without qualification): renaming the parameter
            ' to one of them would change what that identifier refers to.
            Dim used As New HashSet(Of String)(block.DescendantTokens().
                Where(Function(t As SyntaxToken) t.IsKind(SyntaxKind.IdentifierToken)).
                Select(Function(t As SyntaxToken) t.ValueText), StringComparer.OrdinalIgnoreCase)

            Dim baseName As String = SenderParameterAnalyzer.GetBaseName(type)
            Dim name As String = baseName
            Dim suffix As Integer = 1
            While used.Contains(name)
                suffix += 1
                name = $"{baseName}{suffix}"
            End While
            Return name
        End Function

        ''' <summary>
        ''' Gets a camel-case name for a type: the last word of its name (<c>SolidBrush</c> → <c>brush</c>, <c>MailAddress</c> → <c>address</c>),
        ''' without the <c>I</c> prefix of interfaces; arrays use the plural of their element (<c>Point()</c> → <c>points</c>).
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type.
        ''' </param>
        '''
        ''' <returns>
        ''' The name, or <see cref="FallbackName"/> (<c>values</c> for arrays) for primitive types (<c>String</c>, <c>Integer</c>, <c>Date</c>...) 
        ''' and names that would be Visual Basic keywords.
        ''' </returns>
        Private Shared Function GetBaseName(type As ITypeSymbol) As String

            Dim arrayType As IArrayTypeSymbol = TryCast(type, IArrayTypeSymbol)
            Dim elementType As ITypeSymbol = If(arrayType Is Nothing, type, arrayType.ElementType)
            Dim typeName As String = elementType.Name
            If elementType.TypeKind = TypeKind.Interface AndAlso typeName.Length > 1 AndAlso typeName(0) = "I"c AndAlso Char.IsUpper(typeName(1)) Then
                typeName = typeName.Substring(1)
            End If

            ' Primitive types (Int32, Boolean, DateTime...) have no meaningful word: they get the fallback name.
            If elementType.SpecialType <> SpecialType.None Then
                Return If(arrayType Is Nothing, SenderParameterAnalyzer.FallbackName, $"{SenderParameterAnalyzer.FallbackName}s")
            End If

            ' The last word of the Pascal-case name: it starts at an upper-case letter that follows a lower-case letter or digit, 
            ' or that starts a word after an acronym (HTMLDocument → Document).
            Dim start As Integer = 0
            For index As Integer = typeName.Length - 1 To 1 Step -1
                Dim startsWord As Boolean = Char.IsUpper(typeName(index)) AndAlso
                                            (Not Char.IsUpper(typeName(index - 1)) OrElse (index + 1 < typeName.Length AndAlso Char.IsLower(typeName(index + 1))))
                If startsWord Then
                    start = index
                    Exit For
                End If
            Next index
            Dim word As String = typeName.Substring(start)
            Dim name As New StringBuilder(word.ToLowerInvariant())
            If arrayType IsNot Nothing Then
                name.Append("s"c)
            End If

            Dim result As String = name.ToString()
            Dim isValid As Boolean = result.Length > 0 AndAlso Char.IsLetter(result(0)) AndAlso
                                     SyntaxFacts.GetKeywordKind(result) = SyntaxKind.None AndAlso result.All(Function(c As Char) Char.IsLetterOrDigit(c))
            Return If(isValid, result, SenderParameterAnalyzer.FallbackName)
        End Function

#End Region

    End Class

End Namespace

#End Region
