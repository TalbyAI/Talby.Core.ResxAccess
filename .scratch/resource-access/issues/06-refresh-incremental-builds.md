# Refresh incremental builds after resource changes

Status: resolved
Type: AFK
User stories covered: 49-50

## Parent

[Resource Access Specification](../spec.md)

## What to build

Make resource-only edits, additions, and removals invalidate Resource Access generation and validation during incremental consumer builds. A consumer must see current Raw Text, Formatted Text, generated signatures, discovered cultures, and diagnostics without touching C# sources or cleaning the build.

Verify the smallest workable external-file integration rather than assuming ordinary resource embedding already refreshes aspect execution. The mechanism is open; inability to establish it remains an unmet acceptance requirement.

## Acceptance criteria

- [x] Begin each scenario with a successful real SDK consumer build, mutate only resource files, and rebuild incrementally without cleaning, forcing a rebuild, or editing C# sources.
- [x] Reference and Localized Resource text edits update invoked Raw Text and Formatted Text. Reference Resource Key edits update the generated API and applicable Localized Resource consistency diagnostics.
- [x] Reference Placeholder Contract edits update generated argument identities, order, types, and nullable annotations. Localized Placeholder Contract edits update diagnostics. Correcting invalid content clears stale diagnostics on a later incremental build.
- [x] Adding an associated Localized Resource updates discovery and runtime lookup. New invalid resources fail validation even when their cultures are outside `ExpectedCultures`; unrelated resources remain outside the Resource Set.
- [x] Removing an associated Localized Resource updates discovery and runtime fallback. Removing a required Expected Culture causes a compilation error; adding it back clears the error.
- [x] Changes to an entry omitted by identifier handling still refresh its validation diagnostics.
- [x] Retain runnable consumer-level regression checks for content changes and resource additions/removals using the established fixture boundary. Snapshot or helper tests alone do not establish incremental invalidation.
- [x] Record the verified integration mechanism and reproducible commands in the issue's Comments when resolving it. Report any remaining failure explicitly rather than treating a clean build as completion.

## Blocked by

- [05 - Apply Resource Key identifier policies and collision diagnostics](05-handle-resource-key-identifiers.md)

## Answer

Implemented explicit compiler dependencies for resource content and discovery in
the existing transitive targets. Six consumer-level regressions prove incremental
runtime, API and diagnostic refresh after resource-only changes. All acceptance
criteria are complete; Release build, formatting and the full 66-test inventory
passed. Independent Standards and Spec reviews each returned no findings.

## Comments

- Implementation on 2026-10-05 uses project `.resx` files and the resource map as compiler `AdditionalFiles`. The map records discovered file paths as well as SDK metadata and uses `WriteOnlyWhenDifferent`; content changes invalidate compilation, and path changes invalidate discovery after additions/removals, including associated files excluded from embedding. This uses the existing transitive targets import with no custom task or generated C# input.
- Six real SDK consumer regression tests start from successful builds, mutate only resource files, and rebuild in place. Their fixture excludes Metalama 2026.1.28's unconditional `MetalamaBuild.touch` from compiler inputs to prevent incidental compilation from masking missing dependencies. All six passed, and every test failed against the original targets in dependency-isolated negative controls. An unchanged consumer build preserves its assembly timestamp.
- Reproduce from the repository root:

  ```powershell
  dotnet restore Talby.Core.ResxAccess.slnx
  dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
  dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~IncrementalBuildConsumerTests
  pwsh -NoProfile -File tests/run.ps1 -Mode full
  ```

- The [coverage and verification report](../results/06-incremental-builds.md) records the mechanism, consumer mutations and negative controls. Release restore/build, formatting checks and the full 66-test inventory passed on .NET SDK 10.0.401. Standards review: 0 findings. Spec review: 0 findings. All original 60 test identities and assertions remain; the current inventory is 32 fast / 66 full. Supported IDE refresh remains the separate requirement in [issue 07](07-refresh-ide-resource-access.md).
