# Test execution policy experiments

Status: ready-for-human

## Goal and review boundary

Evaluate options 2, 3, and 4 from the test-performance discussion as three separate change sets. Each experiment must deliver working code, objective before/after measurements, and an assertion coverage review before the user decides whether to adopt it. An additional task separates unit tests and integration tests into explicitly named projects. Implementation requires an explicit request; delivering a candidate does not establish a permanent test classification policy. Experiment 01 has been implemented on request and awaits user review before adoption. Task 04 is planned only in the current pull request.

| Issue | Previous option | Experiment | Main question | Progress |
| --- | --- | --- | --- | --- |
| [01](issues/01-separate-fast-and-full-execution.md) | 2 | Separate fast and full execution | How much local feedback time is saved by deferring integration tests? | Implemented; [results](results/01-execution.md) awaiting user review |
| [02](issues/02-precompile-consumer-fixtures.md) | 3 | Precompile consumer fixtures | Does removing builds from runtime tests reduce the complete development cycle? | Not started |
| [03](issues/03-share-testable-aspect-logic.md) | 4 | Share testable aspect logic | Can broader fast coverage validate production logic while SDK smoke tests retain the integration boundary? | Not started |
| [04](issues/04-separate-unit-and-integration-test-projects.md) | Additional task | Separate unit and integration test projects | Can project boundaries make unit and integration execution explicit while preserving every scenario? | Planned; not started |

Recommended evaluation order: 01, then 02, then 03. Keep each candidate independently reviewable and revertible. Start each from the latest approved baseline; do not include an unapproved candidate in the next change set. Rejecting one experiment does not prevent evaluating the others against the retained baseline.

Task 04 is a separate project-organization task, with no mandatory dependency on
experiments 02 or 03. Implement it from the latest approved baseline after an
explicit implementation request; this pull request records its scope only.

## Current evidence

Experiment 01 used approved baseline `7fa01ab`, which differs from the original planning baseline `62f653d` only in documentation. Candidate `c6d29e3` is on `test/separate-fast-and-full-execution`; it is not an approved baseline for subsequent experiments. The solution still discovers eight tests: five ordinary xUnit tests and three AspectTests. Four ordinary tests exercise consumers through five temporary SDK builds: one successful consumer, one grouped diagnostic consumer, one malformed XML consumer, and two runtime failure consumers.

The [previous diagnosis](../test-performance/diagnosis.md) records full test command times of 16.02 s and 26.81 s after grouping. These historical samples demonstrate variation. Experiment 01 collected fresh measurements; experiments 02 and 03 must do the same against their latest approved baseline.

## Experiment 01 results and review handoff

The [report](results/01-execution.md) and [raw timings](results/01-timings.csv)
record 39 observations collected on 2026-10-02: six excluded warm-ups, thirty
samples (five paired comparisons per mode for each candidate route), and three
additional clean-output observations. Initial restore is excluded; temporary
consumer restores and builds remain included in full test time.

| Mode | Baseline full median | Candidate fast median | Candidate full median | Full-to-fast reduction |
| --- | ---: | ---: | ---: | ---: |
| Test-only | 16.08 s | 5.90 s | 16.50 s | 10.18 s (63.33%) |
| Warm build + test | 17.16 s | 8.35 s | 17.81 s | 8.81 s (51.36%) |

Fast saves local work by deferring four Consumer integration tests and their
five temporary SDK builds. It retains the setup unit test and all three
AspectTests. Both routes still require building all three solution projects
after source or fixture changes. Full retains every test and assertion; the
standard solution-level `dotnet test` command continues to select all eight.
Equivalent full execution has slightly worse medians and mixed paired deltas;
no full-suite speed improvement is demonstrated.

Candidate commands, after the Release restore/build prerequisites below:

```powershell
pwsh -NoProfile -File tests/run.ps1 -Mode fast
pwsh -NoProfile -File tests/run.ps1 -Mode full
```

The entry points require PowerShell 7, propagate failures, and validate the
experiment's fixed passed-test inventory. Discovery confirmed disjoint fast
and integration selections whose union equals full. Empty and missing
selections were rejected. A temporary integration assertion mutation failed
full while fast passed; every mutation was reverted before final verification.
The final Release build passed without warnings or errors and full passed 8/8.
Independent Standards and Spec reviews found no issues.

The report maps the unchanged assertion inventory and deferred regressions.
Current AspectTests do not validate successful SDK-backed generation or runtime
lookup; fast success does not establish either end to end. Full remains the
required verification before merge. No CI provider is configured, so automatic
enforcement is not claimed.

Recommendation: accept fast for optional local iteration only if the deferred
integration feedback is acceptable, while retaining the full verification gate.
The user has not yet approved adoption. Experiments 02 and 03 remain unstarted;
do not base either on this candidate before that review. The parent status
records the human review handoff, not completion of all three experiments.

## Additional task 04: separate unit and integration test projects

Create `Talby.Core.ResxAccess.IntegrationTests` and rename the existing
`Talby.Core.ResxAccess.Tests` project to `Talby.Core.ResxAccess.UnitTests`, including
their directories, `.csproj` filenames, and namespaces. Move `RawTextConsumerTests`
and its `ConsumerProject` helper to IntegrationTests; keep `MetalamaSetupTests`
in UnitTests. Keep the dedicated AspectTests project.

Expected layout:

```text
tests/
├── Talby.Core.ResxAccess.UnitTests/          # MetalamaSetupTests
├── Talby.Core.ResxAccess.IntegrationTests/   # RawTextConsumerTests + ConsumerProject
├── Talby.Core.ResxAccess.AspectTests/        # existing snapshot tests
└── run.ps1                                 # update paths and test identities if adopted
```

Update the solution, project references, execution commands, and documentation.
Preserve every existing scenario and assertion, nullable/implicit-usings settings,
and `MetalamaEnabled=false` in both ordinary test projects. Full solution execution
must discover and run all three test projects. If the fast/full runner is adopted,
fast must retain UnitTests and AspectTests while excluding IntegrationTests, and
full must retain all tests; update its inventory for the renamed namespaces.
Verify each ordinary test project independently and the complete Release suite.

[Task 04](issues/04-separate-unit-and-integration-test-projects.md) contains the
implementation acceptance criteria. This task changes project organization;
the performance measurement protocol below applies to experiments 01–03.

## Shared measurement protocol

1. Record baseline and candidate revisions, working-tree changes, machine, SDK, configuration, commands, logging, and test parallelism. Use the same environment and NuGet cache. Run the two revisions sequentially, never concurrently.
2. Restore and build each revision before test-only measurements. Exclude initial restore from the timed samples and report this explicitly. Use one untimed warm-up per revision and measurement mode.
3. Collect at least five paired samples per relevant mode, alternating which revision runs first. Measure external wall time with a stopwatch, not the sum of test durations. Record exit codes and test discovery/execution counts for every sample.
4. Always measure full test-only execution and warm build + full test execution. Also measure fast execution when available. Time build and test separately and report their combined wall time. Include one additional before/after pair following a Release clean; label it a clean-output measurement, not a cold-cache benchmark.
5. Report all raw timings, median, minimum, maximum, absolute delta, and percentage delta using `(baseline median - candidate median) / baseline median * 100`. Keep paired deltas visible. If variance obscures the gain, report the conclusion as inconclusive; do not select only favorable samples or promise a fixed percentage.
6. Record temporary SDK build counts, fixture project builds, test process overhead, and scenario coverage separately. A smaller discovered test count or lower test-only time does not prove equivalent protection or a faster build + test cycle.

Reference commands from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

For warm build + test samples, time the last two commands together as well as separately. For the additional clean-output pair, run `dotnet clean Talby.Core.ResxAccess.slnx --configuration Release` before the timed build + test. Rebuild after any code or fixture change before using `--no-build`. Keep verbosity and result logging identical between revisions.

Store reviewable reports as `results/01-execution.md`, `results/02-fixtures.md`, and `results/03-aspect-logic.md` under this feature directory when implementing each issue. Include the raw timing table or CSV in the change set; keep generated TRX, logs, binaries, and temporary projects in ignored `test-results/` directories.

## Shared correctness and approval criteria

- Map every existing scenario/assertion to its retained or replacement test. Identify moved, removed, deferred, and newly covered behavior, including changed isolation and failure reporting. Test counts alone are insufficient.
- Run the complete Release build and suite successfully. Use the issue-specific temporary negative controls to demonstrate that representative regressions still fail; revert the controls before final checks and measurements.
- Preserve the public attribute API, diagnostics, Raw Text behavior, SDK resource-map integration, `MetalamaEnabled=false` in the ordinary test project, and `MetalamaRemoveCompileTimeOnlyCode=false` in the library.
- Preserve the existing ADRs. Localized Resource consistency, formatting features, IDE integration, and packaging expansion are outside these experiments' implementation scope.
- Deliver a diff, reproducible commands, performance report, coverage mapping, known gaps, and an adopt/reject/inconclusive recommendation. The user decides whether the performance gain justifies the regression-protection tradeoff; passing tests never constitutes automatic approval.
- Do not merge, adopt the candidate as policy, or build another experiment on it before that review. Update test classification instructions only in a later explicitly requested task.
