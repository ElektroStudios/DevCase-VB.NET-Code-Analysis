# 📭 DCA074 — `Count` compared with zero instead of `IsEmpty`

**.NET Framework only.** Reports the `Count` property compared with zero or one on a type that also has an `IsEmpty` property, such as `queue.Count > 0` on a `ConcurrentQueue(Of T)`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `IsEmpty` or `Not IsEmpty`
- **Equivalent to:** Microsoft [CA1836](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1836) *"Prefer IsEmpty over Count when available"*

---

## 💡 Why

Some collections compute `Count` instead of storing it. In the concurrent collections, `Count` takes a consistent snapshot of the whole collection (`ConcurrentDictionary` acquires **every** lock, `ConcurrentQueue` walks its segments), while `IsEmpty` only checks whether there is an element. In a loop that polls a queue, the difference adds up.

On modern .NET, the built-in rule CA1836 already reports this code, so this rule stays silent there.

## 🔎 Detection

A comparison of a `Count` property with a constant, in any order (`= 0`, `< 1`, `<= 0` mean empty; `<> 0`, `> 0`, `>= 1` mean not empty), when the type of the instance has an accessible, non-indexed `Boolean` `IsEmpty` instance property: `ConcurrentQueue(Of T)`, `ConcurrentStack(Of T)`, `ConcurrentBag(Of T)`, `ConcurrentDictionary(Of TKey, TValue)`, the immutable collections, and your own types.

## ❌ Non-compliant

```vb
If queue.Count > 0 Then
If cache.Count = 0 Then
```

## ✔️ Compliant

```vb
If Not queue.IsEmpty Then
If cache.IsEmpty Then
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `queue.Count = 0` | `queue.IsEmpty` |
| `map.Count > 0` | `Not map.IsEmpty` |
| `0 <> queue.Count` | `Not queue.IsEmpty` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA074.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA074 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA074
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA074:Count compared with zero instead of IsEmpty", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA074:Count compared with zero instead of IsEmpty", Justification:="Reason.", Scope:="module")>
```
