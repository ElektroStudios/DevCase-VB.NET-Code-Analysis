# 🌊 DCA057 — `IEnumerable` parameter named as a collection

Reports parameters typed `IEnumerable` or `IEnumerable(Of T)` whose name describes a collection, such as `Public Sub AddRange(collection As IEnumerable(Of String))`.

- **Severity:** Info
- **Category:** Naming
- **Code fix:** ✔️ Renames the parameter to `sequence` (`seq` for an abbreviation), in the whole solution

---

## 💡 Why

A parameter typed `IEnumerable(Of T)` is a **sequence**, not a collection: it may be a lazy LINQ query or an iterator, it may be enumerated only once, it may be infinite, and it has no `Count` or indexer. A name such as `collection`, `list` or `array` suggests the opposite and invites code that enumerates it several times or assumes its size. A neutral name describes what the method really receives:

```vb
Public Sub AddRange(collection As IEnumerable(Of String))   ' Is it a collection? It may be a query.
Public Sub AddRange(sequence As IEnumerable(Of String))
```

## 🔎 Detection

A parameter with an `As` clause whose type is **exactly** `System.Collections.IEnumerable` or a construction of `System.Collections.Generic.IEnumerable(Of T)`, with one of these names (in any letter case):

| Name | Suggested names, in order |
|---|---|
| `collection`, `list`, `array` | `sequence`, `source`, `value`, `items` |
| `col`, `coll`, `lst`, `arr` | `seq`, `sequence`, `source`, `value`, `items` |

The first suggested name that is not already used in the member is chosen: another parameter, a local variable, the name of the method (which is also the implicit return variable of a `Function`), a type parameter, a member used without qualification, or `value` in a property (the implicit parameter of its `Set` accessor). Names after a dot (`x.Value`) and names of named arguments (`value:=1`) do not count. When every name is used, the parameter is reported without a code fix: no numbered names (`sequence2`) are generated.

Methods, constructors, properties with parameters, interface members, `MustOverride` members and lambda expressions are analyzed. Not reported:

| Case | Reason |
|---|---|
| `ICollection(Of T)`, `IList(Of T)`, `List(Of T)`, arrays and every other type that implements `IEnumerable` | They are collections: the name is right |
| Other names (`items`, `values`, `source`...) | They already describe a sequence |
| `Overrides` and `Implements` members, and the implementation part of a `Partial` method | The name is defined by the base, interface or definition member (which is reported) |
| Parameters of `Event` and `Delegate` declarations | The name is part of a signature shared by every handler |
| Lambda parameters without an `As` clause | Their type is inferred |

## ❌ Non-compliant

```vb
Public Shared Function JoinAll(collection As IEnumerable(Of String)) As String
    Return String.Join(","c, collection)
End Function

Public Shared Function First(col As IEnumerable(Of Integer)) As Integer
    Return col.First()
End Function
```

## ✔️ Compliant

```vb
Public Shared Function JoinAll(sequence As IEnumerable(Of String)) As String
    Return String.Join(","c, sequence)
End Function

Public Shared Function First(seq As IEnumerable(Of Integer)) As Integer
    Return seq.First()
End Function
```

## 🔧 Code fix

Renames the parameter with Visual Studio's rename engine. Every reference in the solution is updated: the body, `NameOf`, named arguments of callers (`JoinAll(collection:=x)` → `JoinAll(sequence:=x)`) and the XML documentation (`<param name="...">`, `<paramref name="..."/>`).

> [!NOTE]
> Renaming a parameter of a `Public` method is a breaking change only for callers in **other** solutions that use named arguments (`collection:=`).

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA057.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA057 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA057
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "DCA057:IEnumerable parameter named as a collection", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "DCA057:IEnumerable parameter named as a collection", Justification:="Reason.", Scope:="module")>
```
