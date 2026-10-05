# Support typed Named and mixed Formatting Placeholders

Status: ready-for-agent
Type: AFK
User stories covered: 19-23, 26-35

## Parent

[Resource Access Specification](../spec.md)

## What to build

Extend generated formatting methods to Named Placeholders with optional supported Argument Types and nullable forms, including Resource Keys mixing Named and Indexed Placeholders. The Reference Resource controls public parameter identities, order, and Argument Types. Selected Translations may alter occurrence order, repetition, and Argument Formats without changing the public API.

## Acceptance criteria

- [x] Support Named Placeholder syntax with an optional type, alignment, and Argument Format, preserving escaped braces and standard formatting behavior. Untyped Named Placeholders use `object?`.
- [x] Support `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, and `Guid`, including applicable nullable forms. Consumer compilation and snapshots verify value-type signatures and compiler-visible reference nullability.
- [x] Explicit `string` and `string?` remain distinct; nullable value types retain their nullable signatures. Nullable arguments remain required parameters and required placeholder identities. Non-nullable reference annotations add no runtime null checks; null arguments follow standard composite formatting behavior.
- [x] Distinct Named parameters appear first in order of first appearance in the Reference Resource, followed by Indexed parameters in numeric order. A mixed Reference Resource containing `{name} {2} {0}` produces `name`, `arg0`, and `arg2`, without `arg1`.
- [x] Translations may reorder or repeat identities, change Argument Formats, and omit explicit type declarations. Explicit types must match the Reference Resource's Argument Type and nullability. Added or omitted Named or Indexed identities produce compilation errors.
- [x] Invalid Named argument identifiers, collisions between Named and Indexed parameter names, and collisions with `resourceCulture` or `formattingCulture` produce compilation errors. Arguments are never silently renamed.
- [x] Unsupported type spellings, conflicting declarations of an argument in the Reference Resource, and malformed Named Placeholders receive precise compilation diagnostics without expanding the supported syntax or weakening the Placeholder Contract.
- [x] Add aspect snapshots and consumer compilation/invocation checks for typed, nullable, untyped, and mixed cases, including localized reordering and formats. Verify unchanged Raw Text alongside Formatted Text and all supported formatting overloads.

## Blocked by

- [03 - Generate Indexed Placeholder formatting methods](03-format-indexed-placeholders.md)

## Comments

- Implemented on 2026-10-05 on `feat/named-and-mixed-placeholders`, starting from `650e94d`. The shared Placeholder Contract validates Named and Indexed identities across every discovered Localized Resource, including omitted Resource Keys and cultures outside `ExpectedCultures`.
- Generated methods preserve all supported explicit Argument Types and nullable forms, required parameters, Named-first/Indexed-numeric order and all three culture overloads. Compile-time conversion preserves each Translation's alignment, Argument Formats and escaped braces; Raw Text remains unchanged. Non-nullable reference arguments add no runtime null checks.
- Added one UnitTest, two AspectTests and seven IntegrationTests. Exact snapshots, SDK invocation, reflection and an independent compiler consumer verify signatures, reference nullability, runtime behavior and precise diagnostics. All five temporary diagnostic controls failed as expected when an individual expected error was removed. Existing Raw Text assertions remain; previous absence-of-formatting expectations now include the required Named formatting overloads.
- Solution restore, C# and Markdown formatting checks, and the Release solution build succeeded. Full execution passed all 53 tests (20 UnitTests, 10 AspectTests, 23 IntegrationTests) and validated the inventory. Fast inventory is 30 tests. See the [coverage and verification report](../results/04-named-and-mixed.md).
- Two independent `code-review` axes reviewed the diff from `650e94d`. Standards requested isolated reader validation coverage and suggested unifying the supported-type mapping; both were addressed and re-reviewed with no remaining concerns. Spec reported zero findings. The complete suite was rebuilt and passed after the review corrections.
