# 🗂️ DCA014 — File-system loop without `Thread.CurrentThread.Join(0)`

Reports `For Each` loops over file-system enumeration methods whose body does not call `Thread.CurrentThread.Join(0)`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Inserts `Thread.CurrentThread.Join(0)` with an explanatory comment, inside an `#If DEBUG Then` block

---

## 💡 Why

Enumerating large directory trees on an STA thread (e.g. the UI thread of a Windows Forms application) can block COM message pumping for a long time. Under the debugger, this triggers the **ContextSwitchDeadlock** Managed Debugging Assistant. `Thread.CurrentThread.Join(0)` pumps pending messages without waiting.

## 🔎 Detection

Methods of `System.IO.Directory` and `System.IO.DirectoryInfo`: `GetFiles`, `GetDirectories`, `GetFileSystemEntries`, `GetFileSystemInfos`, `EnumerateFiles`, `EnumerateDirectories`, `EnumerateFileSystemEntries`, `EnumerateFileSystemInfos`.

The enumerated expression may contain the call directly (also inside a LINQ chain), or be a local variable whose initializer contains it. Any call to `Thread.Join(0)` inside the loop body (at any depth) satisfies the rule, **including a call inside an inactive conditional block** (such as `#If DEBUG Then` when compiling in Release, where the compiler treats the block as disabled text).

## ❌ Non-compliant

```vb
For Each file As String In Directory.GetFiles(root)
    Console.WriteLine(file)
Next file

For Each file As String In Directory.EnumerateFiles(root, "*.vb", SearchOption.AllDirectories).Where(Function(f As String) f.Length > 3)
Next file

Dim entries As String() = Directory.GetFileSystemEntries(root)
For Each entry As String In entries
Next entry
```

## ✔️ Not reported

```vb
For Each file As String In Directory.GetFiles(root)
    Thread.CurrentThread.Join(0)
Next file

For Each file As String In Directory.GetFiles(root)
#If DEBUG Then
    Thread.CurrentThread.Join(0) ' Also recognized in Release builds, where this block is inactive.
#End If
Next file

For Each line As String In File.ReadAllLines(path)   ' Not a directory enumeration.
Next line
```

## 🔧 Code fix

```vb
For Each file As String In Directory.GetFiles(root)
#If DEBUG Then
    Thread.CurrentThread.Join(0) ' Pumps pending messages to prevent ContextSwitchDeadlock during long-running file system enumerations.
#End If
    Console.WriteLine(file)
Next file
```

Details:

- The call is only compiled in **Debug** builds. ContextSwitchDeadlock is a *Managed Debugging Assistant*: it only exists while a debugger is attached, so Release builds do not need the extra call.
- `DEBUG` is defined by Visual Studio in the Debug configuration of every VB.NET project (the `DefineDebug` property of the project, *Project Properties → Compile → Advanced Compile Options → Define DEBUG constant*), so the fix does not check whether it exists.
- The block is written as `#If DEBUG Then`. The `Then` keyword is optional in VB.NET conditional compilation directives (`#If DEBUG` also compiles), but `#If ... Then` is the canonical form that Visual Studio writes and formats.
- The directives are written at column 0, as Visual Studio formats them, and the statement keeps the indentation of the loop body.
- The `Thread` type is written with the shortest name that resolves correctly (`Thread` with `Imports System.Threading`, otherwise `Threading.Thread`).

> [!NOTE]
> If you also need to pump messages in Release builds (for example, a long enumeration on the UI thread without a debugger), remove the `#If DEBUG Then` / `#End If` lines: the rule is satisfied either way.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA014.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA014 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA014
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA014:File-system loop without Thread.CurrentThread.Join(0)", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA014:File-system loop without Thread.CurrentThread.Join(0)", Justification:="Reason.", Scope:="module")>
```
