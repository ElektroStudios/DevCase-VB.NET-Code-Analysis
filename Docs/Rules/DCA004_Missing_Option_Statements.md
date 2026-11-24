# 📜 DCA004 — Required `Option` settings not in effect

Reports source files that are not compiled with `Option Strict On`, `Option Explicit On` and `Option Infer Off`, taking into account the settings of the project.

- **Severity:** Warning
- **Category:** Style
- **Code fix:** ✔️ Writes the three statements inside a `#Region " Option Statements "` block

---

## 💡 Why

- `Option Strict On` rejects implicit narrowing conversions and late binding, which otherwise fail only at run time (`InvalidCastException`, `MissingMemberException`).
- `Option Explicit On` requires every variable to be declared, so a typo cannot create a new variable.
- `Option Infer Off` requires the type of every local variable to be written, so it is visible without hovering in the editor.

## 🔎 Detection

Each of the three settings is the one that the compiler uses for the file:

1. The `Option` statement of the file, if the file declares it (`Option Strict` and `Option Explicit` without a value count as `On`).
2. Otherwise, the setting of the project: the *Compile* page of the project properties, or `<OptionStrict>`, `<OptionExplicit>` and `<OptionInfer>` in the `.vbproj` file.

The file is reported when one of the three is not the required value:

| Project settings | `Option` statements of the file | Reported |
|---|---|---|
| `Strict On`, `Explicit On`, `Infer Off` | None | ➖ The file inherits the required settings |
| `Strict On`, `Explicit On`, `Infer Off` | `Option Strict On` | ➖ |
| `Strict On`, `Explicit On`, `Infer Off` | `Option Infer On` | ✔️ The file overrides the project |
| `Strict Off`, `Explicit On`, `Infer On` (the defaults of a new project) | None | ✔️ |
| `Strict Off`, `Explicit On`, `Infer On` | The three required statements | ➖ |

`Option Strict Custom` (warnings configured one by one in the project properties) is not `On`. The message lists the statements to declare and the settings that the file uses instead, with their origin:

```text
Declare 'Option Strict On', 'Option Infer Off' because this file compiles with 'Option Strict Off' (project), 'Option Infer On' (project)
```

Generated files (`*.Designer.vb`, `*.g.vb`, ...) are ignored.

> [!NOTE]
> Before version 1.0.8, the rule required the three statements in every file, even when the project already used the required settings.

## ❌ Non-compliant

In a project with the default settings (`Option Strict Off`, `Option Infer On`):

```vb
Option Strict Off
Option Explicit

Imports System
```

## ✔️ Compliant

In any project:

```vb
Option Infer Off
Option Strict
Option Explicit On
```

In a project whose settings are `Option Strict On`, `Option Explicit On` and `Option Infer Off`, also a file without `Option` statements.

## 🔧 Code fix

The fix always writes the three statements, so the file keeps the required settings even if it is moved to another project or the project settings change:

```vb
' Before
#Region " Imports "

Imports System

#End Region

' After
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System

#End Region
```

Conflicting values are replaced, other `Option` statements are kept, and an existing region around the statements is reused:

```vb
' Before
Option Compare Text
Option Strict Off

' After
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off
Option Compare Text

#End Region
```

The fix keeps the file's line endings (CRLF or LF).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA004.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA004 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA004
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA004:Required Option settings not in effect", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA004:Required Option settings not in effect", Justification:="Reason.", Scope:="module")>
```
