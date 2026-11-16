# 🗂️ DCA014 — File-system loop without `Thread.CurrentThread.Join(0)`

Reports `For Each` loops over file-system enumeration methods whose body does not call `Thread.CurrentThread.Join(0)`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Inserts `Thread.CurrentThread.Join(0)` with an explanatory comment

---

## 💡 Why

Enumerating large directory trees on an STA thread (e.g. the UI thread of a Windows Forms application) can block COM message pumping for a long time. Under the debugger, this triggers the **ContextSwitchDeadlock** Managed Debugging Assistant. `Thread.CurrentThread.Join(0)` pumps pending messages without waiting.

## 🔎 Detection

Methods of `System.IO.Directory` and `System.IO.DirectoryInfo`: `GetFiles`, `GetDirectories`, `GetFileSystemEntries`, `GetFileSystemInfos`, `EnumerateFiles`, `EnumerateDirectories`, `EnumerateFileSystemEntries`, `EnumerateFileSystemInfos`.

The enumerated expression may contain the call directly (also inside a LINQ chain), or be a local variable whose initializer contains it. Any call to `Thread.Join(0)` inside the loop body (at any depth) satisfies the rule.

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

For Each line As String In File.ReadAllLines(path)   ' Not a directory enumeration.
Next line
```

## 🔧 Code fix

```vb
For Each file As String In Directory.GetFiles(root)
    Thread.CurrentThread.Join(0) ' Pumps pending messages to prevent ContextSwitchDeadlock during long-running file system enumerations.
    Console.WriteLine(file)
Next file
```

The `Thread` type is written with the shortest name that resolves correctly (`Thread` with `Imports System.Threading`, otherwise `Threading.Thread`).

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
