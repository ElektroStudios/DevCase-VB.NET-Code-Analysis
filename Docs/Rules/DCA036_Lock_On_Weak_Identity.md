# 🔐 DCA036 — `SyncLock` on a publicly accessible or weak-identity object

Reports `SyncLock` statements on objects that code outside the type can also lock.

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ❌ (a dedicated lock field must be added and shared by every `SyncLock` that protects the same data)
- **Equivalent to:** SonarQube [S2551](https://rules.sonarsource.com/vbnet/RSPEC-2551/) *"Shared resources should not be used for locking"* and [S3998](https://rules.sonarsource.com/vbnet/RSPEC-3998/) *"Threads should not lock on objects with weak identity"*

---

## 💡 Why

A lock only works if **nobody else** locks the same object for another purpose. When the object is visible outside the type, any other code can lock it, causing contention and **deadlocks** that depend on unrelated code:

- `Me`: every caller has the reference.
- `String`: literals are **interned**, so `SyncLock "lock"` locks the same object as every other `"lock"` literal in the process, in any library.
- `Type` (`GetType(T)`), `MemberInfo`, `ParameterInfo`: the same object is returned everywhere in the process.
- `Thread`, and `MarshalByRefObject` (forms, controls, streams...): they can cross application domains, and their identity is not reliable.

## 🔎 Detection

`SyncLock` on `Me`, `MyBase` or `MyClass`, or on an expression whose type is (or derives from) `String`, `Type`, `MemberInfo`, `ParameterInfo`, `Thread`, `MarshalByRefObject`, `OutOfMemoryException`, `StackOverflowException` or `ExecutionEngineException`.

## ❌ Non-compliant

```vb
Public Sub Add(item As String)
    SyncLock Me
        Me.items.Add(item)
    End SyncLock
End Sub

Public Shared Sub Log(text As String)
    SyncLock GetType(Logger)
        File.AppendAllText(LogPath, text, Encoding.UTF8)
    End SyncLock
End Sub
```

## ✔️ Compliant

```vb
Private ReadOnly itemsLock As New Object()
Private Shared ReadOnly LogLock As New Object()

Public Sub Add(item As String)
    SyncLock Me.itemsLock
        Me.items.Add(item)
    End SyncLock
End Sub

Public Shared Sub Log(text As String)
    SyncLock LogLock
        File.AppendAllText(LogPath, text, Encoding.UTF8)
    End SyncLock
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA036.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA036 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA036
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA036:SyncLock on a publicly accessible or weak-identity object", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA036:SyncLock on a publicly accessible or weak-identity object", Justification:="Reason.", Scope:="module")>
```
