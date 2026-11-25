# 🧺 DCA078 — Set `Add` or `Remove` guarded by `Contains`

**.NET Framework only.** Reports `If Not items.Contains(item) Then items.Add(item)` and `If items.Contains(item) Then items.Remove(item)` on a set (`HashSet(Of T)`, `SortedSet(Of T)`...).

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Calls `Add` or `Remove` directly
- **Equivalent to:** Microsoft [CA1868](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1868) *"Unnecessary call to 'Contains' for sets"*

---

## 💡 Why

`Add` returns `False` when the item already exists, and `Remove` returns `False` when it does not: neither throws. The `Contains` check doubles the lookup.

On modern .NET, the built-in rule CA1868 already reports this code, so this rule stays silent there.

## 🔎 Detection

An `If` statement without `ElseIf` or `Else` parts whose condition is `Not items.Contains(item)` with `items.Add(item)` as **first** statement, or `items.Contains(item)` with `items.Remove(item)`, when:

- the receiver is the same simple reference and the item is the same expression without side effects;
- the type implements `ISet(Of T)`, and the method returns `Boolean` and takes the item type of `Contains`;
- the statement has no preprocessor directives inside.

A `List(Of T)` (whose `Add` adds duplicates) and mismatched pairs (`Contains` with `Add`) are not reported.

## ❌ Non-compliant

```vb
If Not items.Contains(item) Then
    items.Add(item)
End If
```

## ✔️ Compliant

```vb
items.Add(item)
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `If Not items.Contains(item) Then items.Add(item)` | `items.Add(item)` |
| `If items.Contains(item) Then items.Remove(item)` | `items.Remove(item)` |
| `If Not items.Contains(item) Then` + `items.Add(item)` + other statements | `If items.Add(item) Then` + the other statements |

The comments at the end of the lines of the removed statements are kept after the call, and the comments on lines of their own are kept before it (or in the block, when it keeps other statements). The fix is not offered when a comment that follows code in the middle of the statement (after a `_` line continuation) would be lost, nor for a single-line `If` with several statements.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA078.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA078 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA078
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA078:Set Add or Remove guarded by Contains", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA078:Set Add or Remove guarded by Contains", Justification:="Reason.", Scope:="module")>
```
