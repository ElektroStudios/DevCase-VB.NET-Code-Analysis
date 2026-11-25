# Roslyn and VB.NET pitfalls (learned the hard way)

Each entry is a real defect that shipped or was caught in this repository. Check the whole list before you finish any rule.

## 1. Parentheses

VB keeps redundant parentheses in **both** the syntax tree (`ParenthesizedExpressionSyntax`) and the operation tree (`IParenthesizedOperation`, which C# does not have). `GetSymbolInfo` on a parenthesized expression returns no symbol.

```vb
' ❌ Bad: misses "Return (Nothing)" (DCA003), "= (DateTime.Now)" (DCA044), "Prop = (culture)" (DCA050)
Return returnStatement.Expression.IsKind(SyntaxKind.NothingLiteralExpression)
Dim symbol As ISymbol = model.GetSymbolInfo(assignment.Right).Symbol
Dim parent As SyntaxNode = use.Parent

' ✔️ Good
Return UtilAnalyzerSyntax.SkipParentheses(returnStatement.Expression).IsKind(SyntaxKind.NothingLiteralExpression)
Dim symbol As ISymbol = model.GetSymbolInfo(UtilAnalyzerSyntax.SkipParentheses(assignment.Right)).Symbol
Dim value As ExpressionSyntax = UtilAnalyzerSyntax.WalkUpParentheses(use) ' then compare "... Is value", use value.Parent
```

```vb
' ❌ Bad: skips conversions only
While TypeOf current Is IConversionOperation
    current = DirectCast(current, IConversionOperation).Operand
End While

' ✔️ Good
While TypeOf current Is IConversionOperation OrElse TypeOf current Is IParenthesizedOperation
    current = If(TypeOf current Is IConversionOperation,
                 DirectCast(current, IConversionOperation).Operand,
                 DirectCast(current, IParenthesizedOperation).Operand)
End While
```

**Parentheses break target typing.** An array literal in parentheses gets its natural type:

```vb
Dim values As Long() = ({1, 2})   ' BC30332: Integer() cannot be converted to Long()
Dim values As Long() = {1, 2}     ' Long()
```

So DCA030 decides target typing on the **outermost** parenthesized expression, and its fix replaces the whole `(New Long() {1, 2})` with `{1, 2}`.

## 2. Same logic, many shapes

A rule detects the **logic**. Starting from one statement kind (e.g. only `MultiLineIfBlock`) is the most common false negative. DCA081 had this bug: IDE0046 rewrote the code into a ternary and the rule went silent.

| Form | Must be detected |
|---|---|
| `If map.ContainsKey(k) Then … map(k) … End If` | ✔️ |
| `If map.ContainsKey(k) Then Return map(k)` (single line) | ✔️ |
| `Return If(map.ContainsKey(k), map(k), 0)` (ternary, IDE0046 output) | ✔️ |
| `If Not map.ContainsKey(k) Then … Else … map(k)` / `If(Not …, 0, map(k))` | ✔️ reads in the *false* branch |
| `If map.ContainsKey(k) AndAlso map(k) > 0 Then` | ✔️ right operand + `Then` part |
| `If Not map.ContainsKey(k) OrElse map(k) = 0 Then Return` | ✔️ right operand |
| `If Not map.ContainsKey(k) Then Return` … `map(k)` (guard clause; `Throw`, `Exit`, `Continue` too) | ✔️ the following statements |
| `ElseIf map.ContainsKey(k) Then` | ✔️ |
| `If (map.ContainsKey(k)) Then`, `Not (map.ContainsKey(k))` | ✔️ |

The recommended technique is to start at the check (the invocation), walk up through `Parenthesized`, `Not`, `AndAlso` and `OrElse` while tracking "true/false known", and then collect the regions where the condition is known.

Other equivalences that were missed:

```vb
' Null check (DCA025): both forms are the same
If sender IsNot Nothing AndAlso TypeOf sender Is Button Then
If Not sender Is Nothing AndAlso TypeOf sender Is Button Then   ' was missed

' Rethrow-only Catch (DCA024): both forms are reported
Catch ex As Exception
    Throw
Catch ex As Exception
    Throw ex      ' was missed; it also loses the stack trace
```

## 3. Arguments: overloads, named arguments, call syntax

```vb
' All three are the same guard (DCA011); only the first one was detected
Throw New ArgumentNullException(NameOf(icon))
Throw New ArgumentNullException(NameOf(icon), $"The {NameOf(icon)} cannot be null or empty.")
Throw New ArgumentNullException(paramName:=NameOf(command))
```

- Match arguments **by parameter** (`IArgumentOperation.Parameter.Name`), not by position, so positional and named arguments in any order are treated alike. Overloads with extra arguments are part of the rule. For example, DCA011 reports the custom-message overload: the message stays in the `#Else` branch, and the rule document explains that the helper writes its own message. The only overloads left out are the ones where the replacement would change the exception type or its `ParamName`.
- An **exact equivalence** is required for migrations. `ArgumentNullException("test")` is not `ThrowIfNull(arg1)`, because the parameter name would change.
- A fix that **changes the called method must rename named arguments**:

  ```vb
  ' ❌ Bad: does not compile, Exists has no parameter "predicate"
  items.Any(predicate:=Function(x As Integer) x > 0)   →   items.Exists(predicate:=...)
  ' ✔️ Good
  items.Exists(match:=Function(x As Integer) x > 0)
  ```

- A fix that **adds** an argument writes it named when a positional argument would not reach the parameter (DCA071: `token:=cancellationToken`). It also adds the parentheses of a call written without them, so a call such as `Await Me.LoadAsync` becomes `Await Me.LoadAsync(cancellationToken)`.
- Also handle the `Call` statement (`Call map.Remove(key)`), calls without parentheses, and `Me.`/`MyClass.`-qualified receivers.

## 4. Trivia: comments, `#Region`, `#If`, continuations, line endings

**`#Region` lines live in the leading trivia of the next token.** If you remove a member together with its leading trivia, you delete the `#Region " Constructors "` line and leave a lone `#End Region` (DCA054 shipped this compile error).

```vb
' ❌ Bad result of removing the constructor
    Public Class DCA054

#End Region          ' opening "#Region " Constructors "" was deleted

    End Class

' ✔️ Good: the region stays balanced (empty region kept, or removed as a pair)
    Public Class DCA054

#Region " Constructors "

#End Region

    End Class
```

- Every fix must keep `#Region`/`#End Region` balanced and keep `#If` blocks. `RuleTestingProjectTests` applies every fix with `#Region`/`#If` decorations around every type, member and statement, and fails if a directive disappears.
- Do not report statements that contain directives (`UtilAnalyzerSyntax.HasInnerDirectives`).
- Inactive `#If` branches are `DisabledTextTrivia`. DCA014 must recognize its own `#If DEBUG Then … Thread.CurrentThread.Join(0) … #End If` block also in **Release**, where it is disabled text; otherwise the rule fires again after the fix.
- **Comments.** Full-line comments inside the replaced block are moved **before** the new line, and end-of-line comments go **after** it. Only a comment that follows code in the middle of a statement blocks the fix:

  ```vb
  ' Before
  If map.ContainsKey(key) Then
      ' Forget the key.
      map.Remove(key) ' Cleanup.
  End If
  ' ✔️ After (DCA077)
  ' Forget the key.
  map.Remove(key) ' Cleanup.

  ' No fix offered (the comment would be lost):
  If map.ContainsKey( _ ' The key to forget.
          key) Then map.Remove(key)
  ```

- Since VB 16, a comment may follow a `_` line continuation. Do not assume that comments only appear at the end of a statement.
- **Line endings.** The Roslyn formatter writes the operating system's line ending. Always build inserted text with `UtilAnalyzerSyntax.GetEndOfLine(tree)`; the tests run every fix with LF and with CRLF and fail on mixed endings.

## 5. Lambdas and scopes

- A read inside a lambda may run after the state changes. DCA081 does not report `map(k)` inside a lambda unless the check is inside the same lambda.
- Walk up with `AncestorsAndSelf().TakeWhile(Function(a) a IsNot region)`. With `Ancestors()`, a read that *is* the region node (the body of a single-line lambda) is skipped.
- `ByRef` parameters cannot be captured in a lambda (**BC36639**). Copy them to a local first.
- A declaration cannot be inserted into a single-line `If` or a single-line lambda: report the diagnostic, but offer no action.
- When converting `Async Sub` to `Async Function … As Task` (DCA016), change only the method's own `Exit Sub` statements, not those inside nested lambdas.

## 6. Namespaces, types and project settings

- **RootNamespace.** In VB, a type without a `Namespace` block belongs to the project `RootNamespace`. Wrapping it again produces `MyApp.MyApp.C`. Report only when `type.ContainingNamespace.IsGlobalNamespace` (DCA006). An explicit `Namespace Global` is not reported.
- **.NET Framework type hierarchy.** `IReadOnlyList(Of T)` does not imply `IList(Of T)`. `String` is indexable through its `Chars` default property (DCA022).
- **Value types.** `DesignerSerializationVisibility.Content` is offered only for classes (except `String`), interfaces and arrays. On a structure, the designer would write code that modifies a copy. `ReferenceEquals` on a value type always compares two new boxes (DCA066). `arg2 = Nothing` on a `Decimal` compares with `0D`, not with a null reference.
- **ComImport.** VB *accepts* a constructor in a `<ComImport>` class and silently discards its body. Another AI claimed this was a compile error (BC31522); compiling a probe showed it is not. That is why DCA054 is an **Error**-severity rule (it compiles, but it never runs), and DCA005 must not ask for constructors in `<ComImport>` classes.
- **Project options.** Tests run fixes with `Option Strict Off` / `Option Infer On` too. Under `Infer On`, `Dim x = {1}` infers `Integer()`, so DCA030 never reports declarations without `As`.
- **XML documentation file.** DCA008 and DCA012 need `<GenerateDocumentationFile>` / `DocumentationMode` to see doc comments in project builds.

## 7. Async specifics (DCA001, DCA002, DCA016, DCA027, DCA051)

- `Await o?.GetAsync()` must become `Await (o?.GetAsync()).ConfigureAwait(False)`, and a nested `Await` needs parentheses too. Use `UtilAnalyzerSyntax.RequiresParenthesesForMemberAccess` to decide when to add them.
- `Task.Yield()` returns a `YieldAwaitable`, which has no `ConfigureAwait`. Only rewrite types that expose `ConfigureAwait(Boolean)` (bind it).
- `Dim x As Integer = Await GetAsync()` followed by `x.Result`: `x` is already the value, so this is not a blocking `.Result`. `x.Result(0)` on a non-task type is not either.
- A `Thread.Sleep` fix must keep `ConfigureAwait(False)`: `Await Task.Delay(x).ConfigureAwait(False)`.

## 8. VB semantics that surprised us

| Fact | Consequence |
|---|---|
| `1 << 32` is `1` in VB (the shift count is masked) | Never use a shift to enumerate 32+ combinations; validate the count |
| `"1" + 1` is `2` under `Option Strict Off` | The reason DCA029 exists |
| `#If DEBUG` without `Then` compiles, but VS writes `#If DEBUG Then` | Fixes write the canonical form |
| `DEBUG` is defined by default in Debug configurations (`DefineDebug`) | No need to check that the symbol exists |
| `End` is not allowed in class libraries (BC30615) | The `DCA Rule Testing` projects are `Exe` |
| `Thread.Suspend` is obsolete (BC40000) | `DCA038.vb` wraps it in a commented `#Disable Warning BC40000` |
| `ArgumentException.ThrowIfNullOrWhiteSpace` is declared on `ArgumentException`, not on `ArgumentNullException` | DCA011 generates the declaring type |
| `ArgumentOutOfRangeException.ThrowIf*` helpers only exist in .NET 8+ | DCA011 has the `dca011_condition` option (`NET8_0_OR_GREATER`) |
| ContextSwitchDeadlock is a debugger MDA | DCA014's fix inserts `Join(0)` inside `#If DEBUG Then` |

## 9. Facts that were verified (verify the next ones the same way)

- **Default text encodings** (checked byte by byte, `Año ñandú`):

  | Call | Bytes written |
  |---|---|
  | `File.WriteAllText(path, text)` (no encoding) | UTF-8 **without** BOM: `41-C3-B1-…` |
  | `File.WriteAllText(path, text, Encoding.UTF8)` | UTF-8 **with** BOM: `EF-BB-BF-41-…` |
  | `File.WriteAllText(path, text, Encoding.Default)` (.NET Framework) | ANSI code page (Windows-1252): `41-F1-…` |
  | `My.Computer.FileSystem.WriteAllText` (no encoding) | UTF-8 **with** BOM |

  Editors may display a pure-ASCII file as "Windows-1252"; that is detection, not the bytes. `Encoding.Default` is therefore **not** a behavior-preserving default for DCA026.
- **ComImport constructors** compile (see §6).
- **VSIX build.** With `<Project Sdk="Microsoft.NET.Sdk">`, the SDK targets are imported last and `Microsoft.Common.CurrentVersion.targets` resets `PrepareForRunDependsOn`, which drops `CreateVsixContainer`. The build then succeeds, but no `.vsix` is produced. Import `Sdk.props` first and `Sdk.targets` **before** `Microsoft.VsSDK.targets`. Build the VSIX with Visual Studio's `msbuild`, not with `dotnet build`.
- **VSIX manifest.** `<Description>` has a schema `MaxLength`, and an overlong description breaks the build with a schema validation error. Keep it to one short paragraph that summarizes the rule groups; never list every rule.
- **VSIX analyzers run only in the IDE**, not in `dotnet build` or CI. Info-level rules appear in the *Messages* tab of the Error List.

## 10. Test-host and tooling traps

- **Markup spans are exact.** `[| |]` must cover precisely the reported location (e.g. `New Long()` for DCA030, not the whole creation).
- **Arrays in attributes and tests.** Expected `String()` arrays used in `Assert.Equal` go into `Private Shared ReadOnly` fields; inline arrays trigger CA1861 or DCA084.
- **Python scripts that write VB code.** Python `'''` strings collide with VB `'''` doc comments. Use heredoc files, or `"""` strings that contain no `"""`.
- **Old one-off generator scripts are stale.** Never re-run them; edit the files directly.
- **IDE simplifications change the shape.** Visual Studio's IDE fixes (for example IDE0046, which turns an `If` statement into a ternary `If(...)`) rewrite the code the user writes. Every rule must still fire after the IDE fix that a user is likely to apply to its own example (see §2).
