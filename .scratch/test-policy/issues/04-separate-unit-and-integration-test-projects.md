# Separate unit and integration test projects

Status: ready-for-agent
Type: task
Previous option: Additional task

## Parent

[Test execution policy experiments](../spec.md)

## What to build

Create `tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj`.
Rename the existing `Talby.Core.ResxAccess.Tests` directory and project to
`tests/Talby.Core.ResxAccess.UnitTests/Talby.Core.ResxAccess.UnitTests.csproj`.
Use matching namespaces and default assembly names for both projects.

Move `RawTextConsumerTests` and the internal `ConsumerProject` helper into
IntegrationTests. Keep `MetalamaSetupTests` in UnitTests and preserve the separate
`Talby.Core.ResxAccess.AspectTests` project. Use the existing test SDK, xUnit,
and Metalama package versions; place dependencies with the tests that need them.
Both ordinary projects must reference the library and keep `MetalamaEnabled=false`.

Update solution entries, references, commands, and active documentation to reflect
the split. If the fast/full entry points from experiment 01 have been adopted,
update their project selection and passed-test inventory for the new namespaces.
Preserve the historical experiment reports and measured identities as evidence
of their recorded baseline.

## Acceptance criteria

- [ ] Create IntegrationTests and rename the existing Tests project to UnitTests, including directories, `.csproj` filenames, namespaces, and assembly identities. Remove obsolete solution entries and active references to the old project path.
- [ ] Keep `MetalamaSetupTests` in UnitTests. Move all four `RawTextConsumerTests` and `ConsumerProject` into IntegrationTests without changing their scenarios, assertions, culture restoration, temporary-project isolation, or failure reporting.
- [ ] Preserve the dedicated AspectTests project and its snapshots. Retain .NET 10, nullable reference types, implicit usings, `MetalamaEnabled=false` in both ordinary projects, and `MetalamaRemoveCompileTimeOnlyCode=false` in the library.
- [ ] Update `Talby.Core.ResxAccess.slnx`, required project references, README, and repository test instructions. A standard solution-level Release test command must continue to select the complete suite.
- [ ] Verify UnitTests and IntegrationTests independently. Map old and new discovered identities, accounting for namespace changes; preserve all eight current tests, including the three AspectTests, if implemented against the current baseline. Account explicitly for any tests added by previously approved work.
- [ ] If the fast/full runner is adopted, fast selects UnitTests plus AspectTests and excludes IntegrationTests; full selects every test. Update the inventory and verify that missing or unexpected selections still fail.
- [ ] Run restore, a clean Release solution build, and the full suite successfully. Record the commands, results, and assertion mapping for review. A project split alone does not establish a performance improvement or authorize dropping integration coverage.

## Blocked by

No mandatory technical dependency on experiments 02 or 03. Start from the latest
approved baseline; do not build on an unapproved experiment. Implementation
requires a separate explicit request.

## Comments

2026-10-03: Requested as an additional task in the spec within the current pull
request. This change records the project split for later implementation; it does
not rename or create test projects in the current candidate.
