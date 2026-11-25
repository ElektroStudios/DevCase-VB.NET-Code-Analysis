# ✂️ DCA083 — Redundant length argument of `Substring`

**.NET Framework only.** Reports `text.Substring(start, text.Length - start)`, whose length argument reads to the end of the string.

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Removes the length argument
- **Equivalent to:** Microsoft [CA1514](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1514) *"Avoid redundant length argument"*

---

## 💡 Why

`Substring(start)` already reads to the end. The length argument repeats the receiver and the start index, so the code is longer, evaluates them twice, and can go out of sync when one of them changes.

On modern .NET, the built-in rule CA1514 already reports this code, so this rule stays silent there.

## 🔎 Detection

A call to `String.Substring(start, length)` with positional arguments, where `length` is `receiver.Length - start` with the same receiver and the same start index. Both must be free of side effects (an invocation such as `GetText()` is not reported, because the fix evaluates it once instead of twice). Other lengths (`text.Length - start - 1`, the length of another string) and named arguments are not reported.

## ❌ Non-compliant

```vb
Dim tail As String = text.Substring(start, text.Length - start)
```

## ✔️ Compliant

```vb
Dim tail As String = text.Substring(start)
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `text.Substring(start, text.Length - start)` | `text.Substring(start)` |
| `Me.Name.Substring(2, Me.Name.Length - 2)` | `Me.Name.Substring(2)` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA083.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA083 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA083
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA083:Redundant length argument of Substring", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA083:Redundant length argument of Substring", Justification:="Reason.", Scope:="module")>
```
