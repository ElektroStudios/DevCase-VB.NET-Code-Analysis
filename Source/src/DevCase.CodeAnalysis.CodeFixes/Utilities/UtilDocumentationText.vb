#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Linq
Imports System.Text

Imports Microsoft.CodeAnalysis

#End Region

#Region " UtilDocumentationText "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Generates sensible default XML documentation texts (constructor summaries and parameter descriptions)
    ''' from symbol names and types, so that generated documentation is meaningful enough to satisfy the
    ''' documentation rules while remaining easy to refine.
    ''' </summary>
    '''
    ''' <remarks>
    ''' Parameter descriptions follow the conventions of the .NET reference documentation, for example:
    ''' <list type="bullet">
    '''   <item><description><c>filePath As String</c> → "The file path."</description></item>
    '''   <item><description><c>encoding As Encoding</c> → "The <c>&lt;see cref="Encoding"/&gt;</c> to use."</description></item>
    '''   <item><description><c>overwrite As Boolean</c> → "<c>&lt;see langword="True"/&gt;</c> to overwrite; otherwise, <c>&lt;see langword="False"/&gt;</c>."</description></item>
    '''   <item><description><c>isEnabled As Boolean</c> → "<c>&lt;see langword="True"/&gt;</c> if enabled; otherwise, <c>&lt;see langword="False"/&gt;</c>."</description></item>
    '''   <item><description><c>mode As FileMode</c> → "One of the enumeration values that specifies the mode."</description></item>
    '''   <item><description><c>cancellationToken As CancellationToken</c> → "The token to monitor for cancellation requests."</description></item>
    ''' </list>
    ''' </remarks>
    Friend NotInheritable Class UtilDocumentationText

#Region " Fields "

        ''' <summary>
        ''' Name prefixes of Boolean parameters that state a condition ("isEnabled" → "if enabled").
        ''' </summary>
        Private Shared ReadOnly ConditionPrefixes As String() = {"is", "has", "can", "should", "must", "was", "are", "does"}

        ''' <summary>
        ''' Name prefixes of Boolean parameters that start with a verb ("includeHidden" → "to include hidden").
        ''' </summary>
        Private Shared ReadOnly VerbPrefixes As String() = {
            "add", "allow", "append", "apply", "ask", "auto", "cancel", "check", "clear", "close", "copy", "create", "delete",
            "detect", "disable", "dispose", "enable", "ensure", "exclude", "expand", "force", "hide", "ignore", "include", "keep",
            "load", "merge", "move", "notify", "open", "overwrite", "preserve", "prompt", "recurse", "refresh", "remove", "replace",
            "reset", "resolve", "retry", "run", "save", "search", "show", "skip", "sort", "throw", "trim", "use", "validate", "wait"}

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilDocumentationText"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Gets the conventional summary text of an instance constructor.
        ''' </summary>
        '''
        ''' <param name="type">
        ''' The type that declares the constructor.
        ''' </param>
        '''
        ''' <param name="isPrivate">
        ''' <see langword="True"/> for a private constructor (which prevents instantiation); otherwise, <see langword="False"/>.
        ''' </param>
        '''
        ''' <returns>
        ''' The summary text, e.g. <c>Initializes a new instance of the &lt;see cref="Sample"/&gt; class.</c>
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="type"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function GetConstructorSummary(type As INamedTypeSymbol, isPrivate As Boolean) As String

            If type Is Nothing Then
                Throw New ArgumentNullException(NameOf(type))
            End If

            Dim cref As String = type.Name
            If type.TypeParameters.Length > 0 Then
                cref = $"{type.Name}(Of {String.Join(", ", type.TypeParameters.Select(Function(p As ITypeParameterSymbol) p.Name))})"
            End If

            Dim kind As String = If(type.TypeKind = TypeKind.Structure, "structure", "class")
            Return If(isPrivate,
                      $"Prevents a default instance of the <see cref=""{cref}""/> {kind} from being created.",
                      $"Initializes a new instance of the <see cref=""{cref}""/> {kind}.")
        End Function

        ''' <summary>
        ''' Gets a default description for a parameter.
        ''' </summary>
        '''
        ''' <param name="name">
        ''' The parameter name.
        ''' </param>
        '''
        ''' <param name="type">
        ''' The parameter type, or <see langword="Nothing"/> if it cannot be resolved.
        ''' </param>
        '''
        ''' <returns>
        ''' A one-sentence description, ending with a period.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="name"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        Friend Shared Function GetParameterDescription(name As String, type As ITypeSymbol) As String

            If String.IsNullOrWhiteSpace(name) Then
                Throw New ArgumentNullException(NameOf(name))
            End If

            Dim words As List(Of String) = UtilDocumentationText.SplitIdentifier(name)
            Dim phrase As String = String.Join(" ", words)

            If type IsNot Nothing Then
                Dim typeName As String = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)

                If type.ToDisplayString().Equals("System.Threading.CancellationToken", StringComparison.Ordinal) Then
                    Return "The token to monitor for cancellation requests."
                End If

                If type.SpecialType = SpecialType.System_Boolean Then
                    Return UtilDocumentationText.GetBooleanDescription(words)
                End If

                If type.TypeKind = TypeKind.Enum Then
                    Return $"One of the enumeration values that specifies the {phrase}."
                End If

                ' "encoding As Encoding" -> "The <see cref="Encoding"/> to use."
                If type.TypeKind <> TypeKind.Error AndAlso type.SpecialType = SpecialType.None AndAlso
                   String.Join(String.Empty, words).Equals(type.Name, StringComparison.OrdinalIgnoreCase) Then
                    Return $"The <see cref=""{typeName}""/> to use."
                End If
            End If

            Return $"The {phrase}."
        End Function

        ''' <summary>
        ''' Splits a camelCase / PascalCase / snake_case identifier into lower-case words, keeping acronyms upper-case.
        ''' </summary>
        '''
        ''' <param name="identifier">
        ''' The identifier.
        ''' </param>
        '''
        ''' <returns>
        ''' The words (e.g. <c>"maxURLLength"</c> → <c>"max", "URL", "length"</c>).
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="identifier"/> is <see langword="Nothing"/> or empty.
        ''' </exception>
        Friend Shared Function SplitIdentifier(identifier As String) As List(Of String)

            If String.IsNullOrWhiteSpace(identifier) Then
                Throw New ArgumentNullException(NameOf(identifier))
            End If

            Dim rawWords As New List(Of String)()
            Dim current As New StringBuilder()
            Dim text As String = identifier.TrimStart("_"c)

            For index As Integer = 0 To text.Length - 1
                Dim c As Char = text(index)
                If c = "_"c Then
                    If current.Length > 0 Then
                        rawWords.Add(current.ToString())
                        current.Clear()
                    End If
                    Continue For
                End If

                If current.Length > 0 Then
                    Dim previous As Char = current(current.Length - 1)
                    Dim nextIsLower As Boolean = index + 1 < text.Length AndAlso Char.IsLower(text(index + 1))
                    Dim boundary As Boolean =
                        (Char.IsUpper(c) AndAlso (Char.IsLower(previous) OrElse Char.IsDigit(previous))) OrElse
                        (Char.IsUpper(c) AndAlso Char.IsUpper(previous) AndAlso nextIsLower) OrElse
                        (Char.IsDigit(c) AndAlso Not Char.IsDigit(previous))
                    If boundary Then
                        rawWords.Add(current.ToString())
                        current.Clear()
                    End If
                End If
                current.Append(c)
            Next index

            If current.Length > 0 Then
                rawWords.Add(current.ToString())
            End If

            ' Acronyms (two or more upper-case letters) keep their casing; other words are lower-cased.
            Return rawWords.Select(Function(w As String) If(w.Length > 1 AndAlso w.All(Function(ch As Char) Char.IsUpper(ch) OrElse Char.IsDigit(ch)),
                                                            w, w.ToLowerInvariant())).ToList()
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the default description of a Boolean parameter.
        ''' </summary>
        '''
        ''' <param name="words">
        ''' The words of the parameter name.
        ''' </param>
        '''
        ''' <returns>
        ''' The description, in the "True to ...; otherwise, False." form.
        ''' </returns>
        Private Shared Function GetBooleanDescription(words As List(Of String)) As String

            Const TrueWord As String = "<see langword=""True""/>"
            Const FalseWord As String = "<see langword=""False""/>"

            Dim first As String = words(0).ToLowerInvariant()
            If words.Count > 1 AndAlso UtilDocumentationText.ConditionPrefixes.Contains(first, StringComparer.Ordinal) Then
                Dim condition As String = String.Join(" ", words.Skip(If(first.Equals("is", StringComparison.Ordinal), 1, 0)))
                Return $"{TrueWord} if {condition}; otherwise, {FalseWord}."
            End If

            Dim phrase As String = String.Join(" ", words)
            Return If(UtilDocumentationText.VerbPrefixes.Contains(first, StringComparer.Ordinal),
                      $"{TrueWord} to {phrase}; otherwise, {FalseWord}.",
                      $"{TrueWord} to enable the {phrase} option; otherwise, {FalseWord}.")
        End Function

#End Region

    End Class

End Namespace

#End Region
