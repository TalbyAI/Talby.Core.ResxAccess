# Generate Raw Text methods with standard SDK embedding

Status: ready-for-agent
Type: AFK
User stories covered: 1-4, 6-7, 15-17, 43-44, 48

## Parent

[Resource Access Specification](../spec.md)

## What to build

A consumer applies `GenerateResxAccessAttribute` to their own non-generic static class and retrieves unchanged Raw Text from an embedded, culture-neutral Reference Resource. Introduce public static methods without requiring a partial declaration, preserving the consumer's class name, namespace, and accessibility. Resolve the Reference Resource relative to the consumer project directory and use its effective standard SDK manifest base name.

Establish the consumer compilation and invocation acceptance boundary, alongside a separate aspect snapshot project. Keep this slice verifiable with a Reference Resource alone; Localized Resource discovery and formatting are delivered by the dependent issues.

## Acceptance criteria

- [x] A real SDK consumer compiles a non-partial attributed static class and invokes public static Raw Text methods named after representable Resource Keys. The generated API preserves the consumer's selected class identity and accessibility.
- [x] Each represented Resource Key has a parameterless overload and an overload accepting `CultureInfo resourceCulture`. The default Resource Culture is `CurrentUICulture`; explicit Resource Culture is forwarded to standard resource lookup.
- [x] Raw Text preserves the resource value without substitution, including text containing Formatting Placeholders. Resource Keys without Formatting Placeholders receive no formatting methods.
- [x] Reference Resource resolution is relative to the consumer project directory, including when the build runs from a different working directory. Missing or invalid Reference Resource inputs, a culture-specific reference, and unsupported instance or generic targets receive explicit compilation diagnostics.
- [x] Runtime invocation works with standard SDK naming influenced by resource location, root namespace, and an associated C# type. Include a manifest base name that differs from the target class name.
- [x] Explicit `LogicalName`, explicit `ManifestResourceName`, and linked-resource configurations receive explicit diagnostics rather than generating apparently usable access methods.
- [x] Runtime resource failures produce descriptive exceptions. Assertions check the failure behavior without freezing incidental exception wording.
- [x] Add a dedicated aspect snapshot project using a compatible `Metalama.Testing.AspectTesting` version and Metalama 2026.1 automatic file-based discovery. Include it in solution-level build and test commands; keep ordinary xUnit tests separate.
- [x] Verify snapshot resource-file and consumer-project context support. Review actual transformed output before accepting `.t.cs` baselines, and use real consumer builds for cases the snapshot harness cannot represent. Assert public API and diagnostics rather than private helper layouts.
- [x] Preserve nullable reference types, implicit usings, `MetalamaEnabled=false` in the ordinary test project, and `MetalamaRemoveCompileTimeOnlyCode=false` in the library. Keep consumer fixture infrastructure limited to integration needs.

## Blocked by

None - can start immediately.

## Comments

- Implementation completed on 2026-10-02. `GenerateResxAccessAttribute` introduces the two Raw Text overloads and uses the SDK's effective manifest base name. The packaged `buildTransitive` targets expose resource metadata; source consumers using `ProjectReference` import those targets explicitly, as documented in the README.
- Real consumer builds run from a different working directory and verify class identity, accessibility, unchanged Formatting Placeholders, explicit and default Resource Culture, parent/reference fallback, root namespace and associated C# type naming, invalid inputs and unsupported embedding diagnostics, and descriptive missing-manifest/missing-key exceptions. Runtime assertions do not depend on private helper layouts or exact exception wording.
- `Metalama.Testing.AspectTesting` 2026.1.28 provides automatic file-based discovery in a separate project. Resource files and the targets import are present, but the stock runner does not forward the consumer project path or SDK resource map; its custom runner factory is internal. `UnavailableProjectContext` records that limitation. Generation and context-dependent diagnostics therefore use real SDK consumer builds. Actual diagnostic output was reviewed before accepting the three `.t.cs` baselines; actual generated consumer code was also inspected.
- Malformed resource XML is rejected by SDK resource generation with `MSB3103` before aspect execution. Invalid attribute paths and targets receive `TRESX001`/`TRESX002`; unsupported embedding receives `TRESX003`.
- Verification: solution restore succeeded; Release build succeeded with zero warnings/errors; the full solution test run passed 8 ordinary xUnit tests and 3 snapshots. A locally packed NuGet consumer also built and invoked Raw Text without a manual targets import.
- Standards and Spec reviews completed. Fixture braces were corrected, and identifier-policy warnings were left for issue 05. Localized Resource validation, formatting, configurable identifier policies, and build/IDE refresh verification remain in their dependent issues.
