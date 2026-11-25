# 🎯 DCA022 — LINQ method used instead of the indexer

Reports the LINQ `First()`, `Last()` and `ElementAt(index)` calls on indexable collections: arrays, `String`, and types that implement `IList(Of T)` or `IReadOnlyList(Of T)` (`List(Of T)`, `Collection(Of T)`, `ReadOnlyCollection(Of T)`, `ImmutableArray(Of T)`, `IReadOnlyList(Of T)`...), which can be indexed directly.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ `items(0)`, `items(items.Count - 1)`, `items(index)`
- **Equivalent to:** SonarQube S6608 *"Prefer indexing instead of Enumerable methods on types implementing IList"* and Microsoft [CA1826](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1826) *"Use property instead of Linq Enumerable method"* (whose message this rule uses)

---

## 💡 Why

`Enumerable.First()` and `Enumerable.Last()` receive an `IEnumerable(Of T)`. Before accessing the element they check the real type of the collection at run time and, if it is an `IList(Of T)`, convert it and call the indexer through the interface. When the static type is already a list or an array, that check is unnecessary work. Indexing directly is shorter and faster, and it shows that you know the collection is indexable.

It is worse for the types that LINQ does not optimize: on a `String` or on an `IReadOnlyList(Of T)` that is not an `IList(Of T)`, `Last()` and `ElementAt()` **enumerate the sequence** up to the element, although the indexer reaches it directly.

### Which types?

- **`IEnumerable(Of T)` has no indexer**: it can only be iterated from the start, so there is nothing to replace `First()` with. On a generic sequence, `First()` is the right method.
- **`IReadOnlyList(Of T)` without `IList(Of T)`** is also covered by Microsoft's **CA1826**. In .NET Framework projects, where CA1826 is not available, this rule reports it; in modern .NET projects, it leaves it to CA1826, so the two rules never report the same call.
- **`String`** is indexable through its `Chars` default property, and no Microsoft rule covers it: it is reported on every target framework.

## 🔎 Detection

| Call | Fix |
|---|---|
| `items.First()` | `items(0)` |
| `items.Last()` | `items(items.Count - 1)` (`Length` for arrays and strings) |
| `items.ElementAt(i)` | `items(i)` |

| Type | .NET Framework | Modern .NET |
|---|:---:|:---:|
| Arrays, `IList(Of T)` types | ✔️ | ✔️ |
| `IReadOnlyList(Of T)` types that are not `IList(Of T)` | ✔️ | ➖ (reported by CA1826) |
| `String` | ✔️ | ✔️ |

Not reported:

- Calls with a predicate (`First(predicate)`), which the indexer cannot express.
- The `FirstOrDefault()`/`LastOrDefault()` variants, which return a default value instead of failing.
- `IEnumerable(Of T)`, and types without a public `Integer` indexer (an `IReadOnlyList(Of T)` implemented explicitly).

`Last()` on a receiver that is not a simple name (for example `GetItems().Last()`) is reported without a fix: the fix would evaluate `GetItems()` twice.

## ❌ Non-compliant

```vb
Dim header As String = lines.First()
Dim lastFile As String = files.Last()
Dim third As Integer = values.ElementAt(2)
Dim lastChar As Char = text.Last()
```

## ✔️ Compliant

```vb
Dim header As String = lines(0)
Dim lastFile As String = files(files.Length - 1)
Dim third As Integer = values(2)
Dim lastChar As Char = text(text.Length - 1)
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
