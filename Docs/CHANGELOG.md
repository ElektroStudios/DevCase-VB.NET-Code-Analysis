# DevCase VB.NET Code Analysis Change Log 📋

## v1.0.3 *(current)* 🆕
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
