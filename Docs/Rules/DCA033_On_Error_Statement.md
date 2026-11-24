# 🚧 DCA033 — `On Error` statement

Reports the unstructured error handling statements `On Error GoTo` (any form) and `On Error Resume Next`.

- **Severity:** Info
- **Category:** Reliability
- **Code fix:** ❌ (the handler must be redesigned)
- **Equivalent to:** SonarQube [S2359](https://rules.sonarsource.com/vbnet/RSPEC-2359/) *""On Error" statements should not be used"*

---

## 💡 Why

- `On Error Resume Next` **silently ignores every error** of the rest of the method: a failed file write, a `Nothing` reference or a wrong conversion are skipped, and the method goes on with invalid data.
- `On Error GoTo Label` jumps across the method, handles every exception type the same way, and makes the flow very hard to follow (`Resume`, `Resume Next`, `Err.Number`).

`Try...Catch` handles exceptions in a **visible, scoped and typed** way: only the statements in the `Try` block, only the exception types that are caught.

## 🔎 Detection

`On Error Resume Next`, `On Error GoTo Label`, `On Error GoTo 0` and `On Error GoTo -1`.

## ❌ Non-compliant

```vb
Public Sub SaveSettings()
    On Error Resume Next
    File.WriteAllText(Me.settingsPath, Me.Serialize(), Encoding.UTF8)
    Me.isDirty = False ' Runs even if the file was not written.
End Sub
```

## ✔️ Compliant

```vb
Public Sub SaveSettings()
    Try
        File.WriteAllText(Me.settingsPath, Me.Serialize(), Encoding.UTF8)
        Me.isDirty = False
    Catch ex As IOException
        MessageBox.Show($"The settings could not be saved: {ex.Message}")
    End Try
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA033.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA033 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA033
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA033:'On Error' statement", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA033:'On Error' statement", Justification:="Reason.", Scope:="module")>
```
