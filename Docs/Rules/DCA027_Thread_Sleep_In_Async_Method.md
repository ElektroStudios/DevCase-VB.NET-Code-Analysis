# 💤 DCA027 — `Thread.Sleep` in an `Async` method

Reports calls to `Thread.Sleep` inside `Async` methods and `Async` lambdas.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Two lightbulb actions: `Await Task.Delay(x).ConfigureAwait(False)` or `Await Task.Delay(x).ConfigureAwait(True)`

---

## 💡 Why

`Thread.Sleep` **blocks the thread** for the whole delay. Inside an `Async` method this defeats its purpose:

- In a Windows Forms event handler (`Async Sub Button1_Click`), the code before the first `Await` runs on the **UI thread**: `Thread.Sleep(2000)` freezes the window for two seconds.
- In background code, it keeps a **thread-pool thread** busy doing nothing, which delays other tasks.

`Await Task.Delay(x)` waits **without blocking any thread**: the method is suspended and resumes when the delay expires.

## 🔎 Detection

`System.Threading.Thread.Sleep(Integer)` and `Thread.Sleep(TimeSpan)` calls whose nearest enclosing method or lambda is `Async`.

Not reported:

| Case | Reason |
|---|---|
| `Thread.Sleep(0)` | It does not wait: it only yields the rest of the time slice |
| Calls in synchronous methods | `Await` is not available there |
| Calls in a synchronous lambda inside an `Async` method (`Sub() Thread.Sleep(5)`) | The lambda runs synchronously |

## ❌ Non-compliant

```vb
Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs) Handles RefreshButton.Click
    Thread.Sleep(2000) ' The window freezes for two seconds.
    Await Me.LoadDataAsync()
End Sub
```

## ✔️ Compliant

```vb
Private Async Sub RefreshButton_Click(sender As Object, e As EventArgs) Handles RefreshButton.Click
    Await Task.Delay(2000).ConfigureAwait(True)
    Await Me.LoadDataAsync()
End Sub
```

## 🔧 Code fix

| Action | Result | When to use it |
|---|---|---|
| **`ConfigureAwait(False)`** (first) | `Await Task.Delay(2000).ConfigureAwait(False)` | Library code and background work |
| **`ConfigureAwait(True)`** | `Await Task.Delay(2000).ConfigureAwait(True)` | UI code: the code after the delay keeps running on the UI thread |

> [!WARNING]
> In UI code, choose `ConfigureAwait(True)`. With `ConfigureAwait(False)`, the code after the delay runs on a thread-pool thread and cannot access the controls of the form (`InvalidOperationException`: cross-thread operation).

Details:

- `Call Thread.Sleep(x)` is replaced too, and named arguments (`millisecondsTimeout:=250`) become positional (`Task.Delay(250)`).
- `Task` is written with the shortest name that resolves correctly.
- No fix is offered inside `Catch`, `Finally` or `SyncLock` blocks, where Visual Basic does not allow `Await`, or when the call is part of a larger expression. The diagnostic is still reported.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA027.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA027 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA027
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA027:Thread.Sleep in an Async method", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA027:Thread.Sleep in an Async method", Justification:="Reason.", Scope:="module")>
```
