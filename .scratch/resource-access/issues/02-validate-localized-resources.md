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

- [ ] Discover only resources with the Reference Resource base name and a culture suffix in its directory. Unrelated resources and resources in other directories do not enter the Resource Set.
- [ ] Optional `ExpectedCultures` requires an associated Localized Resource for each listed culture. Invalid culture configuration receives an explicit diagnostic. The list supplements discovery and does not filter validation.
- [ ] Every discovered Localized Resource contains exactly the Reference Resource's case-sensitive Resource Keys. Missing, additional, and case-mismatched keys produce compilation errors, including in a culture not listed in `ExpectedCultures`.
- [ ] Duplicate Resource Keys and non-text values produce compilation errors in either the Reference Resource or any Localized Resource. Validation includes entries that do not receive generated methods.
- [ ] Empty and whitespace-only text is accepted when its Placeholder Contract permits it, and Raw Text preserves it unchanged. Placeholder-specific checks are extended by the formatting issues.
- [ ] Invoke the compiled consumer API against actual satellite resources to verify default and explicit Resource Culture, parent-culture fallback, and Reference Resource fallback. Restore ambient cultures after each test.
- [ ] Add relevant diagnostic aspect snapshots and consumer integration checks. An incomplete present Localized Resource fails compilation even when runtime fallback could otherwise supply its missing text.

## Blocked by

- [01 - Generate Raw Text methods with standard SDK embedding](01-generate-raw-text-methods.md)
