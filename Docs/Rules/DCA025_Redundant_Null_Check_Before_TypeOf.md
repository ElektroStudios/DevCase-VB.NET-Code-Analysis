# 🧹 DCA025 — Redundant null check before `TypeOf`

Reports null checks that are redundant because the next operand is a `TypeOf` expression on the same value: `x IsNot Nothing AndAlso TypeOf x Is T` and `x Is Nothing OrElse TypeOf x IsNot T`, in any of their Visual Basic forms.

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Removes the null check and its operator
- **Equivalent to:** SonarQube S4201 *"Null checks should not be combined with 'is' operator checks"*

---

## 💡 Why

`TypeOf x Is T` already returns `False` when `x` is `Nothing`, so checking for `Nothing` first adds nothing:

```vb
' The null check is redundant:
If sender IsNot Nothing AndAlso TypeOf sender Is Button Then

' Same result:
If TypeOf sender Is Button Then
```

The inverse form works the same way: `TypeOf x IsNot T` returns `True` when `x` is `Nothing`:

```vb
If value Is Nothing OrElse TypeOf value IsNot String Then Return   ' Redundant.
If TypeOf value IsNot String Then Return                          ' Same result.
```

### How is it different from IDE0084?

Visual Studio's built-in **IDE0084** rule only rewrites `Not TypeOf x Is T` / `Not x Is y` as `TypeOf x IsNot T` / `x IsNot y`, which is about operator style. It does not detect redundant null checks, so the two rules complement each other.

## 🔎 Detection

Every Visual Basic form of a null check is recognized:

| Before `AndAlso TypeOf x Is T` | Before `OrElse TypeOf x IsNot T` |
|---|---|
| `x IsNot Nothing` | `x Is Nothing` |
| `Not x Is Nothing` | `Not x IsNot Nothing` |
| `Not (x Is Nothing)` | `Not (x IsNot Nothing)` |
| `Nothing IsNot x` | `Nothing Is x` |
| `Not IsNothing(x)` | `IsNothing(x)` |


Only checks on **simple names and member access chains** are reported (`value`, `Me.sender`, `settings.Owner`). Removing a check on a method call (`GetValue() IsNot Nothing`) could remove a side effect.

The null check must be the operand **immediately before** the `TypeOf` in the same `AndAlso`/`OrElse` chain:

```vb
If enabled AndAlso value IsNot Nothing AndAlso TypeOf value Is String Then   ' Reported.
If value IsNot Nothing OrElse TypeOf value Is String Then                     ' Not reported: different logic.
```

## ❌ Non-compliant

```vb
If e.Data IsNot Nothing AndAlso TypeOf e.Data Is String Then
If control Is Nothing OrElse TypeOf control IsNot TextBox Then
```

## ✔️ Compliant

```vb
If TypeOf e.Data Is String Then
If TypeOf control IsNot TextBox Then
```

## 🔧 Code fix

The fix removes the null check and the `AndAlso`/`OrElse` that follows it, keeping the rest of the chain (`enabled AndAlso TypeOf value Is String`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA025.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA025 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA025
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA025:Redundant null check before TypeOf", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA025:Redundant null check before TypeOf", Justification:="Reason.", Scope:="module")>
```
