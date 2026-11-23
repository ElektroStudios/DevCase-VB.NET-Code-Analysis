# 🔁 DCA024 — `Catch` block that only rethrows

Reports `Catch` blocks whose only statement is `Throw` (or `Throw ex` with the caught exception).

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ✔️ Removes the useless `Catch` blocks, and the whole `Try` statement when nothing else remains
- **Equivalent to:** SonarQube S2737 *"catch clauses should do more than rethrow"*

---

## 💡 Why

```vb
Try
    ProcessFile(path)
Catch ex As Exception
    Throw
End Try
```

This `Try` does nothing: without it, the exception propagates in exactly the same way and with the same stack trace. It only adds noise and nesting, and it suggests error handling that does not exist. It is usually left over after removing a log line or a breakpoint.

## 🔎 Detection

Only the **trailing** rethrow-only blocks of a `Try` statement are reported, with one diagnostic per `Try`. A rethrow-only block **followed by another `Catch`** is not useless:

```vb
Try
    Save()
Catch ex As OperationCanceledException
    Throw                          ' NOT reported: it prevents the next block from logging cancellations.
Catch ex As Exception
    Log(ex)
End Try
```

`Throw ex`, where `ex` is the caught exception, is reported too. It rethrows the same exception object, so the block adds nothing, and on top of that it **discards the original stack trace**: the exception appears to originate in the `Catch` block. Removing the block is strictly better.

```vb
Try
    Load(path)
Catch ex As Exception
    Throw ex                       ' Reported: same exception, but the stack trace now starts here.
End Try
```

> [!NOTE]
> Microsoft's **CA2200** reports `Throw ex` and suggests changing it to `Throw`. DCA024 goes one step further: a block that only rethrows is useless in either form, so it removes the block.

Not reported:

- Blocks with a `When` filter: the filter may have side effects (logging) or be part of the logic.
- `Throw New SomeException("...", ex)`: wrapping the exception is meaningful.
- Blocks with other statements.
- Blocks with conditional code in their body (`#If DEBUG Then` ... `#End If` before or after the `Throw`): in another configuration, the block does more than rethrow the exception.

## ❌ Non-compliant

```vb
Try
    Dim data As String = File.ReadAllText(path)
    Parse(data)
Catch ex As Exception
    Throw
End Try
```

## ✔️ Compliant

```vb
Dim data As String = File.ReadAllText(path)
Parse(data)
```

## 🔧 Code fix

| Situation | Result |
|---|---|
| Other `Catch` blocks remain, or there is a `Finally` | Only the useless blocks are removed (`Try ... Finally` stays) |
| Nothing else remains | The `Try` statement is replaced by its body, re-indented |

The `Try` statement is **not** unwrapped, and no fix is offered, when:

- Its body contains `Exit Try`, which would be invalid outside the block.
- It declares a local variable whose name is also declared elsewhere in the enclosing block. The `Try` block is its own scope, so unwrapping would cause a name conflict.

The preprocessor directives are kept. An `#If` block at the end of the `Try` body, whose `#End If` line is just before the `Catch` keyword, stays around the same statements when the `Try` statement is unwrapped, and its code is re-indented with the rest of the body (also when the condition is false and the code is disabled):

```vb
Try
    Save()
#If DEBUG Then
    Log("Saved")
#End If
Catch ex As Exception
    Throw
End Try
```

becomes:

```vb
Save()
#If DEBUG Then
Log("Saved")
#End If
```

In the same way, the `#If` / `#End If` lines before a removed `Catch` block (the end of the conditional code of the previous block) stay before `End Try`.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA024.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA024 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA024
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA024:Catch block only rethrows the exception", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA024:Catch block only rethrows the exception", Justification:="Reason.", Scope:="module")>
```
