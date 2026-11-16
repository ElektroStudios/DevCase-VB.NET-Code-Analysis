# 👯 DCA040 — Duplicated condition in `If`/`ElseIf` or `Select Case`

Reports a condition of an `If`/`ElseIf` chain that repeats an earlier condition of the same chain, and a `Case` value that repeats an earlier value of the same `Select Case` statement.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the intended condition is unknown)
- **Equivalent to:** SonarQube [S1862](https://rules.sonarsource.com/vbnet/RSPEC-1862/) *"Related "If/ElseIf" statements should not have the same condition"*

---

## 💡 Why

Only the **first** matching branch of an `If`/`ElseIf` chain or of a `Select Case` statement runs. A later branch with the same condition is **dead code**: it never runs. It is almost always a copy-paste mistake, and the condition that was meant is missing.

## 🔎 Detection

- `If`/`ElseIf` conditions are compared syntactically (spaces, line breaks and comments are ignored).
- `Case` values are compared by their constant value (`Case 1`, `Case "a"`, `Case Color.Red`), also between different `Case` clauses (`Case 1, 2` ... `Case 3, 1`).

The message gives the line of the earlier condition.

Not reported:

| Case | Reason |
|---|---|
| Conditions with invocations, object creations, `Await` or lambdas (`reader.Peek() > 0`) | Evaluating them again may produce another value |
| Ranges (`Case 1 To 5`) and relational clauses (`Case Is > 5`) | Overlaps are not always mistakes |

## ❌ Non-compliant

```vb
If status = Status.Ready Then
    Me.Start()
ElseIf status = Status.Paused Then
    Me.Resume()
ElseIf status = Status.Ready Then ' Never runs: Status.Stopped was meant?
    Me.Reset()
End If
```

## ✔️ Compliant

```vb
If status = Status.Ready Then
    Me.Start()
ElseIf status = Status.Paused Then
    Me.Resume()
ElseIf status = Status.Stopped Then
    Me.Reset()
End If
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA040.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA040 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA040
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA040:Duplicated condition in If/ElseIf or Select Case", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA040:Duplicated condition in If/ElseIf or Select Case", Justification:="Reason.", Scope:="module")>
```
