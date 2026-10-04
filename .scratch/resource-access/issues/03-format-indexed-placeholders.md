# Generate Indexed Placeholder formatting methods

Status: ready-for-agent
Type: AFK
User stories covered: 17-18, 21, 24-28, 31-32, 35, 45-46, 48

## Parent

[Resource Access Specification](../spec.md)

## What to build

A consumer supplies arguments to generated `Format` methods for Resource Keys containing Indexed Placeholders and receives Formatted Text from the selected Translation. The Reference Resource establishes numeric argument identities; Translations can reorder or repeat them and change their presentation without changing the call signature. Resource Culture and Formatting Culture remain independent.

## Acceptance criteria

- [x] Resource Keys with Formatting Placeholders receive `Format`-prefixed public static methods with three overloads: arguments alone, arguments followed by `resourceCulture`, and arguments followed by `resourceCulture` and `formattingCulture`. Keys without placeholders retain only Raw Text methods; no formatting-culture-only overload is introduced.
- [x] Indexed arguments use required `object?` parameters named by their identities and ordered numerically. Gaps are allowed: indices 0 and 2 produce `arg0` and `arg2`, with no parameter for index 1.
- [x] Every Translation uses all and only the Reference Resource's argument identities. Reordering, repetition, and different Argument Formats succeed; additional or omitted identities produce compilation errors across the entire Resource Set.
- [x] Support standard composite-format alignment, Argument Formats, escaped braces, and null arguments. Raw Text remains unchanged; invoked formatting methods return expected Formatted Text.
- [x] Malformed Formatting Placeholders produce compilation errors. Empty or whitespace-only text remains valid only when its Placeholder Contract permits it.
- [x] Formatting Culture defaults to `CurrentCulture` independently of Resource Culture, including when Resource Culture is explicit. Verify all overloads using distinct ambient and explicit cultures, selected Translations, and resource fallback; restore ambient cultures afterward.
- [x] Standard formatting failures propagate their corresponding exceptions. For example, a valid placeholder syntax with an incompatible Argument Format fails at runtime rather than being wrapped in an unrelated exception.
- [x] Add generated-signature and diagnostic aspect snapshots plus actual consumer compilation and invocation checks. Assertions observe API, diagnostics, and returned text rather than parser internals or generated helper names.

## Blocked by

- [02 - Discover and validate complete Localized Resources](02-validate-localized-resources.md)
## Comments

- Implemented on 2026-10-04 on `feat/indexed-placeholder-formatting`, starting from main `921bf5d`. Resource Keys with Indexed Placeholders receive three `Format` overloads with required nullable `object?` arguments in numeric identity order, including index gaps. Raw Text and standard resource lookup remain unchanged.
- The Reference Resource establishes each indexed Placeholder Contract. Every discovered Localized Resource is checked for exact identities, including cultures outside `ExpectedCultures` and Resource Keys omitted by identifier handling. Reordering, repetition, alignment changes, and Argument Format changes preserve the contract. Malformed Reference/Localized syntax receives `TRESX001`/`TRESX004` with the resource and Resource Key.
- Compile-time syntax validation uses standard composite formatting with one null argument and a remapped identity; it does not evaluate Argument Formats or allocate an argument array proportional to index gaps. Numeric scanning follows the [.NET 10 formatting implementation](https://github.com/dotnet/runtime/blob/v10.0.0/src/libraries/System.Private.CoreLib/src/System/Text/ValueStringBuilder.AppendFormat.cs). Runtime formatting uses the selected Translation and standard `string.Format`, leaving gaps unexposed in the generated signature and propagating formatting exceptions.
- Six consumer tests verify generated signatures/nullability, all three overloads, independent ambient/explicit Resource and Formatting Cultures, actual satellite lookup and parent/reference fallback, reordered/repeated identities and formats, escaped braces, alignment, null arguments, unchanged Raw Text, standard formatting exceptions, malformed syntax, and changed Localized Resource contracts. Ambient cultures are restored. Successful scenarios use the precompiled SDK ConsumerFixture; failed-compilation scenarios use isolated real SDK consumers.
- The initial consumer API test failed because `FormatSummary` was absent, then passed after generation was implemented. Two AspectTests cover generated signatures and compilation diagnostics; actual transformed code and diagnostic output were inspected before accepting the baselines. All pre-existing snapshot and consumer assertions remain.
- Initial verification (before snapshot consolidation): solution restore succeeded; Release solution build succeeded with zero warnings/errors; `pwsh -NoProfile -File tests/run.ps1 -Mode full` passed all 47 tests (19 unit, 12 aspect snapshots, 16 integration) and validated the inventory. README and AGENTS.md recorded 31 fast / 47 full tests at that point.
- Named and mixed Formatting Placeholders retain Raw Text access; their formatting and full Placeholder Contract validation remain in dependent issue 04. Identifier policies and resource-only build/IDE refresh verification remain in their dependent issues.
- Current verification (after snapshot consolidation and PR #6 review corrections): solution restore succeeded; Release solution build succeeded with zero warnings/errors; `pwsh -NoProfile -File tests/run.ps1 -Mode full` passed all 43 tests (19 unit, 8 aspect snapshots, 16 integration) and validated the inventory. README, AGENTS.md and `tests/run.ps1` now record 27 fast / 43 full tests. README documents the runtime argument-array allocation proportional to the highest index; only compile-time syntax validation avoids that cost.

### Standards review

No documented-standard violations. Two heuristic suggestions were considered: representing named/mixed/empty indexed contracts with an explicit type, and sharing an overload descriptor between signature generation and the template. The current nullable array and three fixed overloads remain sufficient for this slice; introducing additional types now would expand structure before the named/mixed ticket.

### Spec review

No missing/partial indexed requirements, unrequested behavior, or reproduced implementation defects. Generated signatures, exact indexed identities across all discovered cultures and omitted keys, composite formatting, independent cultures, fallback, and error behavior match ticket 03. Named/mixed behavior remains assigned to issue 04.

Review totals: Standards — zero violations, two non-blocking structural heuristics; Spec — zero findings.
