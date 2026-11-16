# ⏸️ DCA038 — `Thread.Suspend` or `Thread.Resume`

Reports calls to `Thread.Suspend` and `Thread.Resume`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (a synchronization primitive must be introduced)
- **Equivalent to:** SonarQube [S3889](https://rules.sonarsource.com/vbnet/RSPEC-3889/) *""Thread.Resume" and "Thread.Suspend" should not be used"*

---

## 💡 Why

`Thread.Suspend` stops the thread at an **arbitrary point**: possibly while it holds a lock, including the internal locks of the runtime (class constructors, the heap, the loader). Any other thread that needs that lock then waits forever, and the process **deadlocks**.

Both methods are marked as obsolete since .NET Framework 2.0 (warning BC40000) and throw `PlatformNotSupportedException` on modern .NET. This rule keeps them visible even when the obsolete warning is suppressed.

## 🔎 Detection

Calls to `System.Threading.Thread.Suspend` and `System.Threading.Thread.Resume`.

## ❌ Non-compliant

```vb
Private Sub PauseButton_Click(sender As Object, e As EventArgs) Handles PauseButton.Click
    Me.worker.Suspend()
End Sub
```

## ✔️ Compliant

```vb
Private ReadOnly running As New ManualResetEventSlim(initialState:=True)

Private Sub PauseButton_Click(sender As Object, e As EventArgs) Handles PauseButton.Click
    Me.running.Reset() ' The worker waits at a safe point: Me.running.Wait()
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA038.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA038 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA038
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA038:Thread.Suspend or Thread.Resume", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA038:Thread.Suspend or Thread.Resume", Justification:="Reason.", Scope:="module")>
```
