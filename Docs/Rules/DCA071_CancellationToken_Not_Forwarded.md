# 📨 DCA071 — `CancellationToken` parameter not forwarded

**.NET Framework only.** Reports calls that could receive the `CancellationToken` of the enclosing method but do not, such as `Await Task.Delay(500)` in a method with a `cancellationToken` parameter.

- **Severity:** Info
- **Category:** Reliability
- **Code fix:** ✔️ Passes the token (named when needed)
- **Equivalent to:** Microsoft [CA2016](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2016) *"Forward the CancellationToken parameter to methods that take one"*

---

## 💡 Why

A method that accepts a `CancellationToken` promises to stop when it is cancelled. Calls that do not receive the token keep running (a delay, a read, a request) after the cancellation, so the operation stops late or not at all, and its resources stay busy.

On modern .NET, the built-in rule CA2016 already reports this code, so this rule stays silent there.

## 🔎 Detection

The enclosing method, or the innermost enclosing lambda, must have **exactly one** `CancellationToken` parameter (a lambda without one is not analyzed, even inside a method that has one: its code may run outside the operation). A call is reported when:

- it omits an optional `CancellationToken` parameter of the method (`Optional token As CancellationToken = Nothing`), or
- the type of the method declares an accessible overload with the same parameters and a `CancellationToken` at the end (extension methods are not searched for overloads).

Calls that already pass a token (`CancellationToken.None` included: it says that not forwarding is intended), and calls that expand a `ParamArray`, are not reported. The rule is only reported when the call with the token binds to the expected method.

## ❌ Non-compliant

```vb
Public Async Function DownloadAsync(stream As Stream, buffer As Byte(), cancellationToken As CancellationToken) As Task
    Await Task.Delay(500).ConfigureAwait(False)
    Dim read As Integer = Await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(False)
End Function
```

## ✔️ Compliant

```vb
Public Async Function DownloadAsync(stream As Stream, buffer As Byte(), cancellationToken As CancellationToken) As Task
    Await Task.Delay(500, cancellationToken).ConfigureAwait(False)
    Dim read As Integer = Await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(False)
End Function
```

## 🔧 Code fix

The fix appends the token after the last argument. It uses a named argument (`token:=cancellationToken`) when the call has named or omitted arguments, or when a positional argument would not reach the parameter (an optional parameter before it is omitted):

| Code | Fixed code |
|---|---|
| `Task.Delay(500)` | `Task.Delay(500, cancellationToken)` |
| `gate.Wait()` / `gate.Wait` | `gate.Wait(cancellationToken)` |
| `Me.SaveAsync("a")` (with `Optional retries`, `Optional token`) | `Me.SaveAsync("a", token:=cancellationToken)` |
| `Me.SaveAsync("a", retries:=2)` | `Me.SaveAsync("a", retries:=2, token:=cancellationToken)` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA071.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA071 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA071
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA071:CancellationToken parameter not forwarded", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA071:CancellationToken parameter not forwarded", Justification:="Reason.", Scope:="module")>
```
