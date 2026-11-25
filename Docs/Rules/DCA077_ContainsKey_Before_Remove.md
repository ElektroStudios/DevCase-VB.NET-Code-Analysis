# 🗝️ DCA077 — `Dictionary.Remove` guarded by `ContainsKey`

**.NET Framework only.** Reports `If map.ContainsKey(key) Then map.Remove(key)` on a dictionary.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Calls `Remove` directly
- **Equivalent to:** Microsoft [CA1853](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1853) *"Unnecessary call to 'Dictionary.ContainsKey(key)'"*

---

## 💡 Why

`Remove` already looks the key up, and returns `False` when the key does not exist: it never throws for a missing key. The `ContainsKey` check doubles the lookup (hashing the key and searching its bucket twice).

On modern .NET, the built-in rule CA1853 already reports this code, so this rule stays silent there.

## 🔎 Detection

An `If` statement without `ElseIf` or `Else` parts whose condition is `map.ContainsKey(key)` and whose **first** statement is `map.Remove(key)` (or `Call map.Remove(key)`), when:

- the receiver is the same simple reference (`map`, `Me.map`) and the key is the same expression without side effects (an invocation such as `key.Trim()` is not reported, because the fix evaluates it once instead of twice);
- the type implements `IDictionary(Of TKey, TValue)`, and `Remove` returns `Boolean` and takes the key type of `ContainsKey`;
- the statement has no preprocessor directives inside (another configuration may compile other statements).

## ❌ Non-compliant

```vb
If map.ContainsKey(key) Then
    map.Remove(key)
End If
```

## ✔️ Compliant

```vb
map.Remove(key)
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `If map.ContainsKey(key) Then map.Remove(key)` | `map.Remove(key)` |
| `If map.ContainsKey(key) Then` + `map.Remove(key)` + other statements | `If map.Remove(key) Then` + the other statements |

The comments at the end of the lines of the removed statements are kept after the call, and the comments on lines of their own are kept before it (or in the block, when it keeps other statements). The fix is not offered when a comment that follows code in the middle of the statement (after a `_` line continuation) would be lost, nor for a single-line `If` with several statements.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA077.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA077 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA077
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA077:Dictionary.Remove guarded by ContainsKey", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA077:Dictionary.Remove guarded by ContainsKey", Justification:="Reason.", Scope:="module")>
```
