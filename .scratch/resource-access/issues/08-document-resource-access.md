# Document the Resource Access API and supported integration

Status: ready-for-agent
Type: AFK
User stories covered: 52

## Parent

[Resource Access Specification](../spec.md)

## What to build

Publish English consumer documentation showing how to adopt the generated Resource Access API and predict its validation, culture selection, formatting, and identifier behavior. Describe the embedding configurations and build/IDE integration actually verified by the preceding issues. Use the Resource Access glossary and preserve both existing domain decisions.

## Acceptance criteria

- [ ] Show attribute usage on a consumer-declared non-generic static class without `partial`, a project-relative culture-neutral Reference Resource, optional `ExpectedCultures`, and `InvalidKeyHandling`.
- [ ] Explain same-directory Localized Resource discovery, complete case-sensitive Resource Keys, text-only values, and validation of every discovered culture and omitted Resource Key. Distinguish standard runtime fallback from validation of present resources.
- [ ] Show Raw Text overloads and the three formatting overloads. Explain independent default and explicit Resource Culture and Formatting Culture, fallback, and runtime failure behavior.
- [ ] Explain Indexed and Named Placeholder syntax, supported Argument Types and nullable forms, required nullable arguments, alignment, formats, escaped braces, and Translation compatibility rules.
- [ ] Show mixed-placeholder ordering with index gaps and recommend a single placeholder style per Resource Key while explaining that mixed styles remain supported.
- [ ] Explain Warn, Ignore, Normalize, keyword escaping, deterministic collision suffixes, and parameter/member collision diagnostics.
- [ ] Document supported standard SDK embedding and diagnosed custom configurations, plus verified resource-only incremental build and IDE refresh behavior with reproducible setup information.
- [ ] Validate representative documentation examples against the implemented consumer API using the existing integration boundary. Check documentation links and avoid unsupported claims or incidental private implementation details.
- [ ] Update setup documentation when the implementation changes prerequisites or commands, including the snapshot project in solution-level checks. Write all added or updated repository documentation in English.

## Blocked by

- [07 - Refresh Resource Access in the supported IDE](07-refresh-ide-resource-access.md)
