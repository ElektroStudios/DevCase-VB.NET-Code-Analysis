# 🧱 DCA002 — Blocking `.Result` on a task

Reports `.Result` (and the VB property-call form `.Result()`) on `Task(Of T)` and `ValueTask(Of T)`.

- **Severity:** Error
- **Category:** Reliability
- **Code fix:** ✔️ Replaces it with `.GetAwaiter().GetResult()`

---

## 💡 Why

`Task(Of T).Result` wraps any fault inside an `AggregateException`, hiding the original exception type and stack trace. `GetAwaiter().GetResult()` blocks in the same way but rethrows the original exception, which keeps fail-fast error handling clean. (Inside an `Async` method, prefer `Await`.)

## 🔎 Detection

The `Result` member is bound through the semantic model: only the `Result` property of `Task(Of T)` and `ValueTask(Of T)` is reported. Look-alike properties on other types are ignored.

## ❌ Non-compliant

```vb
Public Sub Run(work As Task(Of Integer), valueWork As ValueTask(Of String))
    Dim a As Integer = work.Result
    Dim b As Integer = work.Result()
    Dim c As String = valueWork.Result
    Dim d As Integer = Task.FromResult(3).Result
End Sub
```

## ✔️ Compliant / not reported

```vb
Public Class AsyncTaskSettings
    Public Property Result As Integer
End Class

Dim a As Integer = myAsyncTask.Result              ' AsyncTaskSettings.Result is not a task.
Dim name As String = NameOf(work.Result)           ' NameOf is not an access.
Dim b As Integer = work.GetAwaiter().GetResult()
```

## 🔧 Code fix

```vb
' Before
Dim a As Integer = work.Result ' comment
Dim b As Integer = work.Result()
Dim c As Integer? = maybe?.Result
With work
    Dim d As Integer = .Result
End With
Dim e As String = Task.FromResult(1).Result.ToString()

' After
Dim a As Integer = work.GetAwaiter().GetResult() ' comment
Dim b As Integer = work.GetAwaiter().GetResult()
Dim c As Integer? = maybe?.GetAwaiter().GetResult()
With work
    Dim d As Integer = .GetAwaiter().GetResult()
End With
Dim e As String = Task.FromResult(1).GetAwaiter().GetResult().ToString()
```

## 🔕 How to disable

This rule is reported as an **error** by default. The directives below also suppress analyzer errors; to keep seeing it without blocking the Error List, lower it to `warning` or `suggestion` instead of disabling it.

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA002.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA002 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA002
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA002:Blocking .Result access on a task", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA002:Blocking .Result access on a task", Justification:="Reason.", Scope:="module")>
```
