# 💥 DCA012 — Thrown exception is not documented

Reports documented members that explicitly throw exceptions not listed with an `<exception cref="...">` tag.

- **Severity:** Info
- **Category:** Documentation
- **Code fix:** ✔️ Adds **empty**, single-line `<exception cref="..."></exception>` sections

---

## 🔎 Detection

Nothing is inferred; only deterministic sources count:

- `Throw New SomeException(...)` statements in the member body.
- Calls to the .NET throw helpers, whose thrown types are documented by Microsoft:

| Throw helper | Documented exceptions |
|---|---|
| `ArgumentNullException.ThrowIfNull` | `ArgumentNullException` |
| `ArgumentException.ThrowIfNullOrEmpty` / `ThrowIfNullOrWhiteSpace` | `ArgumentNullException`, `ArgumentException` |
| `ArgumentOutOfRangeException.ThrowIf*` | `ArgumentOutOfRangeException` |
| `ObjectDisposedException.ThrowIf` | `ObjectDisposedException` |

Ignored: throws inside lambdas, exceptions caught by an enclosing `Try` of the same member (catch-all or a base type, without a `When` filter), re-throws (`Throw`, `Throw ex`), `NotImplementedException`, undocumented members (see DCA007) and `<inheritdoc/>`/`<include/>` comments. Property and event accessors are reported on the property or event declaration.

An existing tag matches by type name: `cref="ArgumentNullException"`, `cref="System.ArgumentNullException"` and `cref="T:System.ArgumentNullException"` are equivalent.

## ❌ Non-compliant

```vb
''' <summary>Run.</summary>
Public Sub Run(value As Object, name As String, count As Integer)
    If value Is Nothing Then Throw New ArgumentNullException(NameOf(value))
    ArgumentException.ThrowIfNullOrWhiteSpace(name)
    ArgumentOutOfRangeException.ThrowIfNegative(count)
End Sub
' DCA012: 'Run' throws exception(s) not documented with an <exception> tag:
'         ArgumentNullException, ArgumentException, ArgumentOutOfRangeException
```

## ✔️ Not reported

```vb
''' <summary>Caught.</summary>
Public Sub B()
    Try
        Throw New ArgumentNullException("x")
    Catch ex As ArgumentException     ' Base type of ArgumentNullException.
    End Try
End Sub
```

## 🔧 Code fix

The sections are intentionally empty and written on a single line: the condition that throws the exception is yours to describe, and nothing is invented. The new tags form one compact group, with a single blank `'''` line before it (an existing blank line is reused, and dangling blank lines before the declaration are removed). New tags are appended directly to an existing group of single-line `<exception>` tags. They are inserted after the last `<summary>`, `<remarks>`, `<typeparam>`, `<param>`, `<returns>`, `<value>` or `<exception>` section, so before `<example>`:

```vb
''' <summary>
''' Runs.
''' </summary>
'''
''' <param name="value">
''' The value.
''' </param>
'''
''' <exception cref="ArgumentNullException"></exception>
''' <exception cref="ArgumentException"></exception>
'''
''' <example>
''' <code>Run(1)</code>
''' </example>
Public Sub Run(value As String)
    ArgumentException.ThrowIfNullOrEmpty(value)
End Sub
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA012.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA012 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA012
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Documentation", "DCA012:Thrown exception is not documented", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Documentation", "DCA012:Thrown exception is not documented", Justification:="Reason.", Scope:="module")>
```
