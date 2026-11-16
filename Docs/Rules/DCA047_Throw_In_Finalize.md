# ☠️ DCA047 — Exception thrown in `Finalize`

Reports `Throw` statements that leave the `Finalize` method.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the finalizer must be redesigned)
- **Equivalent to:** SonarQube [S1048](https://rules.sonarsource.com/vbnet/RSPEC-1048/) *"Finalize method should not throw exceptions"*
- **Related:** Microsoft [CA1065](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1065) (disabled by default)

---

## 💡 Why

Finalizers run on the **finalizer thread**, where nobody can catch an exception. An unhandled exception there **terminates the process** immediately, at an unpredictable moment (whenever the garbage collector runs), without running other finalizers or `Finally` blocks. It is one of the hardest crashes to diagnose.

## 🔎 Detection

`Throw` statements inside `Protected Overrides Sub Finalize()`.

Not reported: exceptions thrown inside the `Try` section of a `Try` block with `Catch` blocks, and inside lambdas.

## ❌ Non-compliant

```vb
Protected Overrides Sub Finalize()
    If Me.handle = IntPtr.Zero Then
        Throw New InvalidOperationException("The handle was not created.")
    End If
    NativeMethods.CloseHandle(Me.handle)
    MyBase.Finalize()
End Sub
```

## ✔️ Compliant

```vb
Protected Overrides Sub Finalize()
    If Me.handle <> IntPtr.Zero Then
        NativeMethods.CloseHandle(Me.handle)
    End If
    MyBase.Finalize()
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA047.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA047 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA047
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA047:Exception thrown in Finalize", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA047:Exception thrown in Finalize", Justification:="Reason.", Scope:="module")>
```
