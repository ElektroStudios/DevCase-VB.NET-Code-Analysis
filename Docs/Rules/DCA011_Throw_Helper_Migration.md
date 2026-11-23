# 🔁 DCA011 — Argument guards with a modern throw-helper equivalent

**.NET Framework only.** Reports runs of classic argument guards that have an exact equivalent among the throw helpers of modern .NET, so that both versions can coexist in a conditional compilation block.

- **Severity:** Info
- **Category:** Portability
- **Code fix:** ✔️ Wraps the run in `#If NETCOREAPP Then ... #Else ... #End If`
- **Configuration:** `devcase_analyzers.dca011_condition = NET8_0_OR_GREATER`

---

## 💡 Why

When a library targets .NET Framework today but will be multi-targeted or migrated later, writing both versions now saves a migration pass. On modern .NET, the built-in rules CA1510–CA1513 already suggest the throw helpers, so this rule stays silent there.

## 🔎 Detection

The exception must name the parameter of the guard. Its arguments are matched **by parameter**, so positional and named arguments (`paramName:=NameOf(p)`, in any order) are treated alike:

| Exception argument | Accepted |
|---|---|
| `paramName` | `NameOf(p)`, or the literal `"p"` with exactly the parameter name (the helper sets `ParamName` to it) |
| `message` | Yes: the helper writes its own message (see the note below) |
| `actualValue` (`ArgumentOutOfRangeException`) | Only when it is the parameter itself: the helper reports that value |
| `innerException` or anything else | No: the helper cannot reproduce it |

The condition can also use a named argument (`String.IsNullOrEmpty(value:=p)`, `IsNothing(Expression:=p)`).

| Guard | Throw helper |
|---|---|
| `If p Is Nothing Then Throw New ArgumentNullException(NameOf(p))` (also `IsNothing(p)`) | `ArgumentNullException.ThrowIfNull(p)` |
| `If String.IsNullOrEmpty(p) Then Throw New ArgumentNullException(NameOf(p))` | `ArgumentException.ThrowIfNullOrEmpty(p)` |
| `If String.IsNullOrWhiteSpace(p) Then Throw New ArgumentNullException(NameOf(p))` | `ArgumentException.ThrowIfNullOrWhiteSpace(p)` |
| `If p < 0 Then Throw New ArgumentOutOfRangeException(NameOf(p))` | `ArgumentOutOfRangeException.ThrowIfNegative(p)` |
| `p <= 0` / `p = 0` | `ThrowIfNegativeOrZero(p)` / `ThrowIfZero(p)` |
| `p < x`, `p <= x`, `p > x`, `p >= x`, `p = x`, `p <> x` (`x` can be an expression: `If count > sourceCount - startIndex Then ...`) | `ThrowIfLessThan(p, x)`, ..., `ThrowIfNotEqual(p, x)` (`ThrowIfGreaterThan(count, sourceCount - startIndex)`) |

Consecutive guards form a single run (one diagnostic, one block).

> [!NOTE]
> A **custom message** is kept in the `#Else` branch (.NET Framework) and replaced by the standard message of the helper in the `#If NETCOREAPP` branch (*"Value cannot be null. (Parameter 'icon')"*). The exception type and its `ParamName` stay the same. For `ThrowIfNullOrEmpty` and `ThrowIfNullOrWhiteSpace` the helper throws `ArgumentNullException` for `Nothing` and `ArgumentException` (its base class) for an empty or white-space string.

For example, a guard with a custom message:

```vb
If String.IsNullOrEmpty(cultureName) Then
    Throw New ArgumentNullException(NameOf(cultureName), $"The {NameOf(cultureName)} cannot be null or empty.")
End If
```

becomes (the throw helpers have no `message` parameter, so the message can only live in the `#Else` branch):

```vb
#If NETCOREAPP Then
ArgumentException.ThrowIfNullOrEmpty(cultureName)
#Else
If String.IsNullOrEmpty(cultureName) Then
    Throw New ArgumentNullException(NameOf(cultureName), $"The {NameOf(cultureName)} cannot be null or empty.")
End If
#End If
```

> [!TIP]
> **The rule does not appear in Visual Studio?** It runs only in **.NET Framework** projects. A file that belongs to several projects (a linked file, or a multi-targeted project such as `net48;net8.0`) is analyzed with the project selected in the **project context** drop-down list, at the top left of the code editor: select the .NET Framework project there.

## ✔️ Not reported

```vb
If arg1 Is Nothing Then Throw New ArgumentNullException("test")              ' Not the name of the parameter.
If value Is Nothing Then Throw New ArgumentNullException("Missing.", inner)   ' (message, innerException): no parameter name.
If count < 0 Then Throw New ArgumentOutOfRangeException(NameOf(count), count \ 2, "Negative.") ' Another actual value.
If name = Nothing Then Throw New ArgumentNullException(NameOf(name))         ' Also true for "" (not ThrowIfNull).
If value Is Nothing Then Throw New ArgumentException(NameOf(value))          ' Wrong exception type.
If arg2 < 1 Then Throw New ArgumentNullException(NameOf(arg2))               ' Range check with ArgumentNullException.
#If DEBUG Then
If other Is Nothing Then Throw New ArgumentNullException(NameOf(other))      ' Already inside an #If block.
#End If
```

## 🔧 Code fix

```vb
' Before
Public Shared Function ProcessFile(filePath As String, encoding As Encoding,
                                   ParamArray rewriters As VisualBasicSyntaxRewriter()) As Boolean

    If String.IsNullOrWhiteSpace(filePath) Then
        Throw New ArgumentNullException(NameOf(filePath))
    End If
    If encoding Is Nothing Then
        Throw New ArgumentNullException(NameOf(encoding))
    End If

' After
Public Shared Function ProcessFile(filePath As String, encoding As Encoding,
                                   ParamArray rewriters As VisualBasicSyntaxRewriter()) As Boolean

#If NETCOREAPP Then
    ArgumentException.ThrowIfNullOrWhiteSpace(filePath)
    ArgumentNullException.ThrowIfNull(encoding)
#Else
    If String.IsNullOrWhiteSpace(filePath) Then
        Throw New ArgumentNullException(NameOf(filePath))
    End If
    If encoding Is Nothing Then
        Throw New ArgumentNullException(NameOf(encoding))
    End If
#End If
```

> [!NOTE]
> `ThrowIfNullOrWhiteSpace` and `ThrowIfNullOrEmpty` are declared on `ArgumentException`. Calling them through `ArgumentNullException` compiles (it is an inherited `Shared` member), but the fix writes the declaring type. They throw `ArgumentNullException` for `Nothing` and `ArgumentException` for empty or white-space values.

## ⚙️ Configuration

`ThrowIfNull` exists since .NET 6, `ThrowIfNullOrEmpty` since .NET 7, and `ThrowIfNullOrWhiteSpace` and the range helpers since .NET 8. If the project also targets an older modern .NET version, use a more specific symbol:

```ini
[*.vb]
devcase_analyzers.dca011_condition = NET8_0_OR_GREATER
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA011.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA011 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA011
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Portability", "DCA011:Argument guards can be paired with their modern throw helpers", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Portability", "DCA011:Argument guards can be paired with their modern throw helpers", Justification:="Reason.", Scope:="module")>
```
