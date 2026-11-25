# 🔀 DCA062 — `TaskContinuationOptions` passed to a `TaskCompletionSource` constructor

**.NET Framework only.** Reports `TaskContinuationOptions` values passed to the constructor of `TaskCompletionSource(Of T)`, such as `New TaskCompletionSource(Of Integer)(TaskContinuationOptions.RunContinuationsAsynchronously)`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Uses the `TaskCreationOptions` member of the same name
- **Equivalent to:** Microsoft [CA2247](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2247) *"Argument passed to TaskCompletionSource constructor should be TaskCreationOptions enum instead of TaskContinuationOptions enum"*

---

## 💡 Why

`TaskCompletionSource(Of T)` has no constructor that takes `TaskContinuationOptions`. The value binds to `New(state As Object)`: it is boxed, stored as the `AsyncState` of the task, and **the option is silently ignored**. The usual intent, running the continuations asynchronously to avoid deadlocks and reentrancy, is lost. The intended constructor takes `TaskCreationOptions`.

On modern .NET, the built-in rule CA2247 already reports this code, so this rule stays silent there.

## 🔎 Detection

The constructor of `TaskCompletionSource(Of T)` with one `Object` parameter, whose argument is a `TaskContinuationOptions` value (a member, a combination with `Or`, or a variable).

## ❌ Non-compliant

```vb
Dim source As New TaskCompletionSource(Of Integer)(TaskContinuationOptions.RunContinuationsAsynchronously)
```

## ✔️ Compliant

```vb
Dim source As New TaskCompletionSource(Of Integer)(TaskCreationOptions.RunContinuationsAsynchronously)
```

## 🔧 Code fix

The fix is offered when the argument only combines `TaskContinuationOptions` members with `Or` (with parentheses), and every member also exists in `TaskCreationOptions`; each member is replaced with the one of the same name:

| Code | Fixed code |
|---|---|
| `TaskContinuationOptions.RunContinuationsAsynchronously` | `TaskCreationOptions.RunContinuationsAsynchronously` |
| `(TaskContinuationOptions.RunContinuationsAsynchronously Or TaskContinuationOptions.LongRunning)` | `(TaskCreationOptions.RunContinuationsAsynchronously Or TaskCreationOptions.LongRunning)` |

Members without an equivalent (`ExecuteSynchronously`, `OnlyOnFaulted`...) and variables are reported without a fix.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA062.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA062 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA062
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA062:TaskContinuationOptions passed to a TaskCompletionSource constructor", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA062:TaskContinuationOptions passed to a TaskCompletionSource constructor", Justification:="Reason.", Scope:="module")>
```
