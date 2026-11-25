# ⌛ DCA076 — `Task.WaitAll` with a single task

**.NET Framework only.** Reports `Task.WaitAll` called with a single task, such as `Task.WaitAll(work)`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `task.Wait()`
- **Equivalent to:** Microsoft [CA1843](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1843) *"Do not use 'WaitAll' with a single task"*

---

## 💡 Why

`WaitAll` allocates an array to wait for **one** task. `task.Wait()` waits for it directly and throws the same `AggregateException` when it fails.

On modern .NET, the built-in rule CA1843 already reports this code, so this rule stays silent there.

## 🔎 Detection

Only the `WaitAll(ParamArray tasks As Task())` overload with exactly one element is reported. The overloads with a timeout or a cancellation token take an explicit array, and are not reported.

## ❌ Non-compliant

```vb
Task.WaitAll(work)
```

## ✔️ Compliant

```vb
work.Wait()
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `Task.WaitAll(work)` | `work.Wait()` |
| `Task.WaitAll(result)` (`Task(Of Integer)`) | `result.Wait()` |

Blocking on a task is still blocking: in asynchronous code, prefer `Await`.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA076.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA076 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA076
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA076:Task.WaitAll with a single task", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA076:Task.WaitAll with a single task", Justification:="Reason.", Scope:="module")>
```
