# Support typed Named and mixed Formatting Placeholders

Status: ready-for-agent
Type: AFK
User stories covered: 19-23, 26-35

## Parent

[Resource Access Specification](../spec.md)

## What to build

Extend generated formatting methods to Named Placeholders with optional supported Argument Types and nullable forms, including Resource Keys mixing Named and Indexed Placeholders. The Reference Resource controls public parameter identities, order, and Argument Types. Selected Translations may alter occurrence order, repetition, and Argument Formats without changing the public API.

## Acceptance criteria

- [ ] Support Named Placeholder syntax with an optional type, alignment, and Argument Format, preserving escaped braces and standard formatting behavior. Untyped Named Placeholders use `object?`.
- [ ] Support `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `DateTimeOffset`, and `Guid`, including applicable nullable forms. Consumer compilation and snapshots verify value-type signatures and compiler-visible reference nullability.
- [ ] Explicit `string` and `string?` remain distinct; nullable value types retain their nullable signatures. Nullable arguments remain required parameters and required placeholder identities. Non-nullable reference annotations add no runtime null checks; null arguments follow standard composite formatting behavior.
- [ ] Distinct Named parameters appear first in order of first appearance in the Reference Resource, followed by Indexed parameters in numeric order. A mixed Reference Resource containing `{name} {2} {0}` produces `name`, `arg0`, and `arg2`, without `arg1`.
- [ ] Translations may reorder or repeat identities, change Argument Formats, and omit explicit type declarations. Explicit types must match the Reference Resource's Argument Type and nullability. Added or omitted Named or Indexed identities produce compilation errors.
- [ ] Invalid Named argument identifiers, collisions between Named and Indexed parameter names, and collisions with `resourceCulture` or `formattingCulture` produce compilation errors. Arguments are never silently renamed.
- [ ] Unsupported type spellings, conflicting declarations of an argument in the Reference Resource, and malformed Named Placeholders receive precise compilation diagnostics without expanding the supported syntax or weakening the Placeholder Contract.
- [ ] Add aspect snapshots and consumer compilation/invocation checks for typed, nullable, untyped, and mixed cases, including localized reordering and formats. Verify unchanged Raw Text alongside Formatted Text and all supported formatting overloads.

## Blocked by

- [03 - Generate Indexed Placeholder formatting methods](03-format-indexed-placeholders.md)
