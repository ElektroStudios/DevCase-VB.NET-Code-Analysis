# 🚩 DCA069 — `Enum.HasFlag` with a different enum type

**.NET Framework only.** Reports `HasFlag` calls whose argument is a value of another enumeration than the instance, such as `attributes.HasFlag(ConsoleModifiers.Alt)` with a `FileAttributes` value.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (report only)
- **Equivalent to:** Microsoft [CA2248](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2248) *"Provide correct 'enum' argument to 'Enum.HasFlag'"*

---

## 💡 Why

`Enum.HasFlag` takes an `[Enum]` parameter, so the compiler accepts any enumeration. At run time, it checks that the argument has the same type as the instance and **throws `ArgumentException`** otherwise. The mistake usually comes from two enumerations with similar member names.

On modern .NET, the built-in rule CA2248 already reports this code, so this rule stays silent there.

## 🔎 Detection

The instance and the argument (without its conversion to `[Enum]`) are enumerations of different types. An argument typed as `[Enum]` is not reported, because its run-time type is unknown.

## ❌ Non-compliant

```vb
If attributes.HasFlag(ConsoleModifiers.Alt) Then
```

## ✔️ Compliant

```vb
If attributes.HasFlag(FileAttributes.Archive) Then
```

## 🔧 Code fix

There is no code fix: the right flag depends on what the code means.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA069.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA069 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA069
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA069:Enum.HasFlag with a different enum type", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA069:Enum.HasFlag with a different enum type", Justification:="Reason.", Scope:="module")>
```
