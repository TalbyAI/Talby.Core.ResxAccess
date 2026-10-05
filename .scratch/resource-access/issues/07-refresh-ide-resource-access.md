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

- [x] Identify and record the IDE, relevant extension versions, and consumer setup used for verification. Start with an attributed consumer class and a valid Resource Set.
- [ ] Edit only Reference Resource content to add or change a Resource Key or Placeholder Contract. Completion or the generated API updates with the expected method and parameter changes without a C# edit or manual recompilation.
- [ ] Edit Localized Resource content to introduce and then correct a key or Placeholder Contract mismatch. IDE diagnostics appear and clear without a C# edit or manual recompilation.
- [ ] Add an associated Localized Resource, including an invalid culture outside `ExpectedCultures`, and observe updated discovery and validation diagnostics. Remove and restore an Expected Culture resource and observe the corresponding error appear and clear.
- [ ] Record reproducible steps and observed API and diagnostic evidence under Comments, including any required supported IDE configuration. Restarting the IDE or performing a manual build does not substitute for the specified refresh behavior.
- [ ] Add focused automated integration checks where the IDE integration permits them, retaining the consumer boundary and existing test infrastructure. Human-reviewed IDE evidence remains required; snapshots and successful clean builds alone are insufficient.
- [ ] If any scenario cannot be established, record the unmet acceptance requirement rather than marking this issue complete.

## Blocked by

- [06 - Refresh incremental builds after resource changes](06-refresh-incremental-builds.md)

## Comments

- 2026-10-05: The user normally uses Visual Studio Code for source review and implements through AI agents. Verification therefore targets VS Code 1.140.0, C# 11.1.32, C# Dev Kit 11.1.3 and .NET Install Tool 3.2.0, with Metalama 2026.1.28 and SDK 10.0.401. The independent SDK consumer has an attributed static `Texts` class, a Reference Resource and an `es` Localized Resource required by `ExpectedCultures`.
- Implemented complete SDK resource-name preparation for design-time builds, content-sensitive design-time inputs, and a generated C# dependency read by the aspect before validation. A focused consumer integration regression checks SDK metadata, stable unchanged inputs, content edits with preserved timestamps, discovery additions outside `ExpectedCultures`, and Expected Culture removal/restoration. These checks establish design-time compiler inputs, not automatic IDE scheduling or refreshed editor results.
- Live extension-host probes found an installed C# Dev Kit project-loading failure involving `AddAdditionalFilesAsync` deserialization. With C# Dev Kit disabled, the C# language server loaded the consumer. A non-partial target received `LAMA0048`; adding `partial` during initial setup exposed `string Texts.FormatWelcome(string name)` in hover. A subsequent resource-only Placeholder Contract edit left that signature stale and showed no mismatch diagnostic during the 90-second observation window. The C# consumer was unchanged and no build occurred during that acceptance observation.
- Automatic API refresh is unmet. Localized mismatch appearance/clearance and Localized Resource addition/removal/restoration remain unverified in the IDE; human observation and review remain outstanding. The issue stays `ready-for-human` and is not resolved. The [evidence and reproduction guide](../results/07-ide-refresh.md) records the failures, automated coverage and the remaining scenarios. A manually requested design-time build is a separate diagnostic control and cannot satisfy acceptance.
- Release solution build, formatting checks and all 67 tests passed. Independent Standards review found no breaches or baseline smells; Spec review identified the recorded automatic-refresh, non-partial IDE support and editor-output coverage gaps, with no scope creep or separate code defect. The `partial` workaround is diagnostic setup, not approval to narrow the parent specification.
