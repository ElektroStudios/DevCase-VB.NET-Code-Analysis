# 🛂 DCA050 — Parameter of an externally visible method is not validated

Reports parameters of public and protected methods and constructors that are used without being validated first.

- **Severity:** Info
- **Category:** Design
- **Code fix:** ✔️ Inserts a guard clause at the start of the body (several variants for `String` parameters)
- **Equivalent to:** Microsoft [CA1062](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1062) *"Validate arguments of public methods"* (this rule also validates `Enum` values and offers a fix)

---

## 💡 Why

A public method can be called by any code, with any argument. Without a guard clause, an invalid argument fails **later and far from the cause**: a `NullReferenceException` deep inside the method, or a silently wrong result for an `Enum` value that does not exist (`CType(99, RegistryScope)`). A guard clause fails **immediately**, with an exception that names the parameter, and documents the contract of the method.

## 🔎 Detection

Analyzed methods: methods and constructors with a body that are callable from other assemblies (`Public`, `Protected` or `Protected Friend`, in types that are also visible).

Analyzed parameters:

| Type | Reported when | Guard clause of the fix |
|---|---|---|
| `Enum` (without `<Flags>`) | It is used | `[Enum].IsDefined` → `InvalidEnumArgumentException` |
| `String` | It is consumed (see below) | `String.IsNullOrEmpty` → `ArgumentNullException` (also `IsNullOrWhiteSpace` or `Is Nothing`) |
| Other reference types: classes, interfaces, delegates, arrays, `Of T As Class` | It is consumed (see below) | `Is Nothing` → `ArgumentNullException` |

A `String` or reference parameter is **consumed** when `Nothing` makes the code fail: a member access or call on it (`value.Length`, `action()`), an argument of a method or constructor, the collection of a `For Each`, the lock of a `SyncLock`, or the operand of `Await`. A parameter that is only stored, returned, compared or concatenated (`Me.name = name`, `Return $"Hello {name}"`) is not reported: there, `Nothing` is not an error by itself.

A parameter is considered **validated** when any of its uses looks like a validation:

- a condition of `If`, `ElseIf`, `Select Case`, `While` or `Do`;
- an operand of `Is`, `IsNot` or `TypeOf`; the first operand of `If(value, default)` or the condition of `If(c, x, y)`; the target of `?.`;
- the argument of `NameOf` (as in `Throw New ArgumentNullException(NameOf(value))`);
- an argument of a method whose name starts with `ThrowIf`, `Validate`, `Ensure`, `Check`, `Verify`, `Guard`, `Assert` or `Require` (`ArgumentNullException.ThrowIfNull(value)`, `Debug.Assert(...)`);
- an argument of a method of the same project that validates the corresponding parameter (for example, a private helper that throws `ArgumentNullException`);
- an argument of the `MyBase.New` / `Me.New` call of a constructor (the called constructor validates it).

Not reported:

| Case | Reason |
|---|---|
| Other value types (`Integer`, `Boolean`, structures) | They cannot be `Nothing`: `value = Nothing` compares with the default value (`0`, `False`), which is usually valid, and `ArgumentNullException` would be wrong |
| `Nullable(Of T)` (`Integer?`) | It accepts `Nothing` on purpose |
| `<Flags>` enumerations | `[Enum].IsDefined` returns `False` for combined values such as `Read Or Write` |
| Unconstrained type parameters (`Of T`) | `T` can be a value type |
| `Optional`, `ByRef` and `ParamArray` parameters, parameters with the `AllowNull` attribute | `Optional` and `AllowNull` accept the default or `Nothing` by design; `ByRef` parameters are outputs |
| Parameters whose type derives from `EventArgs` | Events never raise `Nothing` arguments |
| Unused parameters | There is nothing to protect |
| Event handlers (`Handles` clause, or `sender As Object, e As EventArgs`) | They are called by the event source |
| `Equals`, `CompareTo`, `Compare` | Their contract accepts `Nothing` |
| `Overrides` and `Implements` of members declared in referenced assemblies (`TypeConverter.CanConvertFrom`, `IFormattable.ToString`...) | Their contract is defined by the framework and may accept `Nothing` (for example, the `context` of a type converter). Overrides of your own `MustOverride` methods are analyzed |
| `Shared Sub Main` | The runtime never passes `Nothing` |
| Methods whose body only throws an exception (`Throw New NotImplementedException()`) | They never use the parameter |
| `Private` and `Friend` members, members of `Friend` types, `Protected` members of `NotInheritable` classes | Only code of the same assembly can call them |

> [!NOTE]
> The detection of validations is deliberately generous (for example, any `If` condition that uses the parameter counts): this rule prefers to miss an unvalidated parameter rather than to report a validated one.

## ❌ Non-compliant

```vb
Public Shared Function ContainsDirectory(scope As RegistryScope,
                                         directory As String) As Boolean

    Return Array.Exists(UtilEnvironmentPath.GetPathData(scope), Function(dir As String) dir.Equals(directory, StringComparison.OrdinalIgnoreCase))

End Function
```

## ✔️ Compliant

```vb
Public Shared Function ContainsDirectory(scope As RegistryScope,
                                         directory As String) As Boolean

    If Not [Enum].IsDefined(GetType(RegistryScope), scope) Then
        Throw New InvalidEnumArgumentException(NameOf(scope), scope, GetType(RegistryScope))
    End If
    If String.IsNullOrEmpty(directory) Then
        Throw New ArgumentNullException(NameOf(directory), $"The {NameOf(directory)} cannot be null or empty.")
    End If

    Return Array.Exists(UtilEnvironmentPath.GetPathData(scope), Function(dir As String) dir.Equals(directory, StringComparison.OrdinalIgnoreCase))

End Function
```

## 🔧 Code fix

| Parameter | Actions (the first one is the default) |
|---|---|
| `Enum` | `If Not [Enum].IsDefined(GetType(T), value) Then Throw New InvalidEnumArgumentException(NameOf(value), value, GetType(T))` |
| `String` | 1. `If String.IsNullOrEmpty(value) Then Throw New ArgumentNullException(NameOf(value), $"The {NameOf(value)} cannot be null or empty.")`<br>2. The same with `String.IsNullOrWhiteSpace` (*"cannot be null, empty or white space"*)<br>3. `If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))` |
| Other reference types | `If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))` |

Details:

- `InvalidEnumArgumentException` stores the value as an `Integer`. For enumerations whose values do not fit in one (`As Long`, `As UInteger`, `As ULong`), the fix throws `ArgumentOutOfRangeException(NameOf(value), value, $"The value is not defined in the {NameOf(T)} enumeration.")`.
- Type names are written in their shortest valid form: `InvalidEnumArgumentException` when `System.ComponentModel` is imported, `ComponentModel.InvalidEnumArgumentException` otherwise.
- **Position:** after the `MyBase.New` / `Me.New` call of a constructor, and among the guard clauses that already start the body (`If ... Then Throw`, `ArgumentNullException.ThrowIfNull(...)`, `Me.ThrowIfDisposed()`) **in parameter order**. A blank line separates the guard clauses from the rest of the code.
- **Fix All** inserts the guard clauses of every parameter of a method in parameter order. Its scope follows the chosen action: *"Is Nothing"* applies to `String` and reference parameters, *"IsNullOrEmpty"* and *"IsNullOrWhiteSpace"* only to `String` parameters, and the `Enum` action only to `Enum` parameters.

> [!IMPORTANT]
> A guard clause **changes the behavior** for invalid arguments: the method throws at the start instead of failing later (or returning a wrong result). This is why the rule is Info and the fix is never applied silently. After the fix, rule [DCA012](DCA012_Missing_Exception_Documentation.md) reports the new exceptions that the XML documentation does not list yet, and its own fix adds them.

> [!TIP]
> The `Is Nothing` guard clause has the exact form that the throw-helper rules recognize: rule [DCA011](DCA011_Throw_Helper_Migration.md) (.NET Framework, in an `#If NETCOREAPP` block) and the built-in CA1510 (modern .NET) offer to convert it into `ArgumentNullException.ThrowIfNull(value)`. The `String` guard clauses keep their custom message, so they are not converted.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA050.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA050 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA050
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA050:Parameter of an externally visible method is not validated", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA050:Parameter of an externally visible method is not validated", Justification:="Reason.", Scope:="module")>
```
