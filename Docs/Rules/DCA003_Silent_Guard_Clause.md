# 🤫 DCA003 — Silent guard clause

Reports `If` statements that check a **parameter** and silently leave the member (`Return`, `Return Nothing`, `Exit Sub`, `Exit Function`, `Exit Property`).

- **Severity:** Warning
- **Category:** Reliability
- **Code fix:** ✔️ Throws `ArgumentNullException`, `ArgumentException` or `ArgumentOutOfRangeException` instead

---

## 💡 Why

A broken precondition hidden by a silent exit turns a caller bug into a mysterious "nothing happened". Fail-fast code throws an explicit argument exception that names the offending parameter.

## 🔎 Detection

The `If` must have no `Else`/`ElseIf`, contain exactly one silent exit statement, and every condition (possibly combined with `OrElse`/`Or`) must be one of these checks on a parameter of the enclosing method, property or constructor:

| Condition | Exception thrown by the fix |
|---|---|
| `p Is Nothing`, `Nothing Is p`, `IsNothing(p)`, `String.IsNullOrEmpty(p)`, `String.IsNullOrWhiteSpace(p)` | `ArgumentNullException(NameOf(p))` |
| `p IsNot Nothing` | `ArgumentException("Value must be Nothing.", NameOf(p))` |
| `p.Length = 0`, `p.Count < 1`, ... | `ArgumentException("Value cannot be empty.", NameOf(p))` |
| `p < 0`, `0 > p`, `p > limit`, `p = Nothing` (value type), ... | `ArgumentOutOfRangeException(NameOf(p))` |

## ❌ Non-compliant

```vb
Public Sub Save(value As Object)
    If value Is Nothing Then Return
End Sub

Public Sub Open(path As String)
    If String.IsNullOrWhiteSpace(path) Then
        Exit Sub
    End If
End Sub

Public Function Take(count As Integer) As Integer
    If count < 0 Then Exit Function
    Return count
End Function

Public WriteOnly Property Name As String
    Set(value As String)
        If value Is Nothing Then Exit Property
    End Set
End Property
```

## ✔️ Not reported

```vb
If value Is Nothing Then Return False     ' Returns a value: a Try-pattern result, not a silent exit.

If value Is Nothing Then                   ' More than one statement.
    Console.WriteLine("missing")
    Return
End If

Dim local As Object = Nothing
If local Is Nothing Then Return            ' Not a parameter.

Dim action As Action = Sub()
                           If value Is Nothing Then Return   ' Inside a lambda.
                       End Sub

Public Function TryGet(input As String, ByRef result As Object) As Object
    result = Nothing
    If result Is Nothing Then Return Nothing   ' ByRef (output) parameter.
End Function

Public Sub Reset(value As Object)
    value = New Object()
    If value Is Nothing Then Return            ' Reassigned before the check.
End Sub
```

## 🔧 Code fix

```vb
' Before
If value Is Nothing Then Return
If items.Length = 0 Then Return Nothing

' After
If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
If items.Length = 0 Then Throw New ArgumentException("Value cannot be empty.", NameOf(items))
```

Combined checks are split, so every exception names its own parameter:

```vb
' Before
If first Is Nothing OrElse String.IsNullOrEmpty(second) Then Return

' After
If first Is Nothing Then
    Throw New ArgumentNullException(NameOf(first))
End If

If String.IsNullOrEmpty(second) Then
    Throw New ArgumentNullException(NameOf(second))
End If
```

## 🔕 How to disable

> [!NOTE]
> **Why Warning and not Error?** Until version 1.0.4 this rule was an error. A silent guard clause is a design problem, not code that fails to compile or always fails at run time, and existing code bases can have many of them: as an error, it blocked the Error List of projects that were otherwise correct. It is now a Warning; projects that want it to stop the build can raise it again (`dotnet_diagnostic.DCA003.severity = error` in `.editorconfig`).

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA003.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA003 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA003
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA003:Silent guard clause on a parameter", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA003:Silent guard clause on a parameter", Justification:="Reason.", Scope:="module")>
```
