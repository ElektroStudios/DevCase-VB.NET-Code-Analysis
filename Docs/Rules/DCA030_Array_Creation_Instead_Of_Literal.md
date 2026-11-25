# 📦 DCA030 — Array creation expression can be an array literal

Reports array creation expressions with an initializer (`New Integer() {1, 2}`) that can be written as an array literal (`{1, 2}`) without changing the type of the array.

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Removes `New T()` and keeps the initializer
- **Equivalent to:** SonarQube [S2355](https://rules.sonarsource.com/vbnet/RSPEC-2355/) *"Array literals should be used instead of array creation expressions"*

---

## 💡 Why

When the type of the target is already written (`Dim values As Integer() = ...`), repeating it in `New Integer()` adds noise. The array literal takes its type from the target and produces exactly the same array.

## 🔎 Detection

Only where the target type is explicit and is **exactly** the created array type, so that the literal produces the same array:

| Position | Example |
|---|---|
| Initializer of a variable, field or property declared with `As` | `Dim values As Integer() = New Integer() {1, 2}` |
| Right side of an assignment | `values = New Integer() {3}` |
| `Return` statement of a method | `Return New Integer() {1, 2}` (the method returns `Integer()`) |

Not reported:

| Case | Reason |
|---|---|
| Arguments (`M(New Integer() {1})`) | An array literal may select another overload |
| Conversions (`Dim items As Object() = New String() {"a"}`) | The literal would create an `Object()` instead of a `String()` |
| `Return` inside a lambda | The return type of the lambda may be inferred from the expression |
| Declarations without `As` (`Option Infer On`) | The literal would infer another element type (`New Long() {1}` → `Integer()`) |
| Bounds (`New Integer(9) {}`), multidimensional and jagged arrays | They cannot be written as a simple literal |
| Empty arrays (`New Integer() {}`) | An empty array should not be allocated at all: `Array.Empty(Of Integer)()` (rule [DCA055](DCA055_Zero_Length_Array_Allocation.md) on .NET Framework, CA1825 on modern .NET) |

## ❌ Non-compliant

```vb
Private ReadOnly Extensions As String() = New String() {".jpg", ".png"}

Public Function GetSizes() As Long()
    Return New Long() {1024, 2048}
End Function
```

## ✔️ Compliant

```vb
Private ReadOnly Extensions As String() = {".jpg", ".png"}

Public Function GetSizes() As Long()
    Return {1024, 2048}
End Function
```

## 🔧 Code fix

Removes `New T()` and keeps the initializer. The elements keep their type: `{1024, 2048}` in a `Long()` context creates a `Long()` array.

A creation wrapped in redundant parentheses (`Return (New Long() {1, 2})`) is also reported, and the fix removes the parentheses as well (`Return {1, 2}`): parentheses break the target typing of an array literal, so `({1, 2})` would be an `Integer()` that cannot be converted to `Long()` (error BC30332).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA030.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA030 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA030
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA030:Array creation expression can be an array literal", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA030:Array creation expression can be an array literal", Justification:="Reason.", Scope:="module")>
```
