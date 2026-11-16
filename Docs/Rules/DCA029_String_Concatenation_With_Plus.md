# ➕ DCA029 — String concatenated with `+`

Reports the `+` and `+=` operators used to concatenate strings.

- **Severity:** Warning
- **Category:** Style
- **Code fix:** ✔️ Replaces `+` with `&` and `+=` with `&=`
- **Equivalent to:** SonarQube [S1645](https://rules.sonarsource.com/vbnet/RSPEC-1645/) *"The '&' operator should be used to concatenate strings"*

---

## 💡 Why

In Visual Basic, `+` is the **addition** operator: it concatenates only when both operands are strings. With `Option Strict Off`, mixed operands are converted to numbers:

```vb
Dim a As String = "1" + 1     ' 2 (a number), not "11"
Dim b As String = "1" & 1     ' "11"
```

`&` **always concatenates** (it converts both operands to `String`), so it states the intent and never adds numbers by mistake. With `Option Strict On` the first line does not compile, but `+` between strings still hides the intent, and the code breaks as soon as one operand changes type.

## 🔎 Detection

`a + b` and `a += b` where both operands are typed as `String` or `Char` (at least one of them a `String`) and the result is a `String`. For those operands, `&` produces exactly the same value.

Not reported:

| Case | Reason |
|---|---|
| Numeric additions (`count + 1`) | They are additions |
| Operands typed as `Object` (late binding, `Option Strict Off`) | `+` may add numbers at run time; replacing it could change the result |

## ❌ Non-compliant

```vb
Dim fullName As String = firstName + " " + lastName
message += Environment.NewLine
```

## ✔️ Compliant

```vb
Dim fullName As String = firstName & " " & lastName
message &= Environment.NewLine
```

## 🔧 Code fix

Replaces the operator token. When the original operator had no surrounding spaces (`a+b`), spaces are added (`a & b`): in Visual Basic, `a&b` would be read as the type character `&` (Long) of `a`, and `&H` as the start of a hexadecimal literal.

> [!TIP]
> For longer texts, string interpolation (`$"{firstName} {lastName}"`) is even more readable.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA029.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA029 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA029
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA029:String concatenated with '+'", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA029:String concatenated with '+'", Justification:="Reason.", Scope:="module")>
```
