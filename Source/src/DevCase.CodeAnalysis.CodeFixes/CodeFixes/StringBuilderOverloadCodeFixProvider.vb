#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Immutable
Imports System.Composition
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " StringBuilderOverloadCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fix for rule <see cref="DiagnosticIds.StringBuilderArgumentConvertedToString"/>: passes the value to the 
    ''' strongly typed overload, for example <c>sb.Append(count.ToString())</c> → <c>sb.Append(count)</c>, 
    ''' <c>sb.Insert(0, flag.ToString())</c> → <c>sb.Insert(0, flag)</c> and <c>sb.Append(New String(c, 2))</c> → <c>sb.Append(c, 2)</c>.
    ''' </summary>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(StringBuilderOverloadCodeFixProvider)), [Shared]>
    Public NotInheritable Class StringBuilderOverloadCodeFixProvider : Inherits CodeFixProvider

#Region " Constants "

        ''' <summary>
        ''' The name of the character parameter of <c>StringBuilder.Append(Char, Integer)</c>.
        ''' </summary>
        Private Const ValueParameterName As String = "value"

        ''' <summary>
        ''' The name of the count parameter of <c>StringBuilder.Append(Char, Integer)</c>.
        ''' </summary>
        Private Const RepeatCountParameterName As String = "repeatCount"

        ''' <summary>
        ''' The name of the count parameter of the <c>String(Char, Integer)</c> constructor.
        ''' </summary>
        Private Const CountParameterName As String = "count"

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.StringBuilderArgumentConvertedToString)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="StringBuilderOverloadCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return WellKnownFixAllProviders.BatchFixer
        End Function

        ''' <inheritdoc/>
        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim document As Document = context.Document
                context.RegisterCodeFix(
                    CodeAction.Create("Use the strongly typed overload",
                                      Function(ct As CancellationToken) StringBuilderOverloadCodeFixProvider.FixAsync(document, diagnostic, ct),
                                      equivalenceKey:=NameOf(StringBuilderOverloadCodeFixProvider)),
                    diagnostic)
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Replaces the argument reported by the specified diagnostic.
        ''' </summary>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic to fix.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        '''
        ''' <exception cref="InvalidOperationException">
        ''' Thrown when the diagnostic no longer points to a convertible argument.
        ''' </exception>
        Private Shared Async Function FixAsync(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)

            Dim root As SyntaxNode = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim value As ExpressionSyntax = UtilCodeFix.FindNodeBySpan(Of ExpressionSyntax)(root, diagnostic.Location.SourceSpan)
            Dim change As TextChange? = If(diagnostic.Properties.ContainsKey(StringBuilderOverloadAnalyzer.RepeatedCharacterPropertyKey),
                                           StringBuilderOverloadCodeFixProvider.GetRepeatedCharacterChange(value),
                                           StringBuilderOverloadCodeFixProvider.GetToStringChange(value))
            If Not change.HasValue Then
                Throw New InvalidOperationException("The diagnostic does not point to a convertible argument.")
            End If
            Return Await UtilCodeFix.ApplyTextChangesAsync(document, {change.Value}, cancellationToken).ConfigureAwait(False)
        End Function

        ''' <summary>
        ''' Gets the change that removes the call to <c>ToString()</c>: <c>count.ToString()</c> → <c>count</c>, 
        ''' <c>(a + b).ToString()</c> → <c>a + b</c>.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The argument: a call to <c>ToString()</c>, with or without parentheses.
        ''' </param>
        '''
        ''' <returns>
        ''' The change, or <see langword="Nothing"/> if the argument has another form.
        ''' </returns>
        Private Shared Function GetToStringChange(value As ExpressionSyntax) As TextChange?

            Dim invocation As InvocationExpressionSyntax = TryCast(value, InvocationExpressionSyntax)
            Dim memberAccess As MemberAccessExpressionSyntax = TryCast(If(invocation IsNot Nothing, invocation.Expression, value), MemberAccessExpressionSyntax)
            If memberAccess?.Expression Is Nothing Then
                Return Nothing
            End If

            ' The argument is a whole expression: the parentheses that were needed before ".ToString()" are not.
            Dim receiver As ExpressionSyntax = memberAccess.Expression
            Dim parenthesized As ParenthesizedExpressionSyntax = TryCast(receiver, ParenthesizedExpressionSyntax)
            If parenthesized IsNot Nothing Then
                receiver = parenthesized.Expression
            End If
            Return New TextChange(value.Span, receiver.WithoutTrivia().ToString())
        End Function

        ''' <summary>
        ''' Gets the change that passes the character and the count of <c>New String(c, count)</c> to <c>Append(Char, Integer)</c>: 
        ''' <c>sb.Append(New String(c, 2))</c> → <c>sb.Append(c, 2)</c>, and <c>sb.Append(value:=New String(c, 2))</c> → 
        ''' <c>sb.Append(value:=c, repeatCount:=2)</c>.
        ''' </summary>
        '''
        ''' <param name="value">
        ''' The argument: an object creation of <see cref="String"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The change, or <see langword="Nothing"/> if the argument has another form.
        ''' </returns>
        Private Shared Function GetRepeatedCharacterChange(value As ExpressionSyntax) As TextChange?

            Dim creation As ObjectCreationExpressionSyntax = TryCast(value, ObjectCreationExpressionSyntax)
            Dim argument As SimpleArgumentSyntax = TryCast(value.Parent, SimpleArgumentSyntax)
            Dim arguments As SeparatedSyntaxList(Of ArgumentSyntax) = If(creation?.ArgumentList Is Nothing, Nothing, creation.ArgumentList.Arguments)
            If argument Is Nothing OrElse arguments.Count <> 2 OrElse Not arguments.All(Function(a As ArgumentSyntax) TypeOf a Is SimpleArgumentSyntax) Then
                Return Nothing
            End If

            ' The arguments of the constructor may be named (New String(count:=2, c:="-"c)).
            Dim first As SimpleArgumentSyntax = DirectCast(arguments(0), SimpleArgumentSyntax)
            Dim second As SimpleArgumentSyntax = DirectCast(arguments(1), SimpleArgumentSyntax)
            Dim countFirst As Boolean = first.IsNamed AndAlso
                first.NameColonEquals.Name.Identifier.ValueText.Equals(StringBuilderOverloadCodeFixProvider.CountParameterName, StringComparison.OrdinalIgnoreCase)
            Dim character As String = If(countFirst, second, first).Expression.WithoutTrivia().ToString()
            Dim count As String = If(countFirst, first, second).Expression.WithoutTrivia().ToString()

            Dim replacement As String = If(argument.IsNamed,
                $"{StringBuilderOverloadCodeFixProvider.ValueParameterName}:={character}, {StringBuilderOverloadCodeFixProvider.RepeatCountParameterName}:={count}",
                $"{character}, {count}")
            Return New TextChange(argument.Span, replacement)
        End Function

#End Region

    End Class

End Namespace

#End Region
