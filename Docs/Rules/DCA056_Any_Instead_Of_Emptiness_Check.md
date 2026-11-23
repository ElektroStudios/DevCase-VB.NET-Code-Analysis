# 🔢 DCA056 — `Enumerable.Any()` instead of `IsEmpty`, `Count` or `Length`

**.NET Framework only.** Reports calls to `Enumerable.Any()` without a predicate on a collection that exposes an `IsEmpty`, `Length` or `Count` property, such as `While queue.Any()`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Replaces the call with the property (`queue.Count <> 0`, `Not bag.IsEmpty`...)
- **Equivalent to:** Microsoft [CA1860](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1860) *"Avoid using 'Enumerable.Any()' extension method"*

---

## 💡 Why

`Enumerable.Any()` is written for any sequence: to know whether a collection has elements, it checks the type of the collection at run time and, when it does not recognize it, creates an enumerator and moves it once. A collection that already knows how many elements it has answers the same question directly through a property, without type checks, allocations or interface calls, and the code says what it means: *"the queue is not empty"* instead of a LINQ query.

On modern .NET, the built-in rule CA1860 already reports these calls, so this rule stays silent there.

## 🔎 Detection

A call to `System.Linq.Enumerable.Any(source)` **without a predicate**, in either form (`items.Any()`, `items.Any` or `Enumerable.Any(items)`), when the static type of the collection has an accessible, readable, parameterless instance property:

| Property, in order of preference | Type | Examples |
|---|---|---|
| `IsEmpty` | `Boolean` | `ConcurrentQueue(Of T)`, `ConcurrentBag(Of T)`, `ConcurrentDictionary(Of TKey, TValue)` (whose `Count` locks the whole collection) |
| `Length` | `Integer` | Arrays, `String` |
| `Count` | `Integer` | `List(Of T)`, `Queue(Of T)`, `Stack(Of T)`, `HashSet(Of T)`, `Dictionary(Of TKey, TValue)` and its `Keys` and `Values`, `ICollection(Of T)`, `IList(Of T)`, `IReadOnlyCollection(Of T)`... |

Not reported:

| Case | Reason |
|---|---|
| `IEnumerable(Of T)` and other types without these properties | `Any()` is the right way to check a sequence |
| `items.Any(Function(x) x > 0)` | The predicate is part of the question (see [DCA021](DCA021_LINQ_Instead_Of_Collection_Method.md) for `List(Of T).Exists`) |
| `items?.Any()` | It returns `Boolean?`, and `Nothing` when `items` is `Nothing` |
| Interfaces that inherit `Count` from two interfaces (`ICollection(Of T)` and `IReadOnlyCollection(Of T)`) | `items.Count` would be ambiguous and would not compile |
| `ImmutableArray(Of T).Any()` | It calls `ImmutableArrayExtensions.Any`, not `Enumerable.Any` |

## ❌ Non-compliant

```vb
While queue.Any()
    Process(queue.Dequeue())
End While

If Not list.Any() Then
    Return
End If
```

## ✔️ Compliant

```vb
While queue.Count <> 0
    Process(queue.Dequeue())
End While

If list.Count = 0 Then
    Return
End If
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `queue.Any()` | `queue.Count <> 0` |
| `Not queue.Any()` (also `Not (queue.Any())`) | `queue.Count = 0` |
| `numbers.Any()` (array or `String`) | `numbers.Length <> 0` |
| `bag.Any()` | `Not bag.IsEmpty` |
| `Not bag.Any()` | `bag.IsEmpty` |
| `Enumerable.Any(list)` | `list.Count <> 0` |

Parentheses are added where the context binds more tightly than the comparison or the `Not` operator: `list.Any().ToString()` → `(list.Count <> 0).ToString()`, `list.Any() = flag` → `(list.Count <> 0) = flag`. As operands of `AndAlso`, `OrElse`, `And`, `Or` and `Xor`, in conditions, arguments and assignments, no parentheses are needed.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA056.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA056 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA056
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA056:Enumerable.Any() instead of IsEmpty, Count or Length", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA056:Enumerable.Any() instead of IsEmpty, Count or Length", Justification:="Reason.", Scope:="module")>
```
