# 🧨 DCA046 — Exception thrown in a `Finally` block

**.NET Framework only.** Reports `Throw` statements that leave a `Finally` block.

- **Severity:** Info
- **Category:** Reliability
- **Code fix:** ❌ (the cleanup code must be redesigned)
- **Equivalent to:** Microsoft [CA2219](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2219) *"Do not raise exceptions in exception clauses"* and SonarQube [S1163](https://rules.sonarsource.com/vbnet/RSPEC-1163/) *"Exceptions should not be thrown in finally blocks"*

> [!NOTE]
> On modern .NET, the built-in rule CA2219 already reports these statements, so this rule stays silent there (since version 1.0.6). Its severity is Info, the same as CA2219 (*suggestion*); before 1.0.6 it was a Warning.

---

## 💡 Why

When the `Try` block throws, the `Finally` block runs while that exception is propagating. If the `Finally` block throws too, the new exception **replaces** the original one: the real error, its message and its stack trace are lost, and the log shows a secondary failure of the cleanup code.

## 🔎 Detection

`Throw` statements inside a `Finally` block, including `Throw` (rethrow) in a `Catch` block of a `Try` statement nested in the `Finally` block.

Not reported:

| Case | Reason |
|---|---|
| `Throw` inside the `Try` section of a nested `Try` with `Catch` blocks | It is handled inside the `Finally` block |
| `Throw` inside a lambda declared in the `Finally` block | It runs later, if ever |

## ❌ Non-compliant

```vb
Try
    Me.WriteData(stream)
Finally
    If Not Me.connection.Close() Then
        Throw New IOException("The connection could not be closed.")
    End If
End Try
```

## ✔️ Compliant

```vb
Try
    Me.WriteData(stream)
Finally
    If Not Me.connection.Close() Then
        Trace.TraceWarning("The connection could not be closed.") ' The original exception, if any, keeps propagating.
    End If
End Try
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA046.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA046 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA046
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA046:Exception thrown in a Finally block", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA046:Exception thrown in a Finally block", Justification:="Reason.", Scope:="module")>
```
