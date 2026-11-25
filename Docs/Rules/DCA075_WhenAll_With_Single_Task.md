# ⏳ DCA075 — `Task.WhenAll` with a single task

**.NET Framework only.** Reports `Task.WhenAll` called with a single task, such as `Await Task.WhenAll(work)`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses the task directly
- **Equivalent to:** Microsoft [CA1842](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1842) *"Do not use 'WhenAll' with a single task"*

---

## 💡 Why

`WhenAll` allocates an array and a combined task, and adds a continuation, to wait for **one** task: awaiting the task itself does the same, and throws the same exception.

On modern .NET, the built-in rule CA1842 already reports this code, so this rule stays silent there.

## 🔎 Detection

Only the non-generic `WhenAll(ParamArray tasks As Task())` overload with exactly one element is reported. The generic overload `WhenAll(Of TResult)` returns an array of results, which the task itself does not; an explicit array (`Task.WhenAll(New Task() {work})`) and several tasks are not reported.

## ❌ Non-compliant

```vb
Await Task.WhenAll(work).ConfigureAwait(False)
```

## ✔️ Compliant

```vb
Await work.ConfigureAwait(False)
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `Await Task.WhenAll(work)` | `Await work` |
| `Return Task.WhenAll(work)` | `Return work` |
| `Task.WhenAll(If(work, other))` | `If(work, other)` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA075.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA075 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA075
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA075:Task.WhenAll with a single task", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA075:Task.WhenAll with a single task", Justification:="Reason.", Scope:="module")>
```
