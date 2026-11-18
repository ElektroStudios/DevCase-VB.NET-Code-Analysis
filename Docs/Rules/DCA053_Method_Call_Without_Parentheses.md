# 🫙 DCA053 — Method call without parentheses

Reports method calls written without parentheses, such as `query.ToArray` or `Me.Close`.

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Adds `()` after the method name

---

## 💡 Why

Visual Basic allows calling a method without arguments without writing the parentheses. The call then looks exactly like a property or field access:

```vb
UtilMail.MailTo((From m As MailAddress In addresses Select m.Address).ToArray, subject, body)
```

`ToArray` is not a value that is read: it is code that runs, allocates a new array and copies every element. The parentheses make that visible, and they are the form used by the documentation and by every other .NET language.

## 🔎 Detection

The analysis is semantic: only explicit calls to **methods** are reported, so it never confuses them with properties, fields or events. Reported forms:

| Form | Example |
|---|---|
| Statement and `Call` | `Me.Close`, `Initialize`, `Call Work` |
| Member access | `text.Trim`, `items.ToArray` |
| Null-conditional access | `text?.GetHashCode` |
| Extension methods, also generic | `values.Count`, `values.Cast(Of Integer)` |
| `Await` operand | `Await Me.LoadAsync` |

Not reported:

| Case | Reason |
|---|---|
| Properties: `text.Length`, `list.Count`, `Me.Name` | They are not method calls (see the note below) |
| `AddressOf Me.Work`, `NameOf(Work)` | The method is referenced, not called |
| `GetItems(0)` | The parentheses are there; they hold the index applied to the result |
| `For Each` (`GetEnumerator`), `Using` (`Dispose`), `Await` (`GetAwaiter`), queries (`Where`, `Select`), collection initializers (`Add`) | The compiler generates those calls; they are not written in the code |
| Late-bound calls (`Option Strict Off`) | It is unknown whether the member is a method |

> [!NOTE]
> **Properties are the opposite case.** A property without parameters is written without parentheses (`Dim name As String = Me.Name`): writing `Me.Name()` compiles, but it makes a property access look like a method call, and the documentation never writes it that way. So this rule does not add parentheses to properties.

## ❌ Non-compliant

```vb
UtilMail.MailTo((From m As MailAddress In addresses Select m.Address).ToArray, subject.Trim, body)
Me.Close
```

## ✔️ Compliant

```vb
UtilMail.MailTo((From m As MailAddress In addresses Select m.Address).ToArray(), subject.Trim(), body)
Me.Close()
```

## 🔧 Code fix

Adds `()` right after the method name, and after its type arguments (`values.Cast(Of Integer)` → `values.Cast(Of Integer)()`). It never changes behavior: with or without parentheses, the compiler calls the same method. Fix All adds the parentheses in the whole document, project or solution.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA053.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA053 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA053
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA053:Method call without parentheses", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA053:Method call without parentheses", Justification:="Reason.", Scope:="module")>
```
