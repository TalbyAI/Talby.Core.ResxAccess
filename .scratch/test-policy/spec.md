# Test execution policy experiments

Status: needs-triage

## Goal and review boundary

Evaluate options 2, 3, and 4 from the test-performance discussion as three separate change sets. Each experiment must deliver working code, objective before/after measurements, and an assertion coverage review before the user decides whether to adopt it. These issues specify experiments; they do not authorize implementation in this planning turn or establish a permanent test classification policy.

| Issue | Previous option | Experiment | Main question |
| --- | --- | --- | --- |
| [01](issues/01-separate-fast-and-full-execution.md) | 2 | Separate fast and full execution | How much local feedback time is saved by deferring integration tests? |
| [02](issues/02-precompile-consumer-fixtures.md) | 3 | Precompile consumer fixtures | Does removing builds from runtime tests reduce the complete development cycle? |
| [03](issues/03-share-testable-aspect-logic.md) | 4 | Share testable aspect logic | Can broader fast coverage validate production logic while SDK smoke tests retain the integration boundary? |

Recommended evaluation order: 01, then 02, then 03. Keep each candidate independently reviewable and revertible. Start each from the latest approved baseline; do not include an unapproved candidate in the next change set. Rejecting one experiment does not prevent evaluating the others against the retained baseline.

## Current evidence

The current baseline is commit `62f653d`. The solution discovers eight tests: five ordinary xUnit tests and three AspectTests. Four ordinary tests exercise consumers through five temporary SDK builds: one successful consumer, one grouped diagnostic consumer, one malformed XML consumer, and two runtime failure consumers.

The [previous diagnosis](../test-performance/diagnosis.md) records full test command times of 16.02 s and 26.81 s after grouping. These two historical samples demonstrate variation, not an adequate baseline for the new experiments. Collect fresh measurements for every candidate.

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
