# Resource Access Specification

Status: ready-for-agent

## Problem Statement

.NET developers maintain translated text in a Resource Set but need to write and maintain access and formatting code themselves. String-based access leaves Resource Key mistakes, inconsistent Localized Resources, and incompatible Formatting Placeholders to be discovered at runtime.

Developers need a generated, discoverable C# API that preserves Raw Text, produces correctly typed Formatted Text, and rejects inconsistent Resource Sets during compilation. Resource Culture and Formatting Culture must remain independent. Changes to resource files must refresh generation and validation during incremental builds and in the IDE without requiring edits to C# sources.

## Solution

A consumer applies `GenerateResxAccessAttribute` to a consumer-declared, non-generic static class and identifies a culture-neutral Reference Resource. Metalama validates the Resource Set and introduces public static methods on that class. The consumer controls the class name, namespace, and accessibility; a partial declaration is not required.

Each representable Resource Key receives Raw Text access methods. Resource Keys with Formatting Placeholders additionally receive formatting methods whose parameter identities, order, and Argument Types come from the Reference Resource. Localized Resources must satisfy the same Resource Key set and Placeholder Contracts.

The initial release supports text resources embedded under standard .NET SDK conventions, standard Indexed Placeholders, and Named Placeholders with supported Argument Types. It reports invalid resources and unsupported embedding configurations explicitly. Runtime access uses standard .NET resource fallback and composite formatting semantics.

## User Stories

1. As a library consumer, I want to annotate my own static class with `GenerateResxAccessAttribute`, so that the Resource Access API fits my existing class name and namespace.
2. As a library consumer, I want public static methods introduced without requiring a partial declaration, so that adopting Resource Access requires minimal changes to my class.
3. As a library consumer, I want to identify a culture-neutral Reference Resource, so that one resource defines the Resource Keys and Placeholder Contracts.
4. As a library consumer, I want the Reference Resource path resolved relative to my project directory, so that configuration works independently of the process working directory.
5. As a library consumer, I want associated Localized Resources discovered only alongside the Reference Resource, so that unrelated resources do not enter my Resource Set.
6. As a library consumer, I want resources embedded using standard .NET SDK conventions to resolve correctly, so that resource lookup works with the effective manifest base name.
7. As a library consumer, I want unsupported custom embedding configurations reported explicitly, so that an apparently successful build does not hide unusable resource access.
8. As a library consumer, I want every discovered Localized Resource validated, so that unexpected cultures cannot bypass consistency checks.
9. As a library consumer, I want to specify optional Expected Cultures, so that compilation fails when a required Localized Resource is absent.
10. As a library consumer, I want Expected Cultures to supplement discovery, so that cultures outside that list are still validated.
11. As a translator, I want each Localized Resource required to contain exactly the Reference Resource's case-sensitive Resource Keys, so that missing, additional, and case-mismatched entries are caught before release.
12. As a library consumer, I want duplicate Resource Keys rejected, so that Resource Access never depends on an ambiguous resource entry.
13. As a library consumer, I want non-text resource values rejected, so that the generated API has a consistent text contract.
14. As a translator, I want empty and whitespace-only Translations permitted when their Placeholder Contract allows them, so that intentionally blank text remains valid.
15. As a library consumer, I want methods named after representable Resource Keys, so that my editor can discover and check resource access.
16. As a library consumer, I want Raw Text methods to preserve the selected Translation without argument substitution, so that I can retrieve its original text.
17. As a library consumer, I want formatting methods only for Resource Keys with Formatting Placeholders, so that the generated API reflects the resource's needs.
18. As a library consumer, I want standard Indexed Placeholders supported, so that existing composite-format resources remain usable.
19. As a library consumer, I want Named Placeholders supported, so that formatting arguments communicate their meaning at call sites.
20. As a library consumer, I want Named Placeholders to declare supported Argument Types, so that C# checks formatting calls.
21. As a library consumer, I want untyped Named Placeholders and Indexed Placeholders to accept `object?`, so that ordinary composite-format argument usage remains available.
22. As a library consumer, I want explicit nullable value and reference Argument Types preserved, so that generated signatures express whether an argument may be null.
23. As a library consumer, I want nullable arguments to remain required parameters, so that nullability does not silently change the Placeholder Contract.
24. As a translator, I want alignment and Argument Formats supported, so that each Translation can present arguments appropriately.
25. As a translator, I want escaped braces supported, so that Translations can contain literal braces.
26. As a translator, I want to reorder Formatting Placeholders, so that translated sentences can follow the selected language's grammar.
27. As a translator, I want to repeat Formatting Placeholders, so that a supplied argument can appear more than once.
28. As a translator, I want to change Argument Formats without changing argument identities, so that formatting can suit the Translation.
29. As a translator, I want to omit explicit type declarations in a Localized Resource, so that the Reference Resource remains the source of Argument Types.
30. As a library consumer, I want explicit types in a Localized Resource required to match the Reference Resource, including nullability, so that Translations cannot change the public API contract.
31. As a library consumer, I want added or omitted argument identities rejected, so that every Translation accepts the same supplied arguments.
32. As a library consumer, I want Indexed Placeholder identities retained even when indices have gaps, so that unused indices do not become parameters.
33. As a library consumer, I want mixed Named Placeholders and Indexed Placeholders supported with stable parameter order, so that changing a Translation cannot change my call sites.
34. As a library consumer, I want invalid Named Placeholder identifiers and parameter collisions rejected, so that generated signatures remain valid without silently renaming my arguments.
35. As a library consumer, I want malformed Formatting Placeholders reported as compilation errors, so that formatting syntax problems are detected before runtime.
36. As a library consumer, I want invalid Resource Keys omitted with a warning by default, so that I can identify entries that cannot become methods.
37. As a library consumer, I want to omit invalid Resource Keys silently when I choose `Ignore`, so that I can opt out of identifier warnings.
38. As a library consumer, I want invalid Resource Keys normalized when I choose `Normalize`, so that those entries can receive usable method names.
39. As a library consumer, I want valid identifiers preserved and normalization collisions resolved reproducibly, so that unrelated resource edits do not unpredictably rename methods.
40. As a library consumer, I want C# keywords escaped as identifiers, so that valid Resource Keys are usable even when they match a keyword.
41. As a library consumer, I want omitted invalid Resource Keys still validated, so that identifier handling cannot conceal Resource Set errors.
42. As a library consumer, I want collisions with existing members or generated member families rejected, so that generation cannot silently replace or merge incompatible API members.
43. As a library consumer, I want Resource Culture to default to `CurrentUICulture`, so that lookup follows my application's UI language.
44. As a library consumer, I want an explicit Resource Culture overload, so that I can select a Translation for a particular request.
45. As a library consumer, I want Formatting Culture to default independently to `CurrentCulture`, so that argument presentation follows the user's formatting preferences.
46. As a library consumer, I want a separate explicit Formatting Culture, so that I can control argument presentation independently of Translation selection.
47. As a library consumer, I want standard parent-culture and Reference Resource fallback, so that requested cultures without a resource can still retrieve text.
48. As a library consumer, I want standard formatting exceptions to propagate and resource failures to be descriptive, so that I can diagnose runtime failures.
49. As a library consumer, I want resource-content edits reflected in incremental builds without C# edits, so that generated methods and diagnostics stay current.
50. As a library consumer, I want resource additions and removals reflected in incremental builds, so that discovery and Expected Culture validation stay current.
51. As a library consumer, I want resource edits, additions, and removals reflected in the IDE without manual recompilation, so that completion and diagnostics match the current Resource Set.
52. As a library consumer, I want documentation to explain culture selection, nullable arguments, identifier handling, and mixed-placeholder ordering, so that I can use the generated API predictably.

## Implementation Decisions

- The public entry point is `GenerateResxAccessAttribute`, backed by Metalama member introduction. The supported target is a consumer-declared, non-generic static class. Generated methods are public and static; the consumer selects the target class name and namespace. A partial declaration is not required.
- The attribute requires a culture-neutral Reference Resource. Its path is resolved relative to the consumer project directory.
- Associated Localized Resources follow the Reference Resource's base name plus a culture suffix and are discovered only in its directory.
- `ExpectedCultures` is optional. Each listed culture requires an associated Localized Resource. This property does not filter discovered cultures or exempt them from validation.
- The initial release supports embedded resources under standard .NET SDK conventions. Resolve the effective manifest base name rather than assuming that it matches the generated class name. Standard resource naming can depend on resource location, root namespace, and an associated C# type.
- Custom embedding or naming configurations, including explicit `LogicalName`, explicit `ManifestResourceName`, and linked resources, are outside this release and receive explicit diagnostics.
- Every associated Localized Resource must contain exactly the Reference Resource's case-sensitive Resource Keys. Missing and additional keys are compilation errors. Runtime fallback does not excuse an incomplete Localized Resource that is present.
- Resource validation applies to the entire Resource Set, including entries whose Resource Keys do not receive generated methods.
- Support text values only. Duplicate Resource Keys, non-text values, and malformed Formatting Placeholders are compilation errors. Empty and whitespace-only text is valid if it satisfies the Placeholder Contract.
- Support standard .NET Indexed Placeholders and the Named Placeholder syntax `{name[@type][,alignment][:format]}`. Escaped braces use `{{` and `}}`. Preserve standard alignment and composite-format semantics.
- Explicit Named Placeholder Argument Types initially include `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, and `Guid`. A nullable `?` suffix is supported for each applicable reference or value type.
- Untyped arguments, including Indexed Placeholders, use `object?`. Explicit `@string` produces `string`, `@string?` produces `string?`, `@decimal` produces `decimal`, and `@decimal?` produces `decimal?`.
- Non-nullable reference Argument Types use compiler annotations without additional runtime null checks. Null arguments follow standard .NET composite-format behavior.
- The Reference Resource defines the Placeholder Contract for each Resource Key: argument identities and Argument Types, including nullability. Every corresponding Translation must use all and only those identities.
- Translations may reorder or repeat arguments and use different Argument Formats. They may omit type declarations; any explicit declaration must match the Reference Resource's Argument Type and nullability.
- Nullable arguments remain required parameters and required placeholder identities. Nullability does not make an argument optional or permit a Translation to omit it.
- Indexed Placeholder identities remain numeric and independent of Named Placeholders. Index gaps are allowed; only used identities become parameters. A Translation cannot introduce an index that is absent from the Reference Resource.
- Named parameters appear first, in order of first appearance in the Reference Resource. Indexed parameters follow in numeric order with names such as `arg0` and `arg2`. The mixed reference text `{name} {2} {0}` therefore produces parameters `name`, `arg0`, and `arg2`, with no parameter for index 1.
- Named and Indexed Placeholders may coexist. Documentation recommends a single style per Resource Key without rejecting mixed styles.
- Named argument identifiers must be valid C# identifiers and cannot collide with other generated parameters, including Indexed Placeholder parameter names, `resourceCulture`, and `formattingCulture`. Violations are compilation errors; argument names are not silently renamed.
- `InvalidKeyHandling` supports `Warn`, `Ignore`, and `Normalize`. The default is `Warn`. Warn omits invalid Resource Key members and emits a warning; Ignore omits them without an identifier warning. Neither mode skips resource validation.
- Normalize replaces invalid identifier characters with underscores, preserves valid identifiers, and resolves normalization collisions with suffixes. C# keywords use identifier escaping.
- Reserve valid Resource Key identifiers before assigning normalized identifiers. Process keys requiring normalization in ordinal order, appending `_2`, `_3`, and subsequent suffixes when needed. Results must be reproducible.
- Each representable Resource Key receives two Raw Text overloads: one without culture parameters and one accepting `CultureInfo resourceCulture`. Raw Text retains the selected Translation without substitution.
- Only Resource Keys with Formatting Placeholders additionally receive methods prefixed with `Format`. These expose the Reference Resource's ordered, typed arguments in three overloads: arguments alone, arguments followed by `resourceCulture`, and arguments followed by `resourceCulture` and `formattingCulture`. A Resource Key without placeholders receives only Raw Text methods.
- Collisions with existing target-class members or between generated member families are compilation errors. Identifier normalization does not authorize silently renaming conflicting member families.
- Resource Culture defaults to `CurrentUICulture`. Formatting Culture defaults independently to `CurrentCulture`, including when Resource Culture is supplied explicitly.
- Runtime lookup uses standard .NET resource fallback through parent cultures and finally the Reference Resource. Formatted Text substitutes arguments into the selected Translation using the selected Formatting Culture.
- Formatting failures propagate corresponding standard exceptions. Runtime resource failures produce descriptive exceptions.
- Resource-only edits, additions, and removals must refresh both generated API and diagnostics during incremental builds and in the IDE, without C# edits or manual recompilation. The integration mechanism remains to be verified; this requirement cannot be dropped based on current uncertainty.
- Extend the existing library and ordinary xUnit test project, and add a dedicated aspect snapshot test project for generation and diagnostics. Preserve nullable reference types, implicit usings, `MetalamaEnabled=false` in the ordinary test project, and `MetalamaRemoveCompileTimeOnlyCode=false` in the library. The snapshot project references a `Metalama.Testing.AspectTesting` version compatible with the library's Metalama version and is included in solution-level build and test commands.
- Keep the aspect snapshot project separate from ordinary `[Fact]` tests: Metalama's snapshot package changes project-item semantics and xUnit discovery. Use the Metalama 2026.1 automatic file-based discovery rather than the removed `AspectTestClass` or custom runner pattern. [Metalama snapshot testing documentation](https://doc.metalama.net/conceptual/aspects/testing/snapshot-testing).
- Keep implementation choices for parsing, runtime support, and external-file dependency integration open until technical verification establishes the smallest solution that satisfies these behaviors. This spec does not prescribe additional abstraction layers or a new dependency.

## Testing Decisions

- The user confirmed the consumer-project test boundary and requested evaluation of aspect tests. Keep the consumer compilation and generated public API as the acceptance boundary: compile a consumer-declared attributed static class together with actual embedded Reference and Localized Resources, inspect its generated public API and compilation diagnostics, and invoke the compiled API for runtime assertions. Reuse this boundary for resource-only rebuild scenarios.
- Add dedicated Metalama aspect snapshot tests for introduced members and diagnostics, using attributed consumer code and representative Resource Sets. Keep them at the aspect application boundary rather than testing private generation helpers. Verify that the harness can supply the resource files and consumer project context; use real consumer builds for cases that it cannot represent. [Metalama snapshot testing documentation](https://doc.metalama.net/conceptual/aspects/testing/snapshot-testing).
- Snapshot inputs are standalone source-file test cases; expected transformed code is stored in reviewed `.t.cs` baselines, which can include errors and warnings. Inspect actual output before accepting a baseline. Include warnings in relevant tests so that default Warn and explicit Ignore remain distinguishable. Use framework options to limit output to the target API when appropriate, and avoid assertions about incidental generated locals or helper layouts. [Metalama snapshot testing documentation](https://doc.metalama.net/conceptual/aspects/testing/snapshot-testing).
- Snapshot testing supplements consumer integration checks; transformed-code baselines alone do not establish embedding, runtime fallback, or resource-file invalidation. Retain the ordinary xUnit project for runtime/integration orchestration and focused compile-time helper tests only when they add useful coverage.
- A good test asserts externally observable behavior: generated signatures, compiler diagnostics, returned Raw Text and Formatted Text, fallback, and exceptions. It does not couple assertions to parser internals, private helpers, generated local variable names, or the chosen normalization implementation.
- Existing prior art is the xUnit Metalama compilation test using `UnitTestClass`, a disposable `CreateTestContext()`, and compilation code-model queries. Reuse that pattern for code-model or compile-time helper checks where useful. Metalama documents this as helper-code unit testing, distinct from aspect snapshot testing. The existing test establishes setup only; it does not demonstrate aspect execution, embedded-resource loading, incremental rebuilds, or IDE refresh. [Metalama compile-time helper testing documentation](https://doc.metalama.net/conceptual/aspects/testing/compile-time-testing).
- Use a real SDK consumer project at the same external boundary where the existing in-memory compilation context cannot represent embedding, satellite resources, or build invalidation. Keep any added fixture infrastructure limited to those needs.
- Assert public static member introduction on a non-generic static class without a partial declaration, with the consumer's chosen class name and namespace. Verify generated Raw Text methods and the presence or absence of formatting methods.
- Assert all supported Argument Types and nullable forms, untyped `object?`, required nullable parameters, named-first ordering, numeric indexed ordering, and mixed placeholders with gaps. Check compiler-visible nullable annotations as well as value-type signatures.
- Exercise alignment, formats, escaped braces, null arguments, empty text, and whitespace-only text. Assert unchanged Raw Text and the expected Formatted Text.
- Compile Resource Sets with reordered and repeated placeholder occurrences and changed formats. Accept omitted translation type declarations; reject additional or omitted identities, changed explicit types, and changed explicit nullability.
- Assert compilation errors for missing Expected Cultures, missing or additional case-sensitive keys, duplicate keys, non-text values, and malformed placeholders. Include a discovered culture outside `ExpectedCultures` to prove that the list does not filter validation.
- Assert Warn, Ignore, and Normalize behavior, keyword escaping, reservation of valid identifiers, deterministic suffix assignment, and resource validation for omitted keys. Reorder resource entries to check that normalization remains stable.
- Assert diagnostics for invalid argument identifiers, collisions with named or indexed parameters and culture parameters, existing target-class members, and collisions between generated Raw Text and formatting member families.
- Exercise supported manifest naming and satellite-resource lookup, including a case where the effective manifest base name differs from the target class name. Verify explicit diagnostics for the excluded custom metadata and linked-resource configurations.
- Invoke generated methods with distinct Resource and Formatting Cultures. Verify default `CurrentUICulture` and `CurrentCulture`, explicit overrides, parent-culture fallback, and Reference Resource fallback. Restore ambient cultures after each test.
- Assert standard formatting exception propagation and descriptive runtime resource failures without prescribing an exception message verbatim unless its text is deliberately part of the public contract.
- Start from a successful consumer build, then edit resource text or placeholders and perform an incremental build without touching C# sources. Verify updated runtime results, signatures, and diagnostics as appropriate. Repeat for adding and removing Localized Resources, including Expected Culture failures.
- Verify equivalent external-file edit, addition, and removal scenarios in the supported IDE integration. Observe updated completion or generated API and diagnostics without a C# edit or manual recompilation. Record reproducible evidence; a clean build alone does not satisfy this criterion.
- Establish technical feasibility of static member introduction, nullable signatures, effective manifest naming, satellite lookup, and build/IDE resource invalidation before treating those capabilities as proven. Do not substitute helper tests for the consumer-level acceptance checks.

## Out of Scope

- Implementing the feature as part of this specification-writing task.
- Instance or generic target classes.
- Non-text resource values.
- Custom embedding and naming configurations, including explicit resource manifest metadata and linked resources.
- Discovery of Localized Resources outside the Reference Resource's directory.
- Partial Localized Resources, relaxed case-sensitive key matching, or using runtime fallback to excuse missing keys in an included Localized Resource.
- Custom Argument Types beyond the listed built-in types.
- Silently renaming invalid argument identifiers or resolving member-family conflicts by inventing alternative public API names.
- Additional runtime null checks for non-nullable reference arguments.
- Formatting methods for Resource Keys without Formatting Placeholders.
- A formatting-culture-only overload, runtime resource editing, or hot reload of resources in an already running application.
- A translation editor, translation service, or resource-authoring UI.

## Further Notes

- Source: [Resource Access Design](../../docs/resource-access-design.md). That document records agreed requirements and states that final shared-understanding confirmation was pending; implementation has not started.
- Use the [Resource Access glossary](../../CONTEXT.md) throughout implementation and documentation.
- Preserve [the complete Localized Resources decision](../../docs/adr/0001-require-complete-localized-resources.md) and [the mixed-placeholder parameter order decision](../../docs/adr/0002-order-mixed-placeholder-parameters.md). This specification introduces no contradiction with either ADR.
- The current repository contains a .NET 10 library referencing Metalama and one compilation-setup test. It contains no Resource Access implementation or resource assets.
- The source design's technical evidence is prior research, not implementation verification performed during this specification-writing task.
- The official Metalama 2026.1 testing documentation was reviewed for this spec. It supports a separate snapshot-only project with automatic file-based test discovery, while the existing UnitTesting setup remains appropriate for helper-code tests. Resource-file provisioning in the snapshot harness still needs verification; no test projects or packages are added by this documentation task.
- Build and IDE regeneration after external resource changes remains an open technical verification item. Failure to establish it must be reported as an unmet acceptance requirement.
- Invalid attribute inputs, unsupported type spellings, and conflicting declarations of one argument within the Reference Resource need precise diagnostic behavior during implementation. These details must preserve the stated target, type, and Placeholder Contracts; they are not permission to broaden the supported syntax.
- Publish this spec as one feature in the local Markdown issue tracker with `ready-for-agent` status. Implementation ticket decomposition is a separate task.

## Comments

- The user confirmed the consumer-project acceptance boundary and asked whether a dedicated aspect test project was needed. After reviewing Metalama's official documentation, the specification includes a separate aspect snapshot project for introduced code and diagnostics, with consumer integration checks retained for runtime and build/IDE behavior.
