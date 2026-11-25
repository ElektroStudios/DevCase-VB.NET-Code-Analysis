# 🎨 DCA060 — Component property without designer serialization configuration

**.NET Framework only.** Reports read/write properties of components and controls (`Component`, `Control`, `Form`, `UserControl`...) that do not tell the Windows Forms designer whether and how to serialize them.

- **Severity:** Warning
- **Category:** Design
- **Code fix:** ✔️ Adds `<DesignerSerializationVisibility(Visible)>`, `<DesignerSerializationVisibility(Content)>`, `<DefaultValue(value)>` or `<DesignerSerializationVisibility(Hidden)>`
- **Equivalent to:** Microsoft [WFO1000](https://learn.microsoft.com/dotnet/desktop/winforms/compiler-messages/wfo1000) *"Missing code serialization configuration for property content"*

---

## 💡 Why

By default, the designer writes the value of every public property of a component into the `InitializeComponent` code (or the resources of the form) when the component is placed on a designer. A property that holds runtime data (a connection string, a token, a large object, a cache) ends up serialized in the generated code: data leaks into the source, the designer file grows, and the value is restored every time the form is created. Declaring the serialization makes the intent explicit, and is what .NET 9 and later require (WFO1000 is an **error** by default there).

On modern .NET, the built-in rule WFO1000 already reports this code, so this rule stays silent there.

## 🔎 Detection

The properties declared in a class that implements `System.ComponentModel.IComponent` are analyzed when they are:

- Instance (not `Shared`), non-indexed, and at least `Friend` (`Public`, `Friend` or `Protected Friend`).
- Read/write, with a setter that is not `Private`.
- Not an `Overrides` (the base property carries its configuration), not a `WithEvents` variable, and not the `Site` implementation of `IComponent`.

A property is configured, and not reported, when it has `<DesignerSerializationVisibility>` or `<DefaultValue>` (or an attribute derived from them), or when its type or a base type declares a `ShouldSerialize{Name}() As Boolean` method (the designer finds it by name, also when it is `Private`).

## ❌ Non-compliant

```vb
Public Class Gauge
    Inherits Component

    Public Property Title As String
    Public Property Maximum As Integer = 100
End Class
```

## ✔️ Compliant

```vb
Public Class Gauge
    Inherits Component

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Title As String

    <DefaultValue(100)>
    Public Property Maximum As Integer = 100
End Class
```

## 🔧 Code fix

The fix adds the attribute on its own line, before the declaration (after its documentation comment and before its other attributes), with the names that the file can use (`ComponentModel.DesignerSerializationVisibility(...)` when `System.ComponentModel` is not imported). It offers up to four actions, in this order:

| Action | Effect | When |
|---|---|---|
| `<DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)>` | **Keeps the current behavior**: the designer keeps writing the value. Choose it for properties that are meant to be set in the designer. | Always |
| `<DesignerSerializationVisibility(DesignerSerializationVisibility.Content)>` | The designer writes the **contents** of the object instead of the object itself: the items of a collection (`Items.AddRange(...)`) or the properties of a nested object (`Padding.Left = ...`), as for `ListBox.Items`. Choose it for read-only collections and for objects created by the component. | Properties whose type is a class (other than `String`), an interface or an array. Not offered for value types (structures, enumerations, numbers), where the designer would write code that modifies a copy, nor for `String`, which has no contents to serialize |
| `<DefaultValue(value)>` | The designer only writes the value when it differs from the default one, and **Reset** in the Properties window restores it. | Auto-implemented properties whose initial value is a constant, or the default value of a numeric, `Boolean`, `Char` or enumeration type |
| `<DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>` | The designer never writes the property. Choose it for runtime data. This is the fix that Microsoft offers for WFO1000. | Always |

The argument of `<DefaultValue>` is typed so that it binds to the overload of the property type: `5L` for `Long`, `0S` for `Short`, `CByte(7)` for `Byte`, `0.5R` for `Double`, `1.5F` for `Single`, `"x"c` for `Char`, `DayOfWeek.Friday` for an enumeration. Otherwise `DefaultValue(Integer)` would compare an `Integer` with the `Long` value of the property, and the designer would always write it.

> [!TIP]
> Why does `<DefaultValue>` also configure the property? Because the designer only serializes values that differ from it: the attribute declares which value is "nothing to save". It is the right choice when the default value is meaningful in the designer; `Hidden` is the right choice when the property should never be saved.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA060.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA060 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA060
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA060:Component property without designer serialization configuration", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "DCA060:Component property without designer serialization configuration", Justification:="Reason.", Scope:="module")>
```
