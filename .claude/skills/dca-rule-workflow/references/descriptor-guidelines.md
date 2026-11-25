# Descriptor guidelines (`DiagnosticIds.vb`, `DiagnosticDescriptors.vb`)

## 1. Identifier

- Use the next free number: `DCA085`, `DCA086`, … IDs are never reused or renumbered.
- `DiagnosticIds`: a `Public Const` whose name says what is reported (`ContainsKeyThenIndexer`, `ValueTypeInReferenceEquals`), with a `<summary>` "The identifier of the rule that reports …".
- `DevCaseDescriptors`: a `Public Shared ReadOnly` field with the same name, with the `<summary>` "The descriptor of rule `<see cref="DiagnosticIds.X"/>`.".
- When adding an ID, update the regex in `DescriptorAndRegistrationTests` (`^DCA0(0[1-9]|[1-7][0-9]|8[0-4])$` today) and the analyzer and provider counts.

```vb
Public Shared ReadOnly ValueTypeInReferenceEquals As New DiagnosticDescriptor(
    id:=DiagnosticIds.ValueTypeInReferenceEquals,
    title:="Value type passed to ReferenceEquals",
    messageFormat:="Do not pass value type '{0}' to 'ReferenceEquals' because each call boxes it into a new object, causing the reference comparison to always return 'False'",
    category:=DevCaseDescriptors.CategoryReliability,
    defaultSeverity:=DiagnosticSeverity.Warning,
    isEnabledByDefault:=True,
    description:="Object.ReferenceEquals compares references. A value-type argument is boxed into a new object for each call, so the comparison is False even for the same value, never a meaningful identity check. Equivalent of CA2013, for .NET Framework projects.")
```

- `isEnabledByDefault` is always `True` (a test enforces it). A rule that must be off by default (DCA010) reads an option (`devcase_analyzers.dca010_enabled`) instead.
- `helpLinkUri` is only set when an external page is the reference (DCA017 → the regex timeout guidance).

## 2. Title

- A noun phrase in sentence case: the *problem*, not the instruction. No trailing period, no quotes, no backticks (code is written plainly).
- The title is reused **verbatim** in the following places, so a change to it means updating all of them:
  - `SuppressMessage("Category", "DCAnnn:Title")` in the rule document;
  - the README rules table, where it may add backticks around code and an emoji;
  - the document header `# 🔍 DCAnnn — Title`, with backticks around code;
  - the CHANGELOG;
  - the `<summary>` of the `DCA Rule Testing` file: `DCAnnn: Title.`

| ❌ Bad | ✔️ Good |
|---|---|
| `Use TryGetValue.` | `ContainsKey followed by the indexer instead of TryGetValue` |
| `Type outside a Namespace block` (wrong once RootNamespace was considered) | `Type declared in the global namespace` |
| `Avoid ReferenceEquals with value types!` | `Value type passed to ReferenceEquals` |

## 3. Message format

**Pattern:** `[what to do]` + `[where '{0}']` + `[why, optional]`.

- One short sentence, starting with an imperative verb: Add, Use, Remove, Rename, Declare, Pass, Replace, Do not…
- `where` names the element with `'{0}'` placeholders.
- `why` is a short clause introduced by `to …`, `because …` or `, causing …`.
- **No trailing period.** RS1032 forbids a period at the end of a single-sentence message, and the Error List reads better without it.
- **No colon followed by an explanation.** Never write "Do X: the compiler does Y, so Z". The Error List is not the place to explain the compiler.
- If two clauses are unavoidable (e.g. a Microsoft message that has two sentences), join them with a **semicolon**, never with a period.
- Quote code, members, values and types-as-code in **single quotes**: `'ConfigureAwait(False)'`, `'{0}.Result'`, `'False'`. Attributes keep their angle brackets: `<STAThread>`, `<ComImport>`.
- Messages must read well in the Error List with real arguments. `DescriptorAndRegistrationTests.MessageFormats_AcceptTheirArguments` formats each one with sample arguments: add the new descriptor there.

### Good vs. Bad (real corrections from this repository)

| ❌ Bad (rejected) | ✔️ Good (shipped) |
|---|---|
| `Use '&' instead of '+' to concatenate strings: '+' is the addition operator and can add numbers instead` | `Concatenate strings using '{1}' instead of '{0}' to avoid operator ambiguity` |
| `Mark 'DCA028.Main' with <STAThread>: Windows Forms needs an STA thread for the clipboard, drag and drop and common dialogs` | `Use <STAThread> on the Windows Forms entry point '{0}' to ensure COM apartment compatibility` |
| `Remove 'Sub New' from the ComImport class 'DCA054': the compiler discards its body, so its code never runs` | `Remove {0} from <ComImport> class '{1}' because the compiler discards its body` |
| `Do not pass value type 'Integer' to 'ReferenceEquals' because each call boxes it into a new object` (incomplete *why*) | `… because each call boxes it into a new object, causing the reference comparison to always return 'False'` |
| `Concatenate strings using '&' instead of '+' to avoid operator ambiguity.` (trailing period) | `Concatenate strings using '&' instead of '+' to avoid operator ambiguity` |
| `Do not use Enumerable methods on indexable collections. Instead use the collection directly` (two sentences) | `Do not use Enumerable methods on indexable collections; instead use the collection directly` |
| `Type '{0}' must be declared inside a Namespace block` (statement, not an action) | `Declare type '{0}' inside a namespace to prevent global naming collisions` |
| `TryGetValue should be used here.` | `Use 'TryGetValue' instead of 'ContainsKey' and the indexer to avoid a double lookup` |

### Replicating a Microsoft rule

- Reuse Microsoft's wording when the user asks for it. Examples:
  - DCA058 = CA1830 `Prefer strongly-typed StringBuilder overload`;
  - DCA022 = CA1826, with a semicolon instead of a period.
- Otherwise, adapt Microsoft's wording to the pattern and generalize it with placeholders. DCA049 generalizes CA1834 to `Use '{1}(Char)' instead of '{1}(String)' when the input is a constant unit string`.
- Name the equivalent in the description: "Equivalent of CAnnnn, for .NET Framework projects."

## 4. Description

- One to three complete sentences, each ending with a period. Explain **why** the code is a problem, plus any important limit.
- Mention when the rule is silent: "Not reported when the project RootNamespace is set, because the types already belong to it."

## 5. Category

Use the private constants of `DevCaseDescriptors`: `Reliability`, `Style`, `Design`, `Documentation`, `Globalization`, `Portability`, `Security`, `Performance`, `Naming`. When replicating a Microsoft rule, use Microsoft's category unless it does not exist here.

## 6. Severity policy

| Severity | Use for | Examples |
|---|---|---|
| **Error** | Code that the compiler accepts but that can **never** work | DCA054 (code in a `<ComImport>` class never runs) |
| **Warning** | Runtime failures, hangs, deadlocks, lost exceptions or data, security issues, hidden defects (duplicated or unreachable code) | DCA001, DCA017, DCA040–DCA043, DCA060–DCA066 |
| **Info** | Style, structure, documentation, naming, readability, migration helpers, micro-performance | DCA003, DCA007, DCA011, DCA030, DCA072–DCA084 |

- Using an API exactly as .NET exposes it is **never** an Error. When unsure, the user decides; ask once with a recommendation.
- Align with the default severity of the Microsoft equivalent. DCA006 (CA1050) and DCA046 (CA2219) were moved from Warning to Info.
- Do not lower a rule to "disabled" just because Microsoft disables its equivalent by default (CA1062, CA2007…); keep it active and tell the user.
- When the severity changes, update `Severities_MatchTheSpecification`, `AnalyzerReleases.Shipped.md` (`### Changed Rules`), README (rules table and Severity table), the rule doc and the CHANGELOG.

## 7. .NET Framework-only rules

- Gate in `OnCompilationStart`:
  ```vb
  If Not UtilAnalyzerSemantic.IsNetFramework(context.Compilation) Then
      Exit Sub ' Modern .NET: the built-in rule CA1840 reports the same code.
  End If
  ```
- Mark the rule *(.NET Framework)* in the following places:
  - the README table title;
  - the README paragraph after the table;
  - the document, with a first line starting with `**.NET Framework only.**` and an `Equivalent to` bullet;
  - `RuleTestingProjectTests.NetFrameworkOnlyRules`.
- The rule's `DCA Rule Testing` file goes only in the net 48 project.

## 8. Release tracking (`AnalyzerReleases.Shipped.md`)

New rules are added directly to the current version's section; `Unshipped.md` stays empty.

```markdown
## Release 1.0.10

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DCA085 | Performance | Info | ExampleAnalyzer

### Changed Rules

Rule ID | New Category | New Severity | Old Category | Old Severity | Notes
--------|--------------|--------------|--------------|--------------|-------
DCA006 | Design | Info | Design | Warning | NamespaceDeclarationAnalyzer
```

`Notes` is the analyzer class name.
