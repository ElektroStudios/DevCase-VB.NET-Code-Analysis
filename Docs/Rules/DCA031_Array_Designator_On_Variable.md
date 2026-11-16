# 🏷️ DCA031 — Array designator on the variable name

Reports array designators written on the name of a variable, field or parameter (`Dim values() As Integer`) instead of on its type (`Dim values As Integer()`).

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Moves the designator to the type
- **Equivalent to:** SonarQube [S1197](https://rules.sonarsource.com/vbnet/RSPEC-1197/) *"Array designators "()" should be on the type, not the variable"*

---

## 💡 Why

Both forms declare exactly the same type, but `Dim values As Integer()` keeps the **whole type in one place**, as every other declaration (`As List(Of Integer)`, function return types, generic arguments) and as the type is shown in IntelliSense and in the debugger.

## 🔎 Detection

Names with an array designator (`values()`, `grid(,)`, `jagged()()`) in variable, field and parameter declarations (including `ParamArray`).

Not reported, because the designator cannot be written on the type:

| Case | Example |
|---|---|
| Bounds | `Dim buffer(1023) As Byte` |
| Several names that share one `As` clause | `Dim a(), b As Integer` |
| Names without an `As` clause | `Dim values()` |
| Nullable names | `Dim value? As Integer` |

## ❌ Non-compliant

```vb
Private items() As String

Public Sub Process(ParamArray args() As Object)
    Dim grid(,) As Integer = New Integer(1, 1) {}
End Sub
```

## ✔️ Compliant

```vb
Private items As String()

Public Sub Process(ParamArray args As Object())
    Dim grid As Integer(,) = New Integer(1, 1) {}
End Sub
```

## 🔧 Code fix

Removes the designator from the name and appends it to the type, also for generic types (`List(Of String)()`) and multidimensional arrays (`Integer(,)`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA031.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA031 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA031
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA031:Array designator on the variable name", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA031:Array designator on the variable name", Justification:="Reason.", Scope:="module")>
```
