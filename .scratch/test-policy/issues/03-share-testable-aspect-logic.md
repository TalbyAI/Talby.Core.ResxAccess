# Share production aspect logic with fast tests

Status: ready-for-agent
Type: HITL
Previous option: 4

## Parent

[Test execution policy experiments](../spec.md)

## What to build

Separate SDK project/resource-map acquisition from the existing validation and member introduction logic, using the smallest internal compile-time helpers and concrete inputs needed. The production attribute must call the same implementation exercised by tests. Preserve its public API and unavailable-project-context failure; do not add a production fallback solely to accommodate AspectTests.

Use ordinary unit tests for resource reading, metadata/path decisions, XML/text-entry validation, and Resource Key identifier decisions. Use AspectTests for diagnostic mapping and generated API/templates, supplying deterministic resource inputs through a minimal test aspect that delegates to the shared production implementation. Verify snapshots against observable generated behavior rather than incidental helper layout. Keep real SDK consumer smoke coverage for wiring, embedding, manifest naming, resource lookup, and SDK failures.

Use the approved consumer fixtures from issue 02 if available; otherwise retain the existing consumer tests. No new feature behavior, public testing API, custom Metalama runner, dependency-injection framework, or duplicated generation algorithm is part of this experiment.

## Acceptance criteria

- [ ] The real attribute and fast test adapter execute shared production validation/generation code. Identify precisely which SDK acquisition steps the adapter bypasses and how integration smoke tests cover them.
- [ ] Retain existing InvalidPaths, UnsupportedTargets, and UnavailableProjectContext assertions. Add successful AspectTests checking the generated Raw Text overloads, ResourceManager manifest name, and keyword/Unicode/invalid Resource Key behavior using supported facilities in the installed Metalama version.
- [ ] Add focused unit checks for existing validation branches, including invalid XML/root, duplicate or unnamed Resource Keys, invalid value/type structure, missing files, culture-specific Reference Resources, and unsupported resource-map metadata. Keep SDK failure behavior distinct from helper validation behavior.
- [ ] Retain the real entry-point smoke checks: successful SDK map acquisition and embedding/associated-type naming, runtime culture/fallback and error behavior, all five invalid-consumer diagnostics, and malformed XML rejection without an aspect crash. Remove a slow scenario only if its full assertion mapping demonstrates equivalent boundary coverage and the removal is explicitly highlighted for review.
- [ ] As temporary negative controls, introduce a regression in a shared validation decision and another in generated member behavior. Demonstrate the corresponding unit/AspectTests failures. Also break SDK resource-map acquisition and demonstrate that a real consumer check fails even if deterministic-input AspectTests pass. Revert all controls before final verification.
- [ ] Follow the parent measurement protocol for fast, full test-only, and full build + test paths. Report new scenario coverage and SDK build counts separately; do not interpret faster execution of a subset as equivalent full coverage or promise elimination of the remaining SDK builds.
- [ ] Publish `results/03-aspect-logic.md` with the assertion mapping, adapter limitations, production diff risks, measurements, commands, and recommendation. Present this change set for user review before adoption.

## Blocked by

No mandatory technical dependency. Prefer evaluation after issue 02's review to reuse approved fixtures, but start from the retained baseline if that experiment is rejected. Do not combine either earlier candidate with this change set before its approval.

## Comments

Human review is required because this experiment changes production structure and divides regression protection between deterministic-input tests and SDK smoke tests. The final policy for unit tests, AspectTests, or both remains a separate decision after evidence from the three experiments is reviewed.
