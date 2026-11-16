# 🚩 DCA020 — `<Flags>` enumeration without a `None` zero value

Reports `<Flags>` enumerations whose zero value is not a member named `None`.

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ✔️ Adds `None = 0`, or renames the existing zero member to `None`
- **Equivalent to:** SonarQube S2346 *"Flags enumerations zero-value members should be named 'None'"* (and Microsoft's CA1008, which is disabled by default)

---

## 💡 Why

In a flags enumeration, the zero value means *no flag is set*. A member named `None` makes that state readable, and it is the convention of the .NET Framework design guidelines:

```vb
options = FileOptions.None               ' Clear.
options = CType(0, FileOptions)          ' Without a None member.
If options = FileOptions.Empty Then      ' A different name for the same idea.
```

## 🔎 Detection

| Case | Diagnostic location | Fix |
|---|---|---|
| No member has the value `0` | The enumeration name | Adds `None = 0` as the first member |
| The zero member has another name (`Empty = 0`, `Default = 0`) | The zero member | Renames it to `None` **in the whole solution** |
| A member named `None` already exists with a non-zero value | The enumeration name | No fix: the conflict must be resolved by hand |

## ❌ Non-compliant

```vb
<Flags>
Public Enum SearchOptions
    Recursive = 1 << 0
    IncludeHidden = 1 << 1
End Enum

<Flags>
Public Enum Styles
    Empty = 0
    Bold = 1 << 0
End Enum
```

## ✔️ Compliant

```vb
<Flags>
Public Enum SearchOptions
    None = 0
    Recursive = 1 << 0
    IncludeHidden = 1 << 1
End Enum
```

## 🔧 Code fix

- **Add `None = 0`**: the member is inserted first, with the same indentation. If the first member has an XML documentation comment, the new member gets one too (`No flags are set.`), and the blank-line separation between members is kept:

  ```vb
  ''' <summary>
  ''' No flags are set.
  ''' </summary>
  None = 0

  ''' <summary>
  ''' Search in subdirectories.
  ''' </summary>
  Recursive = 1 << 0
  ```

- **Rename the zero member to `None`**: uses the Roslyn renamer, so every reference in the solution (`Styles.Empty` → `Styles.None`) is updated.

> [!WARNING]
> Renaming a public member is a **breaking change** for external code that uses it. In a public library, consider keeping the old name as an obsolete alias (`<Obsolete> Empty = None`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA020.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA020 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA020
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA020:Flags enumeration without a 'None' zero value", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA020:Flags enumeration without a 'None' zero value", Justification:="Reason.", Scope:="module")>
```
