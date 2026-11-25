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

#End Region

#Region " FixReplacement "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Represents a code fix action computed by an analyzer: a title, an equivalence key and the replacement text of each edited 
    ''' span (see <see cref="UtilFixProperties"/>).
    ''' </summary>
    Friend NotInheritable Class FixReplacement

#Region " Properties "

        ''' <summary>
        ''' Gets the title of the code action, shown in the light bulb menu.
        ''' </summary>
        Friend ReadOnly Property Title As String

        ''' <summary>
        ''' Gets the key that identifies the action among the actions of the rule (the equivalence key of Fix All).
        ''' </summary>
        Friend ReadOnly Property Key As String

        ''' <summary>
        ''' Gets the replacement text of each edited span, in the order of the spans.
        ''' </summary>
        Friend ReadOnly Property Texts As ImmutableArray(Of String)

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="FixReplacement"/> class.
        ''' </summary>
        '''
        ''' <param name="title">
        ''' The title of the code action.
        ''' </param>
        '''
        ''' <param name="key">
        ''' The key of the action among the actions of the rule.
        ''' </param>
        '''
        ''' <param name="texts">
        ''' The replacement text of each edited span; an empty string deletes the span.
        ''' </param>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="title"/>, <paramref name="key"/> or <paramref name="texts"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Sub New(title As String, key As String, ParamArray texts As String())

            If String.IsNullOrEmpty(title) Then
                Throw New ArgumentNullException(NameOf(title))
            End If
            If String.IsNullOrEmpty(key) Then
                Throw New ArgumentNullException(NameOf(key))
            End If
            If texts Is Nothing OrElse texts.Length = 0 OrElse Array.Exists(texts, Function(t As String) t Is Nothing) Then
                Throw New ArgumentNullException(NameOf(texts))
            End If

            Me.Title = title
            Me.Key = key
            Me.Texts = texts.ToImmutableArray()
        End Sub

#End Region

    End Class

End Namespace

#End Region

#Region " UtilFixProperties "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

    ''' <summary>
    ''' Stores code fixes computed by an analyzer in the properties of a diagnostic, for the generic replacement code fix provider.
    ''' </summary>
    '''
    ''' <remarks>
    ''' The edited spans are the additional locations of the diagnostic or, when it has none, its location. Each action replaces 
    ''' every span with one text: <c>Fix.Count</c> holds the number of actions, <c>Fix.{n}.Title</c> and <c>Fix.{n}.Key</c> the 
    ''' title and key of action <c>n</c>, and <c>Fix.{n}.{m}</c> the replacement text of span <c>m</c>. The analyzer validates the 
    ''' replacement (usually with a speculative binding) before reporting it, so the code fix never has to bind it again.
    ''' </remarks>
    Friend NotInheritable Class UtilFixProperties

#Region " Constants "

        ''' <summary>
        ''' The key of the property that holds the number of actions.
        ''' </summary>
        Friend Const CountKey As String = "Fix.Count"

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="UtilFixProperties"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Friend Methods "

        ''' <summary>
        ''' Creates the properties of a diagnostic from its code fix actions.
        ''' </summary>
        '''
        ''' <param name="actions">
        ''' The actions, in the order in which they are offered; none to report the diagnostic without a code fix.
        ''' </param>
        '''
        ''' <returns>
        ''' The properties.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when <paramref name="actions"/> is <see langword="Nothing"/>.
        ''' </exception>
        Friend Shared Function Create(ParamArray actions As FixReplacement()) As ImmutableDictionary(Of String, String)

            If actions Is Nothing Then
                Throw New ArgumentNullException(NameOf(actions))
            End If

            Dim builder As ImmutableDictionary(Of String, String).Builder = ImmutableDictionary.CreateBuilder(Of String, String)(StringComparer.Ordinal)
            If actions.Length = 0 Then
                Return builder.ToImmutable()
            End If

            builder.Add(UtilFixProperties.CountKey, actions.Length.ToString(CultureInfo.InvariantCulture))
            For index As Integer = 0 To actions.Length - 1
                builder.Add(UtilFixProperties.GetTitleKey(index), actions(index).Title)
                builder.Add(UtilFixProperties.GetEquivalenceKey(index), actions(index).Key)
                For edit As Integer = 0 To actions(index).Texts.Length - 1
                    builder.Add(UtilFixProperties.GetTextKey(index, edit), actions(index).Texts(edit))
                Next edit
            Next index
            Return builder.ToImmutable()
        End Function

        ''' <summary>
        ''' Gets the key of the property that holds the title of an action.
        ''' </summary>
        '''
        ''' <param name="action">
        ''' The zero-based index of the action.
        ''' </param>
        '''
        ''' <returns>
        ''' The key, <c>Fix.{action}.Title</c>.
        ''' </returns>
        Friend Shared Function GetTitleKey(action As Integer) As String

            Return $"Fix.{action.ToString(CultureInfo.InvariantCulture)}.Title"
        End Function

        ''' <summary>
        ''' Gets the key of the property that holds the equivalence key of an action.
        ''' </summary>
        '''
        ''' <param name="action">
        ''' The zero-based index of the action.
        ''' </param>
        '''
        ''' <returns>
        ''' The key, <c>Fix.{action}.Key</c>.
        ''' </returns>
        Friend Shared Function GetEquivalenceKey(action As Integer) As String

            Return $"Fix.{action.ToString(CultureInfo.InvariantCulture)}.Key"
        End Function

        ''' <summary>
        ''' Gets the key of the property that holds the replacement text of an edited span for an action.
        ''' </summary>
        '''
        ''' <param name="action">
        ''' The zero-based index of the action.
        ''' </param>
        '''
        ''' <param name="edit">
        ''' The zero-based index of the edited span.
        ''' </param>
        '''
        ''' <returns>
        ''' The key, <c>Fix.{action}.{edit}</c>.
        ''' </returns>
        Friend Shared Function GetTextKey(action As Integer, edit As Integer) As String

            Return $"Fix.{action.ToString(CultureInfo.InvariantCulture)}.{edit.ToString(CultureInfo.InvariantCulture)}"
        End Function

        ''' <summary>
        ''' Creates a diagnostic with code fix actions.
        ''' </summary>
        '''
        ''' <param name="descriptor">
        ''' The descriptor of the rule.
        ''' </param>
        '''
        ''' <param name="location">
        ''' The location of the diagnostic.
        ''' </param>
        '''
        ''' <param name="editedSpans">
        ''' The locations edited by the actions; empty to edit <paramref name="location"/>. They must not overlap.
        ''' </param>
        '''
        ''' <param name="actions">
        ''' The actions; empty to report the diagnostic without a code fix.
        ''' </param>
        '''
        ''' <param name="messageArguments">
        ''' The arguments of the message.
        ''' </param>
        '''
        ''' <returns>
        ''' The diagnostic.
        ''' </returns>
        '''
        ''' <exception cref="ArgumentNullException">
        ''' Thrown when an argument is <see langword="Nothing"/>.
        ''' </exception>
        '''
        ''' <exception cref="ArgumentException">
        ''' Thrown when an action does not have one text per edited span.
        ''' </exception>
        Friend Shared Function CreateDiagnostic(descriptor As DiagnosticDescriptor, location As Location, editedSpans As IEnumerable(Of Location),
                                                actions As IEnumerable(Of FixReplacement), ParamArray messageArguments As Object()) As Diagnostic

            If descriptor Is Nothing Then
                Throw New ArgumentNullException(NameOf(descriptor))
            End If
            If location Is Nothing Then
                Throw New ArgumentNullException(NameOf(location))
            End If
            If editedSpans Is Nothing Then
                Throw New ArgumentNullException(NameOf(editedSpans))
            End If
            If actions Is Nothing Then
                Throw New ArgumentNullException(NameOf(actions))
            End If

            Dim spans As List(Of Location) = editedSpans.ToList()
            Dim fixes As FixReplacement() = actions.ToArray()
            Dim expected As Integer = Math.Max(1, spans.Count)
            If Array.Exists(fixes, Function(f As FixReplacement) f.Texts.Length <> expected) Then
                Throw New ArgumentException("Every action must have one replacement text per edited span.", NameOf(actions))
            End If
            Return Diagnostic.Create(descriptor, location, spans, UtilFixProperties.Create(fixes), messageArguments)
        End Function

#End Region

    End Class

End Namespace

#End Region
