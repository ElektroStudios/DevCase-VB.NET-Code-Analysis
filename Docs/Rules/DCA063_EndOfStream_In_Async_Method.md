# 🌊 DCA063 — `StreamReader.EndOfStream` in an `Async` method

**.NET Framework only.** Reports `StreamReader.EndOfStream` read in an `Async` method or lambda, typically in `While Not reader.EndOfStream` loops around `ReadLineAsync`.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (report only)
- **Equivalent to:** Microsoft [CA2024](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2024) *"Do not use StreamReader.EndOfStream in async methods"*

---

## 💡 Why

`EndOfStream` is a synchronous property. When the buffer of the reader is empty, it has to know whether more data exists, so it **reads the underlying stream synchronously**: the thread of the `Async` method blocks on the disk or the network, which is exactly what the asynchronous reads were meant to avoid (in a UI application, the window freezes; on a server, a thread-pool thread is wasted). `ReadLineAsync` already returns `Nothing` at the end of the stream, so the loop can test its result instead of asking first.

On modern .NET, the built-in rule CA2024 already reports this code, so this rule stays silent there.

## 🔎 Detection

Any read of `EndOfStream` on a `StreamReader` (or a derived type) inside the nearest enclosing `Async` method or lambda. A synchronous lambda inside an `Async` method is not reported, because it does not run in the asynchronous code path of the method.

## ❌ Non-compliant

```vb
Public Async Function CountLinesAsync(reader As StreamReader) As Task(Of Integer)
    Dim count As Integer = 0
    While Not reader.EndOfStream
        Await reader.ReadLineAsync().ConfigureAwait(False)
        count += 1
    End While
    Return count
End Function
```

## ✔️ Compliant

```vb
Public Async Function CountLinesAsync(reader As StreamReader) As Task(Of Integer)
    Dim count As Integer = 0
    Dim line As String = Await reader.ReadLineAsync().ConfigureAwait(False)
    While line IsNot Nothing
        count += 1
        line = Await reader.ReadLineAsync().ConfigureAwait(False)
    End While
    Return count
End Function
```

## 🔧 Code fix

There is no code fix: the loop has to be restructured around the result of `ReadLineAsync` (or `ReadAsync`), which changes where the line is read.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA063.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA063 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA063
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA063:StreamReader.EndOfStream in an Async method", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA063:StreamReader.EndOfStream in an Async method", Justification:="Reason.", Scope:="module")>
```
