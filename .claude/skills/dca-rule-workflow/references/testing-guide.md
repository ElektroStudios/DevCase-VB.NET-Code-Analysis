# Testing guide (`Source/tests/DevCase.CodeAnalysis.Tests`, xUnit v3, net10.0)

## 1. Test class layout

File: `Rules/DCAnnn_ShortNameTests.vb`. Class: `Public NotInheritable Class DCAnnn_ShortNameTests`, in the namespace `DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests`.

```vb
#Region " DCA080_ManagedThreadIdTests "

Namespace DevCase.Core.Diagnostics.Dotnet.CodeAnalysis.Tests

    ''' <summary>
    ''' Tests of rule DCA080 (Thread.CurrentThread.ManagedThreadId, .NET Framework) and its code fix.
    ''' </summary>
    Public NotInheritable Class DCA080_ManagedThreadIdTests

#Region " Fields "

        ''' <summary>
        ''' The analyzer under test.
        ''' </summary>
        Private Shared ReadOnly Analyzer As New ManagedThreadIdAnalyzer()

        ''' <summary>
        ''' The code fix provider under test.
        ''' </summary>
        Private Shared ReadOnly Provider As New ReplacementCodeFixProvider()

#End Region

#Region " Constructors "

        ''' <summary>
        ''' Initializes a new instance of the <see cref="DCA080_ManagedThreadIdTests"/> class.
        ''' </summary>
        Public Sub New()
        End Sub

#End Region

#Region " Test Methods "

        ''' <summary>
        ''' Verifies that rule DCA080 reports <c>Thread.CurrentThread.ManagedThreadId</c> and ignores other threads.
        ''' </summary>
        '''
        ''' <returns>
        ''' A <see cref="Task"/> that represents the asynchronous test.
        ''' </returns>
        <Fact>
        Public Async Function Reports_CurrentThreadManagedThreadId() As Task
            Await AnalyzerTestHost.VerifyAnalyzerAsync(Analyzer, "
Imports System.Threading

Public Class C
    Public Function Id() As Integer
        Return [|Thread.CurrentThread.ManagedThreadId|]
    End Function
End Class
", targetNetFramework:=True).ConfigureAwait(True)
        End Function

#End Region

    End Class

End Namespace

#End Region
```

- Every test has an XML `<summary>` starting with "Verifies that …", plus a `<returns>` section for async tests.
- Use `ConfigureAwait(True)` in tests and `ConfigureAwait(False)` in `src/`.
- Expected arrays (titles, messages) go into `Private Shared ReadOnly` fields, never inline (CA1861/DCA084).
- Test sources are VB string literals: `""` escapes a quote, and `'''` inside the literal is fine.

## 2. Test host API (`TestHost/AnalyzerTestHost.vb`)

| Method | Use |
|---|---|
| `VerifyAnalyzerAsync(analyzer, markup, editorConfig, allowCompilerErrors, targetNetFramework, optionStatements)` | Exact spans: `[| |]` marks each expected diagnostic, and anything else reported fails the test. Returns the diagnostics (for message asserts). |
| `VerifyCodeFixAsync(analyzer, provider, markup, expected, equivalenceKey:="DCAnnn.Key", editorConfig, allowRemainingDiagnostics, allowCompilerErrors, targetNetFramework, optionStatements)` | Applies the fix repeatedly (re-parsing between passes like the IDE), compares the text, checks that it compiles, and checks that no diagnostic remains. |
| `ApplyFixAllInDocumentAsync(...)` | Fix All in document; assert the merged text. |
| `GetRegisteredActionsAsync(analyzer, provider, source, targetNetFramework)` | The offered action titles. Assert `Array.Empty(Of String)()` when the fix must not be offered. |
| `CreateProject(...)` / `GetAnalyzerDiagnosticsAsync(project, analyzers)` | Multi-file and custom project cases (e.g. `rootNamespace:="MyApp"`). |

Built-in guarantees, which you must not duplicate by hand:

- **LF and CRLF.** Every analyzer, fix and Fix All test runs twice, with LF and with CRLF, and fails on mixed line endings.
- **Syntax variations** (`TestHost/SyntaxVariations.vb`). Unless `allowCompilerErrors` is set, every analyzer and fix test runs again on six rewrites of its source:

  | Variation | Rewrite |
  |---|---|
  | `Parentheses` | Redundant parentheses on conditions, `Return` values, initializers, assigned values and ternary conditions |
  | `LineContinuations` | ` _` after commas, `(`, member-access dots, `AndAlso`, `OrElse` and `&` |
  | `Regions` | Method bodies inside `#Region " Variation "` |
  | `ConditionalCompilation` | Method bodies inside `#If True Then` |
  | `UsingBlock` | Method bodies inside `Using variationScope As New IO.MemoryStream()` |
  | `Comments` | A comment line before every statement |

  Each variant must compile and report the same diagnostic counts per ID, and the fix must still compile with no diagnostic left.
  - If a variation legitimately changes the result, add `{"DCAnnn:Variation", "reason"}` to `SyntaxVariations.Exemptions` (today: DCA011 and DCA013 under `ConditionalCompilation`, because they are silent inside `#If` by design).
  - **Never** exempt a rule to hide a bug: fix the rule.
  - When you find a new shape of code that broke a rule, consider adding a variation, so that every rule gets it.
- **.NET Framework 4.8 reference assemblies.** `targetNetFramework:=True` compiles against the real .NET Framework 4.8 reference assemblies.

## 3. Required tests per rule

| Test | Content |
|---|---|
| `Reports_*` | Every syntax form from the spec, with exact spans. Include named arguments, the overloads with extra arguments, `Me.`-qualified receivers, single-line and multi-line forms, the ternary, `Not`, `AndAlso`/`OrElse`, guard clauses and `ElseIf` where applicable. |
| `DoesNotReport_*` | False positives: look-alike names, other types, lambdas, partial matches, side effects, directives inside, and generated files where relevant. |
| `Fix_*` | One test per action (`equivalenceKey:="DCAnnn.Key"`). Also test comment preservation (end-of-line and full-line), `#Region` and `#If` around the code, and `Option Strict Off` / `Option Infer On` via `optionStatements:=(OptionStrict.Off, True, True)` when the fix writes types or literals. |
| `FixAll_*` | Several diagnostics in one document: unique names and merged insertions. |
| `Actions_*` | The offered titles in order, and `Actions_None*` where the fix must not be offered (lost comment, single-line lambda, failed binding). |
| `Message_*` | The full formatted message for a typical case. |
| Framework gate | .NET Framework-only rules: nothing is reported with `targetNetFramework:=False`. |

When fixing a false positive or negative reported by the user, first add a failing test with the user's exact code, then fix the rule.

## 4. Infrastructure tests to update

| File | What to change |
|---|---|
| `Infrastructure/DescriptorAndRegistrationTests.vb` | `AnalyzerAndProviderCounts_MatchTheRuleSet` (rules and providers), the ID regex `^DCA0(…)$`, `Severities_MatchTheSpecification` (`errors` / `infos` lists), `MessageFormats_AcceptTheirArguments` (sample arguments for the new descriptor) |
| `Infrastructure/RuleTestingProjectTests.vb` | `NetFrameworkOnlyRules` for .NET Framework-only rules. Also checks that every rule has its `DCAnnn.vb` file, triggers only its rule, and survives every code fix with `#Region` and `#If` decorations |
| `Infrastructure/SelfAnalysisTests.vb` | No change needed: it runs every DCA rule on the analyzer source and forbids recursion. New code must pass it |
| `Infrastructure/SuppressionTests.vb` | Check that the documented suppressions still work if you add a new suppression form |

## 5. Running

```bash
export PATH=/opt/dotnet:$PATH DOTNET_ROOT=/opt/dotnet
cd Source
dotnet build tests/DevCase.CodeAnalysis.Tests -c Release 2>&1 | grep -E " warning | error |Warning\(s\)|Error\(s\)"
dotnet test tests/DevCase.CodeAnalysis.Tests -c Release --no-build 2>&1 | grep -E "Passed!|Failed!|\[FAIL\]"
```

- Report the exact pass count to the user ("543/543"), never "all tests pass" without running them.
- A failing variation message shows the variation name and the rewritten source: read it before you change anything.
- The IDE code-style analyzers (IDE0xxx) are enforced in the build through `EnforceCodeStyleInBuild`. For a deeper check of new files, a scratch tool can run the SDK's `codestyle/vb` analyzers at Warn severity. Hidden-level hints such as IDE0058 are acceptable.
