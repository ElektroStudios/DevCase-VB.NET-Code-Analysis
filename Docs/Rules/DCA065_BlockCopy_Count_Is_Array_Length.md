# 📏 DCA065 — `Buffer.BlockCopy` count is an array length

**.NET Framework only.** Reports `Buffer.BlockCopy` calls whose `count` argument is the `Length` of the source or destination array when its elements are larger than a byte, such as `Buffer.BlockCopy(source, 0, target, 0, source.Length)` with two `Integer()`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Uses `Buffer.ByteLength(array)`
- **Equivalent to:** Microsoft [CA2018](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2018) *"The count argument to Buffer.BlockCopy should specify the number of bytes to copy"*

---

## 💡 Why

The offsets and the count of `Buffer.BlockCopy` are **numbers of bytes**, not of elements. With an `Integer()`, `Length` is a quarter of the size in bytes, so only the first quarter of the array is copied, without any error.

On modern .NET, the built-in rule CA2018 already reports this code, so this rule stays silent there.

## 🔎 Detection

The `count` argument is `array.Length`, where `array` is the same code as the source or the destination argument, and the element type is not `Byte`, `SByte` or `Boolean` (whose length is their size in bytes). The `Length` of another array, and computed counts (`source.Length * 4`), are not reported.

## ❌ Non-compliant

```vb
Buffer.BlockCopy(source, 0, target, 0, source.Length)
```

## ✔️ Compliant

```vb
Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `Buffer.BlockCopy(source, 0, target, 0, source.Length)` | `Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(source))` |
| `Buffer.BlockCopy(source, 0, target, 0, target.Length)` | `Buffer.BlockCopy(source, 0, target, 0, Buffer.ByteLength(target))` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA065.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA065 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA065
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA065:Buffer.BlockCopy count is an array length", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA065:Buffer.BlockCopy count is an array length", Justification:="Reason.", Scope:="module")>
```
