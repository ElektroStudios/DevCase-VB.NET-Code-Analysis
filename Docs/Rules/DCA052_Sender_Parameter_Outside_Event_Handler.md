# 📛 DCA052 — Parameter named `sender` outside an event handler

Reports parameters named `sender` in methods that are not event handlers, such as `Public Function ToPen(sender As SolidBrush) As Pen`.

- **Severity:** Info
- **Category:** Naming
- **Code fix:** ✔️ Renames the parameter after its type (`brush`), in the whole solution

---

## 💡 Why

In .NET, `sender` has one meaning: the object that **raises an event**. It is typed `Object` and followed by the event arguments: `Sub Button1_Click(sender As Object, e As EventArgs)`. It appears in every event handler, so it is easy to copy it into other methods as if it were a generic name. There it hides what the argument really is:

```vb
Public Function ToPen(sender As SolidBrush) As Pen   ' Who sends what? It is just a brush.
```

## 🔎 Detection

A parameter named `sender` (in any letter case) of a method or constructor with a body.

Not reported, because the method can be an event handler or a callback whose signature is defined elsewhere:

| Case | Reason |
|---|---|
| `sender As Object` | The convention of every event and callback delegate (`EventHandler`, `RemoteCertificateValidationCallback`...) |
| Methods with a `Handles` clause | Event handlers |
| Methods with another parameter whose type derives from `EventArgs` (`sender As Button, e As EventArgs`) | Event handlers with a typed sender |
| Methods whose name appears after `AddressOf` anywhere in the project | Handlers of custom events with a typed sender (`Event Changed(sender As MyControl, value As Integer)`), attached with `AddHandler` |
| `Overrides` and `Implements` methods | The parameter name is defined by the base member |
| `sender As T` (a type parameter) | No better name can be derived |

> [!NOTE]
> A method used as a handler **only from another project** (with `AddressOf` there) is not known here. That combination, a typed `sender` that is not `Object`, with no `EventArgs` parameter, used from another assembly, is very unusual; suppress the diagnostic if it happens.

## ❌ Non-compliant

```vb
Public Function ToPen(sender As Drawing.SolidBrush) As Drawing.Pen

    If sender Is Nothing Then
        Throw New ArgumentNullException(NameOf(sender))
    End If

    Return New Drawing.Pen(sender.Color)

End Function
```

## ✔️ Compliant

```vb
Public Function ToPen(brush As Drawing.SolidBrush) As Drawing.Pen

    If brush Is Nothing Then
        Throw New ArgumentNullException(NameOf(brush))
    End If

    Return New Drawing.Pen(brush.Color)

End Function
```

## 🔧 Code fix

Renames the parameter with Visual Studio's rename engine. Every reference in the solution is updated: the body, `NameOf`, named arguments of callers (`ToPen(sender:=b)` → `ToPen(brush:=b)`) and the XML documentation (`<param name="...">`, `<paramref name="..."/>`).

The new name is derived from the type of the parameter:

| Type | Name |
|---|---|
| `SolidBrush` | `brush` (the last word of the name) |
| `HTMLDocument` | `document` |
| `IMailAddress` (interface) | `address` |
| `Point()` (array) | `points` |
| `String`, `Integer`, `Date` and other primitive types | `value` (`values` for arrays) |

When the name is already used in the method (another parameter, a local, or a member used without `Me.`), a number is appended (`brush2`), so that no identifier changes what it refers to.

> [!NOTE]
> Renaming a parameter of a `Public` method is a breaking change only for callers in **other** solutions that use named arguments (`sender:=`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA052.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA052 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA052
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "DCA052:Parameter named 'sender' outside an event handler", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "DCA052:Parameter named 'sender' outside an event handler", Justification:="Reason.", Scope:="module")>
```
