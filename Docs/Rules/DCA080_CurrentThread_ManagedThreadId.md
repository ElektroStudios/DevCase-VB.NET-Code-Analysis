# 🆔 DCA080 — `Thread.CurrentThread.ManagedThreadId` instead of `Environment.CurrentManagedThreadId`

**.NET Framework only.** Reports `Thread.CurrentThread.ManagedThreadId`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `Environment.CurrentManagedThreadId`
- **Equivalent to:** Microsoft [CA1840](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1840) *"Use Environment.CurrentManagedThreadId instead of Thread.CurrentThread.ManagedThreadId"*

---

## 💡 Why

`Thread.CurrentThread` gets (and, the first time on a thread, creates) the `Thread` object of the current thread only to read its identifier. `Environment.CurrentManagedThreadId` returns the same value directly.

On modern .NET, the built-in rule CA1840 already reports this code, so this rule stays silent there.

## 🔎 Detection

`ManagedThreadId` read on `Thread.CurrentThread`. The rule is only enabled when `Environment.CurrentManagedThreadId` exists (.NET Framework 4.5 and later). The identifier of another thread (`worker.ManagedThreadId`) is not reported.

## ❌ Non-compliant

```vb
Dim id As Integer = Thread.CurrentThread.ManagedThreadId
```

## ✔️ Compliant

```vb
Dim id As Integer = Environment.CurrentManagedThreadId
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `Thread.CurrentThread.ManagedThreadId` | `Environment.CurrentManagedThreadId` |

The type name is written as the file can use it (`System.Environment` when `System` is not imported).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA080.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA080 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA080
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA080:Thread.CurrentThread.ManagedThreadId instead of Environment.CurrentManagedThreadId", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA080:Thread.CurrentThread.ManagedThreadId instead of Environment.CurrentManagedThreadId", Justification:="Reason.", Scope:="module")>
```
