# 📜 DCA004 — Missing `Option` statements

Reports source files that do not explicitly declare `Option Strict On`, `Option Explicit On` and `Option Infer Off`.

- **Severity:** Warning
- **Category:** Style
- **Code fix:** ✔️ Writes the three statements inside a `#Region " Option Statements "` block

---

## 💡 Why

Project-level settings can be changed or forgotten; explicit file-level statements make the strictness of each file self-evident and portable (copy a file to another project and it keeps its rules).

## 🔎 Detection

`Option Strict` and `Option Explicit` without a value count as `On`. A file is reported when any of the three declarations is missing or has the opposite value. Generated files (`*.Designer.vb`, `*.g.vb`, ...) are ignored.

## ❌ Non-compliant

```vb
Option Strict Off
Option Explicit

Imports System
```

## ✔️ Compliant

```vb
Option Infer Off
Option Strict
Option Explicit On
```

## 🔧 Code fix

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
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA004:Missing Option statements", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA004:Missing Option statements", Justification:="Reason.", Scope:="module")>
```
