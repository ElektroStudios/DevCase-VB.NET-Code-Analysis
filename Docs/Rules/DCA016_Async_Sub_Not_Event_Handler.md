# ⚡ DCA016 — `Async Sub` that is not an event handler

Reports `Async Sub` methods that are not event handlers.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Converts it into `Async Function ... As Task`

---

## 💡 Why

An `Async Sub` cannot be awaited: the caller cannot know when it finishes, and an exception thrown inside it is raised on the synchronization context, which usually crashes the process. `Async Sub` is only meant for event handlers.

## 🔎 Detection

Not reported, because their signature cannot or must not change:

- Methods with a `Handles` clause.
- Methods with the event-handler signature `(sender As Object, e As <EventArgs or derived>)`, e.g. handlers attached with `AddHandler`.
- `Overrides` members and members with an `Implements` clause.

Async lambdas are not analyzed.

## ❌ Non-compliant

```vb
Public Async Sub Refresh()
    Await Task.Delay(1)
End Sub

Private Async Sub Save(path As String)
    Await File.WriteAllTextAsync(path, Me.text)
End Sub
```

## ✔️ Not reported

```vb
Private Async Sub OnChanged(sender As Object, e As EventArgs) Handles Me.Changed
    Await Task.Delay(1)
End Sub

Private Async Sub Button_Click(sender As Object, e As EventArgs)
    Await Task.Delay(1)
End Sub

Public Overrides Async Sub OnTick()
    Await Task.Delay(1)
End Sub
```

## 🔧 Code fix

`Sub` becomes `Function`, `As Task` is added (a parameter list is added if missing), `End Sub` becomes `End Function`, and the method's own `Exit Sub` statements become `Exit Function` (those of nested `Sub` lambdas are kept):

```vb
' Before
Public Async Sub Refresh(force As Boolean)
    If Not force Then
        Exit Sub
    End If
    Dim action As Action = Sub()
                               Exit Sub
                           End Sub
    Await Task.Delay(1)
End Sub

' After
Public Async Function Refresh(force As Boolean) As Task
    If Not force Then
        Exit Function
    End If
    Dim action As Action = Sub()
                               Exit Sub
                           End Sub
    Await Task.Delay(1)
End Function
```

> [!IMPORTANT]
> Call sites are not modified: await the returned task where the method is called. A method passed with `AddressOf` to an `Action` delegate must be adapted by hand.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA016.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA016 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA016
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA016:Async Sub is not an event handler", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA016:Async Sub is not an event handler", Justification:="Reason.", Scope:="module")>
```
