---
name: dca-rule-workflow
description: Use whenever you create, modify, refactor, test or document a DCA rule (DCAnnn analyzer, descriptor or code fix) in the DevCase-VB.NET-Code-Analysis repository, including messages, severities, the DCA Rule Testing projects, Docs/Rules, README and CHANGELOG. Enforces the repository's VB.NET standards, message wording, Roslyn VB pitfalls and the definition of done.
---

# DCA Rule Workflow

Standing rules for every change to a `DCAnnn` rule of **DevCase VB.NET Code Analysis**: Roslyn analyzers and code fixes for Visual Basic .NET, written in VB.NET, shipped as a VSIX for Visual Studio 2022/2026.

Detailed references, loaded on demand:

- [references/descriptor-guidelines.md](references/descriptor-guidelines.md): IDs, titles, messages (with Good vs. Bad), descriptions, severities, categories, release tracking.
- [references/analyzer-and-fix-patterns.md](references/analyzer-and-fix-patterns.md): class skeletons, registration, the `FixReplacement` protocol, fix rules.
- [references/roslyn-vb-pitfalls.md](references/roslyn-vb-pitfalls.md): every VB.NET syntax quirk and API trap already paid for, with Good vs. Bad code.
- [references/testing-guide.md](references/testing-guide.md): test host API, required test cases, syntax variations, infrastructure tests to update.
- [references/doc-templates.md](references/doc-templates.md): rule document, `DCA Rule Testing` file, README and CHANGELOG templates and checklists.

---

## 1. Non-negotiable constraints

1. **Zero warnings.** The solution, the tests and both `DCA Rule Testing` projects build with 0 warnings and 0 errors. Fix the code: document it, add `ConfigureAwait`, validate arguments. **Never** silence a diagnostic with `<NoWarn>`, `#Disable Warning`, `SuppressMessage` or `.editorconfig` severity changes in `src/` or `tests/`.
   - The only exception is a `DCAnnn.vb` file of `DCA Rule Testing` whose demonstration code triggers another diagnostic on purpose (IDE0046, IDE0060, BC40000). There, suppress at the narrowest scope with a `Justification` that names the rule (see [doc-templates.md](references/doc-templates.md)).
2. **Every syntax form, not one.** A rule must recognize all the ways VB.NET writes the same logic: parentheses, `Not`, `AndAlso`/`OrElse`, ternary `If(...)`, single-line `If`, guard clauses, `ElseIf`, named arguments, extra overload arguments, `Call`, `_` continuations, comments, `#Region`/`#If`. The test host reruns every test on six syntax variations, but it only covers the code that the tests contain. Write the forms into the tests.
3. **Behavior first.** A fix never changes what the code does silently. When it offers several actions, the **first one keeps the current behavior** and only makes it explicit (e.g. `New UTF8Encoding(False)` for an API that writes UTF-8 without BOM, `DesignerSerializationVisibility.Visible`). If no behavior-preserving rewrite exists, report only.
4. **Fixes always compile.** The analyzer validates every replacement with speculative binding (`UtilSpeculativeBinding`) at the position of the code. If the replacement does not bind to the expected symbol, it offers no action and still reports.
5. **Never invent content.** A fix writes only what it can derive from the code. For example, `<exception cref="X"></exception>` stays empty and the condition is not guessed. Default `<param>` texts are derived from the parameter name and type.
6. **No hardcoded project values.** This is a public analyzer: nothing that a fix writes may contain `DevCase`, a fixed namespace, or a fixed path. Derive names from the code, the options or the project (e.g. the DCA006 namespace order: option → namespace already in the file → sanitized assembly name → no fix).
7. **Preserve trivia.** Fixes keep comments (end-of-line and full-line), `#Region`/`#End Region` pairs, `#If` blocks, indentation and the file's line ending (`UtilAnalyzerSyntax.GetEndOfLine`). A fix that would lose a comment or a directive is not offered.
8. **Verify before asserting.** Check every claim about the compiler, the runtime, the BCL or Visual Studio with a compiled probe or the official documentation before you write code or reply. This applies to claims from the user, from other AIs and from your own memory. Recorded cases are in [roslyn-vb-pitfalls.md](references/roslyn-vb-pitfalls.md) §9.
9. **Apply derived changes; don't just suggest them.** If a change affects docs, README, CHANGELOG, `DCA Rule Testing`, tests, versions or the VSIX manifest, update them in the same delivery.
10. **Answer questions honestly before changing code.** When the user asks "is this a bug or correct?", answer with evidence (a compiled sample, the spec). Change the rule only if it is wrong.

## 2. Communication and delivery

- Reply to the user in **Spanish**. Write all code, identifiers, comments, XML docs, Markdown docs, messages and commits in **English**.
- After any change set, always give an **English commit title and body**. The user commits; you never commit, push or add attribution lines.
- **Versioning:**
  - Adding a CHANGELOG version implies bumping, in the same delivery:
    - `Source/Directory.Build.props` (`Version`, `AssemblyVersion`, `FileVersion`);
    - `Identity Version` in `source.extension.vsixmanifest`;
    - a `## Release x.y.z` section in `AnalyzerReleases.Shipped.md`.
  - When the user says "same commit / same changelog", do **not** bump: merge the changes into the current version section.
- **Delivery:** a ZIP of `git ls-files -co --exclude-standard` without `bin/` and `obj/`, sent with `SendUserFile`. The user's local repository may have commits you do not have: pull or ask before editing files the user changed.
- When an instruction is ambiguous, pick the most reasonable reading, apply it, and state it in the reply (e.g. "I read 'the same for DCA081.vb' as the IDE0046 suppression, because IDE0060 does not fire there").

## 3. Repository map

| Path | Content |
|---|---|
| `Source/src/DevCase.CodeAnalysis.Analyzers/` | `DiagnosticIds.vb`, `DiagnosticDescriptors.vb` (`DevCaseDescriptors`), `DevCaseAnalyzerOptions.vb`, `AnalyzerReleases.*.md`, `Analyzers/*Analyzer.vb`, `Utilities/Util*.vb` |
| `Source/src/DevCase.CodeAnalysis.CodeFixes/CodeFixes/` | `ReplacementCodeFixProvider.vb` (generic, driven by diagnostic properties) and dedicated `*CodeFixProvider.vb` |
| `Source/src/DevCase.CodeAnalysis.Vsix/` | VSIX project and `source.extension.vsixmanifest` |
| `Source/tests/DevCase.CodeAnalysis.Tests/` | `Rules/DCAnnn_*Tests.vb`, `Infrastructure/*.vb`, `TestHost/AnalyzerTestHost.vb`, `TestHost/SyntaxVariations.vb` |
| `RuleTesting/DCA Rule Testing (net 48)/` | Legacy `.vbproj`, one `DCAnnn.vb` per rule (all 84+), each needs `<Compile Include>` |
| `RuleTesting/DCA Rule Testing (net 10)/` | SDK `.vbproj` (`net10.0-windows`), one file per rule **except** .NET Framework-only rules |
| `Docs/Rules/DCAnnn_Title_With_Underscores.md` | One document per rule |
| `Docs/CHANGELOG.md`, `README.md` | Change log and the root README (rules table and rule lists) |

The namespace of every project is `DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.<Analyzers|CodeFixes|Tests>`; `DCA Rule Testing` uses `DCA_Rule_Testing`.

## 4. VB.NET coding standard (applies to src, tests and RuleTesting)

- File header, in this order:
  - `#Region " Option Statements "` containing `Option Strict On`, `Option Explicit On` and `Option Infer Off`;
  - `#Region " Imports "`, grouped as System, then Microsoft, then DevCase, then Xunit, with a blank line between groups;
  - `#Region " TypeName "` wrapping the namespace.
- Inside types, use `#Region` blocks in this order: `Fields`/`Constants`, `Properties`, `Constructors`, `Public Methods`, `Friend Methods`, `Private Methods`.
- Every variable, lambda parameter and return value is explicitly typed; every conversion is explicit (`DirectCast`, `TryCast`, `CType`). Never use `Dim x = ...`.
- Use string interpolation `$"..."` instead of `&`.
- **No recursion.** Walk trees iteratively with `AncestorsAndSelf`, `DescendantNodes`, or a `Stack`/`Queue`. `SelfAnalysisTests` fails on recursion.
- **Fail fast:**
  - Validate arguments with `Throw New ArgumentNullException(NameOf(x))` and similar.
  - A silent `If x Is Nothing Then Return` on a parameter is forbidden: DCA003 reports it on our own code.
  - A legitimate "nothing to do" exit is a documented early `Exit Sub` with a comment explaining it.
- **`ConfigureAwait`:** use `ConfigureAwait(False)` on every `Await` in `src/`, and `ConfigureAwait(True)` in tests.
- **XML documentation on every member, private ones included:**
  - `<summary>`, then `<param>`, `<returns>`, `<exception>` and `<remarks>` as applicable, each section separated by one `'''` line.
  - Constructors use the standard texts ("Initializes a new instance of the `<see cref="T"/>` class." or, when private, "Prevents a default instance of the `<see cref="T"/>` class from being created.").
  - Document every thrown exception (DCA012).
- `NotInheritable` classes; private constructors for static-only classes.
- **File encoding:**
  - `src/`, `tests/` `.vb` files and all `.md` docs: UTF-8 **without** BOM, LF.
  - `RuleTesting` `.vb` files: UTF-8 **with** BOM, LF.
  - Never change the encoding or line endings of an existing file.
- Lines wrap at about 150 columns in `src/`/`tests/` and about 120 in `RuleTesting`.

## 5. Descriptor essentials

Full rules and Good vs. Bad examples are in [descriptor-guidelines.md](references/descriptor-guidelines.md).

- **ID:** the next free `DCAnnn`, with a constant in `DiagnosticIds` and a descriptor in `DevCaseDescriptors`. One analyzer supports exactly one descriptor.
- **Title:** a noun phrase in sentence case with no trailing period. Docs and `SuppressMessage` reuse it **verbatim**: `"Category", "DCAnnn:Title"`.
- **Message:** `[what to do]` + `[where '{0}']` + optional `[why: "to …" / "because …"]`.
  - It is one short imperative sentence with **no trailing period**, no colon explanations, and code in single quotes.
  - If two clauses are unavoidable, join them with a semicolon (RS1032).
- **Description:** one to three full sentences ending with a period. It explains why, and names the equivalent rule when one exists ("Equivalent of CA2013, for .NET Framework projects.").
- **Severity policy:**
  - **Error** only for code that compiles but can never work (DCA054).
  - **Warning** for runtime failures, hangs, lost exceptions or data, and hidden defects.
  - **Info** for style, documentation, naming, readability and micro-performance.
  - Using an API exactly as .NET exposes it is never an Error.
  - When Microsoft has an equivalent rule, match its default severity.
- **Microsoft overlap:** if a built-in CA, IDE or WFO rule reports the same code on modern .NET, gate the analyzer with `UtilAnalyzerSemantic.IsNetFramework` in `RegisterCompilationStartAction` and mark the rule *(.NET Framework)* everywhere. Never report the same line twice.

## 6. Definition of Done: every new rule or rule change

Copy this checklist into the task list and tick every item. A change is not done while any item is open.

**A. Specification**
- [ ] Search Microsoft's CA/IDE/WFO rules and SonarQube VB.NET rules for an equivalent. Decide: new rule, .NET Framework-only replica, or not worth it. Cite the equivalent in the description and the doc.
- [ ] Decide the severity and category with the policy in §5. When replicating a Microsoft rule, reuse its message wording, adapted to the pattern (see the guidelines).
- [ ] List every syntax form the rule must recognize, and every form it must **not** report (false positives). Use [roslyn-vb-pitfalls.md](references/roslyn-vb-pitfalls.md) as the checklist.

**B. Implementation**
- [ ] `DiagnosticIds` constant and `DevCaseDescriptors` field, both with XML docs.
- [ ] Analyzer in `Analyzers/` following [analyzer-and-fix-patterns.md](references/analyzer-and-fix-patterns.md): `ConfigureGeneratedCodeAnalysis(None)`, `EnableConcurrentExecution()`, `UtilAnalyzerFiles.SkipExcluded*`, the .NET Framework gate if applicable.
- [ ] Fix: prefer computing it in the analyzer (`FixReplacement` + `UtilFixProperties.CreateDiagnostic`) and applying it with `ReplacementCodeFixProvider`. Use a dedicated provider only when a fix needs the workspace (other documents, formatting). Every provider supports Fix All.
- [ ] Equivalence keys are `DCAnnn.Key` and action titles are imperative, without a period ("Use Environment.CurrentManagedThreadId").
- [ ] `AnalyzerReleases.Shipped.md`: add a row under `### New Rules` of the current `## Release` section, or under `### Changed Rules` for a severity or category change.

**C. Tests** ([testing-guide.md](references/testing-guide.md))
- [ ] `Rules/DCAnnn_NameTests.vb` with:
  - `Reports_*` tests covering every form, with exact `[| |]` spans;
  - `DoesNotReport_*` tests for false positives;
  - `Fix_*` tests for each action (by equivalence key), including comment and `#Region` preservation;
  - `FixAll_*` and `Actions_*` tests (offered titles, and no action when unsafe);
  - `Message_*` tests;
  - for .NET Framework-only rules, a "nothing on modern .NET" test;
  - for fixes sensitive to project options, a test with `optionStatements:=(OptionStrict.Off, True, True)`.
- [ ] Infrastructure:
  - `DescriptorAndRegistrationTests`: the analyzer and provider counts, the ID regex, the `infos`/`errors` severity lists and the `MessageFormats` samples;
  - `RuleTestingProjectTests.NetFrameworkOnlyRules`, if the rule is .NET Framework-only.
- [ ] If a syntax variation legitimately changes the result, add an entry to `SyntaxVariations.Exemptions` with a reason. Add one **only** when the behavior is by design; otherwise fix the rule.

**D. DCA Rule Testing**
- [ ] Create or update `RuleTesting/DCA Rule Testing (net 48)/DCAnnn.vb` (BOM, LF) and add its `<Compile Include>`. If the rule also runs on modern .NET, add `RuleTesting/DCA Rule Testing (net 10)/DCAnnn.vb` as well.
- [ ] The file triggers **only** its rule: no other DCA, CA, IDE or BC diagnostic, also after its code fix is applied with `#Region` and `#If` decorations (`RuleTestingProjectTests` checks this).
- [ ] Run Visual Studio's own IDE fixes mentally or with the scratch tool. If an IDE simplification (e.g. IDE0046 → ternary) would rewrite the demonstrated code, make sure the DCA rule **still fires on the simplified form**, then suppress the IDE suggestion with a justification.
- [ ] Ask the user to verify the file in Visual Studio. State what you could not run (the VSIX and the IDE cannot run in the Linux sandbox).

**E. Documentation**
- [ ] `Docs/Rules/DCAnnn_Title.md` with the exact layout of [doc-templates.md](references/doc-templates.md): header, metadata bullets, Why, Detection, Non-compliant, Compliant, Code fix, and How to disable (with the exact category and title).
- [ ] `README.md`:
  - the rules table row;
  - the Severity table lists;
  - the Info-level list in *Getting Started*;
  - the *(.NET Framework)* paragraph;
  - the `DCA Rule Testing` table ranges;
  - any rule count.
- [ ] `Docs/CHANGELOG.md`: new rules, changed behavior, fixed false positives and negatives, Rule testing solution, Tests.
- [ ] If the user maintains a Marketplace README copy, remind them that it needs the same row and lists.

**F. Final consistency check** (run every time)
- [ ] The ID, title, category, severity, message and *(.NET Framework)* flag match 1:1 across the descriptor, `AnalyzerReleases`, `README.md`, `Docs/Rules/DCAnnn_*.md` (header and How to disable), `DCA Rule Testing` (summary line, `' DCAnnn` markers, expected severity) and the CHANGELOG.
- [ ] Build `Source/tests/DevCase.CodeAnalysis.Tests` in Release: **0 warnings**. Then run the full test suite: **all green**.
- [ ] Encoding and line-ending check over every modified `.vb` and `.md` file (§4).
- [ ] Check every relative link in the new and changed docs (`Docs/Rules/...`).
- [ ] Repackage the ZIP and write the Spanish summary with the English commit title and body.

## 7. Build and verification commands (Linux sandbox)

```bash
export PATH=/opt/dotnet:$PATH DOTNET_ROOT=/opt/dotnet
cd Source
dotnet build tests/DevCase.CodeAnalysis.Tests -c Release 2>&1 | grep -E " warning | error |Warning\(s\)|Error\(s\)"
dotnet test tests/DevCase.CodeAnalysis.Tests -c Release --no-build 2>&1 | grep -E "Passed!|Failed!|\[FAIL\]"
dotnet test tests/DevCase.CodeAnalysis.Tests -c Release --no-build --filter "FullyQualifiedName~DCA081"
```

- The VSIX can only be built on Windows with `msbuild /restore /p:Configuration=Release`; `dotnet build` does not create the `.vsix`.
- To check a claim about the compiler, write a minimal VB probe and compile it against the real reference assemblies (.NET Framework 4.8 via `Microsoft.NETFramework.ReferenceAssemblies.net48`) before relying on it.
- Write VB code from Python with heredoc files, never with Python `'''` strings, which collide with VB `'''` doc comments.
- Never re-run old one-off generator scripts: they are stale and overwrite later fixes.

## 8. Top pitfalls (details and examples in the references)

1. `IsKind`, `TypeOf` and `GetSymbolInfo` on an expression fail when it is parenthesized.
   - In syntax: use `UtilAnalyzerSyntax.SkipParentheses` / `WalkUpParentheses`.
   - In operations: VB produces `IParenthesizedOperation`, so skip it together with conversions.
2. Parentheses break array-literal target typing: `Dim a As Long() = ({1, 2})` is BC30332. A fix that removes `New Long()` must remove the parentheses too.
3. Detect the logic, not the statement shape. `If x.ContainsKey(k) Then … x(k)`, `If(x.ContainsKey(k), x(k), 0)`, `If Not x.ContainsKey(k) Then Return` followed by `x(k)` are all the same rule.
4. Overloads and named arguments:
   - `New ArgumentNullException(NameOf(p), "msg")` and `paramName:=NameOf(p)` must match like `New ArgumentNullException(NameOf(p))`.
   - A fix that changes the called method must rename named arguments (`Any(predicate:=…)` → `Exists(match:=…)`).
5. `Not x Is Nothing` equals `x IsNot Nothing`, and `Throw ex` is the same smell as `Throw` in a rethrow-only `Catch`.
6. Removing a member must not remove the `#Region` line in its leading trivia (DCA054 left a lone `#End Region`). A statement with inner directives is not reported.
7. Full-line comments inside a removed block move before the replacement. A comment after a `_` mid-statement blocks the fix.
8. A type without `Namespace` in a project with `RootNamespace` is not in the global namespace: check `ContainingNamespace.IsGlobalNamespace`.
9. `Await` fixes need parentheses around `o?.M()` and nested `Await`. `Task.Yield()` has no `ConfigureAwait`.
10. `ByRef` parameters cannot be used inside lambdas (BC36639). Use `AncestorsAndSelf()` when the starting node itself may be the lambda body.
