# 👻 DCA054 — Code in a `ComImport` class never runs

Reports constructors, methods, properties, operators, custom events and field initializers declared in a class marked with `<ComImport>`.

- **Severity:** Error
- **Category:** Reliability
- **Code fix:** ✔️ Removes `Shared Sub New` and `Public Sub New()` (when removing them changes nothing); other members are only reported

---

## 💡 Why

A class marked with `<ComImport>` is a **COM coclass**: `New ProgressDialog()` does not run Visual Basic code, the runtime creates the COM object through its GUID (`CoCreateInstance`).

C# rejects code in such a class (error CS0669 for constructors, CS0423 for methods). **Visual Basic accepts it without an error or a warning**, but silently compiles every member as a runtime-implemented member (`MethodImplAttributes.InternalCall Or MethodImplAttributes.Runtime`) **without a body**. The code is still in the source file, it looks like it runs, and it never does:

```vb
<ComImport>
<Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")>
Public Class ProgressDialog
    Public Sub New()
        Console.WriteLine("Created")   ' Never runs: the compiled constructor has no body.
    End Sub
End Class
```

This was verified by compiling such classes for .NET Framework 4.8 and .NET 10 and inspecting the compiled members: instance and `Shared` constructors, constructors with parameters, methods, `Shared` functions, property getters and auto-implemented properties all have the `InternalCall | Runtime` flags and no IL body.

## 🔎 Detection

In a class marked with `System.Runtime.InteropServices.ComImportAttribute`:

| Member | Reported |
|---|---|
| `Sub New` (any accessibility or parameters) and `Shared Sub New` | ✔️ |
| `Sub` / `Function` (also `Shared`), `Operator` | ✔️ |
| `Property`, with accessors or auto-implemented | ✔️ |
| `Custom Event` | ✔️ |
| Field with an initializer (`Private x As Integer = 1`, `As New ...`) | ✔️ (initializers run inside the constructors) |
| Field without an initializer, nested types, `Declare` statements | ➖ |

Classes without `<ComImport>` and `<ComImport>` interfaces (whose members never have bodies) are not analyzed.

## ❌ Non-compliant

```vb
<ComImport>
<Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

    Public Sub New()
    End Sub

End Class
```

## ✔️ Compliant

```vb
<ComImport>
<Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog
End Class
```

Use the COM object through its interface (`DirectCast(New ProgressDialog(), IProgressDialog)`), and put any code of your own in another class.

## 🔧 Code fix

Removes the constructor, with its comments, when that changes nothing:

- `Shared Sub New`: its body never runs.
- `Public Sub New()` without parameters, in a class that is not `MustInherit`: the compiler then declares the same public constructor, without a body.

Other members are only reported: a method, a property, or a constructor with parameters or another accessibility is part of the public surface of the class, and removing it would break the code that uses it.

The preprocessor directives around the constructor are kept. When the constructor is the only member of a region, the region stays, empty, so `#Region` and `#End Region` remain balanced:

```vb
<ComImport>
<Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

#Region " Constructors "

    ''' <summary>
    ''' Initializes a new instance of the <see cref="ProgressDialog"/> class.
    ''' </summary>
    Public Sub New()
        Console.WriteLine("Created")
    End Sub

#End Region

End Class
```

becomes:

```vb
<ComImport>
<Guid("F8383852-FCD3-11d1-A6B9-006097DF5BD4")>
<ClassInterface(ClassInterfaceType.None)>
Public Class ProgressDialog

#Region " Constructors "

#End Region

End Class
```

The same applies to an `#If` block around the constructor. The directives inside its body (an `#If DEBUG Then` block, for example) are part of its code, and are removed with it.

> [!NOTE]
> **Why Error?** It is the only rule reported as an Error (since version 1.0.8; before, it was a Warning). The compiler accepts the code, but the code can never run: its body is discarded at compile time. That is a defect hidden by the compiler, not a matter of style or performance. An empty `Public Sub New()` is reported too, because it has no meaning in a COM coclass; the code fix removes it. Projects that need time to clean up existing code can lower the severity (`dotnet_diagnostic.DCA054.severity = warning` in `.editorconfig`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA054.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA054 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA054
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA054:Code in a ComImport class never runs", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "DCA054:Code in a ComImport class never runs", Justification:="Reason.", Scope:="module")>
```
