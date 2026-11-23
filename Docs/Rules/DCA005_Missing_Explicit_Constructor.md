# 🏗️ DCA005 — Missing explicit constructor

Reports classes that do not explicitly declare an instance constructor.

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ✔️ Injects a documented constructor inside a `#Region " Constructors "` block

---

## 💡 Why

An implicit constructor is always `Public`. Declaring it explicitly makes its accessibility a conscious decision (e.g. a `Private` constructor for a static utility class) and gives it documentation.

## 🔎 Detection

| Not reported | Reason |
|---|---|
| `Module`, `Structure` | Not classes / no instance constructor to declare. |
| `Partial` class whose constructor is in another file | Resolved through the type symbol. |
| Classes marked `<DesignerGenerated>` (Windows Forms) | The compiler synthesizes a constructor that calls `InitializeComponent`; an empty explicit constructor would leave the form blank. |
| Classes marked `<ComImport>` (COM coclasses such as `ProgressDialog`) | The runtime creates the COM object, and the compiler discards the body of any constructor: an explicit `Sub New` would be dead code. Rule [DCA054](DCA054_Code_In_ComImport_Class.md) reports code in those classes. |
| A class that only declares `Shared Sub New` | Still reported: that is not an instance constructor. |

Partial classes are reported once, on their first non-generated declaration.

## ❌ Non-compliant

```vb
Public Class Sample
    Public ReadOnly Property Value As Integer
End Class
```

## 🔧 Code fix

The accessibility follows the shape of the class:

```vb
Public NotInheritable Class Util      ' Only Shared members -> Private constructor.

#Region " Constructors "

    ''' <summary>
    ''' Prevents a default instance of the <see cref="Util"/> class from being created.
    ''' </summary>
    Private Sub New()
    End Sub

#End Region

    Public Shared Function Twice(value As Integer) As Integer
        Return value * 2
    End Function
End Class

Public MustInherit Class Base(Of T)   ' MustInherit -> Protected constructor.

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="Base(Of T)"/> class.
    ''' </summary>
    Protected Sub New()
    End Sub

#End Region

    Public MustOverride Sub Run()
End Class
```

The region is placed after fields, properties and events, and before the first method; when that member is preceded by an `#End Region`, the new region goes right after it:

```vb
#Region " Properties "
    ...
#End Region

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="Sample"/> class.
    ''' </summary>
    Public Sub New()
    End Sub

#End Region

#Region " Public Methods "
    ...
```

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA005.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA005 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA005
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA005:Class lacks an explicit constructor", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA005:Class lacks an explicit constructor", Justification:="Reason.", Scope:="module")>
```
