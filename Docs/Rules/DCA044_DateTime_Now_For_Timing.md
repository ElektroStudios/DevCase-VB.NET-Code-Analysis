# ⏲️ DCA044 — `DateTime.Now` used to measure elapsed time

Reports elapsed-time measurements made with the system clock: `DateTime.Now - start` or `DateTime.Now.Subtract(start)`, where `start` is a local variable that received the current time in the same method.

- **Severity:** Info
- **Category:** Reliability
- **Code fix:** ❌ (the measurement must be rewritten with `Stopwatch`)
- **Equivalent to:** SonarQube [S6561](https://rules.sonarsource.com/vbnet/RSPEC-6561/) *"Avoid using "DateTime.Now" for benchmarking or timing operations"*

---

## 💡 Why

- **Resolution:** `DateTime.Now` usually advances in steps of about 15 ms, so short operations measure 0 or 15 ms.
- **Jumps:** the system clock changes with daylight saving time (`Now`, one hour), time synchronization and manual changes. A measurement may be negative or one hour longer.
- **Cost:** `DateTime.Now` converts to local time on every call.

`Stopwatch` uses a **monotonic, high-resolution** counter that is not affected by clock changes.

## 🔎 Detection

`DateTime.Now`, `DateTime.UtcNow` and the Visual Basic `Now` property. A subtraction is reported only when its right operand is a local variable that is initialized or assigned with one of them in the same method.

Subtractions with other dates are not reported: `deadline - DateTime.Now` (time remaining) and `DateTime.Now - file.LastWriteTime` (age of a file) are not elapsed-time measurements.

## ❌ Non-compliant

```vb
Dim start As Date = DateTime.Now
Me.ProcessFiles()
Dim elapsed As TimeSpan = DateTime.Now - start
Me.StatusLabel.Text = $"Done in {elapsed.TotalSeconds:N1} s"
```

## ✔️ Compliant

```vb
Dim watch As Stopwatch = Stopwatch.StartNew()
Me.ProcessFiles()
Me.StatusLabel.Text = $"Done in {watch.Elapsed.TotalSeconds:N1} s"
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA044.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA044 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA044
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA044:DateTime.Now used to measure elapsed time", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA044:DateTime.Now used to measure elapsed time", Justification:="Reason.", Scope:="module")>
```
