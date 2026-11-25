# 🚫 DCA061 — `ArgumentNullException` for a value-type parameter

**.NET Framework only.** Reports `New ArgumentNullException(NameOf(p))` (or `New ArgumentNullException("p")`) for a parameter `p` of a non-nullable value type, such as `If count = 0 Then Throw New ArgumentNullException(NameOf(count))`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (report only)
- **Equivalent to:** Microsoft [CA2264](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2264) *"Do not pass a non-nullable value to 'ArgumentNullException.ThrowIfNull'"* (similar: CA2264 analyzes `ThrowIfNull`, this rule the classic guard)

---

## 💡 Why

A value of a non-nullable value type (`Integer`, `Date`, a `Structure`, a type parameter with the `Structure` constraint) can never be `Nothing`: a guard that throws `ArgumentNullException` for it is either dead code, or (with `value = Nothing`, which compares with the default value) a check of zero or `#1/1/0001#` that throws the wrong exception. The caller gets a message about a null argument that it never passed.

This rule is the .NET Framework counterpart of CA2264, which reports non-nullable values passed to `ArgumentNullException.ThrowIfNull` (a method that does not exist in .NET Framework): here, the classic `Throw New ArgumentNullException` guard is analyzed. On modern .NET, the rule stays silent.

## 🔎 Detection

The `paramName` argument of an `ArgumentNullException` creation (positional or named) must name a parameter:

- `NameOf(p)`, or
- a string literal equal (case-sensitively) to the name of a parameter of the enclosing method, accessor or lambda.

The parameter is reported when its type is a value type that is not `Nullable(Of T)`. Reference types, `Integer?` and unconstrained type parameters are not reported.

## ❌ Non-compliant

```vb
If count = 0 Then
    Throw New ArgumentNullException(NameOf(count))
End If
```

## ✔️ Compliant

```vb
If count <= 0 Then
    Throw New ArgumentOutOfRangeException(NameOf(count), count, "The count must be positive.")
End If
```

## 🔧 Code fix

There is no code fix: whether the guard must be removed, or replaced with `ArgumentOutOfRangeException` or `ArgumentException`, depends on what the code means.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA061.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA061 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA061
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA061:ArgumentNullException for a value-type parameter", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA061:ArgumentNullException for a value-type parameter", Justification:="Reason.", Scope:="module")>
```
