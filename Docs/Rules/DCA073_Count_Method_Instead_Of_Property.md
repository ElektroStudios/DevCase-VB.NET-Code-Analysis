# 📐 DCA073 — `Enumerable.Count()` instead of the `Length` or `Count` property

**.NET Framework only.** Reports `Enumerable.Count()` without a predicate on a sequence that exposes its size as a property, such as `values.Count()` on an array or a `String`, or `Enumerable.Count(list)` on a `List(Of T)`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `Length` or `Count`
- **Equivalent to:** Microsoft [CA1829](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1829) *"Use Length/Count property instead of Enumerable.Count method"*

---

## 💡 Why

The property returns a stored value. `Enumerable.Count()` first checks the type of the sequence at run time and calls the interface; on a `String`, which implements neither `ICollection(Of Char)` nor `ICollection`, it **enumerates every character**.

Visual Basic binds `list.Count()` to the `Count` property of a collection (a property can be called with parentheses), so the extension method is only reached on arrays and strings, which have `Length`, and through calls like `Enumerable.Count(list)`.

On modern .NET, the built-in rule CA1829 already reports this code, so this rule stays silent there.

## 🔎 Detection

A call to `Enumerable.Count` without a predicate (as an extension, also without parentheses, or through `Enumerable`) whose source is a one-dimensional array or a `String` (`Length`), or a type with an accessible `Integer` `Count` property. Calls with a predicate, and sequences without a size property (`IEnumerable(Of T)`), are not reported.

## ❌ Non-compliant

```vb
Dim total As Integer = values.Count()
Dim characters As Integer = text.Count()
Dim items As Integer = Enumerable.Count(list)
```

## ✔️ Compliant

```vb
Dim total As Integer = values.Length
Dim characters As Integer = text.Length
Dim items As Integer = list.Count
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `values.Count()` (array) | `values.Length` |
| `text.Count()` | `text.Length` |
| `Enumerable.Count(list)` | `list.Count` |
| `Me.GetValues().Count()` | `Me.GetValues().Length` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA073.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA073 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA073
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA073:Enumerable.Count() instead of the Length or Count property", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA073:Enumerable.Count() instead of the Length or Count property", Justification:="Reason.", Scope:="module")>
```
