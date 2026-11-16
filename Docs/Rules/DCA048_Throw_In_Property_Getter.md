# 🫳 DCA048 — Exception thrown in a property getter

Reports `Throw` statements in property getters that throw an exception type other than `InvalidOperationException`, `NotSupportedException` or `NotImplementedException`.

- **Severity:** Info
- **Category:** Design
- **Code fix:** ❌ (the validation must be moved)
- **Equivalent to:** SonarQube [S2372](https://rules.sonarsource.com/vbnet/RSPEC-2372/) *"Exceptions should not be thrown from property getters"*
- **Related:** Microsoft [CA1065](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1065) (disabled by default)

---

## 💡 Why

Reading a property is expected to be **safe and cheap**: the debugger evaluates properties to show them in the *Locals* and *Watch* windows, data binding reads them to fill controls, and serializers and `PropertyGrid` read every property. A getter that throws breaks all of them.

Argument validation belongs to the **setter** (or to a method). A getter should only throw when the object itself is in a state where the value does not exist, which is what `InvalidOperationException` (and its derived `ObjectDisposedException`) means, or when the property is not supported (`NotSupportedException`, `NotImplementedException`).

## 🔎 Detection

`Throw` statements with an exception whose type is not `InvalidOperationException`, `NotSupportedException`, `NotImplementedException` or a type derived from them.

Not reported:

| Case | Reason |
|---|---|
| Parameterized (indexed) properties: `Property Item(index As Integer)` | Argument validation (`ArgumentOutOfRangeException`) is expected |
| `Throw` without an expression (rethrow) | It propagates an exception thrown elsewhere |
| Setters | Validation belongs there |
| Exceptions handled inside the getter, and exceptions thrown inside lambdas | They do not leave the getter |

## ❌ Non-compliant

```vb
Public ReadOnly Property Port As Integer
    Get
        If Me.portText Is Nothing Then
            Throw New ArgumentNullException(NameOf(Me.portText))
        End If
        Return Integer.Parse(Me.portText, CultureInfo.InvariantCulture)
    End Get
End Property
```

## ✔️ Compliant

```vb
Public ReadOnly Property Port As Integer
    Get
        If Me.portText Is Nothing Then
            Throw New InvalidOperationException("The port has not been configured.")
        End If
        Return Integer.Parse(Me.portText, CultureInfo.InvariantCulture)
    End Get
End Property
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA048.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA048 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA048
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA048:Exception thrown in a property getter", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA048:Exception thrown in a property getter", Justification:="Reason.", Scope:="module")>
```
