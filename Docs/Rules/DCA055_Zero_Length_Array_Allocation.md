# 🈳 DCA055 — Zero-length array allocation

**.NET Framework only.** Reports the creation of arrays with no elements, such as `New String() {}`, which can be replaced with the cached instance returned by `Array.Empty(Of T)()`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Replaces the array with `Array.Empty(Of T)()`
- **Equivalent to:** Microsoft [CA1825](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1825) *"Avoid zero-length array allocations"*

---

## 💡 Why

An empty array can never change: it has no element to modify and its length is fixed. Yet every evaluation of `New String() {}` allocates a new one, which the garbage collector must collect later. `Array.Empty(Of T)()` (available since .NET Framework 4.6) returns a single, cached instance per element type, shared by the whole application, at no cost.

On modern .NET, the built-in rule CA1825 already reports these arrays, so this rule stays silent there. It also stays silent in projects that target .NET Framework 4.5.2 or earlier, which do not have `Array.Empty`.

## 🔎 Detection

| Code | Reported |
|---|---|
| `New String() {}` | ✔️ |
| `New String(-1) {}` (any constant upper bound of `-1`, such as `New String(0 - 1) {}`) | ✔️ |
| The empty array literal `{}` converted to an array type (`Dim values As Long() = {}`) | ✔️ |
| Jagged arrays (`New String()() {}` → `Array.Empty(Of String())()`) and arrays of a type parameter (`New T() {}`) | ✔️ |
| Arrays with elements, or whose size is not a constant zero (`New String(count - 1) {}`) | ➖ |
| Multidimensional arrays (`New Integer(,) {}`) | ➖ (`Array.Empty` returns one-dimensional arrays) |
| Attribute arguments (`<Tags(New String() {})>`) | ➖ (attribute arguments must be constant expressions) |
| Arrays created by the compiler: `ParamArray` arguments (`String.Format("x")`) and `Dim values(-1) As String` | ➖ |
| Arrays of anonymous types | ➖ (the type cannot be written) |

> [!NOTE]
> Rule [DCA030](DCA030_Array_Creation_Instead_Of_Literal.md) does not suggest the array literal `{}` for an empty `New Integer() {}`: an empty array should not be allocated at all.

## ❌ Non-compliant

```vb
Dim parameters As Object() = New Object() {New String() {}}
Dim numbers As Integer() = New Integer(-1) {}
Dim values As Long() = {}
```

## ✔️ Compliant

```vb
Dim parameters As Object() = New Object() {Array.Empty(Of String)()}
Dim numbers As Integer() = Array.Empty(Of Integer)()
Dim values As Long() = Array.Empty(Of Long)()
```

## 🔧 Code fix

Replaces the array creation with `Array.Empty(Of T)()`, where `T` is the element type of the array, written as in the rest of the file (`String`, `Generic.List(Of Integer)`, `T`). When a type named `Array` is in scope, the call is written `System.Array.Empty(Of T)()`, so that it still refers to `System.Array`.

> [!NOTE]
> The only observable difference is the **identity** of the array: two calls to `Array.Empty(Of String)()` return the same object, so `a Is b` becomes `True`, and a `SyncLock` on it would lock a shared object. Code that compares empty arrays by reference, or locks on them, should keep `New T() {}` (and suppress the diagnostic).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA055.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA055 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA055
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA055:Zero-length array allocation", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA055:Zero-length array allocation", Justification:="Reason.", Scope:="module")>
```
