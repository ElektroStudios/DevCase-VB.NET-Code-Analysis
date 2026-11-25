#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Linq

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Operations
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

#End Region

#Region " ConstantArrayArgumentAnalyzer "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Reports arrays of constants created as arguments of a call (rule <see cref="DiagnosticIds.ConstantArrayArgument"/>;
    ''' equivalent of CA1861 for .NET Framework projects), for example <c>text.IndexOfAny(New Char() {","c, ";"c})</c>.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' The array is allocated again on every call, although its content never changes. Only explicit, one-dimensional, non-empty array
    ''' creations whose elements are all constants are reported (<c>New Char() {...}</c> or an array literal <c>{...}</c>), when they are
    ''' passed <see langword="ByVal"/> to a method or constructor; arrays expanded from a <see langword="ParamArray"/> are implicit and
    ''' not reported. Code that runs once (the <see langword="Shared"/> constructor and initializers of <see langword="Shared"/> fields
    ''' and properties), the methods of <see cref="Array"/> (which may modify the array), attribute arguments and interfaces are not
    ''' analyzed. Extracting the array shares it between calls, so the called method must not modify it.
    ''' </para>
    ''' <para>
    ''' The code fix declares a <c>Private Shared ReadOnly</c> field (<c>Private ReadOnly</c> in a <see langword="Module"/>) before the
    ''' member that contains the call, and passes it. The field is named after that member, the called method and a number
    ''' (<c>FindSeparatorIndexOfAny0</c>): the numbers are assigned per type and skip the names already in use, so that Fix All
    ''' declares a different field for each array, also across the files of a partial type.
    ''' </para>
    ''' </remarks>
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public NotInheritable Class ConstantArrayArgumentAnalyzer : Inherits DiagnosticAnalyzer

#Region " Types "

        ''' <summary>
        ''' An argument that is an array of constants, collected until the analysis of its type ends.
        ''' </summary>
        Private NotInheritable Class ArrayArgument

            ''' <summary>
            ''' Gets the location of the array creation.
            ''' </summary>
            Friend ReadOnly Property Location As Location

            ''' <summary>
            ''' Gets the position where the field is inserted.
            ''' </summary>
            Friend ReadOnly Property InsertionPosition As Integer

            ''' <summary>
            ''' Gets the indentation of the member that contains the argument.
            ''' </summary>
            Friend ReadOnly Property Indentation As String

            ''' <summary>
            ''' Gets the name of the field without its number: the name of the member and the name of the invoked method.
            ''' </summary>
            Friend ReadOnly Property BaseName As String

            ''' <summary>
            ''' Gets the name of the invoked method or created type.
            ''' </summary>
            Friend ReadOnly Property InvokedName As String

            ''' <summary>
            ''' Gets the name of the array type as the code writes it.
            ''' </summary>
            Friend ReadOnly Property TypeName As String

            ''' <summary>
            ''' Gets the text of the collection initializer (<c>{1, 2, 3}</c>).
            ''' </summary>
            Friend ReadOnly Property Initializer As String

            ''' <summary>
            ''' Gets the identifiers used in the member, which the field must not hide.
            ''' </summary>
            Friend ReadOnly Property Identifiers As HashSet(Of String)

            ''' <summary>
            ''' Initializes a new instance of the <see cref="ArrayArgument"/> class.
            ''' </summary>
            '''
            ''' <param name="location">
            ''' The location of the array creation.
            ''' </param>
            '''
            ''' <param name="insertionPosition">
            ''' The position where the field is inserted.
            ''' </param>
            '''
            ''' <param name="indentation">
            ''' The indentation of the member.
            ''' </param>
            '''
            ''' <param name="baseName">
            ''' The name of the field without its number.
            ''' </param>
            '''
            ''' <param name="invokedName">
            ''' The name of the invoked method or created type.
            ''' </param>
            '''
            ''' <param name="typeName">
            ''' The name of the array type.
            ''' </param>
            '''
            ''' <param name="initializer">
            ''' The text of the collection initializer.
            ''' </param>
            '''
            ''' <param name="identifiers">
            ''' The identifiers used in the member.
            ''' </param>
            Friend Sub New(location As Location, insertionPosition As Integer, indentation As String, baseName As String, invokedName As String,
                           typeName As String, initializer As String, identifiers As HashSet(Of String))

                Me.Location = location
                Me.InsertionPosition = insertionPosition
                Me.Indentation = indentation
                Me.BaseName = baseName
                Me.InvokedName = invokedName
                Me.TypeName = typeName
                Me.Initializer = initializer
                Me.Identifiers = identifiers
            End Sub

        End Class

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property SupportedDiagnostics As ImmutableArray(Of DiagnosticDescriptor)
            Get
                Return ImmutableArray.Create(DevCaseDescriptors.ConstantArrayArgument)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ConstantArrayArgumentAnalyzer"/> class.
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
            context.RegisterCompilationStartAction(AddressOf ConstantArrayArgumentAnalyzer.OnCompilationStart)
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
                Exit Sub ' Modern .NET: the built-in rule CA1861 reports the same code.
            End If

            context.RegisterSymbolStartAction(AddressOf ConstantArrayArgumentAnalyzer.OnTypeStart, SymbolKind.NamedType)
        End Sub


        ''' <summary>
        ''' Registers the collection of the array arguments of a type, which are reported together when the analysis of the type ends.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The symbol start analysis context.
        ''' </param>
        Private Shared Sub OnTypeStart(context As SymbolStartAnalysisContext)

            Dim type As INamedTypeSymbol = DirectCast(context.Symbol, INamedTypeSymbol)
            If type.TypeKind <> TypeKind.Class AndAlso type.TypeKind <> TypeKind.Struct AndAlso type.TypeKind <> TypeKind.Module Then
                Exit Sub
            End If

            Dim candidates As New ConcurrentQueue(Of ArrayArgument)()
            context.RegisterOperationAction(UtilAnalyzerFiles.SkipExcludedOperations(
                Sub(c As OperationAnalysisContext) ConstantArrayArgumentAnalyzer.AnalyzeArgument(c, type, candidates)), OperationKind.Argument)
            context.RegisterSymbolEndAction(Sub(c As SymbolAnalysisContext) ConstantArrayArgumentAnalyzer.ReportCandidates(c, type, candidates))
        End Sub

        ''' <summary>
        ''' Collects an argument that is an array of constants.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The operation analysis context.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The analyzed type.
        ''' </param>
        '''
        ''' <param name="candidates">
        ''' The collected arguments of the type.
        ''' </param>
        Private Shared Sub AnalyzeArgument(context As OperationAnalysisContext, type As INamedTypeSymbol, candidates As ConcurrentQueue(Of ArrayArgument))

            Dim argument As IArgumentOperation = DirectCast(context.Operation, IArgumentOperation)
            If argument.ArgumentKind <> ArgumentKind.Explicit OrElse argument.Parameter Is Nothing OrElse argument.Parameter.RefKind <> RefKind.None Then
                Exit Sub
            End If

            Dim creation As IArrayCreationOperation = TryCast(UtilAnalyzerSemantic.SkipImplicitConversions(argument.Value), IArrayCreationOperation)
            Dim arrayType As IArrayTypeSymbol = TryCast(creation?.Type, IArrayTypeSymbol)
            If creation Is Nothing OrElse creation.IsImplicit OrElse creation.Initializer Is Nothing OrElse arrayType Is Nothing OrElse Not arrayType.IsSZArray OrElse
               creation.Initializer.ElementValues.IsEmpty OrElse Not creation.Initializer.ElementValues.All(Function(e As IOperation) e.ConstantValue.HasValue) Then
                Exit Sub
            End If

            Dim invokedName As String = ConstantArrayArgumentAnalyzer.GetInvokedName(argument.Parent)
            Dim memberName As String = ConstantArrayArgumentAnalyzer.GetMemberName(context.ContainingSymbol)
            Dim syntax As SyntaxNode = creation.Syntax
            Dim initializer As CollectionInitializerSyntax = If(TryCast(syntax, ArrayCreationExpressionSyntax)?.Initializer, TryCast(syntax, CollectionInitializerSyntax))
            Dim member As SyntaxNode = syntax.Ancestors().FirstOrDefault(Function(a As SyntaxNode) TypeOf a.Parent Is TypeBlockSyntax)
            If invokedName Is Nothing OrElse memberName Is Nothing OrElse initializer Is Nothing OrElse member Is Nothing OrElse
               Not SymbolEqualityComparer.Default.Equals(context.ContainingSymbol.ContainingType, type) OrElse
               syntax.FirstAncestorOrSelf(Of AttributeSyntax)() IsNot Nothing Then
                Exit Sub
            End If

            Dim typeName As String = UtilSpeculativeBinding.GetTypeName(argument.SemanticModel, member.SpanStart, arrayType)
            If typeName Is Nothing Then
                Exit Sub
            End If

            Dim identifiers As New HashSet(Of String)(member.DescendantTokens().Where(Function(t As SyntaxToken) t.IsKind(SyntaxKind.IdentifierToken)).
                                                          Select(Function(t As SyntaxToken) t.ValueText), StringComparer.OrdinalIgnoreCase)
            candidates.Enqueue(New ArrayArgument(syntax.GetLocation(), ConstantArrayArgumentAnalyzer.GetInsertionPosition(member, context.CancellationToken),
                                                 UtilAnalyzerSyntax.GetLineIndentation(member.GetFirstToken()), $"{memberName}{invokedName}", invokedName,
                                                 typeName, initializer.ToString(), identifiers))
        End Sub

        ''' <summary>
        ''' Reports the collected arguments of a type, with field names that do not collide with each other or with the names in use.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The symbol analysis context.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The analyzed type.
        ''' </param>
        '''
        ''' <param name="candidates">
        ''' The collected arguments of the type.
        ''' </param>
        Private Shared Sub ReportCandidates(context As SymbolAnalysisContext, type As INamedTypeSymbol, candidates As ConcurrentQueue(Of ArrayArgument))

            If candidates.IsEmpty Then
                Exit Sub
            End If

            ' The names of the members of the type and of its base types, which a new field must not hide.
            Dim existing As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            Dim current As INamedTypeSymbol = type
            While current IsNot Nothing
                existing.UnionWith(current.GetMembers().Select(Function(m As ISymbol) m.Name))
                current = current.BaseType
            End While

            Dim modifiers As String = If(type.TypeKind = TypeKind.Module, "Private ReadOnly", "Private Shared ReadOnly")
            For Each group As IGrouping(Of String, ArrayArgument) In candidates.GroupBy(Function(c As ArrayArgument) c.BaseName, StringComparer.OrdinalIgnoreCase).
                                                                                OrderBy(Function(g As IGrouping(Of String, ArrayArgument)) g.Key, StringComparer.Ordinal)
                ' Siblings take consecutive numbers; a sibling whose name is in use skips ahead by the size of the group, so 
                ' no two siblings can end with the same number.
                Dim ordered As List(Of ArrayArgument) = group.OrderBy(Function(c As ArrayArgument) c.Location.SourceTree.FilePath, StringComparer.Ordinal).
                                                              ThenBy(Function(c As ArrayArgument) c.Location.SourceSpan.Start).ToList()
                For index As Integer = 0 To ordered.Count - 1
                    Dim candidate As ArrayArgument = ordered(index)
                    Dim number As Integer = index
                    While existing.Contains($"{candidate.BaseName}{number}") OrElse candidate.Identifiers.Contains($"{candidate.BaseName}{number}")
                        number += ordered.Count
                    End While

                    Dim name As String = $"{candidate.BaseName}{number}"
                    existing.Add(name)
                    Dim tree As SyntaxTree = candidate.Location.SourceTree
                    Dim endOfLine As String = UtilAnalyzerSyntax.GetEndOfLine(tree)
                    Dim declaration As String = $"{candidate.Indentation}{modifiers} {name} As {candidate.TypeName} = {candidate.Initializer}{endOfLine}{endOfLine}"
                    Dim spans As Location() = {Location.Create(tree, New Text.TextSpan(candidate.InsertionPosition, 0)), candidate.Location}
                    Dim actions As FixReplacement() = {New FixReplacement("Extract to a Shared ReadOnly field", "Extract", declaration, name)}
                    context.ReportDiagnostic(UtilFixProperties.CreateDiagnostic(DevCaseDescriptors.ConstantArrayArgument, candidate.Location, spans, actions,
                                                                                candidate.InvokedName))
                Next index
            Next group
        End Sub

        ''' <summary>
        ''' Gets the name of the method or type that receives an argument.
        ''' </summary>
        '''
        ''' <param name="owner">
        ''' The parent of the argument: an invocation or an object creation.
        ''' </param>
        '''
        ''' <returns>
        ''' The name of the invoked method or of the created type; <see langword="Nothing"/> for other owners and for the methods 
        ''' of <see cref="Array"/>.
        ''' </returns>
        Private Shared Function GetInvokedName(owner As IOperation) As String

            Dim invocation As IInvocationOperation = TryCast(owner, IInvocationOperation)
            Dim creation As IObjectCreationOperation = TryCast(owner, IObjectCreationOperation)
            Select Case True
                Case invocation IsNot Nothing
                    Return If(invocation.TargetMethod.ContainingType?.SpecialType = SpecialType.System_Array, Nothing, invocation.TargetMethod.Name)
                Case creation IsNot Nothing AndAlso creation.Constructor IsNot Nothing
                    Return creation.Constructor.ContainingType.Name
                Case Else
                    Return Nothing
            End Select
        End Function

        ''' <summary>
        ''' Gets the name of the member whose code contains an argument, for the name of the field.
        ''' </summary>
        '''
        ''' <param name="containingSymbol">
        ''' The symbol that contains the argument.
        ''' </param>
        '''
        ''' <returns>
        ''' The name of the method, of the property or event of an accessor, <c>Constructor</c> for an instance constructor, or 
        ''' the name of an instance field or property with an initializer; <see langword="Nothing"/> for code that runs once 
        ''' (the <see langword="Shared"/> constructor and <see langword="Shared"/> initializers).
        ''' </returns>
        Private Shared Function GetMemberName(containingSymbol As ISymbol) As String

            Dim method As IMethodSymbol = TryCast(containingSymbol, IMethodSymbol)
            If method IsNot Nothing Then
                Select Case method.MethodKind
                    Case MethodKind.SharedConstructor
                        Return Nothing
                    Case MethodKind.Constructor
                        Return "Constructor"
                    Case Else
                        Return If(method.AssociatedSymbol, method).Name
                End Select
            End If

            Dim isInitializer As Boolean = TypeOf containingSymbol Is IFieldSymbol OrElse TypeOf containingSymbol Is IPropertySymbol
            Return If(isInitializer AndAlso Not containingSymbol.IsStatic, containingSymbol.Name, Nothing)
        End Function

        ''' <summary>
        ''' Gets the position where a field is inserted before a member: the start of the line of its first comment, 
        ''' documentation comment or token, after the blank lines and preprocessor directives that precede it (so that the 
        ''' field stays inside the <c>#Region</c> of the member).
        ''' </summary>
        '''
        ''' <param name="member">
        ''' The declaration of the member.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The position.
        ''' </returns>
        Private Shared Function GetInsertionPosition(member As SyntaxNode, cancellationToken As Threading.CancellationToken) As Integer

            Dim position As Integer = member.SpanStart
            Dim pastDirectives As Boolean = True
            For Each trivia As SyntaxTrivia In member.GetLeadingTrivia().Reverse()
                Select Case True
                    Case trivia.IsDirective
                        pastDirectives = False
                    Case pastDirectives AndAlso (trivia.IsKind(SyntaxKind.CommentTrivia) OrElse trivia.IsKind(SyntaxKind.DocumentationCommentTrivia))
                        position = trivia.SpanStart
                End Select
            Next trivia
            Return member.SyntaxTree.GetText(cancellationToken).Lines.GetLineFromPosition(position).Start
        End Function

#End Region

    End Class

End Namespace

#End Region
