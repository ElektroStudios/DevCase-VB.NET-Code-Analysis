# 🧬 DCA042 — Two branches of a conditional structure are identical

Reports a branch of an `If`/`ElseIf`/`Else` chain or of a `Select Case` statement whose implementation is the same as the implementation of an earlier branch.

- **Severity:** Info
- **Category:** Design
- **Code fix:** ❌ (merging conditions must be reviewed: one of the branches may be a mistake)
- **Equivalent to:** SonarQube [S1871](https://rules.sonarsource.com/vbnet/RSPEC-1871/) *"Two branches in a conditional structure should not have exactly the same implementation"*

---

## 💡 Why

Two branches with the same code should be **merged**: `OrElse` between the conditions of an `If` chain, or a comma-separated `Case` clause in `Select Case`. Duplicated code must be changed twice, and sooner or later it is changed only once. And sometimes the duplication is itself a copy-paste mistake.

## 🔎 Detection

The message gives the line of the earlier branch. Statements are compared syntactically (spaces, line breaks and comments are ignored).

| Case | Reported |
|---|---|
| `Select Case` branches with the same statements | ✔️ Always: the `Case` clauses can simply be merged |
| `If` chain branches with **two or more** identical statements | ✔️ |
| `If` chain branches with a **single** statement (e.g. `Return False`) | ❌ Common and readable |
| Empty branches | ❌ |
| Every branch identical (with `Else`) | ❌ Reported by [DCA041](DCA041_All_Branches_Identical.md) instead |

## ❌ Non-compliant

```vb
Select Case extension
    Case ".jpg"
        Return ImageFormat.Jpeg
    Case ".png"
        Return ImageFormat.Png
    Case ".jpeg"
        Return ImageFormat.Jpeg
End Select
```

## ✔️ Compliant

```vb
Select Case extension
    Case ".jpg", ".jpeg"
        Return ImageFormat.Jpeg
    Case ".png"
        Return ImageFormat.Png
End Select
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA042.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA042 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA042
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA042:Two branches of a conditional structure are identical", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA042:Two branches of a conditional structure are identical", Justification:="Reason.", Scope:="module")>
```
