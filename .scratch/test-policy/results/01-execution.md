# Fast/full execution experiment

Status: candidate pending user review; not adopted or merged.

Origin: [issue 01](../issues/01-separate-fast-and-full-execution.md), following
the [parent measurement protocol](../spec.md).

## Change set and reproducible commands

- Add only `[Trait("Category", "Integration")]` to `RawTextConsumerTests`.
- Add `tests/run.ps1 -Mode fast|full`: Release test-only entry points using the
  existing solution, VSTest, xUnit filtering, and TRX logger. Fast applies
  `Category!=Integration`; full has no filter. No dependency or provider is added.
- Document prerequisites, deferred tests, and full verification before merge in
  README. Leave AGENTS.md and the final classification policy unchanged.
- Publish this report and the raw timing CSV. No production code, assertion,
  test fixture, project configuration, public API, diagnostic, or ADR changes.

From the repository root, with PowerShell 7 and SDK 10.0.401 (or a permitted
10.0.4xx patch), prepare fresh or changed source:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
```

Restore requires NuGet.org access. Rebuild after changes to source, resources, or
fixtures before either entry point; both deliberately use `--no-build --no-restore`.
Neither omits a solution project from the prerequisite build.

```powershell
# Local iteration: four tests; four integration tests deferred visibly.
pwsh -NoProfile -File tests/run.ps1 -Mode fast

# Required verification before merge: all eight tests.
pwsh -NoProfile -File tests/run.ps1 -Mode full

# Existing standard command still selects the complete suite.
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

The README contains a CI-ready restore/build/full sequence with exit propagation.
No CI provider exists here; no automatic enforcement is claimed. Each entry
point returns the native test exit code on failure, or a nonzero exit for a
selection mismatch. Successful completion checks exact passed identities in
fresh, uniquely named TRX directories under ignored `test-results/execution/`.
Empty, missing, skipped, duplicate, and unexpected results cannot satisfy the
fixed inventory. Adding or renaming tests requires reviewing that inventory;
this is a deliberately bounded experiment guard, not a general runner framework.

## Discovery and correctness checks

The full ordinary test prefix below is
`Talby.Core.ResxAccess.Tests.RawTextConsumerTests.`; the setup test prefix is
`Talby.Core.ResxAccess.Tests.MetalamaSetupTests.`. AspectTests use their displayed
file-based identities directly.

| Discovered identity | Fast | Integration | Full |
| --- | --- | --- | --- |
| CanCreateAndQueryCompilation (setup prefix) | Yes | No | Yes |
| InvalidPaths | Yes | No | Yes |
| UnsupportedTargets | Yes | No | Yes |
| UnavailableProjectContext | Yes | No | Yes |
| CanCompileAndInvokeIndependentResourceSets (consumer prefix) | No | Yes | Yes |
| ReportsEachInvalidResourceAndEmbeddingInOneBuild (consumer prefix) | No | Yes | Yes |
| ReportsMalformedReferenceResourceWithoutAspectCrash (consumer prefix) | No | Yes | Yes |
| DescribesMissingRuntimeManifestAndResourceKey (consumer prefix) | No | Yes | Yes |

Actual VSTest discovery was run with these commands (not inferred from Fact
counts or class names):

```powershell
$common = @('test', 'Talby.Core.ResxAccess.slnx', '--configuration', 'Release',
    '--no-build', '--no-restore', '--list-tests', '--verbosity', 'normal')
$sets = @{}
foreach ($selection in 'fast', 'integration', 'full')
{
    $filter = switch ($selection)
    {
        'fast' { @('--filter', 'Category!=Integration') }
        'integration' { @('--filter', 'Category=Integration') }
        'full' { @() }
    }
    $output = & dotnet @common @filter
    if ($LASTEXITCODE -ne 0) { throw "Discovery failed: $selection" }
    $sets[$selection] = @($output | Where-Object {
        $_ -match '^    (Talby\.Core\.ResxAccess\.Tests\.|InvalidPaths$|UnsupportedTargets$|UnavailableProjectContext$)'
    } | ForEach-Object { $_.Trim() })
}
if ($sets.fast.Count -ne 4 -or $sets.integration.Count -ne 4 -or $sets.full.Count -ne 8)
{ throw 'Unexpected discovery count' }
if (@($sets.fast | Where-Object { $_ -cin $sets.integration }).Count)
{ throw 'Fast and integration overlap' }
if (Compare-Object $sets.full ($sets.fast + $sets.integration) -CaseSensitive)
{ throw 'Fast + integration differs from full' }
```

All three commands exited 0. Fast = 4, integration = 4, full = 8; intersection
is empty and union equals full by identity. Integration discovery correctly
finds no tests in AspectTests, while fast retains all three. Every measured run
also checked selected TRX definitions and executed identities against these sets.

Before adding the trait, the fast command ran eight passing tests but exited 1
because its expected selection was four. After adding the trait and rebuilding,
it passed exactly four and exited 0. This checks the entry point rather than
assuming a filter that returns success has selected the intended tests.

Two additional temporary filter controls tested the guard: `Category=NotPresent`
selected zero tests, and
`Category!=Integration&FullyQualifiedName!~CanCreateAndQueryCompilation` selected
only the three AspectTests. Native `dotnet test` exited 0 in both cases, but the
entry point rejected each with exit 1 and listed expected/executed identities.
Both filter mutations were reverted byte-for-byte; the entry point's hash still
matches the measured version.

For the required integration negative control, temporarily replace
`Assert.Equal("Descriptive failure", invocation.Output.Trim())` with an expected
`"NEGATIVE CONTROL"` in `DescribesMissingRuntimeManifestAndResourceKey`, then
rebuild. The documented fast command exited 0 with four passes and excluded that
identity. The documented full command exited 1 with seven passes and one failure:
expected `NEGATIVE CONTROL`, actual `Descriptive failure`. The native failure
propagated through `tests/run.ps1` and `pwsh`. Restore the original file bytes and
rebuild before measurements and final verification; the mutation is absent from
this change set.

## Unchanged assertion inventory and deferred failures

| Retained test | Assertions/scenarios retained | SDK consumer builds | Deferred by fast |
| --- | --- | ---: | --- |
| CanCreateAndQueryCompilation | A single compiled type, named Sample. | 0 | No |
| InvalidPaths | Exact TRESX001 diagnostics/messages for null, empty, whitespace, and wrong extension paths. | 0 | No |
| UnsupportedTargets | Exact TRESX002 diagnostics/messages for instance and generic targets. | 0 | No |
| UnavailableProjectContext | Exact TRESX003 diagnostic/message for unavailable consumer project directory. | 0 | No |
| CanCompileAndInvokeIndependentResourceSets | Successful compilation and invocation; three independent Resource Sets; non-partial static target; target identity, namespace and accessibility; four public static methods and no Format methods on the Basic target; invalid identifier omitted; parameterless and explicit Resource Culture; CurrentUICulture independent of CurrentCulture; es-MX/es and fr-CA/fr parent lookup; de-DE Reference Resource fallback; implicit SDK associated type naming and RootNamespace; null culture rejected; Raw Text including placeholders and whitespace; culture-named neutral file; keyword and Unicode keys; empty and blank text. | 1 | Yes |
| ReportsEachInvalidResourceAndEmbeddingInOneBuild | Build fails; every target filename, diagnostic code and full message on the same output line: LogicalName, ManifestResourceName and linked embedding (TRESX003), missing Reference Resource and culture-specific Reference Resource (TRESX001). Five cases in one build, all required. | 1 | Yes |
| ReportsMalformedReferenceResourceWithoutAspectCrash | Build fails with SDK MSB3103; no LAMA0041 aspect crash diagnostic. | 1 | Yes |
| DescribesMissingRuntimeManifestAndResourceKey | Two isolated consumer builds and invocations succeed as test harnesses; each lookup raises InvalidOperationException naming Resource Key, manifest base and culture; missing manifest retains MissingManifestResourceException as inner exception; both print the exact descriptive-failure marker. | 2 | Yes |

The Consumer source equals the baseline after removing only the new class trait.
The helper, setup test, AspectTests inputs/snapshots, production sources, projects,
solution, SDK selection, and ADRs have no diff. Nothing is moved, removed, or
newly covered; all eight identities and the five-build scenario inventory remain
in full. Failure reporting and isolation within grouped Facts are unchanged.

Fast defers regressions in generated signatures/accessibility, SDK resource-map
integration, manifest naming, culture lookup/fallback, Raw Text preservation,
context-dependent diagnostics, malformed XML handling, and runtime errors until
full runs. Current AspectTests cannot forward the SDK project path/resource map:
`UnavailableProjectContext` asserts that limitation, not successful generation.
They do not execute generated code or validate runtime resource lookup. The setup
test only proves that a Metalama compilation can be created and queried.
Localized Resource consistency, formatting, incremental/IDE refresh, and expanded
packaging remain existing gaps outside this experiment. Fast success therefore
does not establish generation and runtime lookup end to end.

## Measurement protocol and environment

Measured 2026-10-02 on Windows 11 Pro x64 10.0.26200, Intel Core i9-10900K
(10 cores, 20 logical processors), 63.9 GiB RAM, SDK 10.0.401/MSBuild 18.9.11, runtime 10.0.12,
PowerShell 7.6.6, Metalama 2026.1.28, Microsoft.NET.Test.Sdk 17.14.1,
xUnit 2.9.3 and adapter 3.1.4. Configuration: Release.

Baseline: `7fa01ab8dd1259ce58cc4ed5f1aa01f8bf3d6d44` (the current approved
baseline; the parent plan's historical `62f653d` differs only in documentation).
Candidate: the working tree based on that commit on
`test/separate-fast-and-full-execution`, subsequently committed with this report.
Its executable changes are the trait above and `tests/run.ps1`.
SHA-256 of the measured entry point:
`AF2950DF9D1349EE11B5E5FA46F2F298F007C1B18181ED68CA8416C48629C60A`.
SHA-256 of the measured consumer source:
`AD4EBE5C95E58A7A595C069B8DAA2E1EA470965129ED3B5414D648B26DC57AF3`.
These are working-file byte hashes before Git line-ending normalization.
Normalized Git blob identities are `958ec455b481f61a53a20a2a03e3ec9082174560`
for the entry point and `4e220209cc124d7b3507cd9c5d97c7199480bd1d` for the
consumer source, allowing verification after a checkout with different line endings.

Baseline runs from an isolated detached worktree at
`E:\talby.net\Talby.Core.ResxAccess\test-results\test-policy-baseline`; candidate
runs from `E:\talby.net\Talby.Core.ResxAccess`. Each has its own build outputs;
both use the same machine, installed tools, global NuGet cache, and default
process environment. Initial solution restore and setup build were outside the
timed test-only samples. Consumer tests still perform their existing implicit
restores inside all five temporary SDK builds; those costs are included.

Revisions run sequentially, never concurrently. No parallelism setting changed:
solution test-project hosts may overlap, xUnit uses its defaults, and the four
consumer Facts in one class run sequentially with awaited SDK builds. Two test
hosts run in every route. Fast removes zero prerequisite project builds; all
three projects are built for its build + test samples. No precompiled fixture
project was introduced. Baseline/full still create five temporary consumer
projects/builds; fast creates zero. Those builds may also rebuild the referenced
library, exactly as before.
The CSV's consumer build counts are derived from this unchanged scenario
inventory, rather than independent child-process instrumentation.

Test logging is identical (`--verbosity normal`,
`--logger 'trx;LogFilePrefix=execution'`); output is redirected to per-run logs,
with fresh result directories. Outer builds use `--verbosity minimal` in both
revisions. Baseline invokes `dotnet test` directly; candidate timings include the
documented `pwsh -NoProfile -File tests/run.ps1` process, inventory validation,
and deferred-test output. This extra startup/check cost belongs to the candidate.

One excluded warm-up was run for each revision/route in each mode. Each mode
then has five triples, giving five pairs for both baseline-full/candidate-fast
and baseline-full/candidate-full; the two comparisons share the baseline sample.
Odd triples run baseline-full, candidate-fast, candidate-full; even triples
reverse that order. Stopwatch measurements capture external wall time, with
build and test timed separately and the combined interval timed around both
commands. Parsing benchmark TRX happens after that interval; entry point parsing
remains inside candidate test time. Counts and exit codes are in every CSV row.

Test-only uses the entry points above. Warm build + test prepends:

```powershell
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity minimal
```

An additional baseline-full/candidate-full pair, plus a candidate-fast observation,
each follows its own untimed `dotnet clean Talby.Core.ResxAccess.slnx
--configuration Release --verbosity minimal`. This is a clean-output measurement,
not a cold-cache benchmark; NuGet caches and restore assets remain available.

## Measurements

All times below are seconds. [Raw CSV](01-timings.csv) contains all 39 external
stopwatch observations: six excluded warm-ups, thirty samples, and three
clean-output observations. Every row has build/test exit codes, selected TRX
definition count, executed/passed count, and temporary consumer build count.
All exits were 0. Every baseline/full and candidate/full run selected and passed
8/8; every fast run selected and passed 4/4, deferring the other four. For
test-only rows, build time/exit 0 means no outer build was run in that interval.

Test process overhead is recorded separately as
`OutsideTestRunEnvelopeSeconds`: external test wall time minus the envelope from
the earliest TRX run start to the latest run finish across both hosts. The CSV
retains that envelope as `TestRunEnvelopeSeconds`. This measures work outside
the runner intervals (launching, shutdown, outer logging and candidate validation),
not isolated startup time; discovery and Metalama processing inside those
intervals are still included in the envelope. Overlapping hosts are not summed.

| Mode | Route | Outside-run overhead median [min, max] |
| --- | --- | ---: |
| test-only | baseline/full | 1.14 [1.06, 1.50] |
| test-only | candidate/fast | 1.58 [1.54, 1.62] |
| test-only | candidate/full | 1.55 [1.49, 1.58] |
| warm-build-test | baseline/full | 1.12 [1.09, 1.31] |
| warm-build-test | candidate/fast | 1.59 [1.56, 2.39] |
| warm-build-test | candidate/full | 1.57 [1.52, 1.60] |

| Mode | Route | Build median [min, max] | Test median [min, max] | Combined median [min, max] |
| --- | --- | ---: | ---: | ---: |
| test-only | baseline/full | 0.00 [0.00, 0.00] | 16.08 [15.61, 17.08] | 16.08 [15.61, 17.08] |
| test-only | candidate/fast | 0.00 [0.00, 0.00] | 5.90 [5.75, 6.00] | 5.90 [5.75, 6.00] |
| test-only | candidate/full | 0.00 [0.00, 0.00] | 16.50 [16.31, 16.66] | 16.50 [16.31, 16.66] |
| warm-build-test | baseline/full | 1.81 [1.81, 1.94] | 15.28 [15.19, 18.06] | 17.16 [17.00, 20.00] |
| warm-build-test | candidate/fast | 2.06 [1.78, 3.66] | 5.80 [5.75, 8.06] | 8.35 [7.55, 11.72] |
| warm-build-test | candidate/full | 1.88 [1.85, 2.33] | 15.76 [15.48, 17.42] | 17.81 [17.34, 19.43] |

Combined medians are taken from combined observations, rather than summing
independently calculated build/test medians. Positive deltas mean less time;
percentage delta is `(baseline median - candidate median) / baseline median * 100`.

| Mode | Comparison | Absolute median delta | Percentage delta |
| --- | --- | ---: | ---: |
| test-only | Full -> fast | 10.18 s | 63.33% |
| test-only | Full -> full | -0.43 s | -2.65% |
| warm-build-test | Full -> fast | 8.81 s | 51.36% |
| warm-build-test | Full -> full | -0.65 s | -3.80% |

Paired combined wall times and deltas, keeping every sample visible:

| Mode | Pair | Baseline full | Candidate fast | Delta full-fast | Candidate full | Delta full-full |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| test-only | 1 | 17.06 | 6.00 | 11.06 | 16.54 | 0.52 |
| test-only | 2 | 15.61 | 5.90 | 9.72 | 16.66 | -1.04 |
| test-only | 3 | 17.08 | 5.91 | 11.18 | 16.50 | 0.58 |
| test-only | 4 | 15.92 | 5.75 | 10.17 | 16.45 | -0.52 |
| test-only | 5 | 16.08 | 5.81 | 10.26 | 16.31 | -0.23 |
| warm-build-test | 1 | 17.00 | 8.35 | 8.66 | 18.27 | -1.27 |
| warm-build-test | 2 | 17.16 | 8.39 | 8.77 | 19.43 | -2.27 |
| warm-build-test | 3 | 20.00 | 11.72 | 8.28 | 17.81 | 2.19 |
| warm-build-test | 4 | 17.01 | 7.72 | 9.29 | 17.62 | -0.61 |
| warm-build-test | 5 | 17.67 | 7.55 | 10.12 | 17.34 | 0.33 |

Additional clean-output observations, each following its own Release clean:

| Clean-output route | Build | Test | Combined | Exit | Selected/executed/passed |
| --- | ---: | ---: | ---: | ---: | --- |
| baseline/full | 2.03 | 15.17 | 17.20 | 0 | 8/8/8 |
| candidate/full | 2.01 | 15.61 | 17.62 | 0 | 8/8/8 |
| candidate/fast | 1.96 | 6.06 | 8.03 | 0 | 4/4/4 |

Fast saves time in every pair despite the warm build/test outlier (pair 3,
11.72 s). This is reduced work: zero consumer builds and four selected tests,
not an equivalent-work full-suite optimization. Outer solution build costs
remain included. No additional omitted-project build cost exists.

Equivalent full execution has slightly worse medians with mixed paired deltas
and overlapping ranges. The wrapper adds a PowerShell process and validation;
the measurements do not isolate how much of the difference is that overhead
versus ordinary variation. A full-suite speed improvement is inconclusive and
unsupported by these samples; no fixed savings percentage is promised.

## Final verification and recommendation

Final Release solution build and the documented full command pass after all
negative controls are restored: zero build warnings/errors, eight passed tests,
zero deferred tests, exit 0. The executable file hashes match those measured.
Generated logs, TRX files, binaries, and the benchmark harness remain ignored
under `test-results/`; raw timings needed for review are checked in as CSV.

Recommendation: adopt the opt-in fast command for local iteration only if the
user accepts deferring the four integration Facts. Retain full verification
before merge and whenever integration feedback is needed; it preserves the
existing scenario protection and has no demonstrated speed gain. The observed
local savings support reviewing this tradeoff, not weakening the full gate.
Do not adopt a permanent classification policy, merge this candidate, or start
experiment 02/03 on top of it before the user reviews this change set.

## Code review

The `code-review` skill reviewed the staged candidate against fixed baseline
`7fa01ab` in independent Standards and Spec agents before committing.

### Standards

**Standards review: pass.** No hard violations found against `AGENTS.md`,
`docs/agents/issue-tracker.md`, `docs/agents/triage-labels.md`,
`docs/agents/domain.md`, or `CONTEXT.md`. The issue uses the documented
`ready-for-human` status and keeps its update under `## Comments`; added
documentation and domain terms are in English. The class-level xUnit trait
follows the existing test conventions.

No actionable baseline smells found. Repeated test names and commands across
the runner, README, and experiment report serve the requested guard and
documentation. Tooling-enforced issues were skipped. No files were changed;
no tests or processes were run by the reviewer.

### Spec

**Spec review: no findings.**

- **Missing or partial requirements:** None identified. The commands, selection
  checks, negative control, coverage inventory, measurements, AspectTests gaps,
  and review handoff are documented against the issue's acceptance criteria.
- **Scope creep:** None identified. The changes stay within test classification,
  execution entry points, documentation, and experiment results.
- **Apparently incorrect implementation:** None identified. The fixed test
  inventory matches the documented selections; the report's 39 CSV observations
  and summarized measurements are consistent with the raw data.

No edits or tests performed by the reviewer.

Standards: 0 findings. Spec: 0 findings. No worst issue in either axis.
