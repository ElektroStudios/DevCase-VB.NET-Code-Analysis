# 🕒 DCA018 — `DateTime` created without `DateTimeKind`

Reports `New DateTime(...)` (or `New Date(...)`) calls that do not specify a `DateTimeKind` although a constructor overload accepts one.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Three lightbulb actions: `Unspecified` (keeps the current behavior), `Utc` or `Local`
- **Equivalent to:** SonarQube S6562 *"Always set the DateTimeKind when creating new DateTime instances"*

---

## 💡 Why

A `DateTime` created without a kind has `Kind = DateTimeKind.Unspecified`. That value is ambiguous, and different APIs interpret it differently:

- `ToUniversalTime()` assumes it is **local** time and converts it.
- `ToLocalTime()` assumes it is **UTC** and converts it.
- Serializers (JSON, XML) write it without an offset, so whoever reads it decides what it means.

Writing the kind explicitly documents what the value represents and avoids errors of hours when the program runs in another time zone or across a daylight-saving change.

## 🔎 Detection

A constructor is reported when an overload exists that starts with the same parameters, optionally followed by `Integer` time components, and ends with a `DateTimeKind` parameter:

| Constructor | Overload used by the fix |
|---|---|
| `New DateTime(ticks)` | `New DateTime(ticks, kind)` |
| `New DateTime(y, m, d)` | `New DateTime(y, m, d, 0, 0, 0, kind)` |
| `New DateTime(y, m, d, h, min, s)` | `New DateTime(y, m, d, h, min, s, kind)` |
| `New DateTime(y, m, d, h, min, s, ms)` | `New DateTime(y, m, d, h, min, s, ms, kind)` |

Not reported: `New DateTime()` (`DateTime.MinValue`), constructors that already pass a kind, and overloads with a `Calendar` before the time components (there is no equivalent overload with a kind).

## ❌ Non-compliant

```vb
Dim releaseDate As New DateTime(2026, 11, 16)
Dim timestamp As New DateTime(ticks)
```

## ✔️ Compliant

```vb
Dim releaseDate As New DateTime(2026, 11, 16, 0, 0, 0, DateTimeKind.Unspecified)
Dim timestamp As New DateTime(ticks, DateTimeKind.Utc)
Dim now As DateTime = DateTime.UtcNow ' Not a constructor: never reported.
```

## 🔧 Code fix

Three lightbulb actions are offered, in this order:

| Action | When to use it |
|---|---|
| **Add `DateTimeKind.Unspecified`** (keeps the current behavior) | It is the kind the constructor already uses: the value **does not change**, it only becomes explicit. It is the safe choice for **Fix All**. |
| **Add `DateTimeKind.Utc`** | The value is a point in time (timestamps, logs, data exchanged between machines). |
| **Add `DateTimeKind.Local`** | The value is a time on this machine's clock (for example, a time shown to the user). |

> [!IMPORTANT]
> Why is the first action `Unspecified` and not `Utc`? Because changing the kind **changes the result** of `ToUniversalTime()`, `ToLocalTime()` and serialization. A code fix must not change the behavior of the program without you deciding it. `Unspecified` makes the current behavior visible; switching to `Utc` or `Local` is your decision.

When the overload needs time components, the fix adds them with `0`, which creates exactly the same value. If the call uses named arguments, the new ones are named too (`hour:=0, minute:=0, second:=0, kind:=DateTimeKind.Local`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA018.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA018 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA018
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA018:DateTime created without DateTimeKind", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA018:DateTime created without DateTimeKind", Justification:="Reason.", Scope:="module")>
```
