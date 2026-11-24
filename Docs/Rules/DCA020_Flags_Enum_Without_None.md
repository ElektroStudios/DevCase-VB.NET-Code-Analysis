# 🚩 DCA020 — `<Flags>` enumeration without a zero value

Reports `<Flags>` enumerations that have no member whose value is zero.

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ✔️ Adds `None = 0` (or another free name)
- **Similar to:** Microsoft's CA1008 *"Enums should have zero value"* (disabled by default), in its *"add a zero member"* case only

---

## 💡 Why

In a flags enumeration, the zero value means *no flag is set*: it is the default value of every field and variable of the type, and the result of removing every flag. Without a member for it, code has to write the literal `0` or a conversion:

```vb
options = SearchOptions.None             ' With a zero member.
options = CType(0, SearchOptions)        ' Without it.
```

## 🔎 Detection

The enumeration is reported when **no member** has the value `0`, explicit (`None = 0`, `Off = &H0`) or implicit (the first member without a value is `0`).

A zero member that already exists is **never** reported, whatever its name (`Null`, `Empty`, `Default`...): renaming it would break every piece of code that uses it, in the solution and outside it.

```vb
<Flags>
Friend Enum DesktopWallpaperStyle As Integer
    Null                                 ' 0 (implicit): not reported.
    Tile
    Center
End Enum
```

> [!NOTE]
> Before version 1.0.8, the rule also reported a zero member with a name other than `None`, and its code fix renamed it. SonarQube S2346 and CA1008 still ask for that name; DCA020 only asks for the zero value to exist.

## ❌ Non-compliant

```vb
<Flags>
Public Enum SearchOptions
    Recursive = 1 << 0
    IncludeHidden = 1 << 1
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

<Flags>
Public Enum Styles
    Empty = 0                            ' Any name.
    Bold = 1 << 0
End Enum
```

## 🔧 Code fix

Adds the zero member as the first member, with the same indentation. If the first member has an XML documentation comment, the new member gets one too (`No flags are set.`), and the blank-line separation between members is kept:

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

The member is named `None`. When a member with another value already uses that name, the fix takes the first free name of this list (names are compared without case, as Visual Basic does):

| Order | Name |
| :---: | :--- |
| 1 | `None` |
| 2 | `[Nothing]` (between brackets, because `Nothing` is a keyword) |
| 3 | `Null` |
| 4 | `Zero` |
| 5 | `Empty` |

When all of them are used, the enumeration is reported without a code fix.

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
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA020:Flags enumeration without a zero value", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA020:Flags enumeration without a zero value", Justification:="Reason.", Scope:="module")>
```
