# DevCase VB.NET Code Analysis Change Log 📋

## v1.0.9 *(current)* 🆕

### 🧰 26 new rules: Microsoft's modern code-quality rules, for .NET Framework

.NET Framework projects do not get many of the code-quality rules that Microsoft ships with modern .NET. DCA059–DCA084 bring 25 of those CA rules, and WFO1000 (the Windows Forms designer rule of .NET 9), to .NET Framework projects. Like DCA055, DCA056 and DCA058, they **run only in .NET Framework projects**: on modern .NET, the original rule reports the same code, so nothing is reported twice. 20 of them have a code fix, and all of those support Fix All; the other 6 only report, because the right change depends on what the code means.

| Rule | Title | Replicates | Severity | Code fix |
|---|---|---|---|---|
| DCA059 | `System.Type` argument instead of a generic overload | [CA2263](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2263) | Info | `Activator.CreateInstance(Of T)()`, without the redundant cast |
| DCA060 | Component property without designer serialization configuration | [WFO1000](https://learn.microsoft.com/dotnet/desktop/winforms/compiler-messages/wfo1000) | Warning | `<DesignerSerializationVisibility(Visible)>`, `(Content)`, `<DefaultValue(value)>` or `(Hidden)` |
| DCA061 | `ArgumentNullException` for a value-type parameter | [CA2264](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2264) | Warning | — |
| DCA062 | `TaskContinuationOptions` passed to a `TaskCompletionSource` constructor | [CA2247](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2247) | Warning | `TaskCreationOptions.X` |
| DCA063 | `StreamReader.EndOfStream` in an `Async` method | [CA2024](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2024) | Warning | — |
| DCA064 | `Enumerable.Cast` or `OfType` with an incompatible type | [CA2021](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2021) | Warning | — |
| DCA065 | `Buffer.BlockCopy` count is an array length | [CA2018](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2018) | Warning | `Buffer.ByteLength(array)` |
| DCA066 | Value type passed to `ReferenceEquals` | [CA2013](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2013) | Warning | — |
| DCA067 | String literal instead of `NameOf` | [CA1507](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1507) | Info | `NameOf(name)` |
| DCA068 | `IsCancellationRequested` check that throws `OperationCanceledException` | [CA2250](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2250) | Info | `token.ThrowIfCancellationRequested()` |
| DCA069 | `Enum.HasFlag` with a different enum type | [CA2248](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2248) | Warning | — |
| DCA070 | `ThreadStatic` field initialized inline or in a `Shared` constructor | [CA2019](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2019) | Warning | — |
| DCA071 | `CancellationToken` parameter not forwarded | [CA2016](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca2016) | Info | Passes the token (named when needed) |
| DCA072 | `Count()` compared with zero instead of `Any()` | [CA1827](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1827) | Info | `Any()` / `Not Any()` |
| DCA073 | `Enumerable.Count()` instead of the `Length` or `Count` property | [CA1829](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1829) | Info | `Length` / `Count` |
| DCA074 | `Count` compared with zero instead of `IsEmpty` | [CA1836](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1836) | Info | `IsEmpty` / `Not IsEmpty` |
| DCA075 | `Task.WhenAll` with a single task | [CA1842](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1842) | Info | The task itself |
| DCA076 | `Task.WaitAll` with a single task | [CA1843](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1843) | Info | `task.Wait()` |
| DCA077 | `Dictionary.Remove` guarded by `ContainsKey` | [CA1853](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1853) | Info | `map.Remove(key)` |
| DCA078 | Set `Add` or `Remove` guarded by `Contains` | [CA1868](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1868) | Info | `items.Add(item)` / `items.Remove(item)` |
| DCA079 | `Keys.Contains` or `Values.Contains` on a dictionary | [CA1841](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1841) | Info | `ContainsKey` / `ContainsValue` |
| DCA080 | `Thread.CurrentThread.ManagedThreadId` instead of `Environment.CurrentManagedThreadId` | [CA1840](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1840) | Info | `Environment.CurrentManagedThreadId` |
| DCA081 | `ContainsKey` followed by the indexer instead of `TryGetValue` | [CA1854](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1854) | Info | `Dim value ... = Nothing` + `TryGetValue` |
| DCA082 | `IndexOf` compared with zero instead of `StartsWith` | [CA1858](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1858) | Info | `StartsWith` |
| DCA083 | Redundant length argument of `Substring` | [CA1514](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1514) | Info | `Substring(start)` |
| DCA084 | Constant array passed as an argument | [CA1861](https://learn.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/ca1861) | Info | `Private Shared ReadOnly` field |

Details that go beyond the original rules:
 - **DCA060** offers up to four actions, the first one keeping the current behavior: `Visible` (the designer keeps serializing the property), `Content` (the designer serializes the items of a collection or the properties of a nested object; offered for classes other than `String`, interfaces and arrays, not for value types, where the designer would modify a copy), `DefaultValue` (only for auto-implemented properties with a constant initial value; the argument is typed for the overload of the property type: `5L`, `0S`, `CByte(7)`, `0.5R`, `"x"c`, `DayOfWeek.Friday`), and `Hidden`, the fix that Microsoft offers for WFO1000.
 - **DCA084** names the field after the member, the called method and a number, as requested: `FindSeparatorIndexOfAny0`. The numbers are assigned per type, also across the files of a `Partial` type, and skip the names already in use, so Fix All declares a different field for each array. The field goes before the member, inside its `#Region` and before its documentation comment.
 - **DCA081** recognizes every way of writing the check, not only `If map.ContainsKey(key) Then`: the ternary `If(map.ContainsKey(key), map(key), 0)` (the form that IDE0046 produces), `Not` with the reads in the `Else` part, `AndAlso` and `OrElse` (reads on the right side), guard clauses (`If Not map.ContainsKey(key) Then Return`, with the reads in the statements that follow), `ElseIf`, and redundant parentheses. It declares `value`, or `value1`, `value2`... when the name is used or the method has several of these checks, so Fix All never declares the same variable twice.
 - **DCA059** checks the constraints of the generic overload itself, because speculative binding does not. In .NET Framework, `Activator.CreateInstance(Of T)()` has no `New` constraint, so `DirectCast(Activator.CreateInstance(GetType(T)), T)` is reported for any `T`, as CA2263 does.
 - **DCA072**: on .NET Framework, `Any()` always creates an enumerator while `Count()` reads `ICollection(Of T).Count`, so `Count()` without a predicate on a collection is left to DCA073 (the property beats both). The rule reports lazy sequences and calls with a predicate, where the gain is real.
 - **DCA068**, **DCA077** and **DCA078** keep the comments at the end of the removed lines (after the call) and the comments on lines of their own (before the call), and do not report statements with preprocessor directives inside (another configuration may compile other statements).
 - **DCA071** writes a named argument (`token:=cancellationToken`) when a positional one would not reach the parameter, and adds the parentheses of a call written without them.

Every fix is computed and validated by the analyzer (the replacement is bound speculatively at the position of the code) and applied by a single new code fix provider. Its Fix All merges the changes of every diagnostic of a document, concatenating the insertions at the same position (several fields extracted before the same member).

### Changes
 - **DCA022** was imprecise: it ignored `IReadOnlyList(Of T)` types that do not implement `IList(Of T)` and strings. It now reports them too: `IReadOnlyList(Of T)` types in .NET Framework projects (in modern .NET projects, CA1826 reports them), and `String`, indexed through its `Chars` default property, on every target framework. On both, `Last()` and `ElementAt()` enumerate the sequence. `Last()` on a string is fixed with `text(text.Length - 1)`. The message now matches CA1826: *"Do not use Enumerable methods on indexable collections; instead use the collection directly"*. CA1826 writes two sentences separated by a period; here they are joined with a semicolon, because the message has no final period and a message with two sentences must end with one (RS1032).
 - **DCA049**: the message follows CA1834, generalized with the name of the called method: *"Use 'StringBuilder.Append(Char)' instead of 'StringBuilder.Append(String)' when the input is a constant unit string"* (`String.Replace`, `String.Join`, `TextWriter.Write`...).
 - **DCA058**: the message matches CA1830: *"Prefer strongly-typed StringBuilder overload"*.
 - **DCA066**: the message says what goes wrong: *"Do not pass value type 'Integer' to 'ReferenceEquals' because each call boxes it into a new object, causing the reference comparison to always return 'False'"*.
 - Redundant parentheses broke several rules, which the new syntax variations of the tests (see below) found:
   - **DCA003** did not report `If items.Length = 0 Then Return (Nothing)`.
   - **DCA030** did not report `Dim values As Long() = (New Long() {1, 2})`. Its fix now also removes the parentheses, because `({1, 2})` is not target-typed: it would be an `Integer()` that cannot be converted to `Long()` (BC30332).
   - **DCA044** did not report `Dim start As Date = (DateTime.Now)`, nor `(DateTime.Now - start)`.
   - **DCA050** did not report `Thread.CurrentThread.CurrentCulture = (culture)`, nor other parenthesized uses that consume the parameter.
 - **DCA011**: its document names the rules that it replicates as conditional blocks, CA1510, CA1511 and CA1512. Every `ArgumentOutOfRangeException` helper of CA1512 was already covered (the nine `ThrowIf...` methods, with the operands in either order); a new test uses the reported example, `If decimalCount < 0 Then Throw New ArgumentOutOfRangeException(NameOf(decimalCount))` → `ArgumentOutOfRangeException.ThrowIfNegative(decimalCount)`.
 - CA1860 was already replicated by DCA056.

### Rule testing solution
 - New files `DCA059.vb` … `DCA084.vb` in the .NET Framework project (the rules do not run in the .NET 10 project). Each one triggers only its rule, also after applying its code fix with `#Region` and `#If` decorations.
 - `DCA022.vb` (.NET Framework) also reports `values.Last()` on an `IReadOnlyList(Of Integer)`.
 - `DCA060.vb` also has a `List(Of String)` property, for the `Content` action.
 - `DCA071.vb` and `DCA081.vb` suppress the IDE suggestions that their own example code triggers on purpose: IDE0060 (*"Remove unused parameter"*) on `PauseAsync`, whose not forwarded token is the subject of DCA071, and IDE0046 (*"Convert to conditional expression"*) on `GetAge`, whose `If` statement is the subject of DCA081.

### Tests
 - One test class per new rule: what is reported and what is not, nothing on modern .NET, the code fix (also with `Option Strict Off` / `Option Infer On`), Fix All, the offered actions and the messages.
 - New tests for DCA022 (`IReadOnlyList(Of T)` and strings, on .NET Framework and modern .NET, and the message), DCA049 and DCA058 (messages), and DCA011 (the CA1512 example).
 - The test host can list the code actions of a .NET Framework project. The registration tests expect 84 rules and 41 code fix providers.
 - **Syntax variations**: every analyzer and code fix test now also runs on six rewrites of its source that write the same code in another way: redundant parentheses (conditions, `Return` values, initializers, assigned values, ternary conditions), explicit line continuations (` _` after commas, opening parentheses, member-access dots, `AndAlso`, `OrElse` and `&`), method bodies inside `#Region` blocks, inside `#If True Then` blocks and inside a `Using` block, and comment lines between statements. Each rewrite must compile; the analyzer must report the same diagnostics as for the original source, and the code fix must still produce code that compiles and has no diagnostic left. This applies to the existing rules and to every future one, with no test written by hand; a rule that is silent on purpose in a variation is listed with its reason (DCA011 and DCA013 inside `#If` blocks). The variations found the parenthesis bugs listed above, and the comment lines that blocked the fixes of DCA068, DCA077 and DCA078.
 - New tests for DCA060 (`Content`), DCA081 (every form of the check, reads where the key may not exist, single-line lambdas), DCA030 (parenthesized creations) and DCA068/DCA077 (comment lines kept).

## v1.0.8 🔖

### 🔤 Every message and every severity, reviewed rule by rule

This version does not add rules: it rewrites **how all of them speak** and **how loud they are**. It is the largest change since the first release. No code fix changes its result, but the Error List looks different for every rule.

**Messages.** The message of all 58 rules was rewritten with a single pattern: **what to do** + **where** (`'{0}'`) + **why** (optional), in one short sentence. The action comes first, so a long Error List can be scanned at a glance, the reason is a few words instead of an explanation, and every placeholder was checked against the values that its analyzer passes. Some examples:

| Rule | Before | Now |
|---|---|---|
| DCA029 | Strings are concatenated with '+'; use '&' instead | Concatenate strings using '&' instead of '+' to avoid operator ambiguity |
| DCA005 | Class 'C' does not explicitly declare an instance constructor | Explicitly declare an instance constructor in class 'C' to avoid an implicit public constructor |
| DCA044 | 'Date.Now' measures elapsed time with the system clock; use Stopwatch | Use Stopwatch instead of 'Date.Now' to measure elapsed time accurately |
| DCA046 | An exception thrown in a Finally block replaces the exception that is being propagated | Do not throw exceptions in a Finally block to preserve the original exception |
| DCA054 | 'Sub New' is declared in the ComImport class 'C': the compiler discards its body, so its code never runs | Remove the constructor from &lt;ComImport&gt; class 'C' because the compiler discards its body |

DCA054 now names the kind of member (*the constructor*, *the shared constructor*, *method 'X'*, *property 'X'*, *the initializer of field 'X'*...), and DCA011 quotes each throw helper. The descriptions of DCA004, DCA020 and DCA024 were updated.

**Severities.** New policy, applied to every rule:

| Severity | Meaning |
|---|---|
| **Error** | Code that the compiler accepts but that can never work. Using an API as the .NET class library exposes it (for example `Task.Result`) is never an Error: it compiles and works. |
| **Warning** | Code that can fail at run time, hang, lose exceptions or data, or that hides a defect. |
| **Info** | Style, structure, documentation, naming, readability and micro-performance: the code works, and the fix makes it clearer or faster. |

Changes:
 - Error → Warning: DCA001, DCA002, DCA017.
 - Warning → Error: DCA054, now the only Error rule (its code compiles but never runs).
 - Warning → Info: DCA003, DCA005, DCA007, DCA008, DCA009, DCA010, DCA012, DCA018, DCA019, DCA021, DCA022, DCA023, DCA025, DCA026, DCA033.
 - Info → Warning: DCA042, DCA046.

Every severity can still be changed in `.editorconfig`. The repository `.editorconfig` raises the 15 rules that became Info to warnings for the repository's own source code, which keeps following them at build time. The README has a new severity table, and the rule table, the Info-level rule list, the rule documents and the comments of the rule testing solution show the new severities.

### Fixes
 - DCA020: the rule reported a zero member whose name was not `None`, and its code fix renamed it in the whole solution (`Null` → `None`), which breaks every piece of code that uses the old name. A zero member that already exists, explicit (`Empty = 0`) or implicit (the first member without a value), is no longer reported, whatever its name. Only enumerations without a zero value are reported, and the fix adds `None = 0`, or, when `None` is already used by a member with another value, the first free name among `[Nothing]`, `Null`, `Zero` and `Empty` (compared without case). The rename code fix was removed. New title: *Flags enumeration without a zero value*.
 - DCA004: the rule required the three `Option` statements in every file, even when the project already compiled it with `Option Strict On`, `Option Explicit On` and `Option Infer Off`. Each setting is now taken from the `Option` statement of the file or, when the file does not declare it, from the project settings (`<OptionStrict>`, `<OptionExplicit>`, `<OptionInfer>`): a file that inherits the required settings is not reported, and a file whose statement contradicts them is. `Option Strict Custom` is not `On`. The message says which settings are wrong and where they come from (*"... because this file compiles with 'Option Strict Off' (project), 'Option Infer On' (file)"*). The code fix still writes the three statements. New title: *Required Option settings not in effect*.

### Tests
 - The test host can set the `Option Strict`, `Option Explicit` and `Option Infer` settings of the project.
 - New DCA004 tests (inherited settings, a file that overrides the project, `Option Strict Custom`) and DCA020 tests (implicit zero with another name, the next free name, every name used).

## v1.0.7 🔖
Fixes (code fixes that removed preprocessor directives):
 - DCA054: the code fix removed the directives before the constructor. When the constructor was the only member of a region, as in `DCA054.vb` of the rule testing solution, the `#Region` line was removed and its `#End Region` was left alone (error BC30680). The directives before a removed constructor (`#Region`, `#End Region`, `#If`..., with the comments and conditional code between them) now stay, and a region whose only member was the constructor stays, empty. The directives inside the body of the constructor are still removed with it.
 - DCA024: when the `Try` statement was unwrapped, the `#If` / `#End If` lines before the removed `Catch` and `End Try` keywords were lost. A body that ended with an `#If DEBUG Then ... #End If` block was left with an `#If` without its `#End If` (error BC30012), and the code of a body that only had conditional code was removed. These directives, and the code between them, now stay after the unwrapped statements, and disabled code (an `#If` block whose condition is false) is re-indented with the body. The `#If` / `#End If` lines before a removed `Catch` block now stay before `End Try`, and a `Try` statement removed because its body was empty keeps the directives that preceded it.
 - DCA024: a `Catch` block with conditional code in its body (`#If DEBUG Then Log(ex) #End If`, before or after the `Throw`) is no longer reported: in another configuration it does more than rethrow the exception.

Tests:
 - New test that applies every code fix (each of its actions, with *Fix All in Document*) to the `DCAnnn.vb` files of the rule testing solution, for .NET Framework 4.8 and .NET 10, in three variants: the files as they are, with complete and enclosing `#Region` blocks around every type and member, and with `#If True Then` blocks and disabled `#If False Then` code around every type, member and statement. It checks that the project still compiles without warnings, that no directive is lost (except those inside the body of a member that the fix removes), that `#Region` / `#End Region` stay balanced, and that the diagnostics of the rule are fixed. The other code fixes already kept every directive.
 - New DCA054 tests (the case of `DCA054.vb`, and complete regions, regions and `#If` blocks around the constructors, with the fix and with Fix All) and DCA024 tests (conditional code at the start and end of the body, in an empty body, at the end of the previous `Catch` block, and `Catch` blocks with conditional code, with `DEBUG` defined and not defined).

## v1.0.6 🔖
Rule testing solution:
 - New `RuleTesting` folder with `DCA Rule Testing Project.slnx`: a .NET Framework 4.8 project with one file per rule (`DCA001.vb` … `DCA058.vb`) and a .NET 10 project with one file per rule that also runs on modern .NET. Each file triggers only the rule of its name, and its XML documentation says what is reported and what the code fix writes. A new test compiles both projects as Visual Studio does (project options, imports, references, `MyType`, `.editorconfig`) and checks that every file reports its rule and no other. See *Try every rule* in the README.

Changes (alignment with Microsoft's built-in rules):
 - DCA006: runs only in .NET Framework projects, and its severity is now **Info** instead of Warning. On modern .NET, the built-in rule CA1050 (*Declare types in namespaces*, a suggestion) already reports the same types.
 - DCA046: runs only in .NET Framework projects, and its severity is now **Info** instead of Warning. On modern .NET, the built-in rule CA2219 (*Do not raise exceptions in exception clauses*, a suggestion) already reports the same statements.
 - DCA049: on modern .NET, `StringBuilder.Append` and `String.Contains` are no longer reported: the built-in rules CA1834 and CA1847 already report them. They are still reported on .NET Framework, and the other methods on every framework.

## v1.0.5 🔖
New rules:
 - DCA054 (Warning): code in a class marked with `<ComImport>`. Visual Basic accepts constructors, methods, properties and field initializers in a COM coclass without an error or a warning, but compiles them without a body: their code never runs (C# rejects it with CS0669/CS0423). The fix removes `Shared Sub New` and a `Public Sub New()` without parameters, which changes nothing; other members are only reported.
 - DCA055 (Info, .NET Framework only, equivalent of CA1825): zero-length array allocation (`New String() {}`, `New String(-1) {}`, `{}`) → `Array.Empty(Of String)()`. Not reported in attribute arguments, `ParamArray` arrays, multidimensional arrays, or projects without `Array.Empty` (.NET Framework 4.5.2 or earlier).
 - DCA056 (Info, .NET Framework only, equivalent of CA1860): `Enumerable.Any()` without a predicate on a collection with an `IsEmpty`, `Length` or `Count` property. `While queue.Any()` → `While queue.Count <> 0`, `Not list.Any()` → `list.Count = 0`, `bag.Any()` → `Not bag.IsEmpty`; parentheses are added where the context needs them.
 - DCA057 (Info): parameter typed `IEnumerable` or `IEnumerable(Of T)` (exactly these interfaces, not the collections that implement them) named `collection`, `list` or `array` → renamed to `sequence`; `col`, `coll`, `lst` or `arr` → `seq`. When that name is used in the member, the next free one among `sequence`, `source`, `value` and `items` is chosen.
 - DCA058 (Info, .NET Framework only, equivalent of CA1830): `StringBuilder` argument converted to a string although a strongly typed overload exists: `sb.Append(count.ToString())` → `sb.Append(count)`, `sb.Insert(0, flag.ToString())` → `sb.Insert(0, flag)`, `sb.Append(New String(c, 2))` → `sb.Append(c, 2)`.

Changes:
 - Every rule now skips designer, generated, assembly-information and suppression files: names ending in `.Designer`, `.AssemblyAttributes`, `AssemblyInfo`, `GlobalSuppressions`, `.g`, `.g.i` or `.generated` immediately followed by the extension (`Form1.Designer.vb`, `AssemblyInfo.vb`, `GlobalSuppressions.vb`/`.cs`...), and names starting with `TemporaryGeneratedFile_`. Files with an `<auto-generated>` header were already skipped. DCA007 uses the same list.
 - DCA003: the default severity is now **Warning** instead of Error. A silent guard clause is a design problem, and as an error it blocked the Error List of projects that were otherwise correct; `dotnet_diagnostic.DCA003.severity = error` restores the previous behavior.
 - DCA052: the code fix no longer derives the new name from the type of the parameter (`brush`, `assembly2`, `enumerable2`). It renames `sender` to `source`, or, when that name is already used in the method (parameters, locals, the method name, members used without qualification), to the next free name among `value`, `input`, `instance` and `item`. When every name is used, the parameter is reported without a code fix; numbered names are never generated.
 - DCA050: an assignment to a framework property whose setter rejects `Nothing` is now a consumption, so `Thread.CurrentThread.CurrentCulture = ci` reports `ci`. Such setters are recognized from the nullable annotations of modern .NET, and, on .NET Framework, from a list (`Thread.CurrentCulture`/`CurrentUICulture`, `CultureInfo.CurrentCulture`/`CurrentUICulture`, `Console.Title`, `Environment.CurrentDirectory`). Properties that accept `Nothing` (`TextWriter.NewLine`, `Exception.HelpLink`, `Control.Text`...) and properties declared in source are not consumptions.
 - DCA030: empty arrays (`New Integer() {}`) are no longer reported: they should become `Array.Empty(Of Integer)()` (DCA055), not the literal `{}`.
 - DCA011: the exception arguments are matched by parameter, so `paramName:=NameOf(p)` and named arguments in any order are recognized, as well as a custom `message` (kept in the .NET Framework branch), a literal parameter name (`"p"`) and the parameter as `actualValue`. The condition can also use a named argument (`String.IsNullOrEmpty(value:=p)`).
 - Named arguments are now recognized like positional ones by DCA003 (`String.IsNullOrWhiteSpace(value:=p)`), DCA013 (all migrations, in any argument order), DCA021 (`Any(predicate:=...)`), DCA022 (`ElementAt(index:=...)`), DCA025 (`IsNothing(Expression:=x)`), DCA049 (`String.Join(separator:=",", ...)`) and DCA050 (validating helpers called with named arguments). A cross-rule test checks every rule that inspects arguments with both forms.

Fixes:
 - DCA005: classes marked with `<ComImport>` are no longer reported: an explicit constructor would never run.
 - DCA013: `BitConverter.ToString(value:=bytes).Replace(...)` produced `Convert.ToHexString(value:=bytes)`, which does not compile (its parameter is `inArray`); the TryAdd and `Enum.Parse` migrations copied named arguments into calls whose parameters are named differently or in another order. The new calls are now written with positional arguments.
 - DCA021: `items.Any(predicate:=...)` produced `items.Exists(predicate:=...)`, which does not compile (the parameter of `Exists` is `match`); the predicate is now written positionally.

## v1.0.4 🔖
New rules:
 - DCA052 (Info): parameter named `sender` in a method that is not an event handler (`Public Function ToPen(sender As SolidBrush) As Pen`). The fix renames it after its type (`brush`) in the whole solution, including named arguments and XML documentation. `Object` senders, `Handles` methods, methods with an `EventArgs` parameter, methods used with `AddressOf`, and overrides are not reported.
 - DCA053 (Info): method call without parentheses (`query.ToArray`, `Me.Close`, `Call Work`) → adds `()`. Semantic analysis: properties, `AddressOf`, `NameOf` and the calls generated by the compiler are never reported.

Changes:
 - DCA049: now also reports `String.Replace`, `StringBuilder.Insert`/`Replace`, `TextWriter.Write`/`WriteLine` (and derived writers), `Console.Write`/`WriteLine`, and, on modern .NET, `String.Split`, `String.Contains` and `StringBuilder.AppendJoin`. Every single-character literal of the call is replaced at once (`s.Replace("a"c, "b"c)`). Methods without an equivalent `Char` overload (`AppendLine`, `IndexOf`, `StartsWith`...) are still not reported.
 - DCA050: on .NET 6+ the fix writes `ArgumentNullException.ThrowIfNull(value)` (and `ArgumentException.ThrowIfNullOrEmpty` / `ThrowIfNullOrWhiteSpace` on .NET 7/8) instead of an `If` block, but only when every project that compiles the file (multi-targeting, linked files) has the helper.
 - DCA050: `ReferenceEquals` methods are no longer analyzed, and parameters passed to framework methods that accept `Nothing` (`Object.ReferenceEquals`, `Console.WriteLine`, `StringBuilder.Append`, Visual Basic string functions, generic `T` parameters...) are no longer considered consumed.
 - DCA045: the code fix now also handles calls inside a larger expression (`New FileInfo(Path.Combine(Path.GetTempPath, path2:=Path.GetTempFileName))`): the file is created in a new variable before the statement. Calls whose evaluation is conditional or repeated (lambdas, `If(...)`, `AndAlso`, loop conditions...) are still reported without a fix.

## v1.0.3 🔖
New rules:
 - DCA049 (Info): single-character string literal passed to `String.Join` or `StringBuilder.Append` → character literal (`String.Join("&"c, parts)`). It compiles and behaves the same on .NET Framework and modern .NET, so no `#If` block is needed.
 - DCA050 (Info): parameter of a public or protected method or constructor used without being validated. Three kinds of guard clause:
   - `Enum` (without `<Flags>`): `[Enum].IsDefined` → `InvalidEnumArgumentException` (`ArgumentOutOfRangeException` for `Long` enumerations).
   - `String`: `String.IsNullOrEmpty` (default), `String.IsNullOrWhiteSpace` or `Is Nothing` → `ArgumentNullException`.
   - Other reference types: `Is Nothing` → `ArgumentNullException`.

   Guard clauses are inserted in parameter order, after `MyBase.New` and among the existing guard clauses; Fix All keeps that order. Value types, `Nullable(Of T)`, `Optional`/`ByRef`/`ParamArray` parameters, event handlers, overrides of framework members, and parameters that are only stored, returned or concatenated are not reported.
 - DCA051 (Warning): `GetAwaiter()` or `ConfigureAwait(...)` called as a statement (`Task.Run(...).GetAwaiter()`, `Me.WorkAsync().ConfigureAwait(False)`). Such a statement does not wait for the task, and the task's exceptions are lost. Report only.

Changes:
 - DCA013: `String.Join(",", values)` is no longer reported. Its fix duplicated the statement into an `#If NETCOREAPP` block that was not needed; DCA049 now reports it on every target framework, and its fix just writes `","c`.

Fixes:
 - DCA007: no longer reports the other declarations of a partial type when one declaration is documented. Documenting them would produce compiler warning BC42314 (*"XML comment cannot be applied more than once on a partial class"*). When no declaration is documented, only one is reported: the main declaration, or the first one.
 - DCA045: the code fix no longer fails on calls written without parentheses (`Dim tmp As String = Path.GetTempFileName`). Before, Visual Studio disabled `InsecureTempFileCodeFixProvider` and `BatchFixAllProvider` with *"encountered an error and has been disabled"*.

## v1.0.2 🔖
22 new rules (DCA027 – DCA048), most of them VB.NET equivalents of SonarQube rules:
 - With code fix:
   - DCA027 (Warning): `Thread.Sleep` in an `Async` method → `Await Task.Delay(x).ConfigureAwait(False/True)`.
   - DCA028 (Warning): Windows Forms `Sub Main` without `<STAThread>` (S4210) → adds the attribute.
   - DCA029 (Warning): strings concatenated with `+` / `+=` (S1645) → `&` / `&=`.
   - DCA030 (Info): `New T() {...}` that can be an array literal (S2355) → `{...}`.
   - DCA031 (Info): array designator on the variable name (S1197) → moved to the type.
   - DCA045 (Warning): `Path.GetTempFileName` (S5445) → random name created atomically with `FileMode.CreateNew`.
 - Report only (no fix keeps the behavior of the code):
   - DCA032, DCA033 (Warning) and DCA034 (Info): `End` (S1147), `On Error` (S2359) and `GoTo` (S907) statements.
   - DCA035 (Warning): `Application.DoEvents` inside a loop.
   - DCA036 (Warning): `SyncLock` on `Me`, strings, types and other weak-identity objects (S2551, S3998).
   - DCA037 (Warning): `Monitor.Enter` without `Monitor.Exit` in a `Finally` block (S2222).
   - DCA038 (Warning): `Thread.Suspend` / `Thread.Resume` (S3889).
   - DCA039 (Warning): delegate `BeginInvoke` without `EndInvoke` (S4583).
   - DCA040, DCA041 (Warning) and DCA042 (Info): duplicated conditions (S1862), all branches identical (S3923) and two identical branches (S1871).
   - DCA043 (Warning): identical expressions on both sides of a binary operator (S1764).
   - DCA044 (Info): `DateTime.Now` used to measure elapsed time (S6561).
   - DCA046, DCA047 (Warning) and DCA048 (Info): exceptions thrown in `Finally` blocks (S1163), in `Finalize` (S1048) and in property getters (S2372).

## v1.0.1 🔖
New rule:
 - DCA026 (Warning): text APIs called without an explicit `Encoding` (`File.WriteAllText`, `File.ReadAllLines`, `New StreamWriter`, `My.Computer.FileSystem.WriteAllText`...). Three fixes: UTF-8 without BOM, UTF-8 with BOM, `Encoding.Default`; the one that keeps the current behavior is offered first.

Changes:
 - DCA017: the default match timeout of the code fix is now **30 seconds** (`TimeSpan.FromSeconds(30)`, `devcase_analyzers.dca017_timeout_ms = 30000`), instead of 1 second.
 - DCA014: the code fix now writes `Thread.CurrentThread.Join(0)` inside an `#If DEBUG Then` … `#End If` block (ContextSwitchDeadlock only exists under the debugger). The analyzer also recognizes the call inside inactive conditional blocks.
 - DCA024: `Throw ex` inside a rethrow-only `Catch` block is now reported too.
 - DCA025: the `Not x Is Nothing` form of the null check is now recognized.
 - The VSIX package now includes the extension icon, preview image and project link shown in Visual Studio's *Extensions* dialog.

Fixes:
 - Code fixes no longer mix CRLF line breaks into files that use LF (and vice versa); every test now runs with both line endings.
 - The analyzer's own source code now builds with every DevCase rule and the enforced IDE/CA rules, without warnings or suppressions.

## v1.0 🔖
Initial Release, with 25 rules:
 - DCA001 – DCA003, DCA017 (Error): `ConfigureAwait(False)`, blocking `.Result`, silent guard clauses, `Regex` without match timeout (ReDoS).
 - DCA004 – DCA010 (Warning): `Option` statements, explicit constructors, namespaces, XML documentation, `<param>` tags, string comparisons, `<Flags>` enums.
 - DCA011, DCA013 (Info): .NET Framework → modern .NET migration helpers (throw helpers and APIs).
 - DCA012, DCA014 – DCA016 (Warning): `<exception>` documentation, `Thread.CurrentThread.Join(0)` in file-system loops, recursion, `Async Sub`.
 - DCA018 – DCA025 (Warning): `DateTimeKind`, date/time parsing with a format provider, `<Flags>` `None` member, `Find`/`Exists`/`TrueForAll`, indexer instead of `First`/`Last`/`ElementAt`, negated comparisons, rethrow-only `Catch` blocks, redundant null checks before `TypeOf` (SonarQube equivalents).
