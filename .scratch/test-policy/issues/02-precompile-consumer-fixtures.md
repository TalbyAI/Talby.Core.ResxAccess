# Precompile consumer fixtures for generated runtime behavior

Status: ready-for-agent
Type: HITL
Previous option: 3

## Parent

[Test execution policy experiments](../spec.md)

## What to build

Build a minimal real SDK consumer fixture as part of the solution build, and exercise its generated assembly from the ordinary test project. Replace the temporary builds for successful generation/lookup and the two runtime failure scenarios with this precompiled fixture. Keep the grouped invalid-consumer build and malformed XML build temporary because their expected outcome is a failed compilation.

Reuse the current Resource Sets, distinct Translations, associated-type naming case, and runtime assertions. Prefer one fixture project if resource-specific sabotage can isolate the missing-manifest and missing-Resource-Key cases without altering the positive Resource Sets. Process aspects in the fixture, not in the ordinary test project. Do not add manual assembly caching or a fixture framework.

## Acceptance criteria

- [ ] A normal Release solution build generates the consumer API and resources. Tests use that build's output without starting a temporary SDK build for the migrated scenarios.
- [ ] Retain positive assertions for independent Resource Sets, class identity/accessibility, overloads, invalid identifiers, SDK manifest naming, culture selection/fallback, null culture, keyword/Unicode Resource Keys, and empty/whitespace/placeholder-preserving Raw Text.
- [ ] Retain both runtime failure assertions, including Resource Key, manifest base name, Resource Culture, and the missing-manifest inner exception. Restrict sabotage to the intended resources so positive scenarios still verify real embedding.
- [ ] Keep all five target-specific diagnostic assertions and the malformed XML SDK error/no-aspect-crash assertion. The expected temporary SDK build count is five to two per full suite; explain any different result.
- [ ] Preserve culture restoration and scenario isolation. Avoid dependence on test execution order or stale assemblies.
- [ ] Verify a clean build. Then temporarily change a fixture Translation, rebuild without changing C# source, and show that the corresponding test detects the changed resource. Temporarily remove a required runtime failure condition and show its assertion fails. Revert both controls before final verification.
- [ ] Follow the parent measurement protocol. Report test-only gains and any build cost increase separately, plus the warm and clean-output build + test totals. Explicitly identify cost moved from test execution into compilation.
- [ ] Publish `results/02-fixtures.md` with the old-to-new assertion mapping, changed fresh-project isolation, measurements, commands, diff summary, and recommendation. Present this change set for user review before adoption.

## Blocked by

No technical dependency on issue 01. Evaluate after its review where practical, starting from the latest approved baseline. Do not include unapproved execution-cadence changes in this change set.

## Comments

Human review is required because a precompiled fixture no longer rebuilds a fresh consumer for each runtime scenario. A lower `dotnet test --no-build` time is insufficient evidence of a faster complete cycle.
