# 🧩 DCA058 — `StringBuilder` argument converted to a string

**.NET Framework only.** Reports arguments of `StringBuilder.Append` and `StringBuilder.Insert` that are converted to a string although a strongly typed overload exists, such as `sb.Append(count.ToString())` or `sb.Append(New String(c, 2))`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Passes the value to the strongly typed overload (`sb.Append(count)`, `sb.Append(c, 2)`)
- **Equivalent to:** Microsoft [CA1830](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1830) *"Prefer strongly-typed Append and Insert method overloads on StringBuilder"*

---

## 💡 Why

`StringBuilder.Append` and `StringBuilder.Insert` have an overload for every primitive type, and `Append(Char, Integer)` repeats a character. Converting the value first, with `ToString()` or `New String(c, count)`, allocates a temporary string that is copied into the builder and then discarded. The overload writes the value directly, with the same result: it formats numbers with the current culture, exactly as `ToString()` does.

On modern .NET, the built-in rule CA1830 already reports these arguments, so this rule stays silent there.

## 🔎 Detection

| Code | Strongly typed overload |
|---|---|
| `sb.Append(value.ToString())` | `Append(value)` |
| `sb.Insert(index, value.ToString())` | `Insert(index, value)` |
| `sb.Append(New String(c, count))` | `Append(c, count)` |

`value` must be of a primitive type with its own overload: `Boolean`, `Char`, `SByte`, `Byte`, `Short`, `UShort`, `Integer`, `UInteger`, `Long`, `ULong`, `Single`, `Double` or `Decimal`. `ToString` can be written with or without parentheses, and the arguments can be named (`sb.Append(value:=New String(c, count:=3))`).

Not reported:

| Case | Reason |
|---|---|
| `value.ToString("X")`, `value.ToString(CultureInfo.InvariantCulture)` | The format or the culture would be lost |
| `String`, `Date`, enumerations, `Integer?` and other types | There is no strongly typed overload (or only `Append(Object)`, which boxes the value) |
| `sb.AppendLine(value.ToString())` | `AppendLine` has only a `String` overload |
| `sb.Insert(index, New String(c, count))` | `Insert(Integer, String, Integer)` repeats a string, it does not take a character |

## ❌ Non-compliant

```vb
sb.Append(New String(" "c, indentation))
sb.Append(count.ToString())
sb.Insert(0, enabled.ToString())
```

## ✔️ Compliant

```vb
sb.Append(" "c, indentation)
sb.Append(count)
sb.Insert(0, enabled)
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `sb.Append(count.ToString())` | `sb.Append(count)` |
| `sb.Append((a + b).ToString())` | `sb.Append(a + b)` |
| `sb.Insert(0, flag.ToString())` | `sb.Insert(0, flag)` |
| `sb.Append(New String(c, 2))` | `sb.Append(c, 2)` |
| `sb.Append(value:=New String(c, count:=n))` | `sb.Append(value:=c, repeatCount:=n)` |
| `sb.Append(New String(count:=3, c:="-"c))` | `sb.Append("-"c, 3)` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA058.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA058 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA058
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA058:StringBuilder argument converted to a string", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA058:StringBuilder argument converted to a string", Justification:="Reason.", Scope:="module")>
```
