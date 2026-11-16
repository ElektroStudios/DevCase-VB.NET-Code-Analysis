# 🪞 DCA041 — All branches of a conditional structure are identical

Reports conditional structures whose branches **all** have the same implementation, including the `Else` branch.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (either the condition is useless or a branch is wrong)
- **Equivalent to:** SonarQube [S3923](https://rules.sonarsource.com/vbnet/RSPEC-3923/) *"All branches in a conditional structure should not have exactly the same implementation"*

---

## 💡 Why

When every branch does the same thing, the condition does not change the behavior. Either one of the branches was meant to be different (a copy-paste mistake), or the whole conditional structure can be removed.

## 🔎 Detection

| Structure | Example |
|---|---|
| `If`/`ElseIf`/`Else` block | Every branch, including `Else`, has the same statements |
| Single-line `If ... Then ... Else` | `If x Then a = 1 Else a = 1` |
| `Select Case` with `Case Else` | Every `Case`, including `Case Else`, has the same statements |
| `If(condition, a, b)` | `If(isAdmin, limit, limit)` |

Statements are compared syntactically (spaces, line breaks and comments are ignored). Structures **without** an `Else` branch are not reported: doing nothing in the other cases is a different behavior. Empty branches are not reported.

## ❌ Non-compliant

```vb
If user.IsAdministrator Then
    maximum = Me.settings.MaximumItems
Else
    maximum = Me.settings.MaximumItems
End If
```

## ✔️ Compliant

```vb
If user.IsAdministrator Then
    maximum = Me.settings.MaximumAdminItems
Else
    maximum = Me.settings.MaximumItems
End If
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA041.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA041 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA041
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA041:All branches of a conditional structure are identical", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA041:All branches of a conditional structure are identical", Justification:="Reason.", Scope:="module")>
```
