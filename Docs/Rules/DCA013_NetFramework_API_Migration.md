# 🚀 DCA013 — .NET Framework API with a modern .NET equivalent

**.NET Framework only.** Reports API usages that have an exact, better equivalent that only exists on modern .NET, so that both versions can coexist in a conditional compilation block.

- **Severity:** Info
- **Category:** Portability
- **Code fix:** ✔️ Duplicates the statement into `#If <symbol> Then` (modern API) / `#Else` (original code)

---

## 📋 Equivalence table

| .NET Framework code | Modern .NET code | Symbol |
|---|---|---|
| `s.Contains("x")` | `s.Contains("x"c)` | `NETCOREAPP` |
| `s.StartsWith("x", StringComparison.Ordinal)` / `EndsWith` | `s.StartsWith("x"c)` | `NETCOREAPP` |
| `s.IndexOf(v, comparison) >= 0` (also `> -1`, `<> -1`, and `IndexOf(Char)`) | `s.Contains(v, comparison)` | `NETCOREAPP` |
| `s.IndexOf(v, comparison) < 0` (also `= -1`) | `Not s.Contains(v, comparison)` | `NETCOREAPP` |
| `String.Join(",", values)` | `String.Join(","c, values)` | `NETCOREAPP` |
| `s.Split({"ab"}, options)` / `s.Split(New String() {"ab"}, count, options)` | `s.Split("ab", options)` | `NETCOREAPP` |
| `If Not d.ContainsKey(k) Then d.Add(k, v)` (`Dictionary`) | `d.TryAdd(k, v)` | `NETCOREAPP` |
| `DirectCast([Enum].Parse(GetType(T), s), T)` (also `CType`, `ignoreCase`) | `[Enum].Parse(Of T)(s)` | `NETCOREAPP` |
| `File.ReadAllText(...)` and the other `File` read/write/append methods, inside an `Async` method | `Await File.ReadAllTextAsync(...).ConfigureAwait(False)` | `NETCOREAPP` |
| `BitConverter.ToString(b).Replace("-", "")` | `Convert.ToHexString(b)` | `NET5_0_OR_GREATER` |
| `Process.GetCurrentProcess().Id` | `Environment.ProcessId` | `NET5_0_OR_GREATER` |
| `Process.GetCurrentProcess().MainModule.FileName` | `Environment.ProcessPath` | `NET6_0_OR_GREATER` |

Deliberately **not** included, because they are not exact equivalents:

| Candidate | Reason |
|---|---|
| `s.StartsWith("x")` → `s.StartsWith("x"c)` | `StartsWith(String)` is culture-sensitive; `StartsWith(Char)` is ordinal. |
| `s.IndexOf("ab") >= 0` → `s.Contains("ab")` | `IndexOf(String)` is culture-sensitive; `Contains(String)` is ordinal. |
| `Math.Max(min, Math.Min(v, max))` → `Math.Clamp` | `Clamp` throws when `min > max`. |
| `New Random()` → `Random.Shared` | Different seeding and thread-safety semantics. |
| `Environment.TickCount` → `TickCount64` | Different overflow behavior. |

## 🔎 Detection

Every entry is verified through the semantic model (declaring type, overload and constant arguments). A usage is reported only when its statement owns its lines (only white-space or a trailing comment around it) and is not already inside an `#If` block.

## 🔧 Code fix

The whole statement is duplicated; every migratable usage of that statement is converted at once, and the most restrictive symbol is used:

```vb
' Before
Dim text As String = File.ReadAllText(path).Trim() & BitConverter.ToString(bytes).Replace("-", String.Empty)

' After
#If NET5_0_OR_GREATER Then
        Dim text As String = (Await File.ReadAllTextAsync(path).ConfigureAwait(False)).Trim() & Convert.ToHexString(bytes)
#Else
        Dim text As String = File.ReadAllText(path).Trim() & BitConverter.ToString(bytes).Replace("-", String.Empty)
#End If
```

Block headers are wrapped on their own line, which is valid VB:

```vb
#If NETCOREAPP Then
        If (Not s.Contains("ab", StringComparison.Ordinal)) AndAlso s.Contains("-"c) Then ' check
#Else
        If s.IndexOf("ab", StringComparison.Ordinal) < 0 AndAlso s.Contains("-") Then ' check
#End If
            Return
        End If
```

Statement-level patterns are replaced as a whole:

```vb
#If NETCOREAPP Then
        d.TryAdd(key, 1)
#Else
        If Not d.ContainsKey(key) Then
            d.Add(key, 1)
        End If
#End If
```

> [!NOTE]
> `Await` is not allowed inside `SyncLock` blocks or query expressions, so `File` I/O is not reported there.

## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA013.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA013 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA013
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Portability", "DCA013:API has a modern .NET equivalent", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Portability", "DCA013:API has a modern .NET equivalent", Justification:="Reason.", Scope:="module")>
```
