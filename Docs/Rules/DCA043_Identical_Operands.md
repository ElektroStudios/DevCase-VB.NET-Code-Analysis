# 🟰 DCA043 — Identical expressions on both sides of a binary operator

Reports binary operators whose two operands are the same expression.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the intended operand is unknown)
- **Equivalent to:** SonarQube [S1764](https://rules.sonarsource.com/vbnet/RSPEC-1764/) *"Identical expressions should not be used on both sides of a binary operator"*

---

## 💡 Why

Comparing an expression with itself (`a = a`), or combining it with itself (`a AndAlso a`, `a - a`, `a Mod a`), always produces the same result. It is almost always a **copy-paste mistake**: `width = width` instead of `width = height`.

## 🔎 Detection

Operators: `=`, `<>`, `<`, `<=`, `>`, `>=`, `Is`, `IsNot`, `And`, `Or`, `Xor`, `AndAlso`, `OrElse`, `-`, `/`, `\` and `Mod`. Operands are compared syntactically.

Not reported:

| Case | Reason |
|---|---|
| `+`, `*`, `&`, `<<`, `>>` | `a + a` and `a * a` are meaningful |
| `x <> x` / `x = x` on `Double` or `Single` | The classic test for `NaN` |
| Operands with invocations, object creations, `Await` or lambdas (`random.Next() = random.Next()`) | Each evaluation may produce another value |
| Two literals (`While 1 = 1`) | Written on purpose |

## ❌ Non-compliant

```vb
If image.Width = image.Width Then ' image.Height was meant.
    Return AspectRatio.Square
End If
```

## ✔️ Compliant

```vb
If image.Width = image.Height Then
    Return AspectRatio.Square
End If
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA043.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA043 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA043
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA043:Identical expressions on both sides of a binary operator", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA043:Identical expressions on both sides of a binary operator", Justification:="Reason.", Scope:="module")>
```
