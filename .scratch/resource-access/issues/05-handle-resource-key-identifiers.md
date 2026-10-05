# Apply Resource Key identifier policies and collision diagnostics

Status: resolved
Type: AFK
User stories covered: 36-42

## Parent

[Resource Access Specification](../spec.md)

## What to build

Expose the specified `InvalidKeyHandling` policies so consumers can omit or normalize Resource Keys that cannot directly become C# methods. Preserve valid identifiers, escape keywords, assign normalized names reproducibly, and diagnose incompatible target-class or generated member-family collisions. Identifier handling never bypasses Resource Set validation.

## Acceptance criteria

- [x] Default `Warn` omits methods for invalid Resource Keys and emits identifier warnings. Explicit `Ignore` omits the same methods without identifier warnings. Both modes continue to report Resource Set and Placeholder Contract errors for omitted entries.
- [x] `Normalize` replaces invalid identifier characters with underscores and preserves valid Resource Key identifiers. C# keywords are escaped and usable from consumer code.
- [x] Reserve all valid Resource Key identifiers before processing keys requiring normalization in ordinal order. Resolve normalization collisions with `_2`, `_3`, and subsequent suffixes.
- [x] Reordering resource entries produces the same generated names. Include a collision with an already valid identifier to prove reservation takes precedence over normalization.
- [x] Raw Text and formatting methods use the chosen Resource Key identifier while still looking up the original Resource Key and preserving its Translation.
- [x] Collisions with existing target-class members or between generated Raw Text and formatting member families produce compilation errors. Normalization does not authorize silently renaming conflicting member families.
- [x] Aspect snapshots include warnings where relevant so default Warn and explicit Ignore remain distinguishable. Consumer compilation and invocation verify normalized and escaped member names, omitted members, original-key lookup, and collision diagnostics.

## Blocked by

- [04 - Support typed Named and mixed Formatting Placeholders](04-format-named-and-mixed-placeholders.md)

## Answer

Implemented `InvalidKeyHandling.Warn`, `Ignore` and `Normalize`, preserving complete Resource Set and Placeholder Contract validation. Normalization reserves valid identifiers, assigns suffixes in ordinal order and retains original-key lookup. `TRESX006` reports omitted identifiers, `TRESX007` rejects member collisions, and `TRESX008` rejects unsupported policy values.

All acceptance criteria are complete. The Release build, formatting checks and all 60 tests passed, including the exact test inventory. See the [coverage and verification report](../results/05-resource-key-identifiers.md).

## Comments

- Implemented on 2026-10-05 on `feat/resource-key-identifiers`, starting from `0646dc0`. Added two AspectTests and five IntegrationTests; every existing test identity and assertion remains. The current inventory is 32 fast / 60 full.
- Ten temporary negative controls confirmed that each required diagnostic assertion fails when its invalid input is corrected, with other errors retained. All controls were restored before the final Release build and full execution.
- Independent Standards and Spec reviews each identified one issue: a stale documentation count and unsupported enum values silently selecting Ignore. Both were corrected and re-reviewed; neither axis has remaining findings.
