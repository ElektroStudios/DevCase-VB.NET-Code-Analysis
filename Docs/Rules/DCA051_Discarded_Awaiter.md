# 🕳️ DCA051 — Awaiter or `ConfigureAwait` result discarded

Reports statements that call `GetAwaiter()` or `ConfigureAwait(...)` on a task and discard the result.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (awaiting, blocking or running in the background are different behaviors that only the author can choose)

---

## 💡 Why

Neither method waits for the task:

- `GetAwaiter()` returns the object that `Await` uses to wait. The method that waits is its `GetResult()`.
- `ConfigureAwait(False)` returns a configured version of the task, which only matters when it is **awaited**: `Await task.ConfigureAwait(False)`.

As a statement, both calls **do nothing**. The method continues and returns while the task is still running, and any exception of the task (including the exceptions the XML documentation promises) is **lost**. The code reads like a wait, but it is a *fire-and-forget*:

```vb
''' <exception cref="InvalidOperationException">
''' Thrown when another fetch operation is already running.   ← never thrown to the caller
''' </exception>
Public Sub FetchAlbums()
    Task.Run(Of Boolean)(AddressOf Me.FetchAlbumsAsync).GetAwaiter()   ' Does not wait.
End Sub
```

## 🔎 Detection

An expression statement (also with `Call`, and without parentheses) whose call is `GetAwaiter` or `ConfigureAwait` on `Task`, `Task(Of T)`, `ValueTask`, `ValueTask(Of T)`, `ConfiguredTaskAwaitable` or `ConfiguredValueTaskAwaitable`.

Not reported:

| Case | Reason |
|---|---|
| `Await task.ConfigureAwait(False)` | The task is awaited |
| `task.GetAwaiter().GetResult()` | The task is waited (the statement is `GetResult`) |
| `Dim awaiter As TaskAwaiter = task.GetAwaiter()` | The awaiter is used |
| `GetAwaiter` methods of other types | Their meaning is unknown |
| A task-returning call as a statement (`Me.WorkAsync()`) | Possibly an intended *fire-and-forget*; the Visual Basic compiler already warns inside `Async` methods (BC42358) |

## ❌ Non-compliant

```vb
Public Sub FetchAlbums()
    Me.ThrowIfDisposed()
    Task.Run(Of Boolean)(AddressOf Me.FetchAlbumsAsync).GetAwaiter()
End Sub

Public Sub FetchAlbums2()
    Me.FetchAlbumsAsync.ConfigureAwait(False)
End Sub
```

## ✔️ Compliant

Choose the behavior you actually want:

```vb
' 1. Wait synchronously. Task.Run avoids the deadlock when called from a UI thread,
'    and GetResult() throws the original exception instead of an AggregateException.
Public Sub FetchAlbums()
    Me.ThrowIfDisposed()
    Task.Run(Of Boolean)(AddressOf Me.FetchAlbumsAsync).GetAwaiter().GetResult()
End Sub

' 2. Be asynchronous: the caller awaits, and receives the exceptions.
Public Async Function FetchAlbumsAsync() As Task(Of Boolean)
    ...
End Function

' 3. Run in the background on purpose: keep the task and observe its exceptions.
Public Sub StartFetchingAlbums()
    Dim fetch As Task(Of Boolean) = Me.FetchAlbumsAsync()
    fetch.ContinueWith(Sub(t As Task(Of Boolean)) Me.OnFetchFailed(t.Exception), TaskContinuationOptions.OnlyOnFaulted)
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA051.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA051 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA051
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA051:Awaiter or ConfigureAwait result discarded", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA051:Awaiter or ConfigureAwait result discarded", Justification:="Reason.", Scope:="module")>
```
