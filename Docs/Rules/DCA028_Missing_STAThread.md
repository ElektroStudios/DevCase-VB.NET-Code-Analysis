# 🧵 DCA028 — Windows Forms entry point without `<STAThread>`

Reports a custom `Sub Main` entry point that is not marked with `<STAThread>`, in projects that reference Windows Forms.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Adds `<STAThread>`
- **Equivalent to:** SonarQube [S4210](https://rules.sonarsource.com/vbnet/RSPEC-4210/) *"Windows Forms entry points should be marked with STAThread"*

---

## 💡 Why

Windows Forms needs a **single-threaded apartment** (STA) thread. Without `<STAThread>`, the main thread of a program with a custom `Sub Main` may be initialized as MTA, and then:

- the **clipboard** (`Clipboard.SetText`) throws `ThreadStateException`;
- **drag and drop** (`AllowDrop = True`) throws `InvalidOperationException`;
- **common dialogs** (`OpenFileDialog`, `FolderBrowserDialog`) hang or throw;
- **ActiveX/COM controls** (`WebBrowser`, Windows Media Player) fail.

The Visual Basic application framework (*Enable application framework* in the project properties) generates its own `Sub Main`, which is already marked. The problem appears when you disable it and write your own `Sub Main`.

## 🔎 Detection

A `Shared` (or `Module`) method named `Main`, without type parameters, that returns nothing or `Integer`, and takes no parameters or one `String()` parameter: the shape of a Visual Basic entry point. The rule runs only when the project references `System.Windows.Forms`.

Not reported:

| Case | Reason |
|---|---|
| `<MTAThread>` | An explicit, visible decision |
| Projects without Windows Forms | Console and service programs do not need STA |
| Instance methods or methods with other parameters named `Main` | They cannot be entry points |

## ❌ Non-compliant

```vb
Public Module Program
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.Run(New MainForm())
    End Sub
End Module
```

## ✔️ Compliant

```vb
Public Module Program
    <STAThread>
    Public Sub Main()
        Application.EnableVisualStyles()
        Application.Run(New MainForm())
    End Sub
End Module
```

## 🔧 Code fix

Adds `<STAThread>` on its own line, before the existing attributes and modifiers (the documentation comment stays where it is). The attribute is written with the shortest name that resolves correctly.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA028.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA028 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA028
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA028:Windows Forms entry point without <STAThread>", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA028:Windows Forms entry point without <STAThread>", Justification:="Reason.", Scope:="module")>
```
