# 🔑 DCA079 — `Keys.Contains` or `Values.Contains` on a dictionary

**.NET Framework only.** Reports `Contains` called on the `Keys` or `Values` of a dictionary that has `ContainsKey` or `ContainsValue`, such as `settings.Keys.Contains(name)`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Uses `ContainsKey` or `ContainsValue`
- **Equivalent to:** Microsoft [CA1841](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1841) *"Prefer Dictionary Contains methods"*

---

## 💡 Why

`map.Keys.Contains(key)` goes through the key collection: the `Enumerable.Contains` extension, which checks the type at run time before calling the dictionary, or an `ICollection(Of T).Contains` that some dictionaries implement as a **linear search** (the `Keys` of a `ConcurrentDictionary` is a snapshot list). `ContainsKey` looks the key up directly.

On modern .NET, the built-in rule CA1841 already reports this code, so this rule stays silent there.

## 🔎 Detection

`Contains` with one argument (the extension method, or an instance method of the collection) on the `Keys` or `Values` property of an object, when `dictionary.ContainsKey(item)` or `dictionary.ContainsValue(item)` binds at that position to a `Boolean` method with one parameter. Calls with a comparer are not reported, because `ContainsKey` uses the comparer of the dictionary; `Values.Contains` on `IDictionary(Of TKey, TValue)`, which has no `ContainsValue`, is not reported either.

## ❌ Non-compliant

```vb
If settings.Keys.Contains(name) Then
If settings.Values.Contains(value) Then
```

## ✔️ Compliant

```vb
If settings.ContainsKey(name) Then
If settings.ContainsValue(value) Then
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `map.Keys.Contains(key)` | `map.ContainsKey(key)` |
| `map.Values.Contains(value)` | `map.ContainsValue(value)` |

Fix All fixes both forms at once.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA079.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA079 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA079
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA079:Keys.Contains or Values.Contains on a dictionary", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA079:Keys.Contains or Values.Contains on a dictionary", Justification:="Reason.", Scope:="module")>
```
