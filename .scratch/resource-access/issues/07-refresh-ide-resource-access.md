# Refresh Resource Access in the supported IDE

Status: ready-for-human
Type: HITL
User stories covered: 51

## Parent

[Resource Access Specification](../spec.md)

## What to build

Integrate external resource changes with the supported IDE so consumer completion or generated API and compilation diagnostics remain current after resource edits, additions, and removals. Deliver the integration together with reproducible IDE evidence; human participation covers observing and reviewing the supported IDE behavior.

The build integration is prior evidence only. It does not prove IDE refresh, and the required IDE behavior cannot be dropped because its mechanism is uncertain.

## Acceptance criteria

- [ ] Identify and record the IDE, relevant extension versions, and consumer setup used for verification. Start with an attributed consumer class and a valid Resource Set.
- [ ] Edit only Reference Resource content to add or change a Resource Key or Placeholder Contract. Completion or the generated API updates with the expected method and parameter changes without a C# edit or manual recompilation.
- [ ] Edit Localized Resource content to introduce and then correct a key or Placeholder Contract mismatch. IDE diagnostics appear and clear without a C# edit or manual recompilation.
- [ ] Add an associated Localized Resource, including an invalid culture outside `ExpectedCultures`, and observe updated discovery and validation diagnostics. Remove and restore an Expected Culture resource and observe the corresponding error appear and clear.
- [ ] Record reproducible steps and observed API and diagnostic evidence under Comments, including any required supported IDE configuration. Restarting the IDE or performing a manual build does not substitute for the specified refresh behavior.
- [ ] Add focused automated integration checks where the IDE integration permits them, retaining the consumer boundary and existing test infrastructure. Human-reviewed IDE evidence remains required; snapshots and successful clean builds alone are insufficient.
- [ ] If any scenario cannot be established, record the unmet acceptance requirement rather than marking this issue complete.

## Blocked by

- [06 - Refresh incremental builds after resource changes](06-refresh-incremental-builds.md)
