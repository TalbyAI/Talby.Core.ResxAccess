# Option A bounded concurrency Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Adopt Option A's verified scheduling so two independent consumer histories can run concurrently while preserving all scenarios, assertions and mutation order.

**Architecture:** Retain the six histories in an internal static helper and expose each through a thin Fact class in a distinct collection. Give DesignTime its own collection, preserve the diagnostic collection and fixture, and cap IntegrationTests collection workers at two. This matches the measured prototype; separate wrapper source files and a helper filename matching its type are organizational changes only.

**Tech Stack:** .NET 10, SDK selected by `global.json` (measured: 10.0.401), xUnit 2.9.3, Metalama.Framework 2026.1.28, PowerShell 7, CSharpier and markdownlint-cli2.

Prepared on 2026-10-06 for implementation in another session. This session creates
the plan only. Start code changes when the user requests execution of this plan.
Commits, pull requests and merging are not part of this handoff.

## Global Constraints

- Always write repository documents in English, regardless of the language used in conversations with the user.
- Use English for all domain terms in documentation, code identifiers, and conversations, even when the surrounding conversation uses another language.
- Keep `MetalamaEnabled=false` in UnitTests and IntegrationTests and `MetalamaRemoveCompileTimeOnlyCode=false` in the library.
- Keep the ConsumerFixture reference in IntegrationTests.
- Preserve every scenario, assertion and required resource mutation sequence.
- Changed test identities require an explicit old-to-new coverage map.
- A significant improvement means at least 30% lower median IntegrationTests external wall time, relative to a controlled baseline of the current source.
- Verify build plus full execution as well as test-only execution. Reject reproducible full-cycle regressions or concealed work shifted into builds.
- Keep the selected bound of two xUnit collection workers and the conservative scheduler.
- Keep the five diagnostic classes in `SDK consumer builds` and its single `ConsumerDiagnosticsFixture`. Its lazy two-build cache runs once per testhost, not once per history.
- Keep each history sequential, consumer directories isolated and library reuse read-only.
- Apply A only. Leave `ConsumerProject.cs` unchanged: no restore-success marker, graph fingerprint, subsequent-build `--no-restore` adoption, shared MSBuild process or permanent tracing.
- Do not group diagnostics, delete tests, move scenarios to ConsumerFixture, modify production behavior or patch installed packages.
- Do not change `tests/run.ps1`, `$fastProjects`, solution membership, package versions or target frameworks.
- Future prototypes must live under the top-level `prototypes/` directory. Keep frozen source, logs, TRX and outputs under ignored `test-results/`.
- Preserve pending changes, partial staging and completed exploration artifacts. Follow `AGENTS.md` branch rules if commits are separately authorized.

The bound applies to collection workers and independent histories, not all SDK
processes. The fixture can run two builds inside one worker while another history
runs, giving a theoretical peak of three child builds. The exploration observed
two. Do not duplicate the fixture or add a new global SDK semaphore.

## Evidence and starting point

Read before editing:

- [Testing policy](../../docs/agents/testing.md).
- [Completed comparison and recommendation](comparison.md).
- [Coverage map](coverage-map.md).
- [Independent final review](final-review.md).
- [Variant preparation](variant-preparation.md) and [variant review](variant-review.md).
- `prototypes/integration-test-performance/New-Variants.ps1`, especially `Apply-OptionA`.

Measured source: `71f1ab38f59866b35fa685cc58b031c80a6d09b0`.
A reduced IntegrationTests median from 96.245 s to 53.435 s (44.48%),
full from 94.925 s to 54.650 s (42.43%), and warm build plus full from
97.423 s to 56.755 s (41.74%). All five warm-cycle pairs improved.
The clean cycle was 98.224 s versus 53.641 s.

Inventories at that source: 35 IntegrationTests, 12 AspectTests, 32 fast and
67 full. These are evidence counts, not runner constants. Six identities change
exactly as listed in the coverage map; the other 29 IntegrationTests identities
remain unchanged.

Raw prototype A is at `test-results/integration-exploration-c891a32c/A/` if retained.
Do not copy its entire tree: ConsumerProject contains common experiment-only
instrumentation. Port only A's declarations, collections and assembly bound.

## File map

| Action | Path under `tests/Talby.Core.ResxAccess.IntegrationTests/` | Responsibility |
| --- | --- | --- |
| Rename/modify | `IncrementalBuildConsumerTests.cs` → `IncrementalBuildConsumerHistories.cs` | Retain six history bodies, resources and helpers; change declarations only. |
| Create | `AssemblyInfo.cs` | Two-worker assembly limit. |
| Create | `DesignTimeBuildCollection.cs` | DesignTime collection without a fixture. |
| Modify | `DesignTimeResourceConsumerTests.cs` | Change only its collection attribute. |
| Create | `RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests.cs` | One history Fact and collection definition. |
| Create | `RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests.cs` | One history Fact and collection definition. |
| Create | `RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests.cs` | One history Fact and collection definition. |
| Create | `DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests.cs` | One history Fact and collection definition. |
| Create | `RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests.cs` | One history Fact and collection definition. |
| Create | `RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests.cs` | One history Fact and collection definition. |

Also modify `README.md` and create
`.scratch/integration-test-performance/adoption-a.md` during implementation.

Keep `ConsumerBuildCollection.cs`, `ConsumerDiagnosticsFixture.cs` and the five
classes `RawTextConsumerTests`, `LocalizedResourceConsumerTests`,
`IndexedPlaceholderConsumerTests`, `NamedPlaceholderConsumerTests` and
`ResourceKeyIdentifierConsumerTests` unchanged. Testhost culture-sensitive checks
remain in their shared collection.

## Task 1: Adopt the tested scheduling

**Interfaces:** Six existing `Task` methods become six
`public static async Task` methods with the same names and no parameters in
`internal static class IncrementalBuildConsumerHistories`. Each wrapper exposes
one `public Task` Fact returning the corresponding helper Task.

### Preflight

- [x] Read `AGENTS.md` and the evidence; inspect `git status --short`,
  `git rev-parse HEAD`, `dotnet --version` and existing xUnit assembly/configuration
  settings. Reconcile partial implementation rather than duplicate wrappers.
- [x] Use a suitable working branch/worktree and preserve pending changes/staging.
  Preserve old artifacts; use a fresh ignored adoption directory.
- [x] Use the SDK allowed by `global.json`, Node.js 24 and PowerShell 7. If local
  formatter dependencies are absent from the implementation workspace, prepare
  them before source changes or measured commands:

```powershell
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
npm ci
if ($LASTEXITCODE -ne 0) { throw 'Node dependency installation failed.' }
```

- [x] Before editing, freeze current source including pending files. Define this
  function in the execution PowerShell session; retain it for a conditional final
  candidate snapshot, or save it under `prototypes/integration-test-performance/`:

```powershell
function Copy-WorkingSource([string] $destination)
{
    $repository = (Get-Location).Path
    $paths = @(& git -c core.quotePath=false ls-files --cached --others --exclude-standard)
    if ($LASTEXITCODE -ne 0) { throw 'Could not enumerate source inputs.' }
    foreach ($relative in @($paths | Sort-Object -Unique))
    {
        if ($relative -match '^(\.scratch|prototypes)/') { continue }
        $source = Join-Path $repository $relative
        if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { continue }
        $target = Join-Path $destination $relative
        $null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target)
        Copy-Item -LiteralPath $source -Destination $target
    }
}
$ErrorActionPreference = 'Stop'
$adoptionRoot = Join-Path (Get-Location).Path "test-results/option-a-adoption-$([Guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $adoptionRoot
$adoptionRoot | Set-Content test-results/option-a-adoption-latest.txt
Copy-WorkingSource (Join-Path $adoptionRoot 'baseline')
$repositorySdk = (& dotnet --version)
Push-Location ([IO.Path]::GetTempPath())
try { $consumerSdk = (& dotnet --version) }
finally { Pop-Location }
[ordered]@{
    SourceCommit = (& git rev-parse HEAD)
    SDK = $repositorySdk
    ConsumerWorkingDirectorySDK = $consumerSdk
    RecordedUtc = [DateTime]::UtcNow.ToString('o')
} | ConvertTo-Json | Set-Content (Join-Path $adoptionRoot 'baseline-environment.json')
```

- [x] Establish a green current full inventory before changing identities:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
if ($LASTEXITCODE -ne 0) { throw 'Baseline restore failed.' }
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Baseline build failed.' }
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
pwsh -NoProfile -File tests/run.ps1 -Mode full |
    Tee-Object -FilePath (Join-Path $adoptionRoot 'baseline-full.log')
if ($LASTEXITCODE -ne 0) { throw 'Baseline full failed; diagnose before adopting A.' }
```

Check every native exit code before proceeding. Expected at unchanged source:
67 passed. Retain the runner's `Results:` TRX path. This is a correctness baseline,
not a new performance comparison; failures do not authorize weakened assertions.

### Implementation steps

- [x] **Step 1: Mechanically convert and rename the history helper.**

This reversible declaration transformation preserves every body, embedded source,
resource and assertion. It records hash equality before formatting:

```powershell
$ErrorActionPreference = 'Stop'
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
$oldPath = 'tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerTests.cs'
$newPath = 'tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerHistories.cs'
if (Test-Path -LiteralPath $newPath) { throw 'Inspect the existing helper before proceeding.' }
$lf = [string][char]10
$original = [IO.File]::ReadAllText($oldPath).Replace([string][char]13, '')
$methods = @(
    'RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits',
    'RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals',
    'RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration',
    'DetectsAssociatedResourcesExcludedFromSdkEmbedding',
    'RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics',
    'RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources'
)
function Replace-Once([string] $text, [string] $old, [string] $new)
{
    if ([regex]::Matches($text, [regex]::Escape($old)).Count -ne 1)
    { throw "Expected one declaration: $old" }
    return $text.Replace($old, $new)
}
$oldClass = '[Trait("Category", "Integration")]' + $lf +
    '[Collection("SDK consumer builds")]' + $lf +
    'public class IncrementalBuildConsumerTests'
$newClass = 'internal static class IncrementalBuildConsumerHistories'
$helper = Replace-Once $original $oldClass $newClass
foreach ($method in $methods)
{
    $oldDeclaration = '    [Fact]' + $lf + '    public async Task ' + $method + '()'
    $helper = Replace-Once $helper $oldDeclaration ('    public static async Task ' + $method + '()')
}
$restored = Replace-Once $helper $newClass $oldClass
foreach ($method in $methods)
{
    $restored = Replace-Once $restored ('    public static async Task ' + $method + '()') (
        '    [Fact]' + $lf + '    public async Task ' + $method + '()'
    )
}
if ($restored -cne $original) { throw 'History preservation failed.' }
function Get-TextHash([string] $text)
{
    [Convert]::ToHexString(
        [Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($text))
    ).ToLowerInvariant()
}
[ordered]@{
    OriginalHash = Get-TextHash $original
    ReversedHelperHash = Get-TextHash $restored
    MatchesOriginal = ($restored -ceq $original)
} | ConvertTo-Json | Set-Content (Join-Path $adoptionRoot 'history-preservation.json')
Move-Item -LiteralPath $oldPath -Destination $newPath
[IO.File]::WriteAllText($newPath, $helper, [Text.UTF8Encoding]::new($false))
```

For unchanged source, both hashes are
`dc8f9fc4b7602ae168cf980c70587f72269e6699e3748a36f7eae0c0553bac85`.
If source changed, require equality with the captured source rather than that
historical hash. After formatting, inspect whitespace differences separately;
retain the same operations, literals and assertion helpers.

- [x] **Step 2: Add the assembly worker bound.**

Create `tests/Talby.Core.ResxAccess.IntegrationTests/AssemblyInfo.cs`:

```csharp
[assembly: CollectionBehavior(MaxParallelThreads = 2)]
```

Keep it unique to IntegrationTests. Do not add assembly-wide parallelism disabling,
custom schedulers or task batching.

- [x] **Step 3: Create RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests.cs.**

File: tests/Talby.Core.ResxAccess.IntegrationTests/RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests.cs.

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits")]
public sealed class RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits")]
public sealed class RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEditsTests
{
    [Fact]
    public Task RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits() =>
        IncrementalBuildConsumerHistories.RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits();
}
```

- [x] **Step 4: Create RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests.cs.**

File: tests/Talby.Core.ResxAccess.IntegrationTests/RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests.cs.

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals")]
public sealed class RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals")]
public sealed class RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovalsTests
{
    [Fact]
    public Task RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals() =>
        IncrementalBuildConsumerHistories.RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals();
}
```

- [x] **Step 5: Create RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests.cs.**

File: tests/Talby.Core.ResxAccess.IntegrationTests/RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests.cs.

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration")]
public sealed class RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration")]
public sealed class RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestorationTests
{
    [Fact]
    public Task RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration() =>
        IncrementalBuildConsumerHistories.RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration();
}
```

- [x] **Step 6: Create DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests.cs.**

File: tests/Talby.Core.ResxAccess.IntegrationTests/DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests.cs.

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: DetectsAssociatedResourcesExcludedFromSdkEmbedding")]
public sealed class DetectsAssociatedResourcesExcludedFromSdkEmbeddingCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: DetectsAssociatedResourcesExcludedFromSdkEmbedding")]
public sealed class DetectsAssociatedResourcesExcludedFromSdkEmbeddingTests
{
    [Fact]
    public Task DetectsAssociatedResourcesExcludedFromSdkEmbedding() =>
        IncrementalBuildConsumerHistories.DetectsAssociatedResourcesExcludedFromSdkEmbedding();
}
```

- [x] **Step 7: Create RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests.cs.**

File: tests/Talby.Core.ResxAccess.IntegrationTests/RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests.cs.

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics")]
public sealed class RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics")]
public sealed class RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnosticsTests
{
    [Fact]
    public Task RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics() =>
        IncrementalBuildConsumerHistories.RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics();
}
```

- [x] **Step 8: Create RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests.cs.**

File: tests/Talby.Core.ResxAccess.IntegrationTests/RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests.cs.

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Incremental history: RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources")]
public sealed class RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesCollection { }

[Trait("Category", "Integration")]
[Collection("Incremental history: RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources")]
public sealed class RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResourcesTests
{
    [Fact]
    public Task RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources() =>
        IncrementalBuildConsumerHistories.RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources();
}
```

- [x] **Step 9: Separate DesignTime.**

Create `tests/Talby.Core.ResxAccess.IntegrationTests/DesignTimeBuildCollection.cs`:

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("Design-time consumer builds")]
public sealed class DesignTimeBuildCollection { }
```

Change only this attribute in
`tests/Talby.Core.ResxAccess.IntegrationTests/DesignTimeResourceConsumerTests.cs`:

```diff
-[Collection("SDK consumer builds")]
+[Collection("Design-time consumer builds")]
```

Keep `ConsumerBuildCollection.cs` unchanged:

```csharp
namespace Talby.Core.ResxAccess.IntegrationTests;

[CollectionDefinition("SDK consumer builds")]
public sealed class ConsumerBuildCollection : ICollectionFixture<ConsumerDiagnosticsFixture> { }
```

- [x] **Step 10: Format, build and inspect.**

```powershell
npm run format
if ($LASTEXITCODE -ne 0) { throw 'Formatting failed.' }
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Candidate build failed.' }
git diff -- tests/Talby.Core.ResxAccess.IntegrationTests
git status --short
```

Expected: Release succeeds; six wrapper Facts; no Fact in the helper; one worker
bound; the DesignTime attribute change. Inspect untracked files too.
No production, ConsumerProject, fixture, resource, package or runner change.

Use existing regression coverage instead of adding tests mirroring these
declarations. No new behavior or grouped diagnostic compilation is introduced;
grouped-diagnostic negative controls are not required.

## Task 2: Verify equivalence and document adoption

**Interfaces:** Consume Task 1's helper, wrappers and baseline; produce an exact
inventory comparison, read-only-output verification, and an explicit performance
evidence decision in `.scratch/integration-test-performance/adoption-a.md`.

- [x] **Step 1: Capture library metadata and run fresh correctness validation.**

Define this function in the validation PowerShell session and retain it for the
after snapshot, or include it when starting a fresh shell:

```powershell
function Get-LibraryMetadata
{
    $library = Join-Path (Get-Location).Path 'src/Talby.Core.ResxAccess'
    @(foreach ($directory in @('bin/Release', 'obj'))
    {
        Get-ChildItem -LiteralPath (Join-Path $library $directory) -Recurse -File |
            ForEach-Object {
                '{0}|{1}|{2}' -f [IO.Path]::GetRelativePath($library, $_.FullName),
                    $_.Length, $_.LastWriteTimeUtc.Ticks
            }
    }) | Sort-Object
}
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
Get-LibraryMetadata | Set-Content (Join-Path $adoptionRoot 'library-before.txt')
```

Run sequentially after the Release build, checking each exit code:

```powershell
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'IntegrationTests failed.' }
dotnet test tests/Talby.Core.ResxAccess.AspectTests/Talby.Core.ResxAccess.AspectTests.csproj --configuration Release --no-build --no-restore
if ($LASTEXITCODE -ne 0) { throw 'AspectTests failed.' }
pwsh -NoProfile -File tests/run.ps1 -Mode fast
if ($LASTEXITCODE -ne 0) { throw 'Fast failed.' }
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
pwsh -NoProfile -File tests/run.ps1 -Mode full |
    Tee-Object -FilePath (Join-Path $adoptionRoot 'candidate-full.log')
if ($LASTEXITCODE -ne 0) { throw 'Candidate full failed.' }
Get-LibraryMetadata | Set-Content (Join-Path $adoptionRoot 'library-after.txt')
$before = @(Get-Content (Join-Path $adoptionRoot 'library-before.txt'))
$after = @(Get-Content (Join-Path $adoptionRoot 'library-after.txt'))
if (Compare-Object $before $after -CaseSensitive) { throw 'Tests wrote shared library outputs.' }
```

Expected at unchanged source: 35 IntegrationTests, 12 AspectTests, 32 fast and
67 full, all passed. Counts alone do not establish preserved assertions.

- [x] **Step 2: Compare exact baseline/candidate full inventories.**

This writes the actual new-to-old map for correctness and conditional benchmarks:

```powershell
$ErrorActionPreference = 'Stop'
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
$historyPath = Join-Path $adoptionRoot 'baseline/tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerTests.cs'
$methods = @([regex]::Matches(
    [IO.File]::ReadAllText($historyPath), 'public async Task (\w+)\(\)'
) | ForEach-Object { $_.Groups[1].Value })
$namespace = 'Talby.Core.ResxAccess.IntegrationTests'
$map = @{}
foreach ($method in $methods)
{
    $map["$namespace.$($method)Tests.$method"] = "$namespace.IncrementalBuildConsumerTests.$method"
}
$map | ConvertTo-Json | Set-Content (Join-Path $adoptionRoot 'identity-map-a.json')
function Read-FullInventory([string] $logPath, [hashtable] $identityMap)
{
    $match = [regex]::Match(
        (Get-Content -LiteralPath $logPath -Raw),
        '(?m)^Verified full results: \d+ passed\. Results: (.+)$'
    )
    if (-not $match.Success) { throw "Missing verified full result: $logPath" }
    $files = @(Get-ChildItem -LiteralPath $match.Groups[1].Value.Trim() -Filter '*.trx' -Recurse -File)
    if ($files.Count -eq 0) { throw 'Missing TRX files.' }
    $rows = @(foreach ($file in $files)
    {
        [xml]$trx = Get-Content -LiteralPath $file.FullName -Raw
        foreach ($result in @($trx.TestRun.Results.UnitTestResult))
        {
            if ($result.outcome -ne 'Passed') { throw "Nonpassing test: $($result.testName)" }
            $definition = @($trx.TestRun.TestDefinitions.UnitTest | Where-Object id -eq $result.testId)
            if ($definition.Count -ne 1) { throw 'Missing unique test definition.' }
            $assembly = [IO.Path]::GetFileNameWithoutExtension($definition[0].storage)
            $identity = [string]$result.testName
            if ($identityMap.ContainsKey($identity)) { $identity = $identityMap[$identity] }
            "$assembly|$identity"
        }
    })
    if (@($rows | Sort-Object -Unique -CaseSensitive).Count -ne $rows.Count)
    { throw 'Duplicate canonical identity.' }
    return ,@($rows | Sort-Object -CaseSensitive)
}
$baseline = Read-FullInventory (Join-Path $adoptionRoot 'baseline-full.log') @{}
$candidate = Read-FullInventory (Join-Path $adoptionRoot 'candidate-full.log') $map
if (Compare-Object $baseline $candidate -CaseSensitive) { throw 'Inventory changed.' }
$baseline | Set-Content (Join-Path $adoptionRoot 'canonical-full-inventory.txt')
"Preserved $($candidate.Count) passed canonical identities."
```

- [x] **Step 3: Decide whether existing performance evidence applies.**

Reuse the completed A comparison when relevant baseline source, dependencies, SDK
and scheduling are unchanged and adoption differs only in source-file placement
and formatting. Record the semantic/body comparison and reuse justification.
Fresh correctness runs above remain required.

Verify both the repository SDK and the SDK selected from the child consumer
working directory (the temporary directory). Both were 10.0.401 in the experiment.
An unchanged root SDK alone is insufficient if child SDK selection changed.

If relevant source, SDK, dependencies, worker bound, collections or build/assertion
behavior changed, collect a fresh A-only comparison using the captured baseline
and actual final candidate, following the conditional procedure below.
A new machine cannot inherit the historical local timing claim.
Do not repeat B/A+B or the old diagnosis solely to recover context.

- [x] **Step 4: Document the retained scheduling in README.**

Add after the test project overview:

```markdown
### Integration test scheduling

IntegrationTests uses at most two concurrent xUnit collection workers.
Each incremental consumer history has its own Fact class and collection;
mutations, builds and runtime checks within a history remain sequential.
DesignTime uses a separate collection. The five diagnostic classes retain
one shared collection and one lazy ConsumerDiagnosticsFixture.

Temporary consumers use isolated directories and reuse library outputs
without modifying them. The fixture's two builds can overlap another history,
so the worker limit is not a global two-process SDK limit.
The six changed identities are in the
[coverage map](.scratch/integration-test-performance/coverage-map.md).

Fast/full project selection stays unchanged. The
[performance comparison](.scratch/integration-test-performance/comparison.md)
records local experimental results, not CI timing guarantees.
```

Change the existing historical phrase `the current inventory to 32 fast / 66 full`
to `that inventory to 32 fast / 66 full at that point`. Keep current counts aligned
with actual verification; retain historical reports and dynamic runner discovery.

- [x] **Step 5: Publish adoption-a.md, review and format.**

Record actual source versions, SDK, changed files, six identity mappings,
preservation hashes/diff, canonical inventory, library metadata result,
commands/exit codes, and performance reuse or new CSV links.
Link this plan and the completed comparison. Keep exploration artifacts unchanged.

```powershell
npm run format:check
if ($LASTEXITCODE -ne 0) { throw 'Formatting check failed.' }
git diff --check
if ($LASTEXITCODE -ne 0) { throw 'Whitespace check failed.' }
git status --short
```

Expected: zero formatting issues, no whitespace errors, only intended changes.
Review specification compliance and maintainability; close checkboxes and adoption
evidence. Commit/PR/merge actions require a separate explicit instruction.

## Conditional fresh A-only measurement

Not applicable for this adoption: the measured A evidence passed the documented reuse checks. No fresh measurement or summary-script modification was made. The conditional steps below intentionally remain unchecked.

Run only when Task 2 cannot reuse the completed A evidence.
A later implementation session alone is not a reason to repeat benchmarking.

- [ ] Format the actual final candidate; freeze it using `Copy-WorkingSource` from
  preflight. Include its definition when using a fresh PowerShell shell.

```powershell
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
$candidateRoot = Join-Path $adoptionRoot 'A'
if (Test-Path -LiteralPath $candidateRoot) { throw 'Do not overwrite an existing candidate snapshot.' }
Copy-WorkingSource $candidateRoot
```

- [ ] Write maps/manifests and restore/build both trees outside measurement:

```powershell
$ErrorActionPreference = 'Stop'
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
'{}' | Set-Content (Join-Path $adoptionRoot 'baseline/identity-map.json')
Copy-Item -LiteralPath (Join-Path $adoptionRoot 'identity-map-a.json') -Destination (Join-Path $adoptionRoot 'A/identity-map.json')
$trees = @{}
foreach ($variant in @('baseline', 'A'))
{
    $tree = Join-Path $adoptionRoot $variant
    $hashes = @{}
    Get-ChildItem -LiteralPath $tree -Recurse -File | ForEach-Object {
        $relative = [IO.Path]::GetRelativePath($tree, $_.FullName)
        if ($relative -match '(^|[\\/])(bin|obj|test-results|node_modules)([\\/]|$)') { return }
        $hashes[$relative] = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $trees[$variant] = $hashes
}
@{ trees = $trees; sourceCommit = (& git rev-parse HEAD) } |
    ConvertTo-Json -Depth 100 | Set-Content (Join-Path $adoptionRoot 'source-hashes.json')
foreach ($variant in @('baseline', 'A'))
{
    Push-Location (Join-Path $adoptionRoot $variant)
    try
    {
        dotnet restore Talby.Core.ResxAccess.slnx
        if ($LASTEXITCODE -ne 0) { throw "Restore failed: $variant" }
        dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
        if ($LASTEXITCODE -ne 0) { throw "Preparation build failed: $variant" }
    }
    finally { Pop-Location }
}
```

- [ ] Extend strict summary selection in
  `prototypes/integration-test-performance/Summarize-Comparisons.ps1`.
  Add this parameter before `$AllowPartial`:

```powershell
[ValidateSet('A', 'B', 'AB')]
[string[]] $Comparisons = @('A', 'B', 'AB'),
```

After reading `$samples`, add:

```powershell
$samples = @($samples | Where-Object Comparison -In $Comparisons)
```

Replace the strict-validation loop header:

```diff
-    foreach ($comparison in @('A', 'B', 'AB'))
+    foreach ($comparison in $Comparisons)
```

Keep default behavior, pair IDs 1–5, both clean observations, nonzero-exit
rejection and all summary arithmetic. Do not use `-AllowPartial` for acceptance.

- [ ] Run sequentially against the actual baseline/A trees:

```powershell
$adoptionRoot = Get-Content test-results/option-a-adoption-latest.txt
pwsh -NoProfile -File prototypes/integration-test-performance/Run-Comparison.ps1 -ExperimentRoot $adoptionRoot -Candidate A -Phase All
if ($LASTEXITCODE -ne 0) { throw 'A comparison failed.' }
pwsh -NoProfile -File prototypes/integration-test-performance/Summarize-Comparisons.ps1 -ExperimentRoot $adoptionRoot -EvidenceDirectory .scratch/integration-test-performance/adoption-results -Comparisons A
if ($LASTEXITCODE -ne 0) { throw 'Strict A summary failed.' }
```

Expected: 62 observations (ten excluded warm-ups, 50 paired observations across
IntegrationTests/AspectTests/fast/full/WarmFull, two clean observations).
Alternate baseline/candidate order; do not overlap SDK/test/formatting commands.
Record machine, SDK, NuGet cache, timing configuration and all samples.
Verify canonical inventories and shared library metadata. Report medians, ranges,
paired deltas and separate build/test/cycle time; never sum overlapping test durations.
Require at least 30% lower median IntegrationTests wall time and no reproducible
full-cycle regression. Preserve assertions when resolving failures; do not broaden
this plan to B.

## Completion checklist

- [x] Six wrapper Facts retain the original method names and Integration trait.
- [x] All history bodies, embedded inputs, assertions and mutation order are preserved.
- [x] Histories and DesignTime can run concurrently with two collection workers.
- [x] Five diagnostic classes retain one lazy fixture and existing culture isolation.
- [x] Current baseline/candidate canonical inventories match with all outcomes Passed.
- [x] Tests preserve shared library paths, sizes and UTC timestamps.
- [x] Fresh Release correctness validation and formatting checks pass.
- [x] Existing A evidence is reused with justification, or fresh A-only evidence meets the criterion.
- [x] README and adoption evidence describe identity changes, scheduling and limits.
- [x] B/C/D/E, production APIs, ConsumerProject flags and the runner remain outside the change.
- [x] Review and evidence are complete, and original exploration artifacts are preserved.

## Next-session instruction

Use this request to begin execution when ready:

> Implement Option A following `.scratch/integration-test-performance/implementation-plan-a.md`.
> Preserve every assertion and the diagnostic fixture. Complete correctness
> validation and record the performance-evidence decision. Do not add B or create
> commits or pull requests unless I explicitly request them.
