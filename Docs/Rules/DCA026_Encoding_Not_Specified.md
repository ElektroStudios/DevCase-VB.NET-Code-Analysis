# 🔤 DCA026 — Text encoding not specified

Reports method calls and object creations that read or write text **without an explicit `System.Text.Encoding`**, when an overload of the same method accepts one: `File.WriteAllText(path, text)`, `File.ReadAllLines(path)`, `New StreamWriter(path)`, `New StreamReader(stream, True)`, `My.Computer.FileSystem.WriteAllText(path, text, False)`...

- **Severity:** Warning
- **Category:** Globalization
- **Code fix:** ✔️ Three lightbulb actions: UTF-8 without BOM, UTF-8 with BOM, or `Encoding.Default`. The first action always keeps the current behavior.

---

## 💡 Why

Every text API has an encoding, even when you do not write it. The implicit one depends on **which API** you call:

| API | Implicit encoding |
|---|---|
| `System.IO`: `File.*Text`, `File.*Lines`, `StreamWriter`, `StreamReader`... | **UTF-8 without BOM** for writing; UTF-8 with BOM detection for reading |
| `Microsoft.VisualBasic.FileIO.FileSystem` (`My.Computer.FileSystem.WriteAllText`, `OpenTextFileWriter`...) | **UTF-8 with BOM** |
| `Encoding.Default` (often assumed to be "the default") | **ANSI code page** of the system on .NET Framework (Windows-1252 in Spain), **UTF-8** on .NET Core / .NET 5+ |

Leaving it implicit hides the file format from the reader of the code, and makes mistakes easy: a file written by `My.Computer.FileSystem` has a BOM and a file written by `File.WriteAllText` does not; a program that expects ANSI text reads `Ã±` instead of `ñ`. Writing the encoding documents the format and makes these differences visible at the call site.

## 🔎 Detection

A call is reported when an overload exists with the same parameters plus one `Encoding` parameter inserted at any position, or plus `append As Boolean` followed by `Encoding` (`New StreamWriter(path)` → `New StreamWriter(path, append, encoding)`). Overloads are matched through the semantic model, so the rule adapts to every target framework and also covers third-party APIs that follow the same pattern.

Not reported:

| Case | Reason |
|---|---|
| Calls that already pass an `Encoding` | Nothing to add |
| Members of `Encoding` and its derived types (`Encoding.UTF8.GetBytes(text)`) | The encoding is the instance itself |
| Extension methods | Their overloads are not part of the type |
| Methods without an `Encoding` overload (`Console.WriteLine`, `File.ReadAllBytes`) | No encoding to choose |

## ❌ Non-compliant

```vb
File.WriteAllText(path, text)
Dim lines As String() = File.ReadAllLines(path)
Using writer As New StreamWriter(path)
End Using
My.Computer.FileSystem.WriteAllText(path, text, append:=False)
```

## ✔️ Compliant

```vb
File.WriteAllText(path, text, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
Dim lines As String() = File.ReadAllLines(path, Encoding.UTF8)
Using writer As New StreamWriter(path, False, New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False))
End Using
My.Computer.FileSystem.WriteAllText(path, text, append:=False, encoding:=Encoding.UTF8)
```

## 🔧 Code fix

Three lightbulb actions are offered. The one that writes **the encoding the call already uses** comes first and is marked *(keeps the current behavior)*:

| Action | Inserted expression | First for |
|---|---|---|
| **UTF-8 without BOM** | `New UTF8Encoding(encoderShouldEmitUTF8Identifier:=False)` | `System.IO` and every other API |
| **UTF-8 with BOM** | `Encoding.UTF8` | `Microsoft.VisualBasic.FileIO` (`My.Computer.FileSystem`) |
| **`Encoding.Default`** | `Encoding.Default` | Never (always last) |

Details:

- When the overload also requires `append As Boolean` (`StreamWriter`), the fix writes `False`, which is what the original constructor does.
- If the call already uses named arguments, the fix also writes named ones (`encoding:=`).
- Type names are written in their shortest valid form at that position (`Encoding`, `Text.Encoding` or `System.Text.Encoding`, depending on the `Imports`).
- Calls with omitted arguments (`M(a, , b)`) are reported but get no fix.

> [!WARNING]
> `Encoding.Default` is **not** a safe default. On .NET Framework it is the ANSI code page of the machine that runs the program, so the same file is read differently on a Spanish, Russian or Japanese Windows, and characters outside the code page are lost when writing. It is offered only for code that must read or write legacy ANSI files on .NET Framework. For new files, prefer UTF-8.

> [!NOTE]
> For **reading**, the encoding is only a fallback: `StreamReader` and `File.Read*` still detect a byte order mark (UTF-8, UTF-16, UTF-32) and use it. Any UTF-8 action therefore keeps the current behavior when reading.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA026.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA026 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA026
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "DCA026:Text encoding not specified", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "DCA026:Text encoding not specified", Justification:="Reason.", Scope:="module")>
```
