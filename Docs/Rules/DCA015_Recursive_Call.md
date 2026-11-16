# 🔄 DCA015 — Method calls itself

Reports direct recursion: methods that invoke themselves, and property accessors that access their own property in the same direction.

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ❌ None (the iterative rewrite depends on the algorithm)

---

## 💡 Why

Deep inputs (directory trees, JSON documents, linked structures) can overflow the stack, and a `StackOverflowException` cannot be caught: it kills the process. Accidental self-calls, such as a getter that returns itself instead of its backing field, cause infinite recursion. Tree walks should use an explicit `Stack` or `Queue`.

## ❌ Non-compliant

```vb
Public Function Count() As Integer
    Dim total As Integer = 1
    For Each child As Node In Me.Children
        total += child.Count()           ' Recursion through another instance.
    Next child
    Return total
End Function

Public Function Factorial(n As Integer) As Long
    Return If(n <= 1, 1, n * Factorial(n - 1))
End Function

Public Property Value As Integer
    Get
        Return Me.Value                  ' Infinite recursion: should read the backing field.
    End Get
    Set(value As Integer)
        Me.Value = value                 ' Infinite recursion.
    End Set
End Property
```

## ✔️ Not reported

```vb
Public Overrides Sub Run()
    MyBase.Run()                         ' Base member: a different method.
    Dim action As Action = Sub() Me.Run() ' Inside a lambda: runs later, as another method.
    Dim handler As Action = AddressOf Me.Run
End Sub

Public Sub Print(value As Integer)
    Print(value, 1)                      ' Another overload.
End Sub

Public Function Compute() As Integer
    Compute = 5                          ' The function name as return variable.
    Return Compute
End Function
```

## ✔️ Iterative alternative

```vb
Public Function Count() As Integer
    Dim total As Integer = 0
    Dim pending As New Stack(Of Node)()
    pending.Push(Me)
    While pending.Count > 0
        Dim current As Node = pending.Pop()
        total += 1
        For Each child As Node In current.Children
            pending.Push(child)
        Next child
    End While
    Return total
End Function
```

> [!NOTE]
> Indirect recursion (A → B → A) is not detected.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA015.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA015 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA015
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA015:Method calls itself", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA015:Method calls itself", Justification:="Reason.", Scope:="module")>
```
