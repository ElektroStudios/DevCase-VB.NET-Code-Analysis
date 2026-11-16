# 🔓 DCA037 — `Monitor.Enter` without `Monitor.Exit` in a `Finally` block

Reports calls to `Monitor.Enter` and `Monitor.TryEnter` that are not paired with `Monitor.Exit` in a `Finally` block.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the protected region must be wrapped in `Try...Finally` by hand)
- **Equivalent to:** SonarQube [S2222](https://rules.sonarsource.com/vbnet/RSPEC-2222/) *"Locks should be released on all paths"*

---

## 💡 Why

If an exception occurs between `Monitor.Enter` and `Monitor.Exit`, the lock is **never released**: every other thread that tries to acquire it waits forever, and the application hangs. `SyncLock` releases the lock automatically (it compiles to `Try...Finally`); with `Monitor`, the `Finally` block must be written by hand.

## 🔎 Detection

A call is accepted when `Monitor.Exit` on an equivalent lock expression is in the `Finally` block of a `Try` statement that:

| Pattern | Example |
|---|---|
| Contains the call in its `Try` section | `Try` / `Monitor.Enter(gate, lockTaken)` / ... / `Finally` / `If lockTaken Then Monitor.Exit(gate)` |
| Immediately follows the statement of the call | `Monitor.Enter(gate)` / `Try` / ... / `Finally` / `Monitor.Exit(gate)` |
| Is inside the `If` block whose condition contains the call | `If Monitor.TryEnter(gate, 100) Then` / `Try` / ... / `Finally` / `Monitor.Exit(gate)` |

Every other call is reported, including locks acquired in one method and released in another one, because the release cannot be guaranteed.

## ❌ Non-compliant

```vb
Monitor.Enter(Me.gate)
Me.cache.Add(key, value) ' If this throws, the lock is never released.
Monitor.Exit(Me.gate)
```

## ✔️ Compliant

```vb
SyncLock Me.gate
    Me.cache.Add(key, value)
End SyncLock

' Or, when Monitor is required (e.g. TryEnter with a timeout):
If Monitor.TryEnter(Me.gate, TimeSpan.FromSeconds(1)) Then
    Try
        Me.cache.Add(key, value)
    Finally
        Monitor.Exit(Me.gate)
    End Try
End If
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA037.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA037 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA037
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA037:Monitor.Enter without Monitor.Exit in a Finally block", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA037:Monitor.Enter without Monitor.Exit in a Finally block", Justification:="Reason.", Scope:="module")>
```
