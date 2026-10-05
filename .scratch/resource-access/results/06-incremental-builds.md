# Incremental build verification

Issue: [06 - Refresh incremental builds](../issues/06-refresh-incremental-builds.md)

## Integration mechanism

`Talby.Core.ResxAccess.targets` registers project `.resx` files as compiler
`AdditionalFiles`, using the SDK's default output and hidden-folder exclusions.
It also registers the resource map as an `AdditionalFile`. The map retains its
six-field SDK metadata rows and appends standalone discovered paths, which the
existing reader ignores when selecting metadata. These paths include resources
removed from `EmbeddedResource`. `WriteOnlyWhenDifferent` preserves the map's
timestamp for an unchanged inventory; adding or removing a file changes it.

The SDK's `CoreCompile` inputs include `AdditionalFiles`. Resource content edits
therefore invalidate compilation, and map changes invalidate resource discovery
after additions/removals. Metalama then reads the current resources through the
existing aspect and validation helpers. No new package, custom build task,
generated C# input, timestamp forcing, or public API is required.

This deliberately tracks all project resources conservatively. An unrelated
resource may invalidate compilation, but only associated Localized Resources
participate in Resource Set validation. Associated files excluded from embedding
remain validation inputs and receive the existing unsupported-embedding error.

## Consumer boundary and coverage

Each new Fact creates an isolated real SDK `ConsumerProject`, using the existing
Release library outputs and targets import. Every scenario starts with a
successful build and runtime assertions. Later steps change only `.resx` files;
they call ordinary `dotnet build` in the same directory, preserving bin/obj and
all C# sources. Runtime checks invoke the rebuilt assembly in fresh processes.

Metalama 2026.1.28 unconditionally touches `MetalamaBuild.touch` and adds it as a
compiler input. Initial probes without isolation all passed, even for missing
resource dependencies. The incremental test fixture removes only this build
signal from `AdditionalFiles` after `CreateMetalamaTouchFiles`. Its normal
filesystem signal still exists, and aspect execution stays enabled. This
prevents incidental recompilation from masking stale generation or validation.
The text-edit test additionally requires an unchanged build to preserve the
consumer assembly timestamp.

| New IntegrationTest | Retained assertions |
| --- | --- |
| `RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits` | Reference and Localized Resource text edits refresh invoked Raw Text and Named Formatted Text; an unchanged build preserves the assembly timestamp. |
| `RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals` | Adding Spanish translations updates lookup and formatting; adding invalid French content outside `ExpectedCultures = ["en"]` reports the exact `TRESX004`; correction succeeds; removing Spanish restores Reference Resource fallback. |
| `RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration` | Removing required Spanish content reports the exact `TRESX005`; restoring it clears the error and invokes the new translation. |
| `DetectsAssociatedResourcesExcludedFromSdkEmbedding` | An associated file excluded from SDK embedding still refreshes discovery, contract validation, and embedding diagnostics; deleting it clears the error. |
| `RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics` | Reference contract edits change Named and Indexed argument identities/order, types and nullable annotations; reflection and invocation verify the public API; Localized contract/nullability errors appear and clear; key renaming changes both Raw Text and Format method names and key-consistency diagnostics. |
| `RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources` | Unrelated resources stay outside validation; Localized and Reference edits to an omitted key report exact `TRESX004`/`TRESX001` messages; corrections clear each error. |

All prior 60 test identities and assertions remain unchanged. Six independent
IntegrationTests extend the inventory to 34 integration / 32 fast / 66 full.
These are sequential mutation scenarios, without shared diagnostic compilations
or reduced failure granularity for existing tests.

## Negative controls and validation

Before adding the dependency registration, all four initial isolated regressions
failed: adding a Localized Resource and editing its text left stale Named format
mapping at runtime; removing an Expected Culture and adding an excluded
associated file incorrectly returned successful builds. The same four passed
after registration. The complete six-test class passed with exact diagnostic
messages and public API assertions.

Temporarily restoring the original targets also made the final two tests fail:
Localized nullability and omitted-key edits incorrectly compiled successfully.
Both controls exited with assertion failures, and the targets were restored.
Control logs and TRX results are under ignored `test-results/issue06/`.

Final verification on 2026-10-05 used .NET SDK 10.0.401, Metalama 2026.1.28 and
Release outputs. Solution restore, solution build and `npm run format:check`
passed. `tests/run.ps1 -Mode full` passed all 66 tests and verified the exact
inventory: 20 UnitTests, 12 AspectTests and 34 IntegrationTests. Full-run TRX
results are under ignored
`test-results/execution/full-feaf398a3be3433da339b7d720097343/`.

## Standards review

No findings. The review checked repository standards, consumer test placement,
diagnostic assertions, issue/report conventions and the updated test inventory.
No material code-smell concerns were identified.

## Spec review

No findings. The targets and six consumer tests cover issue 06's content edits,
API and diagnostic refresh, discovery and fallback, Expected Cultures, excluded
associated files, omitted keys and unrelated resources. IDE verification remains
in issue 07. Total findings: Standards 0; Spec 0.

## Reproduce

Run from the repository root with the SDK selected by `global.json`:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~IncrementalBuildConsumerTests
npm run format:check
pwsh -NoProfile -File tests/run.ps1 -Mode full
```

The test creates its consumer and performs all resource-only edits, additions,
removals and ordinary incremental builds automatically. `ConsumerProject.Build`
reuses the prebuilt library (`BuildProjectReferences=false`) and restores only
the consumer (`RestoreRecursive=false`); it never requests Clean or Rebuild.

Supported IDE refresh is a separate unmet requirement tracked by
[issue 07](../issues/07-refresh-ide-resource-access.md). This build evidence makes
no claim about automatic completion or diagnostic refresh in an IDE.
