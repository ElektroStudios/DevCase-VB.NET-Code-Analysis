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

An argument is not a consumption either when the called method accepts `Nothing` as a valid input:

- `Object.ReferenceEquals`, `Object.Equals(a, b)`, `String.Concat`, `String.IsNullOrEmpty`, `String.IsNullOrWhiteSpace`, `Convert.ToString`;
- `Console.Write`/`WriteLine`, `TextWriter.Write`/`WriteLine` (and the writers that override them), `StringBuilder.Append`/`AppendLine`/`Insert`, `Debug` and `Trace` output methods, `IsNothing` and every Visual Basic string function (`Strings.Trim`, `Len`...), which treat `Nothing` as an empty string;
- parameters whose type is a type parameter, such as `List(Of T).Add(item As T)`.

String **comparisons** (`dir.Equals(directory, StringComparison.OrdinalIgnoreCase)`) are still a consumption: with `Nothing` they silently return `False`, which hides the missing argument instead of reporting it.

An **assignment to a framework property whose setter rejects `Nothing`** is a consumption too, also in an object initializer (`New T With {.Property = p}`). For example, `Thread.CurrentThread.CurrentCulture = ci` throws `ArgumentNullException` when `ci` is `Nothing`, so `ci` is reported:

```vb
Public Shared Sub SetCurrentCulture(ci As CultureInfo)   ' DCA050: 'ci' is not validated.
    Thread.CurrentThread.CurrentCulture = ci
    Thread.CurrentThread.CurrentUICulture = ci
End Sub
```

These setters are recognized:

- on modern .NET, from the nullable annotations of the framework: a property declared as not nullable, without `<AllowNull>` on the property or its setter;
- on .NET Framework, whose assemblies have no nullable annotations, from this list: `Thread.CurrentCulture`, `Thread.CurrentUICulture`, `CultureInfo.CurrentCulture`, `CultureInfo.CurrentUICulture`, `Console.Title` and `Environment.CurrentDirectory`.

Assignments to properties declared in the source code, and to framework properties that accept `Nothing` (`TextWriter.NewLine`, `Exception.HelpLink`, `Control.Text`...), are not consumptions.

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
| `Equals`, `ReferenceEquals`, `CompareTo`, `Compare` | Their contract accepts `Nothing` (`Object.ReferenceEquals(Nothing, Nothing)` returns `True`) |
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

**Throw helpers.** When every project that compiles the file has them, the fix writes the throw helper instead of the `If` block:

| Action | Throw helper | Available since |
|---|---|---|
| *Is Nothing* | `ArgumentNullException.ThrowIfNull(value)` | .NET 6 |
| *IsNullOrEmpty* | `ArgumentException.ThrowIfNullOrEmpty(value)` | .NET 7 |
| *IsNullOrWhiteSpace* | `ArgumentException.ThrowIfNullOrWhiteSpace(value)` | .NET 8 |

```vb
' .NET 8 project:
Public Shared Sub MailTo(addresses As String(), subject As String, body As String)

    ArgumentNullException.ThrowIfNull(addresses)
    ArgumentNullException.ThrowIfNull(subject)
    ArgumentNullException.ThrowIfNull(body)

    ...
End Sub
```

- The Visual Basic compiler passes the parameter name to the helper (`[CallerArgumentExpression]`), so the exception's `ParamName` is `"addresses"`, as with `NameOf`.
- A file can be compiled by several projects: a multi-targeted project (`<TargetFrameworks>net48;net8.0</TargetFrameworks>`) or the same file linked into a .NET Framework project. The helper is written only when **every** one of them has it; otherwise the fix writes the `If` block, so it never breaks the .NET Framework build.
- `ThrowIfNullOrEmpty` and `ThrowIfNullOrWhiteSpace` throw `ArgumentNullException` for `Nothing` and `ArgumentException` for an empty or white-space string (the `If` block throws `ArgumentNullException` with a custom message in both cases).
- `Enum` parameters have no throw helper: they always get the `If` block.

Details:

- `InvalidEnumArgumentException` stores the value as an `Integer`. For enumerations whose values do not fit in one (`As Long`, `As UInteger`, `As ULong`), the fix throws `ArgumentOutOfRangeException(NameOf(value), value, $"The value is not defined in the {NameOf(T)} enumeration.")`.
- Type names are written in their shortest valid form: `InvalidEnumArgumentException` when `System.ComponentModel` is imported, `ComponentModel.InvalidEnumArgumentException` otherwise.
- **Position:** after the `MyBase.New` / `Me.New` call of a constructor, and among the guard clauses that already start the body (`If ... Then Throw`, `ArgumentNullException.ThrowIfNull(...)`, `Me.ThrowIfDisposed()`) **in parameter order**. A blank line separates the guard clauses from the rest of the code.
- **Fix All** inserts the guard clauses of every parameter of a method in parameter order. Its scope follows the chosen action: *"Is Nothing"* applies to `String` and reference parameters, *"IsNullOrEmpty"* and *"IsNullOrWhiteSpace"* only to `String` parameters, and the `Enum` action only to `Enum` parameters.

> [!IMPORTANT]
> A guard clause **changes the behavior** for invalid arguments: the method throws at the start instead of failing later (or returning a wrong result). This is why the rule is Info and the fix is never applied silently. After the fix, rule [DCA012](DCA012_Missing_Exception_Documentation.md) reports the new exceptions that the XML documentation does not list yet, and its own fix adds them.

> [!TIP]
> In .NET Framework projects, the `Is Nothing` guard clause has the exact form that rule [DCA011](DCA011_Throw_Helper_Migration.md) recognizes: it offers to add `ArgumentNullException.ThrowIfNull(value)` in an `#If NETCOREAPP` block, for code that will be migrated. The `String` guard clauses keep their custom message, so they are not converted.

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
