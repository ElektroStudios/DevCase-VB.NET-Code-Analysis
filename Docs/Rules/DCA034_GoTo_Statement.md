# ↪️ DCA034 — `GoTo` statement

Reports `GoTo` statements.

- **Severity:** Info
- **Category:** Design
- **Code fix:** ❌ (the structured replacement depends on the flow)
- **Equivalent to:** SonarQube [S907](https://rules.sonarsource.com/vbnet/RSPEC-907/) *""GoTo" statements should not be used"*

---

## 💡 Why

Jumps to labels make the control flow hard to follow and to change: the reader has to search the label, and every jump is a hidden entry point into the middle of the code. Loops, `Exit`, `Continue` and `Return` express the same flows in a structured way.

## 🔎 Detection

`GoTo Label` statements. `On Error GoTo` is a different statement, reported by [DCA033](DCA033_On_Error_Statement.md).

## ❌ Non-compliant

```vb
Dim attempts As Integer = 0
Retry:
attempts += 1
If Not Me.TryConnect() AndAlso attempts < 3 Then GoTo Retry
```

## ✔️ Compliant

```vb
Dim attempts As Integer = 0
Do
    attempts += 1
Loop Until Me.TryConnect() OrElse attempts >= 3
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA034.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA034 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA034
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA034:'GoTo' statement", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA034:'GoTo' statement", Justification:="Reason.", Scope:="module")>
```
