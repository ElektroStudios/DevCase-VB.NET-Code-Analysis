# 🏷️ DCA067 — String literal instead of `NameOf`

**.NET Framework only.** Reports string literals passed to a `paramName` or `propertyName` parameter that name a parameter or a property in scope, such as `New ArgumentNullException("path")` or `OnPropertyChanged("Title")`.

- **Severity:** Info
- **Category:** Style
- **Code fix:** ✔️ Uses `NameOf(name)`
- **Equivalent to:** Microsoft [CA1507](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1507) *"Use nameof in place of string"*

---

## 💡 Why

A string does not follow a rename: after renaming `path` to `filePath` with the refactoring tools, `"path"` stays, and the exception names a parameter that no longer exists (and the `PropertyChanged` event notifies a property that does not exist, so bindings stop updating). `NameOf` is checked by the compiler and renamed with the symbol.

On modern .NET, the built-in rule CA1507 already reports this code, so this rule stays silent there.

## 🔎 Detection

An argument passed to a parameter named `paramName` or `propertyName` (the convention of the exception constructors, `PropertyChangedEventArgs` and `OnPropertyChanged` methods) when it is a string literal that:

- for `paramName`, equals (case-sensitively) the name of a parameter of the enclosing method, accessor or lambda;
- for `propertyName`, equals the name of a property;

and the name binds, with the same case, to that parameter or property at that position (so `NameOf` returns the same string). A local variable that hides the name, a literal with another case (`"Path"`) or an unknown name are not reported. Attribute arguments, and projects whose language version is older than Visual Basic 14 (without `NameOf`), are not analyzed.

## ❌ Non-compliant

```vb
Throw New ArgumentNullException("path")
Me.OnPropertyChanged("Title")
```

## ✔️ Compliant

```vb
Throw New ArgumentNullException(NameOf(path))
Me.OnPropertyChanged(NameOf(Title))
```

## 🔧 Code fix

| Code | Fixed code |
|---|---|
| `New ArgumentNullException("path")` | `New ArgumentNullException(NameOf(path))` |
| `New ArgumentException("Empty.", "path")` | `New ArgumentException("Empty.", NameOf(path))` |
| `Me.OnPropertyChanged("Title")` | `Me.OnPropertyChanged(NameOf(Title))` |
| `New ArgumentNullException("error")` (parameter `[error]`) | `New ArgumentNullException(NameOf([error]))` |

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA067.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA067 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA067
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA067:String literal instead of NameOf", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "DCA067:String literal instead of NameOf", Justification:="Reason.", Scope:="module")>
```
