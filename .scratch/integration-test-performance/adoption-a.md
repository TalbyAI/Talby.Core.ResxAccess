# Option A adoption evidence

Status: **adopted and verified on 2026-10-06**. Option A retains the measured
two-worker scheduling and the original six sequential consumer histories.

## Source and environment

The measured source and working `HEAD` are both
`71f1ab38f59866b35fa685cc58b031c80a6d09b0`. The baseline and adoption records
select .NET SDK `10.0.401` from both the repository and the temporary consumer
working directory. The machine was Windows `10.0.26200.0`, x64, with 20 logical
processors. The Release configuration used NuGet cache
`C:\Users\iskan\.nuget\packages` and child-consumer directory
`C:\Users\iskan\AppData\Local\Temp\`. Tool versions were Node.js `24.14.1`
and PowerShell `7.6.6`. See the retained
[baseline environment](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/baseline-environment.json)
and [adoption environment](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/adoption-environment.json).
Preflight and baseline preparation commands exited 0: `dotnet tool restore`
([log](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/tool-restore.log)),
`dotnet restore Talby.Core.ResxAccess.slnx`
([log](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/baseline-restore.log)),
and `dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore`
([baseline log](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/baseline-build.log)).
The candidate Release build used the same build command and exited 0
([log](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/task-1-release-build.log)).
The formatter dependencies were already installed, so `npm ci` was not needed.

Task 1 changed these files under `tests/Talby.Core.ResxAccess.IntegrationTests/`:
it added `AssemblyInfo.cs`,
`DesignTimeBuildCollection.cs`, the six wrapper Fact files
`RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests.cs`,
`RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests.cs`,
`RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests.cs`,
`DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests.cs`,
`RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests.cs`, and
`RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests.cs`;
renamed `IncrementalBuildConsumerTests.cs` to
`IncrementalBuildConsumerHistories.cs`; and changed only the collection attribute
in `DesignTimeResourceConsumerTests.cs`. Task 2 changed `README.md`, created this
adoption record, and closed the executed items in the
[implementation plan](implementation-plan-a.md). The detailed Task 2 log is
retained under the ignored adoption evidence directory.

## Identity and assertion preservation

The six wrapper methods retain their original method names and Integration trait.
The exact new-to-old mappings verified against the full TRX inventories are:

| New identity | Original identity |
| --- | --- |
| `Talby.Core.ResxAccess.IntegrationTests.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits` | `Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits` |
| `Talby.Core.ResxAccess.IntegrationTests.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals` | `Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals` |
| `Talby.Core.ResxAccess.IntegrationTests.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration` | `Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration` |
| `Talby.Core.ResxAccess.IntegrationTests.DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests.DetectsAssociatedResourcesExcludedFromSdkEmbedding` | `Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.DetectsAssociatedResourcesExcludedFromSdkEmbedding` |
| `Talby.Core.ResxAccess.IntegrationTests.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics` | `Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics` |
| `Talby.Core.ResxAccess.IntegrationTests.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources` | `Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources` |

All 29 other IntegrationTests identities remain unchanged. The helper body was
reversed to its original declaration form before and after formatting and had
the same SHA-256 as the frozen source:
`dc8f9fc4b7602ae168cf980c70587f72269e6699e3748a36f7eae0c0553bac85`. Reversing
the DesignTime collection attribute also reproduced its frozen file. The eight
protected files `ConsumerProject.cs`, `ConsumerBuildCollection.cs`,
`ConsumerDiagnosticsFixture.cs`, `RawTextConsumerTests.cs`,
`LocalizedResourceConsumerTests.cs`, `IndexedPlaceholderConsumerTests.cs`,
`NamedPlaceholderConsumerTests.cs`, and `ResourceKeyIdentifierConsumerTests.cs`
match their frozen SHA-256 values; those values and checks are in
[`task-1-preservation-after-format.json`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/task-1-preservation-after-format.json).
The original assertions, inputs, mutation order, and diagnostic fixture remain
intact. No diagnostic cases were grouped, so grouped-diagnostic negative
controls were not required.

## Correctness and read-only outputs

The parent ran the commands sequentially against the Release build. All exited
0:

| Command | Exit | Result | Retained log |
| --- | --- | --- |
| `dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore` | 0 | 35 passed, 0 failed, 0 skipped | [`candidate-integration.log`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/candidate-integration.log) |
| `dotnet test tests/Talby.Core.ResxAccess.AspectTests/Talby.Core.ResxAccess.AspectTests.csproj --configuration Release --no-build --no-restore` | 0 | 12 passed, 0 failed, 0 skipped | [`candidate-aspect.log`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/candidate-aspect.log) |
| `pwsh -NoProfile -File tests/run.ps1 -Mode fast` | 0 | 32 passed | [`candidate-fast.log`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/candidate-fast.log) |
| `pwsh -NoProfile -File tests/run.ps1 -Mode full` | 0 | 67 passed | [`candidate-full.log`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/candidate-full.log) |

Fast TRX files are in `test-results/execution/fast-446dd39ca2534f7986d48e9a955e66f6/`:
`Talby.Core.ResxAccess.UnitTests/execution_net10.0_20261006121034.trx` and
`Talby.Core.ResxAccess.AspectTests/execution_net10.0_20261006121050.trx`.
Candidate full TRX files are in
`test-results/execution/full-95055206ade547da9224d4c94468cb75/Talby.Core.ResxAccess/`:
`execution_net10.0_20261006121054.trx` (UnitTests),
`execution_net10.0_20261006121109.trx` (AspectTests), and
`execution_net10.0_20261006121144.trx` (IntegrationTests). The 67 candidate
identities were mapped and compared case-sensitively with the baseline full
inventory; every outcome passed. The exact canonical inventory is retained in
[`canonical-full-inventory.txt`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/canonical-full-inventory.txt),
with the comparison result in
[`inventory-verification.json`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/inventory-verification.json).
The baseline full log is
[`baseline-full.log`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/baseline-full.log)
inside the adoption evidence directory. Its TRX files are under the repository
root's `test-results/execution/full-e599fb32575a4f32ab2833817621c3a2/Talby.Core.ResxAccess/`:
`execution_net10.0_20261006120013.trx` (UnitTests),
`execution_net10.0_20261006120031.trx` (AspectTests), and
`execution_net10.0_20261006120209.trx` (IntegrationTests).

All 46 files under the library's `bin/Release` and `obj` trees retained the same
relative paths, sizes, and UTC modification ticks across correctness runs. The
46-file acceptance set contains 18 existing `obj/Debug` files as well as the
Release outputs and other `obj` files; the earlier 24-file exploration count is
not the acceptance count. See
[`library-verification.json`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/library-verification.json)
and the retained before/after metadata lists.

Final documentation checks also exited 0: `npm run format:check` checked 58
code files and 49 Markdown files with no issues
([log](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/task-2-format-check.log)),
and `git diff --check` reported no whitespace errors
([log](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/task-2-diff-check.log)).

## Reuse of the performance evidence

The completed A measurement is applicable. The 91 captured build/test inputs
match the measured source archive, both root and child SDK selections are
`10.0.401`, and the adoption environment matches the measured Windows x64
machine, 20 logical processors, NuGet cache, and temporary consumer directory.
The helper, six wrappers and assembly/collection declarations, DesignTime
collection, and conservative scheduler were compared with measured A; they match
after normalizing file placement and whitespace, with `MaxParallelThreads = 2`.
All 714 frozen exploration hashes still match. Evidence is in
[`measured-baseline-equivalence.json`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/measured-baseline-equivalence.json),
[`scheduling-equivalence.json`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/scheduling-equivalence.json),
and [`retained-exploration-verification.json`](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/retained-exploration-verification.json).

The retained historical A results reduced median IntegrationTests wall time
from 96.245 s to 53.435 s (44.48%) and median warm build-plus-full time from
97.423 s to 56.755 s (41.74%). These are the original local experiment results,
not fresh adoption timings or CI guarantees. See the
[comparison](comparison.md), [timings CSV](results/timings.csv),
[paired deltas CSV](results/paired-deltas.csv), and [summary CSV](results/summary.csv).
The verified source, SDK, environment, inputs, and scheduling permit reuse, so
no fresh benchmark was run and the conditional A-only procedure and prototype
summary-script change were not applicable.

## Final review

Task 1 and Task 2 passed independent review. The minor SDK metadata observation
was corrected and passed a scoped re-review. The
[final review](../../test-results/option-a-adoption-379a041dd99b4db8a0ed31a6fb49e81a/final-review.md)
approved the complete adoption with no actionable findings. The implementation
remains uncommitted on `feature/docs/integration-test-performance`.
