# 🔄 DCA023 — Negated comparison

Reports negated comparisons such as `Not (a = b)` that can be written with the inverse operator (`a <> b`).

- **Severity:** Warning
- **Category:** Style
- **Code fix:** ✔️ Replaces `Not` and the operator with the inverse operator
- **Equivalent to:** SonarQube S1940 *"Boolean checks should not be inverted"*

---

## 💡 Why

`Not (count = 0)` makes the reader evaluate the comparison and then invert it in their head. `count <> 0` says the same thing directly.

| Negated | Inverse |
|---|---|
| `Not (a = b)` | `a <> b` |
| `Not (a <> b)` | `a = b` |
| `Not (a < b)` | `a >= b` |
| `Not (a > b)` | `a <= b` |
| `Not (a <= b)` | `a > b` |
| `Not (a >= b)` | `a < b` |

In Visual Basic, `Not` has lower precedence than comparisons, so `Not a = b` is also `Not (a = b)` and is reported too.

## 🔎 Detection

Not reported:

| Case | Reason |
|---|---|
| `Not (x < y)` with `Single` or `Double` | **`NaN`**: `Not (Double.NaN < 1)` is `True`, but `Double.NaN >= 1` is `False`. Only `=` and `<>` are safe with floating-point values. |
| Comparisons with user-defined operators | Nothing guarantees that the type's `<>` is the exact negation of its `=`. |
| `Not x Is Nothing`, `Not TypeOf x Is T` | Already covered by Visual Studio's built-in **IDE0084** rule (`IsNot`). |
| Late-bound `Object` comparisons | The result depends on the run-time types. |

`Nullable(Of T)` comparisons are reported: with `Nothing`, `Not (n > 3)` and `n <= 3` both give `Nothing`.

## ❌ Non-compliant

```vb
If Not (files.Count = 0) Then
If Not index < 0 Then
```

## ✔️ Compliant

```vb
If files.Count <> 0 Then
If index >= 0 Then
```

## 🔧 Code fix

The fix replaces the expression with the inverse comparison, keeping the operands and their comments unchanged.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA023.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA023 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA023
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA023:Negated comparison", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA023:Negated comparison", Justification:="Reason.", Scope:="module")>
```
