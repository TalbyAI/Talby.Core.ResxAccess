# Refresh incremental builds after resource changes

Status: ready-for-agent
Type: AFK
User stories covered: 49-50

## Parent

[Resource Access Specification](../spec.md)

## What to build

Make resource-only edits, additions, and removals invalidate Resource Access generation and validation during incremental consumer builds. A consumer must see current Raw Text, Formatted Text, generated signatures, discovered cultures, and diagnostics without touching C# sources or cleaning the build.

Verify the smallest workable external-file integration rather than assuming ordinary resource embedding already refreshes aspect execution. The mechanism is open; inability to establish it remains an unmet acceptance requirement.

## Acceptance criteria

- [ ] Begin each scenario with a successful real SDK consumer build, mutate only resource files, and rebuild incrementally without cleaning, forcing a rebuild, or editing C# sources.
- [ ] Reference and Localized Resource text edits update invoked Raw Text and Formatted Text. Reference Resource Key edits update the generated API and applicable Localized Resource consistency diagnostics.
- [ ] Reference Placeholder Contract edits update generated argument identities, order, types, and nullable annotations. Localized Placeholder Contract edits update diagnostics. Correcting invalid content clears stale diagnostics on a later incremental build.
- [ ] Adding an associated Localized Resource updates discovery and runtime lookup. New invalid resources fail validation even when their cultures are outside `ExpectedCultures`; unrelated resources remain outside the Resource Set.
- [ ] Removing an associated Localized Resource updates discovery and runtime fallback. Removing a required Expected Culture causes a compilation error; adding it back clears the error.
- [ ] Changes to an entry omitted by identifier handling still refresh its validation diagnostics.
- [ ] Retain runnable consumer-level regression checks for content changes and resource additions/removals using the established fixture boundary. Snapshot or helper tests alone do not establish incremental invalidation.
- [ ] Record the verified integration mechanism and reproducible commands in the issue's Comments when resolving it. Report any remaining failure explicitly rather than treating a clean build as completion.

## Blocked by

- [05 - Apply Resource Key identifier policies and collision diagnostics](05-handle-resource-key-identifiers.md)
