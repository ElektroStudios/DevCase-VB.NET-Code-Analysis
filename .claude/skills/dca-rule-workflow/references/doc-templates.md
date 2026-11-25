# Documentation templates and checklists

All Markdown files are UTF-8 without BOM, with LF line endings. Write in English, in short sentences. Code samples are VB.NET (```` ```vb ````).

## 1. Rule document: `Docs/Rules/DCAnnn_Title_With_Underscores.md`

The file name is the ID plus a short title with underscores (`DCA081_ContainsKey_Then_Indexer.md`). README links point to it, so a rename means updating every link.

Section order is fixed; omit only the sections marked optional:

````markdown
# 🔍 DCA081 — `ContainsKey` followed by the indexer instead of `TryGetValue`

**.NET Framework only.** Reports `map.ContainsKey(key)` checks followed by reads of `map(key)` where the key is known to exist.

- **Severity:** Info
- **Category:** Performance
- **Code fix:** ✔️ Declares the value and uses `TryGetValue`
- **Equivalent to:** Microsoft [CA1854](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1854) *"Prefer the 'IDictionary.TryGetValue(TKey, out TValue)' method"*

---

## 💡 Why

What goes wrong and why it matters, in two to five sentences.

On modern .NET, the built-in rule CA1854 already reports this code, so this rule stays silent there.

## 🔎 Detection

What exactly is reported (use a table for the syntax forms), and what is deliberately not reported, with the reason.

## ❌ Non-compliant

```vb
If ages.ContainsKey(name) Then
    Return ages(name)
End If
```

## ✔️ Compliant

```vb
Dim value As Integer = Nothing
If ages.TryGetValue(name, value) Then
    Return value
End If
```

## 🔧 Code fix

What each action writes, in order (a table when there are several), with before/after code. Also state when the fix is NOT offered, and why.

## ⚙️ Configuration                      (optional: only for rules with devcase_analyzers.* options)

## 🔕 How to disable

(the standard block below, with the exact category and title)
````

Rules for the header block:

- **Header.** `# <emoji> DCAnnn — <descriptor title, with backticks around code>`. Use an em dash `—`.
- **First line.** It starts with `**.NET Framework only.**` for .NET Framework-only rules; otherwise it starts with "Reports …".
- **`Code fix` bullet.**
  - `✔️ <what it does>` when there is one action; list them (`Adds X, Y or Z`) when there are several.
  - Write `✔️ (×2)` in the README when there are two equivalent actions.
  - Write `❌ None (<reason>)` when the rule only reports.
- **`Equivalent to` bullet.** Microsoft `[CAnnnn](learn link)` or SonarQube `[Snnnn](https://rules.sonarsource.com/vbnet/RSPEC-nnnn/)`, followed by the original title in italics and quotes.
- **`Configuration` bullet.** Add it for rules with options: `` `devcase_analyzers.x = value` (optional) ``.
- **Every claim is true for the current code.** When the rule changes, re-read the whole document and update Detection, Code fix, the examples and "not offered when …" sentences (e.g. "comment lines are kept" replaced "no fix when the statement has comments").

### Standard "How to disable" section

Replace `CATEGORY`, `DCAnnn` and `TITLE`; the title must be the descriptor title **verbatim**, so that `SuppressMessage` matches.

````markdown
## 🔕 How to disable

Use the narrowest scope that fits, and write down the reason.

**Solution, project or folder**: in the `.editorconfig` file that applies to that scope:

```ini
[*.vb]
dotnet_diagnostic.DCAnnn.severity = none
```

To keep the rule but change how it is shown, use `suggestion`, `warning` or `error` instead of `none`.

**A few lines of code**: with preprocessor directives:

```vb
#Disable Warning DCAnnn ' Reason.
' Code that does not follow the rule.
#Enable Warning DCAnnn
```

**A type or member**: with an attribute:

```vb
<System.Diagnostics.CodeAnalysis.SuppressMessage("CATEGORY", "DCAnnn:TITLE", Justification:="Reason.")>
```

**The whole project, from code**: in a `GlobalSuppressions.vb` file:

```vb
<Assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("CATEGORY", "DCAnnn:TITLE", Justification:="Reason.", Scope:="module")>
```
````

## 2. `DCA Rule Testing` file: `RuleTesting/DCA Rule Testing (net 48)/DCAnnn.vb`

- Encoding: UTF-8 **with BOM**, LF line endings. Lines wrap at about 120 columns. Namespace: `DCA_Rule_Testing`. Class name: `DCAnnn`.
- **net 48 project.** It is a legacy-format `.vbproj`: add `<Compile Include="DCAnnn.vb" />`.
- **net 10 project.** It is SDK-style (files are included automatically). Add the file there **only** if the rule also runs on modern .NET.
- **Exclusive trigger.** The file must trigger **only** its rule, also after its code fix (`RuleTestingProjectTests` checks it). Mark each reported line with a trailing `' DCAnnn` comment.

```vb
#Region " Option Statements "

Option Strict On
Option Explicit On
Option Infer Off

#End Region

#Region " Imports "

Imports System.Collections.Generic
Imports System.Diagnostics.CodeAnalysis

#End Region

#Region " DCA081 "

Namespace DCA_Rule_Testing

    ''' <summary>
    ''' DCA081: ContainsKey followed by the indexer instead of TryGetValue.
    ''' </summary>
    '''
    ''' <remarks>
    ''' <para>
    ''' Expected: DCA081 (Info) on <c>ages.ContainsKey(id)</c>, followed by a read of <c>ages(id)</c>. This rule runs only in
    ''' .NET Framework projects (on modern .NET, the built-in rule CA1854 reports the same code), so this file is only in the
    ''' .NET Framework project. The rule also reports the <c>If(...)</c> form that IDE0046 produces.
    ''' </para>
    ''' <para>
    ''' Code fix (Ctrl+.): declares <c>Dim value As Integer = Nothing</c> and calls <c>ages.TryGetValue(id, value)</c>.
    ''' </para>
    ''' </remarks>
    Public NotInheritable Class DCA081

#Region " Constructors "

        ''' <summary>
        ''' Prevents a default instance of the <see cref="DCA081"/> class from being created.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

#Region " Members "

        ''' <summary>
        ''' Gets the age of a person.
        ''' </summary>
        '''
        ''' <param name="ages">
        ''' The ages, by identifier.
        ''' </param>
        '''
        ''' <param name="id">
        ''' The identifier.
        ''' </param>
        '''
        ''' <returns>
        ''' The age, or 0.
        ''' </returns>
        <SuppressMessage("Style", "IDE0046:Convert to conditional expression",
                         Justification:="The rule is about using TryGetValue instead of ContainsKey followed by the indexer, so it is used in the code fix.")>
        Friend Shared Function GetAge(ages As Dictionary(Of Integer, Integer), id As Integer) As Integer

            If ages.ContainsKey(id) Then ' DCA081
                Return ages(id)
            End If
            Return 0
        End Function

#End Region

    End Class

End Namespace

#End Region
```

Suppressions in `DCA Rule Testing` (the only place where they are allowed):

- Use one only for a diagnostic that the demonstrated code triggers **on purpose**: IDE0046 on DCA081, IDE0060 on DCA071 (the unused token *is* the subject), BC40000 on DCA038.
- Prefer a `SuppressMessage` on the member, with a `Justification` that names the rule. Use a commented `#Disable Warning` only for compiler warnings.
- Justification texts have no leading or trailing spaces. Use the user's wording when they provide it.

## 3. `README.md` checklist (root)

For every new rule, renamed rule, reworded rule, or change of severity or category:

- [ ] **Rules table row:**
  ```markdown
  | [`DCAnnn`](Docs/Rules/DCAnnn_Title.md) | <emoji> [Title with `code` *(.NET Framework)*](Docs/Rules/DCAnnn_Title.md) | Info | Performance | ✔️ |
  ```
- [ ] **Severity table.** The Warning list (and Error list) of rule IDs, written as ranges (`DCA060–DCA066`).
- [ ] **Info list.** The Info-level list in *Getting Started* (the note about the *Messages* tab), written as ranges.
- [ ] **.NET Framework paragraph.** The paragraph after the table that lists the .NET Framework-only rules and the replicated Microsoft rules.
- [ ] **`DCA Rule Testing` table.** The `DCA001.vb … DCAnnn.vb` range, and the list of rules missing from the net 10 project.
- [ ] **Text sections.** The *Configuration* section, if the rule has options, and the *Features* / *Quality* sections, if the change adds a capability.
- [ ] **Links.** Every link in the README resolves (case-sensitive file names).

When the user also maintains the Visual Studio Marketplace README (absolute GitHub links, no relative paths), apply the same row and list changes there.

## 4. `Docs/CHANGELOG.md`

- **Version headers.** The current version is `## vX.Y.Z *(current)* 🆕`; when you add a new one, the previous header becomes `## vX.Y.Z 🔖`.
- **Bumps.** A new version requires the matching bumps (`Directory.Build.props`, VSIX manifest `Identity Version`, `AnalyzerReleases.Shipped.md`). When the user says "same commit / same changelog", add to the current section and bump nothing.
- **Sections.** Use them in this order, only those that apply:
  - a themed `### <emoji> <headline>` section for big features (e.g. a table of new rules with *Replicates*, *Severity* and *Code fix* columns);
  - `### Changes`: behavior changes, new detected forms, message changes (quote the new message in italics), severity changes;
  - `### Fixes`: false positives and negatives, and broken fixes, each with the failing code;
  - `### Rule testing solution`;
  - `### Tests`.
- **Bullet style.** Each bullet starts with the bold rule ID: `- **DCA081** recognizes …`. Explain the user-visible effect, not the implementation.

## 5. Commit message (English; the user commits)

```
<Imperative summary of the whole change set, ≤ 72 chars if possible>

- DCAnnn: what changed for the user.
- DCAnnn: ...
- Tests: ...
- Rule testing: ...
- Docs: README, CHANGELOG, rule documents.
```

No attribution lines. If the user asked to put the changes in an existing commit, write one message that covers everything in it.
