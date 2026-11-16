# 🚩 DCA010 — `<Flags>` enum member without bit-shift syntax

Reports members of an enumeration marked with `<Flags>` that declare a single-bit value with a numeric literal instead of the bit-shift operator.

- **Severity:** Warning
- **Category:** Style
- **Code fix:** ✔️ Converts the literal to `1 << n`
- **Configuration:** ⚠️ **Disabled by default.** Enable it with `devcase_analyzers.dca010_enabled = true`

---

## 💡 Why

`1 << 3` states the bit position; `8`, `&H8` or `1099511627776` do not, and mistakes in hand-written powers of two are easy to miss.

## ⚙️ Why is it disabled by default?

Legacy P/Invoke flag enumerations usually copy the hexadecimal values of the Windows SDK headers. Enable the rule per project or per folder:

```ini
[*.vb]
devcase_analyzers.dca010_enabled = true
```

## 🔎 Detection

Only literals whose value is a power of two are reported: zero and composite values (e.g. `3`) are left untouched, as are expressions that are not literals (`Read Or Write`, `1 << 4`).

## ❌ Non-compliant

```vb
<Flags>
Public Enum Access
    None = 0
    Read = 1
    Write = 2
    Execute = &H4
    ReadWrite = 3        ' Not reported (composite value).
End Enum
```

## 🔧 Code fix

The literal "1" gets the width of the underlying type, so that the shift is not truncated:

```vb
<Flags>
Public Enum Access
    None = 0
    Read = 1 << 0 ' first bit
    Write = 1 << 1
    Execute = 1 << 2
    ReadWrite = 3
End Enum

<Flags>
Public Enum Wide As Long
    Low = 1L << 0
    High = 1L << 40          ' was 1099511627776
End Enum

<Flags>
Public Enum Unsigned As UInteger
    Top = 1UI << 31          ' was &H80000000UI
End Enum
```

> [!NOTE]
> On an `SByte` enumeration, `1 << 7` (128) does not fit, so that value is not reported.

## 🔕 How to disable

This rule is **disabled by default** (see above): it only runs when `devcase_analyzers.dca010_enabled = true` is set. Once it is enabled, it can be suppressed in the same ways as any other rule.

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA010.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA010 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA010
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA010:Flags enum member does not use bit-shift syntax", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA010:Flags enum member does not use bit-shift syntax", Justification:="Reason.", Scope:="module")>
```
