# 🌍 DCA019 — Date or time parsed without a format provider

Reports `Parse`, `TryParse`, `ParseExact` and `TryParseExact` calls on `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly` and `TimeOnly` that do not pass an `IFormatProvider` although an overload accepts one.

- **Severity:** Warning
- **Category:** Globalization
- **Code fix:** ✔️ Two lightbulb actions: `CultureInfo.CurrentCulture` (keeps the current behavior) or `CultureInfo.InvariantCulture`
- **Equivalent to:** SonarQube S6580 *"Use a format provider when parsing date and time"*. Microsoft's CA1305 covers a broader set of APIs but is disabled by default.

---

## 💡 Why

Without a format provider, these methods use the **regional settings of the current thread**. The same text can therefore be read differently on another computer:

```vb
' "03/04/2026" is 3 April in Spain and 4 March in the United States.
Dim d As DateTime = DateTime.Parse("03/04/2026")
```

On another machine this can also throw a `FormatException`, or produce a wrong value without any error at all, which is worse. Typical cases: dates read from files, logs, INI or XML configuration, web pages (scrapers) or command-line arguments.

## 🔎 Detection

The overload is found through the semantic model: it must be the same method with an `IFormatProvider` inserted at one position (followed, if required, by `DateTimeStyles` or `TimeSpanStyles`). The rule adapts to the target framework:

| Call | .NET Framework 4.8 | .NET 7+ |
|---|---|---|
| `DateTime.Parse(s)` | `DateTime.Parse(s, provider)` | `DateTime.Parse(s, provider)` |
| `DateTime.TryParse(s, result)` | `DateTime.TryParse(s, provider, DateTimeStyles.None, result)` | `DateTime.TryParse(s, provider, result)` |
| `TimeSpan.TryParse(s, result)` | `TimeSpan.TryParse(s, provider, result)` | `TimeSpan.TryParse(s, provider, result)` |

## ❌ Non-compliant

```vb
Dim released As DateTime = DateTime.Parse(fields(2))
Dim duration As TimeSpan = TimeSpan.Parse(fields(3))
If DateTime.TryParse(text, value) Then
```

## ✔️ Compliant

```vb
Dim released As DateTime = DateTime.Parse(fields(2), CultureInfo.InvariantCulture)
Dim duration As TimeSpan = TimeSpan.Parse(fields(3), CultureInfo.InvariantCulture)
If DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, value) Then ' Text typed by the user.
```

## 🔧 Code fix

| Action | When to use it |
|---|---|
| **Add `CultureInfo.CurrentCulture`** (keeps the current behavior) | Text typed or seen by the user. The behavior does not change: the culture only becomes explicit. |
| **Add `CultureInfo.InvariantCulture`** | Text written or read by programs: files, logs, configuration, network protocols, web pages. |

Details:

- The `styles` argument is added as `DateTimeStyles.None` or `TimeSpanStyles.None`, the value the overload without it already uses.
- Arguments are inserted at the right position (before the `ByRef result` of `TryParse`). With named arguments, they are appended as named arguments.
- Type names are written in their shortest valid form (`CultureInfo` or `Globalization.CultureInfo`, depending on the `Imports`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA019.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA019 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA019
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "DCA019:Date and time parsed without a format provider", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "DCA019:Date and time parsed without a format provider", Justification:="Reason.", Scope:="module")>
```
