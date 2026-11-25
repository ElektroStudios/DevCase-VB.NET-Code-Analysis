# 🔍 DCA081 — `ContainsKey` followed by the indexer instead of `TryGetValue`

**.NET Framework only.** Reports `map.ContainsKey(key)` checks followed by reads of `map(key)` where the key is known to exist, which look the key up again: `If map.ContainsKey(key) Then ... map(key)`, `If(map.ContainsKey(key), map(key), 0)`, guard clauses and every combination in between.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Declares the value and uses `TryGetValue`
- **Equivalent to:** Microsoft [CA1854](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1854) *"Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method"*

---

## 💡 Why

`ContainsKey` looks the key up, and each read of `map(key)` looks it up again. `TryGetValue` looks it up **once** and returns the value. In a loop over many keys, it halves the work.

On modern .NET, the built-in rule CA1854 already reports this code, so this rule stays silent there.

## 🔎 Detection

A `map.ContainsKey(key)` call with at least one read of `map(key)` in a part of the code that only runs when the key exists. The check can be the condition of a multi-line or single-line `If`, of an `ElseIf`, or of a ternary `If(...)`, in any of these forms:

| Form | Where the reads are |
|---|---|
| `If map.ContainsKey(key) Then` / `If(map.ContainsKey(key), map(key), 0)` | The `Then` part, or the second argument |
| `If Not map.ContainsKey(key) Then ... Else` / `If(Not map.ContainsKey(key), 0, map(key))` | The `Else` part, or the third argument |
| `If map.ContainsKey(key) AndAlso map(key) > 0 Then` | The right side of `AndAlso`, and the `Then` part |
| `If Not map.ContainsKey(key) OrElse map(key) = 0 Then Return` | The right side of `OrElse` |
| `If Not map.ContainsKey(key) Then Return` (or `Throw`, `Exit`, `Continue`) | The statements that follow the guard clause in the same block |
| `ElseIf map.ContainsKey(key) Then` | The `ElseIf` part |

Redundant parentheses (`If (map.ContainsKey(key)) Then`, `Not (map.ContainsKey(key))`) and line continuations do not change the detection. The rule reports when:

- the dictionary is a variable, parameter, field or property (optionally qualified with `Me` or `MyClass`), and the key a constant or one of those;
- in that part of the code, the dictionary is only used to read the indexer with that key (no writes `map(key) = ...`, no other members such as `Remove`), and the key is not assigned or passed `ByRef`;
- no read is inside a lambda, which could run after the dictionary changes;
- the type has `TryGetValue(key, ByRef value) As Boolean`.

## ❌ Non-compliant

```vb
If ages.ContainsKey(name) Then
    Return ages(name)
End If
```

## ✔️ Compliant

```vb
Dim value As Integer = Nothing
If ages.TryGetValue(name, value) Then
    Return value
End If
```

## 🔧 Code fix

The fix declares the value before the statement that contains the check (with its type, as the file writes it), calls `TryGetValue` in place of `ContainsKey` (keeping `Not`, `AndAlso` and `OrElse`) and replaces every read with the variable:

```vb
Dim value As Integer = Nothing
If Me.map.TryGetValue(key, value) Then
    Console.WriteLine(value)
    Console.WriteLine(value + 1)
End If
```

The ternary form becomes:

```vb
Dim value As Integer = Nothing
Return If(ages.TryGetValue(id, value), value, 0)
```

The variable is named `value`, or `value1`, `value2`... when the method already uses the name (also `value` of a property setter), or after the other `ContainsKey` checks of the method, so that **Fix All** declares a different variable for each statement. The fix is not offered when the statement is the body of a single-line `If` or lambda (including a ternary inside a single-line lambda), where a declaration cannot be inserted.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA081.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA081 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA081
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA081:ContainsKey followed by the indexer instead of TryGetValue", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA081:ContainsKey followed by the indexer instead of TryGetValue", Justification:="Reason.", Scope:="module")>
```
