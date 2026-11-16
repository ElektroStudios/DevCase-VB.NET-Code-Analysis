# 📦 DCA006 — Type in the global namespace

Reports top-level classes, modules, structures, interfaces, enumerations and delegates that are not declared inside a `Namespace` block **and therefore end up in the global namespace**.

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ✔️ Wraps the type in a `Namespace` block whose name is deduced from the project (never hardcoded)
- **Configuration:** `devcase_analyzers.default_namespace = My.Namespace` (optional)

---

## 💡 Why

In Visual Basic, a type without a `Namespace` block lives in the project `RootNamespace`. When that root namespace is empty (common in .NET Framework projects where it was removed, or in projects that declare every namespace explicitly), the type falls into the **global namespace**: it pollutes IntelliSense for every consumer and can collide with types of other assemblies.

## 🔎 Detection

The rule only reports types whose containing namespace is the global namespace:

| Project `RootNamespace` | Type without `Namespace` block | Reported |
|---|---|---|
| Empty | `Global.C` | ✔️ |
| `MyApp` | `MyApp.C` | ❌ (it already has a namespace; wrapping it would produce `MyApp.MyApp.C`) |

Explicit `Namespace Global` blocks, nested types, types marked `<DesignerGenerated>` and partial types with a part in a generated file are not reported: moving only the hand-written part would split the partial type in two, and moving a form would break `Application.Designer.vb`.

## ❌ Non-compliant

```vb
' Project with an empty RootNamespace.
Public Class C
End Class

Public Delegate Sub D()
```

## 🔧 Code fix

The namespace name is chosen deterministically, in this order:

1. The `devcase_analyzers.default_namespace` option, when it contains a valid name.
2. The first top-level `Namespace` block already declared **in the same file**, so the file stays consistent.
3. The project **assembly name** (the same default Visual Studio uses for new projects), sanitized: invalid identifier characters become `_`, segments that start with a digit are prefixed with `_`, and keywords are escaped (`My-App.2026` → `My_App._2026`, `Contoso.Class` → `Contoso.[Class]`).

If no valid name can be determined, no fix is offered.

Region directives that precede the type stay outside, its documentation goes inside, and the content is re-indented (multi-line string literals are not altered). Example for an assembly named `Contoso.Tools`:

```vb
' Before
#Region " Sample "

''' <summary>
''' A sample.
''' </summary>
Public Class Sample
End Class

#End Region

' After
#Region " Sample "

Namespace Contoso.Tools

    ''' <summary>
    ''' A sample.
    ''' </summary>
    Public Class Sample
    End Class

End Namespace

#End Region
```

If the file already contains `Namespace Contoso.Data`, that name is reused instead. When a file contains only top-level types, all of them are wrapped in a single block. Every partial declaration of the type, in every file of the solution, is wrapped.

## ⚙️ Configuration

```ini
[*.vb]
devcase_analyzers.default_namespace = Contoso.Tools
```

An invalid value is ignored and the automatic resolution (steps 2 and 3) applies.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA006.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA006 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA006
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA006:Type declared in the global namespace", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA006:Type declared in the global namespace", Justification:="Reason.", Scope:="module")>
```
