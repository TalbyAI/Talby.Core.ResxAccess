# Document the Resource Access API and supported integration

Status: resolved
Type: AFK
User stories covered: 52

## Parent

[Resource Access Specification](../spec.md)

## What to build

Publish English consumer documentation showing how to adopt the generated Resource Access API and predict its validation, culture selection, formatting, and identifier behavior. Describe the embedding configurations and build/IDE integration actually verified by the preceding issues. Use the Resource Access glossary and preserve both existing domain decisions.

## Acceptance criteria

- [x] Show attribute usage on a consumer-declared non-generic static class without `partial`, a project-relative culture-neutral Reference Resource, optional `ExpectedCultures`, and `InvalidKeyHandling`.
- [x] Explain same-directory Localized Resource discovery, complete case-sensitive Resource Keys, text-only values, and validation of every discovered culture and omitted Resource Key. Distinguish standard runtime fallback from validation of present resources.
- [x] Show Raw Text overloads and the three formatting overloads. Explain independent default and explicit Resource Culture and Formatting Culture, fallback, and runtime failure behavior.
- [x] Explain Indexed and Named Placeholder syntax, supported Argument Types and nullable forms, required nullable arguments, alignment, formats, escaped braces, and Translation compatibility rules.
- [x] Show mixed-placeholder ordering with index gaps and recommend a single placeholder style per Resource Key while explaining that mixed styles remain supported.
- [x] Explain Warn, Ignore, Normalize, keyword escaping, deterministic collision suffixes, and parameter/member collision diagnostics.
- [x] Document supported standard SDK embedding and diagnosed custom configurations, plus verified resource-only incremental build behavior with reproducible setup information. Describe the recorded IDE limitations and deferred requirements without claiming verified automatic IDE refresh or non-partial IDE support.
- [x] Validate representative documentation examples against the implemented consumer API using the existing integration boundary. Check documentation links and avoid unsupported claims or incidental private implementation details.
- [x] Update setup documentation when the implementation changes prerequisites or commands, including the snapshot project in solution-level checks. Write all added or updated repository documentation in English.

## Blocked by

- [07 - Refresh Resource Access in the supported IDE](07-refresh-ide-resource-access.md)

## Answer

Published the English [consumer guide](../../../docs/resource-access.md), linked
from the README, with runnable examples and the implemented API contract. The
guide preserves both ADRs and documents verified SDK/incremental build behavior
separately from the recorded, deferred IDE requirements. One IntegrationTest
compiles and invokes the exact guide examples through the existing consumer
boundary. All 68 tests, Release build and formatting checks passed; see the
[coverage and verification report](../results/08-documentation.md).

## Comments

- 2026-10-05: The user resolved issue 07 by explicitly deferring its unmet technical requirements. This dependency no longer blocks documentation work. Document the existing IDE evidence and limitations; successful IDE verification is deferred and is not a prerequisite for resolving this documentation issue.

- 2026-10-05: Implemented all documentation criteria. The focused documentation test checks 15 runtime outputs from the exact C# and resource snippets; all previous test identities and assertions remain, and full verification passed 20 UnitTests, 12 AspectTests and 36 IntegrationTests. Repository setup retains the snapshot project in solution-level checks. Local documentation links and anchors were checked. No additional IDE support or refresh verification is claimed.
- 2026-10-05: Independent code-review axes checked the staged changes against task-start commit `a6e4bb6587c31e6ae818261c082f911c729c309d`. Standards: 0 findings; Spec: 0 findings. Both reviews confirmed preservation of the ADRs and the documented IDE deferral.
