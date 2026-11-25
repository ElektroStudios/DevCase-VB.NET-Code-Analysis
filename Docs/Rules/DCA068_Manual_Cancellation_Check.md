# 🛑 DCA068 — `IsCancellationRequested` check that throws `OperationCanceledException`

**.NET Framework only.** Reports `If token.IsCancellationRequested Then Throw New OperationCanceledException()`, which `token.ThrowIfCancellationRequested()` does in one call.

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Uses `token.ThrowIfCancellationRequested()`
- **Equivalent to:** Microsoft [CA2250](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2250) *"Use ThrowIfCancellationRequested"*

---

## 💡 Why

`ThrowIfCancellationRequested` is the standard way to stop a cancelled operation: it is shorter, it cannot be written with the wrong exception, and its `OperationCanceledException` carries the token, so the caller can tell its own cancellation from another one (`ex.CancellationToken = token`).

On modern .NET, the built-in rule CA2250 already reports this code, so this rule stays silent there.

## 🔎 Detection

An `If` statement (multi-line or single-line) without `ElseIf` or `Else` parts, whose condition is `token.IsCancellationRequested` and whose only statement is `Throw New OperationCanceledException()` or `Throw New OperationCanceledException(token)` with the same token. Not reported:

| Case | Reason |
|---|---|
| Other statements in the `If` block (logging, cleanup) | They would be lost |
| `Throw New OperationCanceledException("message")` | The message would be lost |
| A derived exception (`TaskCanceledException`) | `ThrowIfCancellationRequested` throws the base type |
| An `Else` part | It is the code that runs when there is no cancellation |
| A token expression with side effects (`GetToken().IsCancellationRequested`) | It would be evaluated differently |
| Preprocessor directives inside the statement | Another configuration may compile other statements |

## ❌ Non-compliant

```vb
If cancellationToken.IsCancellationRequested Then
    Throw New OperationCanceledException()
End If
```

## ✔️ Compliant

```vb
cancellationToken.ThrowIfCancellationRequested()
```

## 🔧 Code fix

The fix replaces the whole `If` statement with the call. The comments at the end of its lines (`If ... Then ' Canceled?`) are kept after the call, and the comments on lines of their own are kept before it. When a comment follows code in the middle of the statement (after a `_` line continuation), the fix is not offered, so that no comment is lost.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA068.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA068 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA068
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA068:IsCancellationRequested check that throws OperationCanceledException", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA068:IsCancellationRequested check that throws OperationCanceledException", Justification:="Reason.", Scope:="module")>
```
