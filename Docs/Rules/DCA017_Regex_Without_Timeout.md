# ⏱️ DCA017 — Regular expression without match timeout

Reports regular expressions evaluated **without a match timeout**: `New Regex(...)` and calls to the static `Regex` methods (`IsMatch`, `Match`, `Matches`, `Replace`, `Split`, `Count`, `EnumerateMatches`...) that use an overload without a `TimeSpan` parameter.

- **Severity:** Error
- **Category:** Security
- **Code fix:** ✔️ Two lightbulb actions: a finite timeout (1 second by default) or an explicit `Regex.InfiniteMatchTimeout`
- **Configuration:** `devcase_analyzers.dca017_timeout_ms = 1000` (optional)
- **Equivalent to:** SonarQube [S6444](https://rules.sonarsource.com/vbnet/type/Security%20Hotspot) *"Not specifying a timeout for regular expressions is security-sensitive"*

---

## 💡 Why

The .NET regular expression engine uses **backtracking**. A pattern with nested or overlapping quantifiers, such as `(a+)+$` or `(\w+\s?)*$`, can take **exponential time** on certain inputs: a string of 30 `a` characters followed by a `!` is enough to keep a CPU core busy for minutes. This is known as **ReDoS** (*Regular expression Denial of Service*, [CWE-1333](https://cwe.mitre.org/data/definitions/1333.html)).

It does not only affect web servers. In a desktop tool, a scraper or a file processor, an unexpected input (a huge HTML page, a corrupt file, a malformed name) can **freeze the application forever** with no exception and no way to cancel.

By default, `Regex` has **no time limit** (`Regex.InfiniteMatchTimeout`). When a timeout is specified, the engine throws a `RegexMatchTimeoutException` when it expires, and the program regains control.

> [!NOTE]
> The timeout applies to **each matching operation** (each `Match`, `IsMatch`, `Replace` call, or each step of a `Matches` iteration), not to the whole lifetime of the `Regex` instance.

## 🔎 Detection

A call is reported only when an overload exists with the same parameters plus a trailing `matchTimeout` parameter (optionally preceded by `options`). Overloads are matched through the semantic model, so the rule adapts to every target framework (.NET Framework 4.5+ and modern .NET).

Not reported:

| Case | Reason |
|---|---|
| `pattern.Match(input)` (instance methods) | They use the timeout given to the constructor; the rule applies to `New Regex(...)` |
| `RegexOptions.NonBacktracking` as a constant (.NET 7+) | This engine guarantees linear-time matching, so ReDoS is not possible |
| Explicit `Regex.InfiniteMatchTimeout` | A deliberate, visible decision |
| `Regex.Escape`, `Regex.Unescape` | They have no timeout overload (they do not evaluate a pattern) |
| `<GeneratedRegex>` | Configured through the attribute's `matchTimeoutMilliseconds` argument |

> [!IMPORTANT]
> The process-wide setting `AppDomain.CurrentDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT", ...)` is not considered (SonarQube does not consider it either): it is invisible at the call site and easy to lose when code moves to another project.

## ❌ Non-compliant

```vb
Imports System.Text.RegularExpressions

Public NotInheritable Class UrlParser

    Private Shared ReadOnly HrefRegex As New Regex("href=""([^""]+)""", RegexOptions.IgnoreCase)

    Public Shared Function IsValidName(name As String) As Boolean
        Return Regex.IsMatch(name, "^(\w+\s?)*$")
    End Function

    Public Shared Function Clean(text As String) As String
        Return Regex.Replace(text, "\s+", " ")
    End Function

End Class
```

## ✔️ Compliant

```vb
Imports System.Text.RegularExpressions

Public NotInheritable Class UrlParser

    Private Shared ReadOnly HrefRegex As New Regex("href=""([^""]+)""", RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1))

    Public Shared Function IsValidName(name As String) As Boolean
        Return Regex.IsMatch(name, "^(\w+\s?)*$", RegexOptions.None, TimeSpan.FromSeconds(1))
    End Function

    Public Shared Function Clean(text As String) As String
        Return Regex.Replace(text, "\s+", " ", RegexOptions.None, TimeSpan.FromSeconds(1))
    End Function

End Class
```

## 🔧 Code fix

Two lightbulb actions are offered:

| Action | Result | When to use it |
|---|---|---|
| **Add match timeout (`TimeSpan.FromSeconds(1)`)** | `Regex.IsMatch(s, p, RegexOptions.None, TimeSpan.FromSeconds(1))` | Recommended in every case |
| **Add `Regex.InfiniteMatchTimeout`** (explicit opt-out) | `Regex.IsMatch(s, p, RegexOptions.None, Regex.InfiniteMatchTimeout)` | Only for constant patterns that process trusted input, when an exception would be worse than a hang |

Details:

- `RegexOptions.None` is added only when the timeout overload also requires the `options` parameter. Existing options are kept.
- If the call already uses named arguments, the fix also writes named ones (`options:=`, `matchTimeout:=`).
- Type names are written in their shortest valid form at that position (`TimeSpan`, `RegexOptions` or `Text.RegularExpressions.RegexOptions`, depending on the `Imports`).
- Arguments are appended right after the last argument, so multi-line calls stay valid and nested calls (`Regex.Replace(Regex.Replace(...), ...)`) can be fixed together with **Fix All**.
- Calls with omitted arguments (`Regex.Match(a, , b)`) are reported but get no fix.

> [!WARNING]
> Adding a timeout changes behavior: when it expires, `RegexMatchTimeoutException` is thrown instead of the operation running forever. Catch it where the input comes from (a file, the network, the user) and treat it as invalid input:
>
> ```vb
> Try
>     isValid = Regex.IsMatch(name, pattern, RegexOptions.None, TimeSpan.FromSeconds(1))
> Catch ex As RegexMatchTimeoutException
>     isValid = False ' The input is treated as invalid.
> End Try
> ```

## 🤔 Which timeout value?

There is no universal value. The timeout must be far above the normal execution time of the pattern (so it never fires with legitimate input) and far below what is acceptable for a hang:

| Scenario | Suggested value |
|---|---|
| Validating short input (names, e-mails, fields) | 100 – 500 ms |
| General use, desktop applications **(default)** | **1 second** |
| Large text: HTML pages, logs, big files | 2 – 5 seconds |

`TimeSpan.Infinite` / `Regex.InfiniteMatchTimeout` is **not** a valid default: it is exactly the behavior the rule protects against. It is only offered as a second, explicit action.

## ⚙️ Configuration

```ini
[*.vb]
# Match timeout (in milliseconds) added by the code fix. Whole seconds are written with TimeSpan.FromSeconds.
devcase_analyzers.dca017_timeout_ms = 2000
```

Invalid values (non-numeric, zero or negative) are ignored and the default (1000 ms) applies.

## 🔕 How to disable

This rule is reported as an **error** by default. The directives below also suppress analyzer errors; to keep seeing it without blocking the Error List, lower it to `warning` or `suggestion` instead of disabling it.

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCA017.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCA017 ' Reason.
' Code that does not follow the rule.
#Enable Warning DCA017
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "DCA017:Regular expression without match timeout", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Security", "DCA017:Regular expression without match timeout", Justification:="Reason.", Scope:="module")>
```

## 📚 See also

- [Best practices for regular expressions in .NET: use time-out values](https://learn.microsoft.com/dotnet/standard/base-types/best-practices-regex#use-time-out-values)
- [`RegexOptions.NonBacktracking`](https://learn.microsoft.com/dotnet/standard/base-types/regular-expression-options#nonbacktracking-mode) (.NET 7+)
