# Discover and validate complete Localized Resources

Status: ready-for-agent
Type: AFK
User stories covered: 5, 8-14, 47

## Parent

[Resource Access Specification](../spec.md)

## What to build

Expand Raw Text access to a Resource Set by discovering associated Localized Resources only alongside the Reference Resource. Every discovered Localized Resource must be complete and consistent, even when its culture is outside optional `ExpectedCultures`. Successful consumer builds retrieve satellite resources and use standard parent-culture and Reference Resource fallback for requested cultures without their own resource.

Preserve the decision to require complete Localized Resources: runtime fallback never excuses an incomplete resource that is present.

## Acceptance criteria

- [x] Discover only resources with the Reference Resource base name and a culture suffix in its directory. Unrelated resources and resources in other directories do not enter the Resource Set.
- [x] Optional `ExpectedCultures` requires an associated Localized Resource for each listed culture. Invalid culture configuration receives an explicit diagnostic. The list supplements discovery and does not filter validation.
- [x] Every discovered Localized Resource contains exactly the Reference Resource's case-sensitive Resource Keys. Missing, additional, and case-mismatched keys produce compilation errors, including in a culture not listed in `ExpectedCultures`.
- [x] Duplicate Resource Keys and non-text values produce compilation errors in either the Reference Resource or any Localized Resource. Validation includes entries that do not receive generated methods.
- [x] Empty and whitespace-only text is accepted when its Placeholder Contract permits it, and Raw Text preserves it unchanged. Placeholder-specific checks are extended by the formatting issues.
- [x] Invoke the compiled consumer API against actual satellite resources to verify default and explicit Resource Culture, parent-culture fallback, and Reference Resource fallback. Restore ambient cultures after each test.
- [x] Add relevant diagnostic aspect snapshots and consumer integration checks. An incomplete present Localized Resource fails compilation even when runtime fallback could otherwise supply its missing text.

## Blocked by

- [01 - Generate Raw Text methods with standard SDK embedding](01-generate-raw-text-methods.md)

## Comments

- Implemented on 2026-10-03. Localized Resources are discovered alongside the Reference Resource and validated before method introduction. Resource Keys use ordinal comparison; diagnostics report missing and additional keys in ordinal order. All entries are checked before identifier handling.
- Optional `ExpectedCultures` supplements discovery, matches culture names without regard to case, and rejects null entries, empty names, whitespace, and unrecognized cultures with `TRESX005`. Missing required resources also receive `TRESX005`. An omitted or null list imposes no required cultures.
- `TRESX004` reports invalid Localized Resources, including malformed XML, duplicate/non-text entries, and inconsistent keys. Standard SDK satellite metadata is verified so excluded or incorrectly embedded Localized Resources cannot satisfy Expected Cultures while being unavailable at runtime.
- Three diagnostic aspect snapshots cover key consistency, entry validation, and Expected Cultures. Their actual diagnostic output was reviewed. Non-text resources can fail SDK resource generation with `MSB3822` before aspect execution; the deterministic aspect adapter verifies the library's diagnostic independently of that SDK limitation.
- Four consumer integration tests cover compiler diagnostics, discovery boundaries, satellite embedding, and runtime lookup. The compiled fixture includes an exact `es-MX` satellite, `es` parent fallback, `fr` outside Expected Cultures, and Reference Resource fallback; it preserves empty/whitespace text and Raw Text placeholders and restores ambient cultures. Integration classes share an xUnit collection to prevent concurrent builds from overwriting the shared library's outputs.
- Verification: solution restore succeeded; Release solution build succeeded with zero warnings/errors; `pwsh -NoProfile -File tests/run.ps1 -Mode full` passed all 36 tests (18 unit, 10 aspect snapshots, 8 integration) and validated the inventory. Placeholder Contract validation remains in the formatting issues.
- Standards and Spec reviews completed against the starting commit. The Standards review identified stale README inventory totals, which were corrected to 28 fast and 36 full. No remaining Standards findings or Spec findings.
