# 🧊 DCA084 — Constant array passed as an argument

**.NET Framework only.** Reports arrays of constants created as arguments of a call, such as `text.IndexOfAny({","c, ";"c})` or `text.Split(New Char() {","c})`.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Extracts the array into a `Shared ReadOnly` field
- **Equivalent to:** Microsoft [CA1861](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1861) *"Avoid constant arrays as arguments"*

---

## 💡 Why

The array is allocated again **on every call**, although its content never changes. In a method that runs often (a parser, a loop), the allocations add up and the garbage collector has to collect them. A `Shared ReadOnly` field creates the array once.

On modern .NET, the built-in rule CA1861 already reports this code, so this rule stays silent there.

## 🔎 Detection

Explicit, one-dimensional, non-empty arrays whose elements are all constants (`New Char() {...}` or an array literal `{...}`), passed `ByVal` to a method or a constructor. Not reported:

| Case | Reason |
|---|---|
| `text.Split(","c, ";"c)` | The `ParamArray` array is implicit |
| The `Shared` constructor, initializers of `Shared` fields and properties | They run once |
| The methods of `System.Array` (`Array.Sort`, `Array.Reverse`...) | They may modify the array |
| Empty arrays, arrays with variables | Rule DCA055 handles empty arrays; the others change |
| `ByRef` arguments, attribute arguments, interfaces | The array can be replaced, is metadata, or cannot have a field |

> [!WARNING]
> Extracting the array shares it between calls: the called method must not modify it. The framework methods that take a constant array (`Split`, `IndexOfAny`, `Trim`, `String.Join`...) only read it.

## ❌ Non-compliant

```vb
Public Function FindSeparator(text As String) As Integer
    Return text.IndexOfAny({","c, ";"c})
End Function
```

## ✔️ Compliant

```vb
Private Shared ReadOnly FindSeparatorIndexOfAny0 As Char() = {","c, ";"c}

Public Function FindSeparator(text As String) As Integer
    Return text.IndexOfAny(FindSeparatorIndexOfAny0)
End Function
```

## 🔧 Code fix

The fix declares a `Private Shared ReadOnly` field (`Private ReadOnly` in a `Module`, whose members are already `Shared`) **before the member** that contains the call, with the type of the array and its initializer, and passes the field. The field goes after the blank lines and the `#Region` directives that precede the member, and before its documentation comment, so it stays in the region of the member.

The name is the name of the member, the name of the called method (or of the created type) and a number: `FindSeparatorIndexOfAny0`. Constructors use `Constructor` (`ConstructorList0`), and property accessors the name of the property. The numbers are assigned per type (also across the files of a `Partial` type) and skip the names that the type, its base types or the member already use, so **Fix All** declares a different field for each array:

```vb
Private Shared ReadOnly SplitSplit0 As Char() = {","c}

Private Shared ReadOnly SplitSplit1 As Char() = {";"c}

Public Function Split(text As String) As String()
    Dim first As String() = text.Split(SplitSplit0)
    Return text.Split(SplitSplit1, StringSplitOptions.None)
End Function
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA084.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA084 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA084
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA084:Constant array passed as an argument", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "DCA084:Constant array passed as an argument", Justification:="Reason.", Scope:="module")>
```
