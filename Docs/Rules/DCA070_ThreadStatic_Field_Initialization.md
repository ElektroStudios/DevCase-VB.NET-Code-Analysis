# 🧶 DCA070 — `ThreadStatic` field initialized inline or in a `Shared` constructor

**.NET Framework only.** Reports `<ThreadStatic>` fields with an initializer (`As New StringBuilder()`, `= 5`) and their assignments in the `Shared` constructor of their type.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (report only)
- **Equivalent to:** Microsoft [CA2019](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2019) *"ThreadStatic fields should not use inline initialization"*

---

## 💡 Why

A thread-static field has one value per thread. But its initializer, like the rest of the `Shared` constructor, runs **once**, on the thread that first uses the type: that thread gets the value, and **every other thread sees `Nothing` (or zero)**. Code that works in a single-threaded test fails with a `NullReferenceException` as soon as another thread uses it.

On modern .NET, the built-in rule CA2019 already reports this code, so this rule stays silent there.

## 🔎 Detection

`Shared` fields (also the fields of a `Module`, which are `Shared`) marked with `<ThreadStatic>` that have an initializer or an `As New` clause (reported on the field name), and assignments to them inside the `Shared` constructor of their type (reported on the assignment). On an instance field, `<ThreadStatic>` has no effect, so it is not analyzed.

## ❌ Non-compliant

```vb
<ThreadStatic>
Private Shared buffer As New StringBuilder()
```

## ✔️ Compliant

```vb
<ThreadStatic>
Private Shared buffer As StringBuilder

Private Shared Function GetBuffer() As StringBuilder
    If buffer Is Nothing Then
        buffer = New StringBuilder()
    End If
    Return buffer
End Function
```

## 🔧 Code fix

There is no code fix: the field has to be initialized lazily where it is read, which depends on how the code uses it (`ThreadLocal(Of T)` is an alternative).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA070.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA070 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA070
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA070:ThreadStatic field initialized inline or in a Shared constructor", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA070:ThreadStatic field initialized inline or in a Shared constructor", Justification:="Reason.", Scope:="module")>
```
