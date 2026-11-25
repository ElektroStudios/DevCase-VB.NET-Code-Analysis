# 📦 DCA066 — Value type passed to `ReferenceEquals`

**.NET Framework only.** Reports each value-type argument of `Object.ReferenceEquals`, such as both arguments of `ReferenceEquals(first, second)` with two `Integer` values.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (report only)
- **Equivalent to:** Microsoft [CA2013](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2013) *"Do not use ReferenceEquals with value types"*

---

## 💡 Why

`ReferenceEquals` compares references. A value type has no reference: each argument is **boxed into a new object** for the call, so two boxes are never the same object and the result is always `False` (even for the same variable), or `True` for two `Nothing` nullable values. The comparison never means what the code intends.

On modern .NET, the built-in rule CA2013 already reports this code, so this rule stays silent there.

## 🔎 Detection

Each argument whose value is converted from a value type to `Object` (structures, enumerations, `Integer?` and other nullable values, type parameters with the `Structure` constraint) is reported on its own: a call with two `Integer` values has two diagnostics. Reference types and `Nothing` are not reported.

## ❌ Non-compliant

```vb
If Object.ReferenceEquals(first, second) Then
```

## ✔️ Compliant

```vb
If first = second Then
```

## 🔧 Code fix

There is no code fix: the intended comparison (`=`, `Equals`, `IsNothing`...) depends on the type and on what the code means.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA066.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA066 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA066
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA066:Value type passed to ReferenceEquals", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA066:Value type passed to ReferenceEquals", Justification:="Reason.", Scope:="module")>
```
