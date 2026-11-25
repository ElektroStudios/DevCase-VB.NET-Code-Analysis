#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Collections.Immutable
Imports System.Composition
Imports System.Globalization
Imports System.Linq
Imports System.Threading
Imports System.Threading.Tasks

Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Text

Imports DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Analyzers

#End Region

#Region " ReplacementCodeFixProvider "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.CodeFixes

    ''' <summary>
    ''' Provides the code fixes that an analyzer computes and validates itself: each action replaces the edited spans of the 
    ''' diagnostic with the texts stored in its properties (see <see cref="UtilFixProperties"/>).
    ''' </summary>
    '''
    ''' <remarks>
    ''' The analyzer binds the replacement speculatively before reporting it, so this provider only applies text changes. The 
    ''' equivalence key of each action is the rule identifier followed by the key of the action, which lets Fix All apply the 
    ''' same kind of action to every diagnostic of the rule. Fix All merges the changes of every diagnostic of a document: 
    ''' insertions at the same position are concatenated (several fields extracted before the same member), and a diagnostic 
    ''' whose changes overlap the changes already accepted is left for a later pass.
    ''' </remarks>
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(ReplacementCodeFixProvider)), [Shared]>
    Public NotInheritable Class ReplacementCodeFixProvider : Inherits CodeFixProvider

#Region " Fields "

        ''' <summary>
        ''' The Fix All provider, which merges the changes of the diagnostics of each document.
        ''' </summary>
        Private Shared ReadOnly FixAll As FixAllProvider = FixAllProvider.Create(AddressOf ReplacementCodeFixProvider.FixAllAsync)

#End Region

#Region " Properties "

        ''' <inheritdoc/>
        Public Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String)
            Get
                Return ImmutableArray.Create(DiagnosticIds.GenericOverloadInsteadOfTypeArgument, DiagnosticIds.MissingDesignerSerializationConfiguration,
                                             DiagnosticIds.TaskContinuationOptionsAsState, DiagnosticIds.BlockCopyLengthAsByteCount,
                                             DiagnosticIds.StringLiteralInsteadOfNameOf, DiagnosticIds.ManualCancellationCheck,
                                             DiagnosticIds.CancellationTokenNotForwarded, DiagnosticIds.CountInsteadOfAny,
                                             DiagnosticIds.CountMethodInsteadOfProperty, DiagnosticIds.CountInsteadOfIsEmpty,
                                             DiagnosticIds.WhenAllWithSingleTask, DiagnosticIds.WaitAllWithSingleTask,
                                             DiagnosticIds.ContainsKeyBeforeRemove, DiagnosticIds.ContainsBeforeSetAddOrRemove,
                                             DiagnosticIds.KeysOrValuesContains, DiagnosticIds.CurrentThreadManagedThreadId,
                                             DiagnosticIds.ContainsKeyThenIndexer, DiagnosticIds.IndexOfComparedWithZero,
                                             DiagnosticIds.RedundantSubstringLength, DiagnosticIds.ConstantArrayArgument)
            End Get
        End Property

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="ReplacementCodeFixProvider"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Public Methods "

        ''' <inheritdoc/>
        Public Overrides Function GetFixAllProvider() As FixAllProvider

            Return ReplacementCodeFixProvider.FixAll
        End Function

        ''' <inheritdoc/>
        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task

            For Each diagnostic As Diagnostic In context.Diagnostics
                Dim document As Document = context.Document
                For index As Integer = 0 To ReplacementCodeFixProvider.GetActionCount(diagnostic) - 1
                    Dim action As Integer = index
                    Dim title As String = diagnostic.Properties(UtilFixProperties.GetTitleKey(action))
                    Dim key As String = diagnostic.Properties(UtilFixProperties.GetEquivalenceKey(action))
                    context.RegisterCodeFix(
                        CodeAction.Create(title,
                                          Function(ct As CancellationToken) ReplacementCodeFixProvider.ApplyAsync(document, diagnostic, action, ct),
                                          equivalenceKey:=$"{diagnostic.Id}.{key}"),
                        diagnostic)
                Next index
            Next diagnostic
            Return Task.CompletedTask
        End Function

#End Region

#Region " Private Methods "

        ''' <summary>
        ''' Gets the number of code fix actions stored in a diagnostic.
        ''' </summary>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <returns>
        ''' The number of actions; 0 if the diagnostic is reported without a code fix.
        ''' </returns>
        Private Shared Function GetActionCount(diagnostic As Diagnostic) As Integer

            Dim countText As String = Nothing
            Dim count As Integer
            Return If(diagnostic.Properties.TryGetValue(UtilFixProperties.CountKey, countText) AndAlso
                      Integer.TryParse(countText, NumberStyles.None, CultureInfo.InvariantCulture, count), count, 0)
        End Function

        ''' <summary>
        ''' Applies an action of a diagnostic: replaces each edited span with its text.
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
        ''' <param name="action">
        ''' The zero-based index of the action.
        ''' </param>
        '''
        ''' <param name="cancellationToken">
        ''' The token to monitor for cancellation requests.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function ApplyAsync(document As Document, diagnostic As Diagnostic, action As Integer,
                                                 cancellationToken As CancellationToken) As Task(Of Document)

            Dim text As SourceText = Await document.GetTextAsync(cancellationToken).ConfigureAwait(False)
            Return document.WithText(text.WithChanges(ReplacementCodeFixProvider.GetChanges(diagnostic, action)))
        End Function

        ''' <summary>
        ''' Applies the action with the equivalence key of a Fix All operation to every diagnostic of a document.
        ''' </summary>
        '''
        ''' <param name="context">
        ''' The Fix All context.
        ''' </param>
        '''
        ''' <param name="document">
        ''' The document to fix.
        ''' </param>
        '''
        ''' <param name="diagnostics">
        ''' The diagnostics of the document.
        ''' </param>
        '''
        ''' <returns>
        ''' The fixed document.
        ''' </returns>
        Private Shared Async Function FixAllAsync(context As FixAllContext, document As Document, diagnostics As ImmutableArray(Of Diagnostic)) As Task(Of Document)

            Dim accepted As New List(Of TextChange)()
            For Each diagnostic As Diagnostic In diagnostics.OrderBy(Function(d As Diagnostic) d.Location.SourceSpan.Start)
                Dim action As Integer = ReplacementCodeFixProvider.FindAction(diagnostic, context.CodeActionEquivalenceKey)
                If action < 0 Then
                    Continue For
                End If

                Dim changes As List(Of TextChange) = ReplacementCodeFixProvider.GetChanges(diagnostic, action)
                If changes.Exists(Function(c As TextChange) ReplacementCodeFixProvider.Conflicts(c, accepted)) Then
                    Continue For ' Left for a later pass: the analyzer reports it again on the fixed document.
                End If

                For Each change As TextChange In changes
                    Dim position As Integer = change.Span.Start
                    Dim index As Integer = accepted.FindIndex(Function(a As TextChange) a.Span.IsEmpty AndAlso a.Span.Start = position)
                    If change.Span.IsEmpty AndAlso index >= 0 Then
                        accepted(index) = New TextChange(accepted(index).Span, $"{accepted(index).NewText}{change.NewText}")
                    Else
                        accepted.Add(change)
                    End If
                Next change
            Next diagnostic

            If accepted.Count = 0 Then
                Return document
            End If

            Dim text As SourceText = Await document.GetTextAsync(context.CancellationToken).ConfigureAwait(False)
            Return document.WithText(text.WithChanges(accepted.OrderBy(Function(c As TextChange) c.Span.Start).ThenBy(Function(c As TextChange) c.Span.Length)))
        End Function

        ''' <summary>
        ''' Gets the index of the action of a diagnostic that has an equivalence key.
        ''' </summary>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <param name="equivalenceKey">
        ''' The equivalence key (the rule identifier, a dot and the key of the action).
        ''' </param>
        '''
        ''' <returns>
        ''' The zero-based index of the action; -1 if the diagnostic does not offer it.
        ''' </returns>
        Private Shared Function FindAction(diagnostic As Diagnostic, equivalenceKey As String) As Integer

            If equivalenceKey Is Nothing Then
                Return -1
            End If

            For action As Integer = 0 To ReplacementCodeFixProvider.GetActionCount(diagnostic) - 1
                If equivalenceKey.Equals($"{diagnostic.Id}.{diagnostic.Properties(UtilFixProperties.GetEquivalenceKey(action))}", StringComparison.Ordinal) Then
                    Return action
                End If
            Next action
            Return -1
        End Function

        ''' <summary>
        ''' Gets the text changes of an action of a diagnostic.
        ''' </summary>
        '''
        ''' <param name="diagnostic">
        ''' The diagnostic.
        ''' </param>
        '''
        ''' <param name="action">
        ''' The zero-based index of the action.
        ''' </param>
        '''
        ''' <returns>
        ''' The changes, sorted by position: each edited span (the additional locations, or the location when there are none) 
        ''' with its text.
        ''' </returns>
        Private Shared Function GetChanges(diagnostic As Diagnostic, action As Integer) As List(Of TextChange)

            Dim spans As List(Of TextSpan) = If(diagnostic.AdditionalLocations.Count = 0,
                                                New List(Of TextSpan) From {diagnostic.Location.SourceSpan},
                                                diagnostic.AdditionalLocations.Select(Function(l As Location) l.SourceSpan).ToList())
            Dim changes As New List(Of TextChange)()
            For edit As Integer = 0 To spans.Count - 1
                changes.Add(New TextChange(spans(edit), diagnostic.Properties(UtilFixProperties.GetTextKey(action, edit))))
            Next edit
            Return changes.OrderBy(Function(c As TextChange) c.Span.Start).ThenBy(Function(c As TextChange) c.Span.Length).ToList()
        End Function

        ''' <summary>
        ''' Determines whether a change conflicts with the changes already accepted by Fix All.
        ''' </summary>
        '''
        ''' <param name="change">
        ''' The change.
        ''' </param>
        '''
        ''' <param name="accepted">
        ''' The accepted changes.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if the change edits text that an accepted change edits, or inserts inside it; 
        ''' <see langword="False"/> otherwise (two insertions at the same position are merged).
        ''' </returns>
        Private Shared Function Conflicts(change As TextChange, accepted As List(Of TextChange)) As Boolean

            Return accepted.Exists(Function(a As TextChange) ReplacementCodeFixProvider.Overlap(a.Span, change.Span))
        End Function

        ''' <summary>
        ''' Determines whether two edited spans overlap.
        ''' </summary>
        '''
        ''' <param name="first">
        ''' The first span.
        ''' </param>
        '''
        ''' <param name="second">
        ''' The second span.
        ''' </param>
        '''
        ''' <returns>
        ''' <see langword="True"/> if both replace common text, if one inserts strictly inside the other, or if one inserts where 
        ''' the other replaces text; <see langword="False"/> for two insertions.
        ''' </returns>
        Private Shared Function Overlap(first As TextSpan, second As TextSpan) As Boolean

            Select Case True
                Case first.IsEmpty AndAlso second.IsEmpty
                    Return False
                Case first.IsEmpty
                    Return first.Start >= second.Start AndAlso first.Start < second.End
                Case second.IsEmpty
                    Return second.Start >= first.Start AndAlso second.Start < first.End
                Case Else
                    Return first.OverlapsWith(second)
            End Select
        End Function

#End Region

    End Class

End Namespace

#End Region
