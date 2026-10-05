# Named and mixed Formatting Placeholders

Issue: [04 - Support typed Named and mixed Formatting Placeholders](../issues/04-format-named-and-mixed-placeholders.md)

Implementation starts at `650e94d` on `feat/named-and-mixed-placeholders`.
The existing design, Placeholder Contract and test boundaries are retained.

## Behavior and coverage

| Requirement | Retained or added evidence |
| --- | --- |
| Named-first order, Indexed numeric order and gaps, reference-controlled identities | `GeneratesNamedArgumentsBeforeOnlyUsedIndexedIdentities` and `NamedPlaceholderGeneration`; the mixed snapshot includes a second Named identity after Indexed occurrences. |
| All nine explicit Argument Types, nullable forms, required parameters and compiler-visible reference nullability | `PreservesEverySupportedArgumentTypeAndNullableRequiredSignature`, all eighteen signatures in `NamedPlaceholderGeneration`, and `ConsumerCompilerEnforcesNamedTypesNullabilityAndRequiredArguments`. |
| Untyped `object?`, nullable invocation, standard formatting, Unicode and keywords | `FormatsUntypedNullableKeywordAndUnicodeArgumentsWithStandardSemantics`; direct SDK-generated calls verify keyword escaping and unchanged Raw Text. |
| Reordered/repeated Localized identities, changed formats, omitted and matching explicit declarations | SDK fixture `Named.es.resx`, runtime checks, and the localized branches in `NamedPlaceholderGeneration`. |
| Independent Resource and Formatting Cultures, all three overloads and parent/reference fallback | `SelectsIndependentCulturesAndReorderedFormatsForEveryNamedOverload`; ambient cultures are restored. |
| Invalid identifiers, supported type spellings, conflicting declarations, parameter collisions, malformed syntax | `RejectsInvalidNamedDeclarationsAndParameterCollisionsPrecisely`: 26 targets, each asserting the source filename, `TRESX001`, resource path, Resource Key and expected message on the same line. |
| Exact Named and Indexed identities, nullability/type mismatches, omitted nullable identities and invalid Resource Keys | `RejectsChangedNamedAndMixedContractsIncludingNullabilityAndOmittedKeys`: 15 targets, each asserting the filename, `TRESX004`, resource path, omitted Resource Key and expected message on the same line. The invalid Localized Resource uses `fr`, outside `ExpectedCultures = ["es"]`. |
| Exact diagnostic mapping | `NamedPlaceholderDiagnostics`: 20 descriptively named targets, with exact diagnostic codes, targets and messages in the snapshot. |
| Isolated production-helper validation | `ValidatesNamedAndMixedContractsWithTranslationTypeInheritance` invokes `ReferenceResourceReader`: omitted Localized type declarations succeed, changed explicit nullability and omitted nullable identities fail with exact resource/key messages. |
| Compiler enforcement of types, non-nullable reference annotations and required nullable arguments | An isolated consumer references the precompiled SDK fixture and requires `CS1503`, `CS8625`, and `CS1501` on their respective source files. Nullable warnings are errors. |
| Existing Indexed/Raw Text behavior | Every existing test identity and Raw Text assertion is retained. `IndexedPlaceholderGeneration` changes only generated formatting bodies to use the shared contract and dense argument positions. `RawTextGeneration` keeps both Raw Text bodies unchanged and adds three Named formatting overloads. The SDK fixture's API inventory changes from four Raw Text methods to those same four plus three `FormatWelcome` overloads, with no `FormatPlain`; it additionally invokes mixed Formatted Text. |

Formatting uses the selected Raw Text to choose its validated composite format.
Conversion happens at compile time, preserving escaped braces, alignment and
Argument Formats. Runtime formatting remains standard `string.Format` and adds
no reference-argument null checks. Each runtime array contains one slot per
public argument, including mixed identities without exposing unused indices.
Control and Unicode formatting characters are escaped in identifier diagnostics;
C# identifier comparisons ignore Unicode formatting characters when detecting
parameter collisions.

## Test isolation and negative controls

The inventory adds one UnitTest, two AspectTests and seven IntegrationTests: 30 fast / 53 full
(20 UnitTests, 10 AspectTests, 23 IntegrationTests). No previous test identity is
removed or renamed. New compatible aspect diagnostics join the existing isolated
SDK compilation. Compiler enforcement uses a separate consumer referencing the
compiled fixture, so aspect-validation failures cannot hide compiler diagnostics.
Snapshots retain separate generation and diagnostic boundaries. No execution
optimization or performance improvement is claimed.

Five temporary controls were run sequentially, with a Release rebuild before
each filtered test and restoration in `finally`. Every corrected invalid input
caused the expected test assertion failure while other diagnostics remained:

| Control | Corrected input | Expected missing diagnostic |
| --- | --- | --- |
| Snapshot Reference Resource | `{bad-name}` becomes `{validName}` | `TRESX001` on `InvalidIdentifier` |
| Snapshot Localized Resource | Localized `@long` becomes reference `@int` | `TRESX004` on `ChangedType` |
| SDK Reference Resource | First invalid identifier becomes valid | `TRESX001` on `NamedInvalid0.cs` |
| SDK Localized Resource | First explicit type matches the Reference Resource | `TRESX004` on `NamedChanged0.cs` |
| Consumer compiler | Wrong string argument becomes `1m` | `CS1503` on `WrongType.cs` |

Temporary scripts, logs and transformed control outputs remain under ignored
`test-results/` or `obj/`. All controls are reverted. The first API test failed
because `FormatMixed` was absent, then passed after implementation. Actual
transformed output and diagnostic messages were inspected before accepting the
new snapshot baselines.

## Verification

- Solution restore succeeded with .NET SDK `10.0.401`.
- `npm run format:check` passed for C# and Markdown.
- Release solution build succeeded with zero warnings and zero errors.
- `pwsh -NoProfile -File tests/run.ps1 -Mode full` passed all 53 tests and validated the complete inventory after restoring every negative control.

## Standards review

The review requested direct UnitTests coverage of the production Placeholder
Contract validation boundary and identified a repeated supported-type list as
a maintenance heuristic. Both were addressed: the focused reader test passes,
and `PlaceholderArgument.GetSupportedType` supplies the single spelling-to-CLR
mapping used by validation and generation. The follow-up review found no
remaining standards concerns. All tests were rebuilt and the complete 53-test
inventory passed after these changes.

## Spec review

No missing or partial requirements, scope creep, or incorrect behavior was
identified against issue 04 and the parent Resource Access Specification.
Final review totals: Standards — zero remaining concerns; Spec — zero findings.
