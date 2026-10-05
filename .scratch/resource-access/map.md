# Resource Access map

## Notes

The [Resource Access Specification](spec.md) defines the feature. This map records the resolution of tickets 04 and 07; other ticket statuses remain in their individual files.

## Decisions-so-far

- [04 - Support typed Named and mixed Formatting Placeholders](issues/04-format-named-and-mixed-placeholders.md) is resolved. The Reference Resource defines required argument identities, order, types and nullability, and Localized Resources preserve that Placeholder Contract. Generated formatting methods support typed Named and mixed arguments, unchanged Raw Text access and all three culture overloads. The Release build and all 53 tests passed; see the [coverage and verification report](results/04-named-and-mixed.md).
- [07 - Refresh Resource Access in the supported IDE](issues/07-refresh-ide-resource-access.md) is resolved by the user's explicit decision on 2026-10-05 to defer its unmet technical requirements. The [evidence report](results/07-ide-refresh.md) retains the failures and unverified scenarios. This resolution permits issue 08 to proceed and does not establish IDE support or approve the review's proposed implementation changes.

## Fog

Remaining work is tracked in the [implementation tickets](issues/). Issue 07's unmet technical requirements are deferred, not verified: automatic IDE refresh, non-partial IDE support, C# Dev Kit compatibility, editor-output coverage, and human verification. The Supported IDE policy and approval of the proposed implementation changes remain undecided. A follow-up issue is needed when this work resumes.
