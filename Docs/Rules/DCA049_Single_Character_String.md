# 🔤 DCA049 — Single-character string where a `Char` can be used

Reports single-character string literals passed as the separator of `String.Join`, or to `StringBuilder.Append`, where a character literal (`"x"c`) can be used.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Replaces the string literal with the character literal
- **Equivalent to:** Microsoft [CA1834](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1834) *"Use StringBuilder.Append(char) for single character strings"* (only for `StringBuilder.Append`; this rule also covers `String.Join`, on every target framework)

---

## 💡 Why

A single character is a `Char`, not a `String`:

- On **modern .NET**, `String.Join(Char, ...)` and `StringBuilder.Append(Char)` are the overloads designed for it: they do not read a string separator character by character.
- On **.NET Framework**, `String.Join` has no `Char` overload. The character literal is converted to `String` by Visual Basic, and that conversion of a constant is done **at compile time**: the compiled program passes exactly the same constant string as before. The code compiles and behaves the same way on every target framework, without an `#If` block, and it already uses the `Char` overload when the project moves to modern .NET.

## 🔎 Detection

| Call | Reported argument |
|---|---|
| `String.Join(",", ...)` (every overload) | The separator |
| `sb.Append("x")` (`StringBuilder.Append(String)` with one argument) | The value |

Before reporting, the call is bound again with the character literal, to be certain that it compiles and calls a method with the same name on the same type.

Not reported:

| Case | Reason |
|---|---|
| Strings with zero or several characters (`""`, `", "`) | They are not a single character |
| Constants and variables (`String.Join(Separator, parts)`) | Only literals can be rewritten without changing their declaration |
| Named arguments (`String.Join(separator:=",", ...)`) | Kept as written |
| Control characters and surrogates | They cannot be written as a readable character literal |
| `sb.Append("x", 0, 1)` and other overloads | There is no equivalent `Char` overload |
| `s.Contains("x")`, `s.StartsWith("x", ...)` | On .NET Framework there is no `Char` overload with the same behavior; rule [DCA013](DCA013_NetFramework_API_Migration.md) handles them with an `#If NETCOREAPP` block |

## ❌ Non-compliant

```vb
Return String.Join("&", parts)

Dim sb As New StringBuilder()
sb.Append(",")
```

## ✔️ Compliant

```vb
Return String.Join("&"c, parts)

Dim sb As New StringBuilder()
sb.Append(","c)
```

## 🔧 Code fix

Replaces the string literal with the character literal. The quotation mark is written as `""""c`. No `#If` block is needed: the result compiles on .NET Framework and on modern .NET (verified by tests on both).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA049.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA049 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA049
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA049:Single-character string where a Char can be used", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA049:Single-character string where a Char can be used", Justification:="Reason.", Scope:="module")>
```
