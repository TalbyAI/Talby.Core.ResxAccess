# Apply Resource Key identifier policies and collision diagnostics

Status: ready-for-agent
Type: AFK
User stories covered: 36-42

## Parent

[Resource Access Specification](../spec.md)

## What to build

Expose the specified `InvalidKeyHandling` policies so consumers can omit or normalize Resource Keys that cannot directly become C# methods. Preserve valid identifiers, escape keywords, assign normalized names reproducibly, and diagnose incompatible target-class or generated member-family collisions. Identifier handling never bypasses Resource Set validation.

## Acceptance criteria

- [ ] Default `Warn` omits methods for invalid Resource Keys and emits identifier warnings. Explicit `Ignore` omits the same methods without identifier warnings. Both modes continue to report Resource Set and Placeholder Contract errors for omitted entries.
- [ ] `Normalize` replaces invalid identifier characters with underscores and preserves valid Resource Key identifiers. C# keywords are escaped and usable from consumer code.
- [ ] Reserve all valid Resource Key identifiers before processing keys requiring normalization in ordinal order. Resolve normalization collisions with `_2`, `_3`, and subsequent suffixes.
- [ ] Reordering resource entries produces the same generated names. Include a collision with an already valid identifier to prove reservation takes precedence over normalization.
- [ ] Raw Text and formatting methods use the chosen Resource Key identifier while still looking up the original Resource Key and preserving its Translation.
- [ ] Collisions with existing target-class members or between generated Raw Text and formatting member families produce compilation errors. Normalization does not authorize silently renaming conflicting member families.
- [ ] Aspect snapshots include warnings where relevant so default Warn and explicit Ignore remain distinguishable. Consumer compilation and invocation verify normalized and escaped member names, omitted members, original-key lookup, and collision diagnostics.

## Blocked by

- [04 - Support typed Named and mixed Formatting Placeholders](04-format-named-and-mixed-placeholders.md)
