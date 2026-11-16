# 🌀 DCA035 — `Application.DoEvents` inside a loop

Reports calls to `Application.DoEvents` (Windows Forms) and `My.Application.DoEvents` inside a loop.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the work must be moved to a background task)
- **Related:** [DCA014](DCA014_FileSystem_Loop_Without_Message_Pumping.md) (message pumping in file-system loops)

---

## 💡 Why

`DoEvents` keeps the window responsive by processing **every pending message** in the middle of the loop. That includes user input, so the loop becomes **reentrant**:

- the user can click the same button again and start the same operation **inside** the running one;
- the user can close the form while the loop still uses its controls (`ObjectDisposedException`);
- any event handler can change the data that the loop is processing.

These bugs are intermittent and very hard to reproduce. Running the work in the background (`Await Task.Run(...)`, `BackgroundWorker`) keeps the window responsive without reentrancy, and `IProgress(Of T)` reports the progress to the UI.

## 🔎 Detection

Calls inside `For`, `For Each`, `While` and `Do` loops of the same method or lambda. A call inside a lambda is reported only when the loop is inside that lambda too.

## ❌ Non-compliant

```vb
Private Sub ProcessButton_Click(sender As Object, e As EventArgs) Handles ProcessButton.Click
    For Each file As String In Me.files
        Me.ProcessFile(file)
        Me.StatusLabel.Text = file
        Application.DoEvents()
    Next file
End Sub
```

## ✔️ Compliant

```vb
Private Async Sub ProcessButton_Click(sender As Object, e As EventArgs) Handles ProcessButton.Click
    Me.ProcessButton.Enabled = False
    Dim progress As New Progress(Of String)(Sub(file As String) Me.StatusLabel.Text = file)
    Await Task.Run(Sub()
                       For Each file As String In Me.files
                           Me.ProcessFile(file)
                           DirectCast(progress, IProgress(Of String)).Report(file)
                       Next file
                   End Sub).ConfigureAwait(True)
    Me.ProcessButton.Enabled = True
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA035.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA035 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA035
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA035:Application.DoEvents inside a loop", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA035:Application.DoEvents inside a loop", Justification:="Reason.", Scope:="module")>
```
