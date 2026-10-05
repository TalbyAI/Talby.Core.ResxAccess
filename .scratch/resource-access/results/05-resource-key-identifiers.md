# Resource Key identifier policies

Issue: [05 - Apply Resource Key identifier policies and collision diagnostics](../issues/05-handle-resource-key-identifiers.md)

Implementation starts at `0646dc0` on `feat/resource-key-identifiers`.
The existing design and approved consumer/aspect test boundaries are retained.

## Behavior and coverage

| Requirement | Evidence |
| --- | --- |
| Default Warn and explicit Ignore omit the same Raw Text and formatting methods | `ResourceKeyIdentifiers` retains the existing valid identifier generation and adds an Ignore target; only the default target receives `TRESX006`. `WarnAndIgnoreOmitTheSameMembersWithDistinctDiagnostics` verifies exact warnings on the Warn source file, their absence on the Ignore source file, the two remaining Raw Text overloads, runtime invocation and `CS0117` for omitted Raw Text and formatting members in both policies. |
| Normalize replaces invalid characters and preserves valid identifiers and keywords | `NormalizesReproduciblyAndInvokesOriginalKeysAndEscapedKeywords` compiles direct calls for punctuation, whitespace, a leading digit, a trailing newline, Unicode letters, `class` and `record`. It checks unchanged Raw Text and Formatted Text. Existing `KeywordResourceKey` DesignTime and SDK keyword assertions remain. |
| Reserve valid identifiers and suffixes before ordinal normalization | The SDK test places `a.b`, `a-b` and `a b` before valid `a_b` and `a_b_2`, requiring `_5`, `_4` and `_3`, respectively. Valid `_Text` and `_` similarly reserve their names. `NormalizedResourceKeyGeneration` snapshots valid-name reservation and all Raw Text and formatting overloads. |
| Reordering Resource entries retains names and lookup | The SDK test embeds independently named resources with reversed entry order, compares every generated signature and invokes every generated overload against both resources. Expected Raw Text and Formatted Text are separately asserted against literal Translations. |
| Original Resource Key lookup and localized formatting | The SDK test invokes normalized methods for original keys containing punctuation and whitespace, and a Localized Resource that reorders Named and Indexed identities. `NormalizedResourceKeyGeneration` snapshots `GetString` using original keys and unchanged Raw Text alongside localized formats. |
| Existing members and generated member families produce errors | `RejectsExistingMembersAndGeneratedMemberFamilyCollisions` asserts exact `TRESX007` messages on each source file for 16 targets: Raw Text methods, distinct overloads, properties, fields, events, nested types, formatting methods, normalized names, the class name, Unicode identifier identity, generated families and ResourceManager conflicts. `ResourceKeyIdentifierDiagnostics` snapshots six representative collision targets. |
| Omitted keys retain Resource Set and Placeholder Contract validation | `OmittedKeysStillReportResourceSetAndPlaceholderContractErrorsInBothPolicies` asserts exact diagnostics on eight source files: malformed Reference Resource placeholders, incompatible Localized Resource Argument Types outside Expected Cultures, missing Localized Resource keys and missing Expected Cultures, each under Warn and Ignore. Six snapshot targets additionally cover omitted non-text entries and precise Reference/Localized diagnostic mapping. |
| C# Unicode identifier identity | Normalization reservation and member collision checks ignore Unicode formatting characters. Generation also uses that C# identity so compiler consumers can call the methods. The SDK test invokes an original key containing an invisible formatting character whose normalized name collides with a valid identifier; lookup retains the original key. Diagnostic display escapes control and formatting characters. The existing reader identifier test adds a trailing-newline rejection. |
| Invalid identifier-policy attribute inputs | `RejectsUnsupportedIdentifierPolicies` and the thirteenth diagnostic snapshot target require precise `TRESX008` errors for `(InvalidKeyHandling)99`. Resource Set validation still completes before identifier-policy validation. |

The library exposes `InvalidKeyHandling.Warn`, `Ignore` and `Normalize` on
`GenerateResxAccessAttribute`. Resource Set validation completes before identifier
assignment, warnings or member generation. Colliding member families report
errors before introducing members; normalization suffixes resolve only Resource
Key identifier collisions.

The identifier recognizer now uses absolute string anchors. Its previous `$`
anchor accepted a trailing newline, which cannot be part of a C# method name.

## Test isolation and negative controls

The inventory adds two AspectTests and five IntegrationTests: 32 fast / 60 full
(20 UnitTests, 12 AspectTests, 28 IntegrationTests). All existing test identities
and assertions remain. `ResourceKeyIdentifiers` adds its Ignore target in the
same snapshot compilation. The 13-target diagnostic snapshot reports failures
at that grouped boundary while preserving each code, target and complete message.
Generation and keyword DesignTime snapshots remain separate.

Three compatible diagnostic tests reuse the existing cached SDK diagnostic build,
bringing that fixture to twelve tests. Each target retains its own source file
and Resource Set. SDK assertions require the filename, error code and complete
message on the same line. The two successful policy/normalization tests use
independent temporary consumers; the omission test then rebuilds its consumer
with source calls to omitted members and checks their compiler errors.

Ten temporary negative controls cover the new diagnostic groups:

| Boundary | Corrected input | Required assertion failure |
| --- | --- | --- |
| SDK existing members | Rename `ExistingRawMethod.Plain` | Missing `ExistingRawMethod` collision; other collisions remain. |
| SDK omitted Reference Resource | Correct only Warn's malformed placeholder | Missing `OmittedReferenceWarn` error; Ignore's error remains. |
| SDK omitted Localized Resource | Restore only Warn's Argument Type | Missing `OmittedLocalizedWarn` error; Ignore's error remains. |
| SDK omitted Resource Keys | Restore only Warn's missing Localized Resource key | Missing `OmittedKeysWarn` error; Ignore's error remains. |
| SDK omitted Expected Cultures | Add only Warn's required Localized Resource | Missing `OmittedExpectedWarn` error; Ignore's error remains. |
| Aspect member collisions | Rename `ExistingRawMember.Plain` | Missing collision in the exact snapshot; generated-family errors remain. |
| Aspect omitted Reference Resource structure | Change only Warn's non-text entry to a string | Missing `WarnNonText` error in the exact snapshot; Ignore's non-text error remains. |
| Aspect omitted Localized Resource contracts | Restore only Warn's declared Argument Type | Missing `WarnLocalizedContract` error in the exact snapshot; Ignore's contract error remains. |
| SDK unsupported policy | Replace the unsupported enum value with Warn | Missing `TRESX008` fails the source-specific assertion; other errors remain. |
| Aspect unsupported policy | Replace the unsupported enum value with Warn | Missing `TRESX008` fails the exact snapshot; other errors remain. |

Control logs and the temporary runner are kept under ignored `test-results/`.
Controls are restored after each execution; the complete solution is rebuilt
before final verification. No performance optimization or timing claim is made.

## Verification and review

Solution restore, `npm run format:check` and the Release solution build succeeded.
The build intentionally reports `TRESX006` for the two existing SDK fixture
classes whose Resource Sets contain omitted keys. Fast iteration validated all
32 selected test identities. After restoring every negative control and
addressing review feedback, the final full runner passed all 60 tests and
validated the exact inventory: 20 UnitTests, 12 AspectTests and 28 IntegrationTests.
Results are under ignored `test-results/execution/full-3a039e17f1d44ff3b1bf48817de1b05a/`.
All ten negative controls produced the expected assertion failures while other
errors remained. The final source contains none of those temporary corrections.

The two independent code-review axes reviewed the staged diff from the start
commit with `git diff --cached 0646dc0 --`, before committing implementation.

## Standards

The initial review found one current README CI count still referring to 53
tests. That reference was corrected, then updated to 60 when the Spec correction
added its consumer test. The reviewer confirmed consistent documentation and
inventory counts and found no remaining documented-standard or smell-baseline
findings.

## Spec

The initial review found that an undefined enum value silently selected the
Ignore behavior, conflicting with the parent specification's invalid attribute
input requirement. `TRESX008`, the source-specific consumer assertion and exact
snapshot resolve it. Both new negative controls demonstrated the missing
diagnostic failure. The reviewer confirmed that the correction resolves the
finding and found no remaining Spec gaps or scope creep.

Final review: Standards 0 remaining findings; Spec 0 remaining findings.
