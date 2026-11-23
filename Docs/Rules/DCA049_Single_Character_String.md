# 🔤 DCA049 — Single-character string where a `Char` can be used

Reports single-character string literals passed to framework methods that have an equivalent `Char` overload, where a character literal (`"x"c`) can be used.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Replaces the string literals of the call with character literals
- **Equivalent to:** Microsoft [CA1834](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1834) *"Use StringBuilder.Append(char) for single character strings"* and [CA1847](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1847) *"Use string.Contains(char) instead of string.Contains(string) with single characters"* (only for `StringBuilder.Append` and `String.Contains` on modern .NET; this rule covers more methods, on every target framework)

---

## 💡 Why

A single character is a `Char`, not a `String`. The `Char` overloads of these methods do not have to read a separator or a value character by character, and they say what the code means.

On **.NET Framework**, `String.Join` has no `Char` overload. The character literal is converted to `String` by Visual Basic, and that conversion of a constant is done **at compile time**: the compiled program passes exactly the same constant string as before. So the code compiles and behaves the same way on every target framework, without an `#If` block, and it already uses the `Char` overload when the project moves to modern .NET.

## 🔎 Detection

Only methods whose `Char` overload produces **exactly the same result** are analyzed:

| Method | .NET Framework | Modern .NET |
|---|:---:|:---:|
| `String.Join(",", ...)` | ✔️ (same overload, constant string) | ✔️ |
| `s.Replace("a", "b")` (both values must be single characters) | ✔️ | ✔️ |
| `s.Split(",")`, `s.Split(",", options)` | ➖ (no `String` overload) | ✔️ |
| `s.Contains("x")`, `s.Contains("x", comparison)` | ➖ (no `Char` overload) | ➖ (reported by CA1847) |
| `sb.Append("x")` | ✔️ | ➖ (reported by CA1834) |
| `sb.Insert(index, "x")`, `sb.Replace("a", "b")` | ✔️ | ✔️ |
| `sb.AppendJoin(",", ...)` | ➖ (no method) | ✔️ |
| `writer.Write("x")`, `writer.WriteLine("x")` (`TextWriter`, `StreamWriter`, `StringWriter`...), `Console.Write("x")`, `Console.WriteLine("x")` | ✔️ | ✔️ |

On modern .NET, `StringBuilder.Append` and `String.Contains` are left to the built-in rules CA1834 and CA1847 (since version 1.0.6), so the same literal is never reported twice.

Before reporting, the call is bound again with the character literals, to be certain that it compiles and calls the `Char` overload of the same method. Named arguments are supported: `String.Join(separator:=",", value:=parts)` → `String.Join(separator:=","c, value:=parts)` (the `Char` overloads use the same parameter names).

Not reported:

| Case | Reason |
|---|---|
| Methods without a `Char` overload: `sb.AppendLine("x")`, `File.WriteAllText(path, "x")`, `String.Concat("x", s)`... | A character literal would be converted back to the same string: nothing changes |
| `s.IndexOf("x")`, `s.LastIndexOf("x")`, `s.StartsWith("x")`, `s.EndsWith("x")` | **Different behavior**: the `String` overloads compare with the current culture, the `Char` overloads ordinally (for `StartsWith`/`EndsWith` with `StringComparison.Ordinal` on .NET Framework, see [DCA013](DCA013_NetFramework_API_Migration.md)) |
| `s.Replace("a", "bc")` | Only one of the values is a single character: there is no `Replace(Char, String)` |
| Format strings: `Console.WriteLine("x", value)`, `sb.AppendFormat("x", value)` | There is no `Char` overload with arguments |
| Strings with zero or several characters (`""`, `", "`) | They are not a single character |
| Constants and variables (`String.Join(Separator, parts)`) | Only literals can be rewritten without changing their declaration |
| Control characters and surrogates | They cannot be written as a readable character literal |

## ❌ Non-compliant

```vb
Return String.Join("&", parts)

Dim path As String = text.Replace("\", "/")

Dim sb As New StringBuilder()
sb.Append(",")
writer.WriteLine(";")
```

## ✔️ Compliant

```vb
Return String.Join("&"c, parts)

Dim path As String = text.Replace("\"c, "/"c)

Dim sb As New StringBuilder()
sb.Append(","c)
writer.WriteLine(";"c)
```

## 🔧 Code fix

Replaces every single-character string literal of the call with the character literal (both values of `Replace` at once). The quotation mark is written as `""""c`. No `#If` block is needed: the result compiles on .NET Framework and on modern .NET (verified by tests on both).

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
