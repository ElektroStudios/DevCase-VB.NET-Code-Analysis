# DevCase VB.NET Code Analysis Change Log 📋

## v1.0 *(current)* 🆕
Initial Release, with 25 rules:
 - DCA001 – DCA003, DCA017 (Error): `ConfigureAwait(False)`, blocking `.Result`, silent guard clauses, `Regex` without match timeout (ReDoS).
 - DCA004 – DCA010 (Warning): `Option` statements, explicit constructors, namespaces, XML documentation, `<param>` tags, string comparisons, `<Flags>` enums.
 - DCA011, DCA013 (Info): .NET Framework → modern .NET migration helpers (throw helpers and APIs).
 - DCA012, DCA014 – DCA016 (Warning): `<exception>` documentation, `Thread.CurrentThread.Join(0)` in file-system loops, recursion, `Async Sub`.
 - DCA018 – DCA025 (Warning): `DateTimeKind`, date/time parsing with a format provider, `<Flags>` `None` member, `Find`/`Exists`/`TrueForAll`, indexer instead of `First`/`Last`/`ElementAt`, negated comparisons, rethrow-only `Catch` blocks, redundant null checks before `TypeOf` (SonarQube equivalents).
