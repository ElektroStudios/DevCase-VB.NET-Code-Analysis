# ⏳ DCA001 — Missing `ConfigureAwait(False)`

Reports `Await` expressions whose awaited value exposes a `ConfigureAwait(Boolean)` method but is not configured.

- **Severity:** Error
- **Category:** Reliability
- **Code fix:** ✔️ Injects `.ConfigureAwait(False)`
- **Configuration:** `devcase_analyzers.configure_await_named_argument = true` emits `ConfigureAwait(continueOnCapturedContext:=False)`

---

## 💡 Why

Reusable library code must not capture the caller's synchronization context. If a consumer blocks synchronously on the returned task (typical in UI or legacy ASP.NET code), a continuation that tries to resume on the captured context deadlocks. Configuring every `Await` with `False` also avoids unnecessary context switches.

## 🔎 Detection

The analyzer first applies a fast syntactic check (the awaited expression already ends with `.ConfigureAwait(...)`), then asks the semantic model whether the awaited type, or one of its base types or generic constraints, declares an instance `ConfigureAwait(Boolean)` method. That excludes awaitables that cannot be configured (no false positives, no uncompilable fixes).

## ❌ Non-compliant

```vb
Public Async Function RunAsync() As Task
    Await Task.Delay(1000)
    Dim value As Integer = Await GetValueAsync()
    Dim other As Integer = Await New ValueTask(Of Integer)(5)
End Function

Public Async Function ProcessAsync(Of T As Task)(work As T) As Task
    Await work ' T is constrained to Task, so it can be configured.
End Function
```

## ✔️ Compliant / not reported

```vb
Await Task.Delay(1000).ConfigureAwait(False)
Await Task.Delay(1000).ConfigureAwait(True)       ' An explicit decision is respected (UI code, xUnit test methods).
Await Task.Yield()                                 ' YieldAwaitable has no ConfigureAwait.

Dim configured As ConfiguredTaskAwaitable = Task.Delay(1).ConfigureAwait(False)
Await configured                                   ' Already configured.
```

> [!TIP]
> In **xUnit** test methods (`<Fact>`, `<Theory>`), use `ConfigureAwait(True)`: xUnit's own rule **xUnit1030** warns against `ConfigureAwait(False)` there because it can bypass the parallelization limits, and DCA001 accepts the explicit `True`. Helper methods called by the tests should keep `ConfigureAwait(False)`.

## 🔧 Code fix

```vb
' Before
Await Task.Delay(1) ' keep this comment
Return Await GetValueAsync() + 1

' After
Await Task.Delay(1).ConfigureAwait(False) ' keep this comment
Return Await GetValueAsync().ConfigureAwait(False) + 1
```

Expressions that would change meaning when followed by a member access are parenthesized:

```vb
' Before
Await source?.GetTaskAsync()
Await Await GetNestedAsync()

' After
Await (source?.GetTaskAsync()).ConfigureAwait(False)
Await (Await GetNestedAsync().ConfigureAwait(False)).ConfigureAwait(False)
```

> [!NOTE]
> `Await x?.RunAsync().ConfigureAwait(False)` does not compile in VB.NET (the result is a `Nullable(Of ConfiguredTaskAwaitable)`), which is why the parentheses are required.

## ⚙️ Configuration

```ini
[*.vb]
devcase_analyzers.configure_await_named_argument = true
```

```vb
Await Task.Delay(1).ConfigureAwait(continueOnCapturedContext:=False)
```

## 🔕 How to disable

This rule is reported as an **error** by default. The directives below also suppress analyzer errors; to keep seeing it without blocking the Error List, lower it to `warning` or `suggestion` instead of disabling it.

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA001.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA001 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA001
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA001:Await expression is missing ConfigureAwait(False)", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA001:Await expression is missing ConfigureAwait(False)", Justification:="Reason.", Scope:="module")>
```
