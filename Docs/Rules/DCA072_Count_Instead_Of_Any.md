# 🔢 DCA072 — `Count()` compared with zero instead of `Any()`

**.NET Framework only.** Reports `Enumerable.Count()` or `LongCount()` compared with zero or one to check whether a sequence is empty, such as `If query.Count() > 0 Then` or `If items.Count(Function(i) i.IsValid) = 0 Then`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `Any()` or `Not Any()`
- **Equivalent to:** Microsoft [CA1827](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1827) *"Do not use Count/LongCount when Any can be used"*

---

## 💡 Why

To compare the total with zero, `Count` enumerates **every** element of the sequence (or tests the predicate on every element). `Any` stops at the first one. On a LINQ query, an iterator or a file reader, the difference is the whole sequence; with a predicate, it is every evaluation after the first match.

Is the gain real in .NET Framework? Yes, for the cases that this rule reports. `Count()` without a predicate is fast on collections (it reads `ICollection(Of T).Count`), and .NET Framework's `Any()` always creates an enumerator, so collections are the one place where `Count()` wins: those cases are **not reported here** (rule DCA073 suggests the `Count`/`Length` property, which beats both). What remains are lazy sequences and calls with a predicate, where `Any` avoids enumerating everything.

On modern .NET, the built-in rule CA1827 already reports this code, so this rule stays silent there.

## 🔎 Detection

A binary comparison of `Enumerable.Count` or `Enumerable.LongCount` (with or without a predicate, as an extension or through `Enumerable`) with a constant, in any order:

| Comparison | Meaning | Fix |
|---|---|---|
| `= 0`, `< 1`, `<= 0` | Empty | `Not query.Any()` |
| `<> 0`, `> 0`, `>= 1` | Not empty | `query.Any()` |

`0 < query.Count()` is read as `query.Count() > 0`. Other comparisons (`> 1`) are not emptiness checks. Calls without a predicate on arrays, strings and types with a `Count` property are left to rule DCA073.

## ❌ Non-compliant

```vb
If query.Count() > 0 Then
If items.Count(Function(i As Item) i.IsValid) = 0 Then
```

## ✔️ Compliant

```vb
If query.Any() Then
If Not items.Any(Function(i As Item) i.IsValid) Then
```

## 🔧 Code fix

The fix renames the method to `Any`, keeping the receiver, the predicate and the form of the call (`Enumerable.Count(query)` becomes `Enumerable.Any(query)`), adds the parentheses of a call written without them (`query.Count > 0` becomes `query.Any()`), and negates it for empty checks. `Not` gets parentheses when the comparison was part of an expression that binds tighter (`query.Count() = 0 = flag` becomes `(Not query.Any()) = flag`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA072.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA072 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA072
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA072:Count() compared with zero instead of Any()", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA072:Count() compared with zero instead of Any()", Justification:="Reason.", Scope:="module")>
```
