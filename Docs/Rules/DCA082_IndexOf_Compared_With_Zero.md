# ⏮️ DCA082 — `IndexOf` compared with zero instead of `StartsWith`

**.NET Framework only.** Reports `String.IndexOf(value)` compared with zero, such as `If path.IndexOf(prefix, StringComparison.Ordinal) = 0 Then`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `StartsWith` or `Not StartsWith`
- **Equivalent to:** Microsoft [CA1858](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1858) *"Use StartsWith instead of IndexOf"*

---

## 💡 Why

When the value is not at the start, `IndexOf` keeps searching the **whole** string only to return a position that the comparison then discards. `StartsWith` compares the start and stops, and says what the code means.

On modern .NET, the built-in rule CA1858 already reports this code, so this rule stays silent there.

## 🔎 Detection

The `IndexOf(String)` and `IndexOf(String, StringComparison)` overloads compared with `= 0` or `<> 0`, in any order. Not reported:

| Case | Reason |
|---|---|
| `> 0`, `>= 0`... | `IndexOf` returns -1 when the value is not found, so they mean something else |
| `IndexOf(Char)` | `String.StartsWith(Char)` does not exist in .NET Framework |
| `IndexOf(value, startIndex)` | It does not search from the start |

## ❌ Non-compliant

```vb
If path.IndexOf(prefix, StringComparison.Ordinal) = 0 Then
```

## ✔️ Compliant

```vb
If path.StartsWith(prefix, StringComparison.Ordinal) Then
```

## 🔧 Code fix

The fix keeps the arguments, so the comparison is the same (with or without `StringComparison`):

| Code | Fixed code |
|---|---|
| `path.IndexOf(prefix) = 0` | `path.StartsWith(prefix)` |
| `path.IndexOf(prefix, StringComparison.Ordinal) <> 0` | `Not path.StartsWith(prefix, StringComparison.Ordinal)` |
| `0 = path.IndexOf(prefix, StringComparison.OrdinalIgnoreCase)` | `path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA082.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA082 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA082
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA082:IndexOf compared with zero instead of StartsWith", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA082:IndexOf compared with zero instead of StartsWith", Justification:="Reason.", Scope:="module")>
```
