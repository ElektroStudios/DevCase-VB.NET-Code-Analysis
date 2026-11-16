# 🗃️ DCA045 — `Path.GetTempFileName` creates a predictable temporary file

Reports calls to `Path.GetTempFileName` and `My.Computer.FileSystem.GetTempFileName`.

- **Severity:** Warning
- **Category:** Security
- **Code fix:** ✔️ Builds a random path with `Path.GetRandomFileName` and creates the file atomically with `FileMode.CreateNew`
- **Equivalent to:** SonarQube [S5445](https://rules.sonarsource.com/vbnet/RSPEC-5445/) *"Insecure temporary file creation methods should not be used"*

---

## 💡 Why

On Windows, `GetTempFileName` creates an empty file named `tmpXXXX.tmp` in the temporary folder, where `XXXX` is a **4-digit hexadecimal number** derived from the system time and incremented until a free name is found:

- **Predictable:** another process can guess the next names and create or replace those files first (a symbolic link, a file with other permissions), so that the program writes its data where the attacker wants, or reads data the attacker prepared.
- **Limited:** there are only 65,535 possible names. When the temporary folder holds that many `tmp*.tmp` files (programs that never delete theirs), `GetTempFileName` throws `IOException`, and every program that uses it fails.

`Path.GetRandomFileName` returns a cryptographically random name (`xxxxxxxx.xxx`) that cannot be predicted, and `FileMode.CreateNew` creates the file **atomically**: if a file with that name already exists, it throws instead of opening it.

## 🔎 Detection

`System.IO.Path.GetTempFileName()`, `Microsoft.VisualBasic.FileIO.FileSystem.GetTempFileName()` and `My.Computer.FileSystem.GetTempFileName()`.

## ❌ Non-compliant

```vb
Dim tempPath As String = Path.GetTempFileName()
File.WriteAllText(tempPath, report, Encoding.UTF8)
```

## ✔️ Compliant

```vb
Dim tempPath As String = Path.Combine(Path.GetTempPath(), Path.ChangeExtension(Path.GetRandomFileName(), "tmp"))
Using New FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None) ' Creates the file atomically: fails instead of opening a file that already exists.
End Using
File.WriteAllText(tempPath, report, Encoding.UTF8)
```

## 🔧 Code fix

The fix keeps the behavior of `GetTempFileName`: after it, the variable holds the path of a **new, empty file** in the temporary folder, with the `.tmp` extension (verified by a test that runs the generated code). Only the name changes: it is random instead of sequential.

| Usage | Result |
|---|---|
| Local variable: `Dim tempPath As String = Path.GetTempFileName()` | The initializer becomes the random path, and the `Using` block is added after the declaration |
| Assignment: `Me.workFile = Path.GetTempFileName()` | The right side becomes the random path, and the `Using` block creates the file at `Me.workFile` |
| `Return Path.GetTempFileName()` | A local variable `tempFilePath` (`tempFilePath2`... if the name is taken) is declared, the file is created, and the variable is returned |

Details:

- The `Using` block has no variable: it only creates the file and closes it immediately, exactly like `GetTempFileName`.
- Type names are written in their shortest valid form at that position. When a variable named `path` hides the `Path` type (Visual Basic is not case-sensitive), `IO.Path` is written.
- Other usages (an argument, a member access such as `Path.GetTempFileName().Length`, a single-line `If`) are reported without a fix, because the file must be created in a separate statement.

> [!TIP]
> If the file is only needed while the program runs, consider `New FileStream(tempPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, 4096, FileOptions.DeleteOnClose)`: Windows deletes the file when the stream is closed, even if the program crashes.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA045.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA045 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA045
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "DCA045:Path.GetTempFileName creates a predictable temporary file", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "DCA045:Path.GetTempFileName creates a predictable temporary file", Justification:="Reason.", Scope:="module")>
```
