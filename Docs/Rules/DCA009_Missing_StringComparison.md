# 🔤 DCA009 — String comparison argument omitted

Reports invocations that omit a string comparison argument although one is available, either through an **overload** or an **optional parameter**.

- **Severity:** Warning
- **Category:** Globalization
- **Code fix:** ✔️ Two lightbulb actions: `Ordinal` or `OrdinalIgnoreCase`

---

## 💡 Why

Overloads without a comparison argument often use culture-sensitive rules (`StartsWith(String)`, `String.Compare`), which change results depending on the user's culture and are slower. Stating the comparison makes the intent explicit.

## 🔎 Detection

Recognized parameter types: `StringComparison`, `StringComparer`, `IComparer(Of String)` and `IEqualityComparer(Of String)`. Overloads are matched through the semantic model, so the rule adapts to the target framework: for example, `String.Contains(String)` is only reported where `Contains(String, StringComparison)` exists (.NET Core 2.1+).

## ❌ Non-compliant

```vb
Dim a As Boolean = s.StartsWith("x")
Dim b As Boolean = String.Equals(s, t)
Dim c As Integer = String.Compare(s, t)
Dim d As Boolean = names.Contains("x")      ' Enumerable.Contains -> IEqualityComparer(Of String)
Me.Find(s)                                   ' Optional comparison parameter omitted.

Public Sub Find(value As String, Optional comparison As StringComparison = StringComparison.CurrentCulture)
End Sub
```

## ✔️ Not reported

```vb
Dim a As Boolean = s.StartsWith("x", StringComparison.Ordinal)
Dim b As Boolean = s.StartsWith("x"c)        ' No comparison overload for Char.
Dim c As Boolean = o.Equals(s)               ' Object.Equals.
Dim d As Boolean = list.Contains("x")        ' List(Of String).Contains has no comparer overload.
```

## 🔧 Code fix

Two actions are offered, because adding a case-insensitive comparison changes the behavior of a case-sensitive call:

```vb
' Add 'StringComparison.Ordinal'
Dim a As Boolean = s.StartsWith("x", StringComparison.Ordinal)
Dim f As Boolean = names.Contains("x", StringComparer.Ordinal)
Dim h As Integer = s.IndexOf(value:="x", comparisonType:=StringComparison.Ordinal)  ' Named when needed.
Me.Find(s, StringComparison.Ordinal)

' Add 'StringComparison.OrdinalIgnoreCase'
Return s.EndsWith("x", StringComparison.OrdinalIgnoreCase)
```

The type is written with the shortest name that resolves correctly (`System.StringComparison` when a local type named `StringComparison` hides it).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA009.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA009 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA009
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "DCA009:String comparison argument omitted", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "DCA009:String comparison argument omitted", Justification:="Reason.", Scope:="module")>
```
