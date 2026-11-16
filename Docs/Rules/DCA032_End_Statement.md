# 🛑 DCA032 — `End` statement

Reports the standalone `End` statement, which terminates the process immediately.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the right replacement depends on the application)
- **Equivalent to:** SonarQube [S1147](https://rules.sonarsource.com/vbnet/RSPEC-1147/) *""End" statements should not be used"*

---

## 💡 Why

`End` stops the process **abruptly**:

- `Finally` blocks, `Using` blocks and `Dispose` methods do not run: files may stay incomplete, connections open, temporary files undeleted;
- the `FormClosing`/`FormClosed` events do not run: settings are not saved, the user is not asked to save changes;
- buffered output (log files, `StreamWriter`) may be lost.

## 🔎 Detection

The standalone `End` statement. Block terminators (`End If`, `End Sub`, `End Class`...) are different statements and are never reported.

> [!NOTE]
> In class library projects, `End` is already a compiler error (BC30615); this rule matters in Windows Forms and console applications.

## ❌ Non-compliant

```vb
Private Sub ExitMenuItem_Click(sender As Object, e As EventArgs) Handles ExitMenuItem.Click
    End
End Sub
```

## ✔️ Compliant

```vb
Private Sub ExitMenuItem_Click(sender As Object, e As EventArgs) Handles ExitMenuItem.Click
    Me.Close() ' Or Application.Exit() to close every form.
End Sub
```

| Program type | Replacement |
|---|---|
| Windows Forms | `Me.Close()` on the main form, or `Application.Exit()` |
| Console | `Return` from `Sub Main`, or `Environment.Exit(code)` when an exit code must be returned from deep inside the program |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA032.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA032 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA032
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA032:'End' statement", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA032:'End' statement", Justification:="Reason.", Scope:="module")>
```
