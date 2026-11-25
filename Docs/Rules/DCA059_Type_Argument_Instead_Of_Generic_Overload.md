# 🧬 DCA059 — `System.Type` argument instead of a generic overload

**.NET Framework only.** Reports calls that pass `GetType(T)` to a method that has a generic overload taking `T` as a type argument, such as `DirectCast(Activator.CreateInstance(GetType(T)), T)` or `Marshal.SizeOf(GetType(NativePoint))`.

- **Severity:** Info
- **Category:** Design
- **Code fix:** ✔️ Calls the generic overload, and removes a cast that it makes redundant
- **Equivalent to:** Microsoft [CA2263](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2263) *"Prefer generic overload when type is known"*

---

## 💡 Why

A `System.Type` argument is only checked at run time: the method returns `Object`, the caller casts it, and a wrong type fails when the code runs. The generic overload takes the type at compile time, returns the type itself (no cast, no boxing for structures) and lets the compiler check its constraints.

On modern .NET, the built-in rule CA2263 already reports this code, so this rule stays silent there.

## 🔎 Detection

A call is reported when:

- The invoked method is not generic, is not an extension method, and has exactly one `System.Type` argument written as `GetType(X)` (an open generic type such as `GetType(List(Of))` is not reported).
- The same type declares an accessible generic overload with **one** type parameter, the other parameters in the same order and with the same types, and the same `Shared` modifier: `Marshal.PtrToStructure(pointer, GetType(T))` has `Marshal.PtrToStructure(Of T)(pointer)`.
- `X` satisfies the constraints of the type parameter (`Structure`, `Class`, `New` and constraint types). Speculative binding does not check constraints, so the rule checks them itself.
- The call with the generic overload binds to that overload at that position.

Examples in .NET Framework: `Activator.CreateInstance(Of T)()`, `Marshal.SizeOf(Of T)()`, `Marshal.PtrToStructure(Of T)(IntPtr)`, `Marshal.OffsetOf(Of T)(String)`, `Marshal.DestroyStructure(Of T)(IntPtr)`, `Marshal.GetDelegateForFunctionPointer(Of TDelegate)(IntPtr)`, and the overloads of your own types.

> [!NOTE]
> In .NET Framework, `Activator.CreateInstance(Of T)()` has **no** `New` constraint: like `Activator.CreateInstance(GetType(T))`, it throws `MissingMethodException` at run time when the type has no public parameterless constructor. So it is reported for any type, as CA2263 does.

Not reported: calls with other arguments that the generic overload does not take (`Activator.CreateInstance(GetType(T), True)`), `Type` values that are not `GetType(...)` (`Activator.CreateInstance(type)`), and methods without a generic overload (`[Enum].GetValues(GetType(DayOfWeek))` in .NET Framework).

## ❌ Non-compliant

```vb
Return DirectCast(Activator.CreateInstance(GetType(T)), T)
Dim size As Integer = Marshal.SizeOf(GetType(NativePoint))
Dim point As NativePoint = CType(Marshal.PtrToStructure(pointer, GetType(NativePoint)), NativePoint)
```

## ✔️ Compliant

```vb
Return Activator.CreateInstance(Of T)()
Dim size As Integer = Marshal.SizeOf(Of NativePoint)()
Dim point As NativePoint = Marshal.PtrToStructure(Of NativePoint)(pointer)
```

## 🔧 Code fix

The fix writes the generic call with the type of `GetType` as type argument and the other arguments (named arguments are kept). When the call is the operand of a `DirectCast`, `CType` or `TryCast` to the same type (also between parentheses), the cast is removed too, because the generic overload already returns that type:

| Code | Fixed code |
|---|---|
| `DirectCast(Activator.CreateInstance(GetType(T)), T)` | `Activator.CreateInstance(Of T)()` |
| `Marshal.SizeOf(GetType(NativePoint))` | `Marshal.SizeOf(Of NativePoint)()` |
| `CType(Marshal.PtrToStructure(pointer, GetType(NativePoint)), NativePoint)` | `Marshal.PtrToStructure(Of NativePoint)(pointer)` |
| `DirectCast(Activator.CreateInstance(GetType(Resource)), IDisposable)` | `DirectCast(Activator.CreateInstance(Of Resource)(), IDisposable)` (another type: the cast stays) |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA059.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA059 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA059
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA059:System.Type argument instead of a generic overload", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA059:System.Type argument instead of a generic overload", Justification:="Reason.", Scope:="module")>
```
