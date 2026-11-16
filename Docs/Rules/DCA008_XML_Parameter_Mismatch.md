# 🧾 DCA008 — `<param>` tags do not match the parameters

Reports XML documentation comments whose `<param>` tags do not exactly match the parameter list of a method, constructor, operator, delegate, event or indexed property.

- **Severity:** Warning
- **Category:** Documentation
- **Code fix:** ✔️ Adds the missing tags and fills the empty ones

---

## 🔎 Detection

| Issue | Example | Fixable |
|---|---|---|
| Missing tag | parameter `b` has no `<param name="b">` | ✔️ |
| Empty tag | `<param name="a"></param>`, `<param name="c"/>`, or a tag with only blank lines | ✔️ |
| Unknown tag | `<param name="old">` for a parameter that no longer exists | ❌ (needs a human decision) |
| Duplicated tag | two `<param name="a">` | ❌ |

Names are compared case-insensitively (VB identifiers are case-insensitive). Comments using `<inheritdoc/>` or `<include/>` are skipped, and undocumented members are left to DCA007.

## ❌ Non-compliant

```vb
''' <summary>Missing.</summary>
''' <param name="a">The a.</param>
Public Sub Missing(a As Integer, b As Integer)
End Sub

''' <summary>Unknown.</summary>
''' <param name="a">The a.</param>
''' <param name="old">Stale.</param>
Public Sub Unknown(a As Integer)
End Sub
```

## 🔧 Code fix

Missing tags are inserted in parameter order (after the previous documented parameter, or after `<summary>`, before `<returns>`/`<remarks>`):

```vb
' Before
''' <summary>
''' Runs.
''' </summary>
'''
''' <param name="b">
''' The b.
''' </param>
'''
''' <returns>
''' A value.
''' </returns>
Public Function Run(a As Integer, b As Integer, c As Integer) As Integer

' After
''' <summary>
''' Runs.
''' </summary>
'''
''' <param name="a">
''' The a.
''' </param>
'''
''' <param name="b">
''' The b.
''' </param>
'''
''' <param name="c">
''' The c.
''' </param>
'''
''' <returns>
''' A value.
''' </returns>
Public Function Run(a As Integer, b As Integer, c As Integer) As Integer
```

Empty tags are rewritten in place with the default description (see [DCA007](DCA007_Missing_XML_Documentation.md)):

```vb
' Before
''' <param name="filePath"></param>
''' <param name="overwrite"/>

' After
''' <param name="filePath">
''' The file path.
''' </param>
''' <param name="overwrite">
''' <see langword="True"/> to overwrite; otherwise, <see langword="False"/>.
''' </param>
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA008.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA008 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA008
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Documentation", "DCA008:XML <param> tags do not match the parameter list", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Documentation", "DCA008:XML <param> tags do not match the parameter list", Justification:="Reason.", Scope:="module")>
```
