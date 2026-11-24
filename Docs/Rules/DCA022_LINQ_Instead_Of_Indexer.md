# 🎯 DCA022 — LINQ method used instead of the indexer

Reports the LINQ `First()`, `Last()` and `ElementAt(index)` calls on types that implement `IList(Of T)` (`List(Of T)`, arrays, `Collection(Of T)`, `IList(Of T)`...), which can be indexed directly.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ `items(0)`, `items(items.Count - 1)`, `items(index)`
- **Equivalent to:** SonarQube S6608 *"Prefer indexing instead of Enumerable methods on types implementing IList"*

---

## 💡 Why

`Enumerable.First()` and `Enumerable.Last()` receive an `IEnumerable(Of T)`. Before accessing the element they check the real type of the collection at run time and, if it is an `IList(Of T)`, convert it and call the indexer through the interface. When the static type is already a list or an array, that check is unnecessary work. Indexing directly is shorter and faster, and it shows that you know the collection is indexable.

### Why `IList(Of T)` and not `IEnumerable(Of T)`?

- **`IEnumerable(Of T)` has no indexer**: it can only be iterated from the start, so there is nothing to replace `First()` with. On a generic sequence, `First()` is the right method.
- **`IReadOnlyList(Of T)` without `IList(Of T)`** is covered by Microsoft's **CA1826**. On those types, `Last()` iterates the **whole** sequence, because LINQ only optimizes `IList(Of T)`. DCA022 covers the complementary case, so the two rules do not overlap.

## 🔎 Detection

| Call | Fix |
|---|---|
| `items.First()` | `items(0)` |
| `items.Last()` | `items(items.Count - 1)` (`Length` for arrays) |
| `items.ElementAt(i)` | `items(i)` |

Not reported:

- Calls with a predicate (`First(predicate)`), which the indexer cannot express.
- The `FirstOrDefault()`/`LastOrDefault()` variants, which return a default value instead of failing.
- `IEnumerable(Of T)`, and types without a public indexer.

`Last()` on a receiver that is not a simple name (for example `GetItems().Last()`) is reported without a fix: the fix would evaluate `GetItems()` twice.

## ❌ Non-compliant

```vb
Dim header As String = lines.First()
Dim lastFile As String = files.Last()
Dim third As Integer = values.ElementAt(2)
```

## ✔️ Compliant

```vb
Dim header As String = lines(0)
Dim lastFile As String = files(files.Length - 1)
Dim third As Integer = values(2)
```

## 🔧 Code fix

When the receiver is a method called without parentheses (`GetItems.First()`, valid in Visual Basic), the fix adds them (`GetItems()(0)`). Otherwise the index would be passed to the method as an argument.

> [!WARNING]
> With an **empty** collection, the exception changes: `First()`/`Last()` throw `InvalidOperationException`, while the indexer throws `ArgumentOutOfRangeException` (lists) or `IndexOutOfRangeException` (arrays). If your code catches `InvalidOperationException` to detect an empty collection, check `Count` instead before applying the fix.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA022.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA022 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA022
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA022:LINQ method used instead of the indexer", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA022:LINQ method used instead of the indexer", Justification:="Reason.", Scope:="module")>
```
