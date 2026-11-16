# 📝 DCA007 — Public member without XML documentation

Reports declarations with the `Public` modifier that have no XML documentation comment.

- **Severity:** Warning
- **Category:** Documentation
- **Code fix:** ✔️ Generates the documentation comment

---

## 💡 Why

Public members are the API surface: IntelliSense, the generated XML documentation file and documentation tools all depend on `'''` comments.

## 🔎 Detection

Analyzed declarations: classes, structures, modules, interfaces, enumerations, methods, constructors, properties, events, operators, delegates, `Declare` statements and fields. A comment that only contains `''' <inheritdoc/>` is accepted. Members without the `Public` modifier (including interface members) are not reported.

> [!NOTE]
> When a project is built without `GenerateDocumentationFile`, the compiler parses `'''` comments as plain comments; they are still recognized as documentation.

## ❌ Non-compliant

```vb
Public Class C
    Public Field As Integer
    Public Sub New()
    End Sub
    Public Function Run(x As Integer) As Integer
        Return x
    End Function
End Class
```

## ✔️ Compliant / not reported

```vb
''' <summary>Documented.</summary>
Public Class C
    ''' <inheritdoc/>
    Public Overrides Function ToString() As String
        Return String.Empty
    End Function
    Private Sub Hidden()
    End Sub
    Friend Sub Internal()
    End Sub
End Class
```

## 🔧 Code fix

The `<summary>` of constructors gets the conventional text; other summaries are left empty for you to write. Every parameter gets a default description derived from its name and type (so DCA008 is satisfied), and functions get an empty `<returns>` section. Empty sections are always written on a single line, never as blocks of blank lines:

```vb
''' <summary></summary>
'''
''' <param name="path">
''' The path.
''' </param>
'''
''' <returns></returns>
Public Function Load(path As String) As String
```

Constructor example:

```vb
''' <summary>
''' Initializes a new instance of the <see cref="LogConsoleBox"/> class.
''' </summary>
'''
''' <param name="encoding">
''' The <see cref="Encoding"/> to use.
''' </param>
'''
''' <param name="isEnabled">
''' <see langword="True"/> if enabled; otherwise, <see langword="False"/>.
''' </param>
'''
''' <param name="overwrite">
''' <see langword="True"/> to overwrite; otherwise, <see langword="False"/>.
''' </param>
'''
''' <param name="mode">
''' One of the enumeration values that specifies the mode.
''' </param>
'''
''' <param name="cancellationToken">
''' The token to monitor for cancellation requests.
''' </param>
'''
''' <param name="maxURLLength">
''' The max URL length.
''' </param>
Public Sub New(encoding As Encoding, isEnabled As Boolean, overwrite As Boolean,
               mode As FileMode, cancellationToken As CancellationToken, maxURLLength As Integer)
End Sub
```

| Parameter | Generated description |
|---|---|
| `filePath As String` | The file path. |
| Same name as its type (`encoding As Encoding`) | The `<see cref="Encoding"/>` to use. |
| `Boolean` starting with a verb (`overwrite`, `includeHidden`) | `True` to overwrite; otherwise, `False`. |
| `Boolean` starting with `is`/`has`/`can`/... (`isEnabled`) | `True` if enabled; otherwise, `False`. |
| Other `Boolean` (`recursive`) | `True` to enable the recursive option; otherwise, `False`. |
| Enumerations | One of the enumeration values that specifies the mode. |
| `CancellationToken` | The token to monitor for cancellation requests. |

> [!TIP]
> Generated descriptions are meaningful enough to remove the warning, but generic names give generic texts: review them.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA007.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA007 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA007
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Documentation", "DCA007:Public member lacks XML documentation", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Documentation", "DCA007:Public member lacks XML documentation", Justification:="Reason.", Scope:="module")>
```
