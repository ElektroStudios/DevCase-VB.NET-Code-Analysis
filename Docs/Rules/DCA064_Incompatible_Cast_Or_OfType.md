# 💥 DCA064 — `Enumerable.Cast` or `OfType` with an incompatible type

**.NET Framework only.** Reports `Cast(Of T)` and `OfType(Of T)` calls on sequences whose elements can never be a `T` at run time, such as `numbers.Cast(Of String)()` on an `Integer()`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (report only)
- **Equivalent to:** Microsoft [CA2021](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2021) *"Do not call Enumerable.Cast<T> or Enumerable.OfType<T> with incompatible types"*

---

## 💡 Why

`Cast` and `OfType` do not convert values: they use a run-time type check (`DirectCast`/`TryCast`), without widening, narrowing or user-defined conversions. So `numbers.Cast(Of String)()` does not call `ToString`: **every element throws `InvalidCastException`**, and `names.OfType(Of Integer)()` is **always empty**. A boxed `Integer` cannot even be unboxed as a `Long`. The code compiles, so the mistake only appears when the sequence is enumerated.

On modern .NET, the built-in rule CA2021 already reports this code, so this rule stays silent there.

## 🔎 Detection

The element type is the element of an array, or the `T` of the only `IEnumerable(Of T)` that the source implements. A conversion is considered **possible** (not reported) when:

- Both types are the same, or one of them is `Object`.
- One class derives from the other.
- The target is an interface and the element is an interface or a class that is not `NotInheritable` (a derived class could implement it), or the element implements it; and the other way around.
- For value types: the target is `ValueType`, `[Enum]` (for an enumeration), an interface that the structure implements, or a type with the same underlying type (an enumeration and its underlying type, which unboxing accepts). `Nullable(Of T)` is compared through `T`.

Type parameters, arrays as elements, error types, non-generic sequences and generic variance are not analyzed (they could be compatible).

## ❌ Non-compliant

```vb
Dim texts As IEnumerable(Of String) = numbers.Cast(Of String)()   ' InvalidCastException.
Dim values As IEnumerable(Of Integer) = names.OfType(Of Integer)() ' Always empty.
Dim longs As IEnumerable(Of Long) = numbers.Cast(Of Long)()        ' Unboxing fails.
```

## ✔️ Compliant

```vb
Dim texts As IEnumerable(Of String) = numbers.Select(Function(n As Integer) CStr(n))
Dim longs As IEnumerable(Of Long) = numbers.Select(Function(n As Integer) CLng(n))
```

## 🔧 Code fix

There is no code fix: the conversion that the code needs (`Select` with `CStr`, `CLng`, `ToString`...) depends on what it means.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA064.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA064 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA064
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA064:Enumerable.Cast or OfType with an incompatible type", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA064:Enumerable.Cast or OfType with an incompatible type", Justification:="Reason.", Scope:="module")>
```
