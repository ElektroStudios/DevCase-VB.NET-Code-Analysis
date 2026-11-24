# 🔍 DCA021 — LINQ method used instead of `Find` / `Exists` / `TrueForAll`

Reports the LINQ `FirstOrDefault`, `Any` and `All` extensions with a predicate on `List(Of T)`, `ImmutableList(Of T)` and arrays, which have specific methods that are faster.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Replaces the call with the collection-specific method
- **Equivalent to:** SonarQube S6602 (`Find`), S6603 (`TrueForAll`) and S6605 (`Exists`)

---

## 💡 Why

| LINQ extension | `List(Of T)` / `ImmutableList(Of T)` | Array |
|---|---|---|
| `FirstOrDefault(predicate)` | `Find(predicate)` | `Array.Find(array, predicate)` |
| `Any(predicate)` | `Exists(predicate)` | `Array.Exists(array, predicate)` |
| `All(predicate)` | `TrueForAll(predicate)` | `Array.TrueForAll(array, predicate)` |

The LINQ versions work on any `IEnumerable(Of T)`: they create an enumerator and go through interface calls for each element. The specific methods walk the internal array directly. The result is **identical**: `Find` returns `Nothing`/the default value when nothing matches, the same as `FirstOrDefault`.

The difference is most visible on .NET Framework 4.8. On .NET 9+ the LINQ methods have improved and the gap is smaller.

## 🔎 Detection

Only extension-method calls whose predicate is a **lambda** or an **`AddressOf`** expression are reported. A variable of type `Func(Of T, Boolean)` is not reported, because Visual Basic cannot pass it where a `Predicate(Of T)` is expected.

Not reported: calls without a predicate (`items.Any()`), the `FirstOrDefault(predicate, defaultValue)` overload, explicit type arguments, and any type that is not a list or an array (`IEnumerable(Of T)`, `HashSet(Of T)`...).

## ❌ Non-compliant

```vb
Dim admin As User = users.FirstOrDefault(Function(u As User) u.IsAdmin)
Dim hasErrors As Boolean = results.Any(Function(r As Result) r.Failed)
Dim allValid As Boolean = files.All(AddressOf IsValidFile)
Dim hasNegatives As Boolean = numbers.Any(Function(n As Integer) n < 0) ' numbers As Integer()
```

## ✔️ Compliant

```vb
Dim admin As User = users.Find(Function(u As User) u.IsAdmin)
Dim hasErrors As Boolean = results.Exists(Function(r As Result) r.Failed)
Dim allValid As Boolean = files.TrueForAll(AddressOf IsValidFile)
Dim hasNegatives As Boolean = Array.Exists(numbers, Function(n As Integer) n < 0)
```

## 🔧 Code fix

On lists only the method name changes. On arrays the call becomes the shared `Array` method, with the array as its first argument.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA021.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA021 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA021
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA021:LINQ method used instead of the collection-specific method", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA021:LINQ method used instead of the collection-specific method", Justification:="Reason.", Scope:="module")>
```
