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

Only guards whose exception receives **exactly** `NameOf(parameter)` are converted (a custom message or a different parameter name would be lost):

| Guard | Throw helper |
|---|---|
| `If p Is Nothing Then Throw New ArgumentNullException(NameOf(p))` (also `IsNothing(p)`) | `ArgumentNullException.ThrowIfNull(p)` |
| `If String.IsNullOrEmpty(p) Then Throw New ArgumentNullException(NameOf(p))` | `ArgumentException.ThrowIfNullOrEmpty(p)` |
| `If String.IsNullOrWhiteSpace(p) Then Throw New ArgumentNullException(NameOf(p))` | `ArgumentException.ThrowIfNullOrWhiteSpace(p)` |
| `If p < 0 Then Throw New ArgumentOutOfRangeException(NameOf(p))` | `ArgumentOutOfRangeException.ThrowIfNegative(p)` |
| `p <= 0` / `p = 0` | `ThrowIfNegativeOrZero(p)` / `ThrowIfZero(p)` |
| `p < x`, `p <= x`, `p > x`, `p >= x`, `p = x`, `p <> x` | `ThrowIfLessThan(p, x)`, ..., `ThrowIfNotEqual(p, x)` |

Consecutive guards form a single run (one diagnostic, one block).

## ✔️ Not reported

```vb
If arg1 Is Nothing Then Throw New ArgumentNullException("test")              ' Not NameOf(parameter).
If value Is Nothing Then Throw New ArgumentNullException(NameOf(value), "Custom message.")
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
