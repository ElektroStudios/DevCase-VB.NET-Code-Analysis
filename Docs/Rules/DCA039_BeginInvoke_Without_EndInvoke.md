# 📮 DCA039 — Delegate `BeginInvoke` without `EndInvoke`

Reports calls to the `BeginInvoke` method of a delegate that are not paired with `EndInvoke`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (the asynchronous call must be redesigned, usually with `Task.Run`)
- **Equivalent to:** SonarQube [S4583](https://rules.sonarsource.com/vbnet/RSPEC-4583/) *"Calls to delegate's method "BeginInvoke" should be paired with calls to "EndInvoke""*

---

## 💡 Why

The asynchronous programming model (APM) requires `EndInvoke` after every `BeginInvoke`:

- it returns the **result** of the call;
- it rethrows the **exception** of the call: without `EndInvoke`, errors are silently lost;
- it releases the **resources** of the call (the wait handle of the `IAsyncResult`).

> [!IMPORTANT]
> Delegate `BeginInvoke`/`EndInvoke` only work on .NET Framework: on modern .NET they throw `PlatformNotSupportedException`. `Task.Run` replaces them on every platform.

## 🔎 Detection

Only **delegate** types are analyzed: `Control.BeginInvoke` and `Dispatcher.BeginInvoke` marshal calls to the UI thread and do not require `EndInvoke`.

A call is accepted when `EndInvoke` is called in the same method or lambda, or in the callback passed to `BeginInvoke` (a lambda, or a method of the same project referenced with `AddressOf`). Calls whose callback is any other expression (a variable, a field) are not reported, because the callback cannot be inspected.

## ❌ Non-compliant

```vb
Me.work.BeginInvoke(Nothing, Nothing) ' Fire and forget: exceptions are lost.
```

## ✔️ Compliant

```vb
Me.work.BeginInvoke(Sub(result As IAsyncResult) Me.OnCompleted(Me.work.EndInvoke(result)), Nothing)

' Better, on every platform:
Dim value As Integer = Await Task.Run(Me.work).ConfigureAwait(False)
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA039.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA039 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA039
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA039:Delegate BeginInvoke without EndInvoke", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA039:Delegate BeginInvoke without EndInvoke", Justification:="Reason.", Scope:="module")>
```
