# AspectTests performance and snapshot consolidation

Date: 2026-10-04. Baseline: `c52b9c326a5a68ce8db971f5e1fe62c2dc4ad44a`.
Environment: Windows, Intel Core i9-10900K (10 cores / 20 logical processors),
.NET SDK 10.0.401, runtime 10.0.12, Metalama 2026.1.28, Release, PowerShell 7.

## Outcome

The approved target is met: median external AspectTests command time decreases
from 11.110 s to 8.751 s, saving 2.359 s (21.2%). Full testing improves 24.4%,
and warm build + full improves 19.3% by median complete-cycle wall time. Every
paired candidate cycle is faster than its corresponding baseline observation.
The final clean-output Release build has zero warnings/errors and full verifies
all 43 current identities. No production code changed.

All 24 original diagnostic expectations remain checked. Five temporary negative
controls each prove that an omitted diagnostic fails while the other 23 remain.
The new repository testing criterion is linked from AGENTS.md and README.md.

## Acceptance results

Five pairs per mode; medians and observed min–max ranges of external cycle wall
time. WarmFull includes Release solution build and full; the other modes use
already-built outputs. Reductions below compare medians, not averaged percentages.

| Mode | Baseline median (range) | Candidate median (range) | Saved | Reduction |
| --- | ---: | ---: | ---: | ---: |
| AspectTests | 11.111 s (11.032–11.636) | 8.752 s (8.655–8.901) | 2.359 s | 21.2% |
| Fast | 13.821 s (13.649–14.146) | 11.289 s (11.019–11.470) | 2.532 s | 18.3% |
| Full | 12.397 s (12.285–14.980) | 9.372 s (9.280–10.223) | 3.024 s | 24.4% |
| Warm build + full | 15.328 s (14.494–15.806) | 12.364 s (11.488–14.900) | 2.964 s | 19.3% |

The AspectTests command timer alone is 11.110 → 8.751 s; the complete harness
cycle adds less than a millisecond around that command. WarmFull component
medians are 2.268 → 2.190 s for build and 12.867 → 10.237 s for testing.
Component medians are not summed to obtain the complete-cycle median.

| Clean-output observation | Build | Full test command | Complete cycle | Passed |
| --- | ---: | ---: | ---: | ---: |
| Baseline | 2.379 s | 12.719 s | 15.099 s | 47 |
| Candidate | 2.386 s | 10.655 s | 13.042 s | 43 |

This single clean-output pair saves 2.056 s (13.6%) overall. Its build alone is
0.007 s slower; one pair does not establish a build regression or a general
clean-build percentage. WarmFull paired reductions range from 5.7% to 20.7%;
its candidate range is wider than the baseline range. Full paired reductions
range from 17.4% to 37.3%, including one slower baseline sample. AspectTests
paired reductions range from 19.9% to 24.2%; fast ranges from 17.0% to 22.1%.
No sample was removed, and no reproducible complete-cycle regression appeared.
These results describe this machine and inventory, not a cross-machine guarantee.
Fast and full are separate series with different project scheduling, so their
times should not be subtracted to estimate IntegrationTests cost.

[Raw timings](timings.csv) retain all 50 observations: 40 paired, eight excluded
warm-ups and two separately reported clean-output runs.
[Summary](summary.csv) records each metric's median/range;
[paired deltas](paired-deltas.csv) records order, absolute and percentage savings.
Every observation has successful exits and the expected passed inventory.
Original 12 / 31 / 47 identities and candidate 8 / 27 / 43 identities were
verified by the harness and existing runner. Detailed logs/TRX files remain
under ignored `test-results/aspect-consolidation/measurements/` and each variant's
`test-results/execution/` tree.

## Slowest original snapshots

The initial ranking used three isolated fresh-process runs per snapshot, with
rotated/reversed order. These medians include initialization and contention;
they do not measure only the production aspect's synchronous work. The largest
TRX start/end intervals were:

| Snapshot | Median test interval | Range | Median external command wall time |
| --- | ---: | ---: | ---: |
| ResourceKeyIdentifiers | 5.378 s | 5.232–6.203 s | 7.045 s |
| IndexedPlaceholderGeneration | 5.182 s | 5.172–5.233 s | 6.797 s |
| RawTextGeneration | 5.068 s | 5.044–5.373 s | 6.733 s |
| IndexedPlaceholderDiagnostics | 4.909 s | 4.907–5.716 s | 6.550 s |
| LocalizedResourceDiagnostics | 4.829 s | 4.805–5.237 s | 6.478 s |

The other four helper-including snapshots were between 4.649 and 4.822 s;
the three simpler snapshots were between 3.086 and 3.113 s. See the complete
[initial ranking](initial-ranking.csv), [36 isolated samples](initial-isolated-samples.csv)
and [three original suite samples](initial-suite-samples.csv). All passed.
The original full AspectTests command had an 11.069 s median in that initial
series. Those samples are background, not the later acceptance comparisons.

Parallel snapshot intervals overlap and include shared setup/waiting, so they
cannot be summed or treated as independent contributions to suite duration.
The upstream executor publishes its passing result before recording elapsed
metrics; the observed TRX `duration=1 ms` is unsuitable for this ranking.
[Pinned TestExecutor source](https://github.com/metalama/Metalama/blob/a285a2d661058d7717be027a522b8538d8c70373/Metalama.Framework/src/Metalama.Testing.AspectTesting/XunitFramework/TestExecutor.cs).

## Diagnosis and chosen change

Independent snapshots create separate Roslyn projects, compile the included
helper source, load compile-time assemblies/metadata, and run Metalama's
pipeline and output checks. Nine original snapshots include the same four
production helpers and deterministic adapter.
[Pinned BaseTestRunner source](https://github.com/metalama/Metalama/blob/a285a2d661058d7717be027a522b8538d8c70373/Metalama.Framework/src/Metalama.Testing.AspectTesting/BaseTestRunner.cs),
[AspectTestRunner source](https://github.com/metalama/Metalama/blob/a285a2d661058d7717be027a522b8538d8c70373/Metalama.Framework/src/Metalama.Testing.AspectTesting/AspectTestRunner.cs).

Controlled local probes support reducing repeated compiler work:

- Sequential execution took 19.059 s compared with a contemporary original
  10.665 s test-run interval. The runner already has concurrency; retain it.
- In a sequential process, UnavailableProjectContext and InvalidPaths took
  0.487 s and 0.414 s after initialization, compared with about 3.1 s in isolated
  processes. Process startup and shared initialization are substantial.
- Instrumented adapter setup/cleanup took milliseconds. Most synchronous
  aspect `Build` calls took about 4–15 ms; the first identifier-generation call
  took 135 ms. XML parsing and temporary-file cleanup are secondary here.
- An experimental precompiled-helper variant took a 6.885 s median test-run
  interval against an instrumented included-helper control of 10.831 s. It
  required experimental public compile-time access, so it was not adopted.
- A diagnostic batching probe took 7.539 s against that control. Its smaller
  source-only change motivated the approved implementation; final percentages
  come from the actual uninstrumented candidate comparisons below.

These are elapsed-time observations, not CPU-time percentages. A partial
thread-time trace included Roslyn compilation, loading and waits, but cannot
establish their percentages of total CPU cost. Probe gains are not additive.
The ignored detailed diagnosis remains at
`test-results/aspect-diagnosis/20261004/report.md` in this workspace.

The implementation groups five compatible diagnostic snapshots into
`ResourceValidationDiagnostics`, including the production helpers once. It
retains all original declarations, 24 exact diagnostic lines and five labeled
groups. Production source, API, dependencies, adapter and concurrency remain
unchanged. Four absorbed snapshot identities are removed from the existing
runner inventory: AspectTests 12 → 8, fast 31 → 27, full 47 → 43. UnitTests
(19) and IntegrationTests (16) are unchanged.

## Assertion preservation and negative controls

[Coverage mapping](coverage.csv) contains each original snapshot, retained
target, diagnostic code and complete message. All 24 lines match the originals
in group order; each original input declaration is retained. The other eight
`.t.cs`/`.i.cs` baseline files are unchanged after newline normalization:
InvalidPaths, UnavailableProjectContext, UnsupportedTargets, RawTextGeneration,
ResourceKeyIdentifiers, KeywordResourceKey (including DesignTime introduced
code), and IndexedPlaceholderGeneration.

| Original snapshot / retained group | Cases | Negative control target | Temporary input correction |
| --- | ---: | --- | --- |
| ResourceValidationDiagnostics / structure and embedding | 6 | InvalidRoot | Replace `<resources />` with `<root />`. |
| ResourceEntryValidationDiagnostics / entries | 6 | NonTextReferenceEntry | Change the entry type from Int32 to String. |
| LocalizedResourceDiagnostics / Resource Keys | 4 | CaseMismatchedKey | Match the Reference Resource Key's casing. |
| ExpectedCultureDiagnostics / Expected Culture | 4 | MissingExpectedCulture | Remove the absent `fr` Expected Culture. |
| IndexedPlaceholderDiagnostics / Placeholder Contract | 4 | MalformedReference | Correct `{0,}` to `{0}`. |

Each temporary control rebuilt successfully, failed the filtered snapshot with
exit code 1, and produced exactly the other 23 original expectations. Thus a
missing diagnostic is detected even while unrelated errors remain. All changes
were reverted; the rebuilt eight-snapshot suite passed.
[Control results](negative-controls.csv). Detailed scripts, logs, actual output
and TRX files are under ignored `test-results/aspect-consolidation/`.

The tradeoff is shared compilation and failure granularity: the five original
categories can no longer be filtered independently. Named targets and labeled
groups still identify each failing expectation. Distinct Resource Sets and
unique GUID temporary directories remain provided by the existing adapter.
Generation, DesignTime and SDK/runtime boundaries retain their existing checks.

## Acceptance measurement method

The baseline is a `git archive` copy of the recorded commit, restored and built
in a separate output tree. Both variants share the same machine, SDK, package
cache, configuration, verbosity and default parallelism. Restore is excluded.
Test-only commands use current Release outputs and `--no-build --no-restore`.
No builds or tests from unrelated review work run during this series.

Each of four modes has one excluded warm-up per variant, then five sequential
baseline/candidate pairs. Odd pairs run baseline first; even pairs run candidate
first. Each command starts a fresh process. The warm-up prepares filesystem,
package and build caches; it does not share a test process with later samples.
External stopwatches measure build/test commands separately and the complete
cycle. Log writing and setup cause a small difference between total time and
the sum of component command times. TRX parsing and CSV publication are excluded.
The existing runner verifies exact fast/full identities, and the harness
verifies every AspectTests identity and all exit codes.

An additional pair runs after `dotnet clean --configuration Release` in each
output tree. Cleaning is outside the timer; build and full testing are timed.
This is a clean-output observation, not a cold operating-system/NuGet-cache
benchmark. It is reported separately and does not enter the five-pair medians.

Reproduce after creating separate baseline and candidate trees and restoring /
building both solutions:

```powershell
pwsh -NoProfile -File .scratch/aspect-test-performance/results/measure.ps1 `
  -BaselineRoot <baseline-root> -CandidateRoot <candidate-root> `
  -ResultsDirectory <new-empty-results-directory>
```

The script resumes samples from an existing results CSV; use a new empty
directory for a new measurement. Baseline must retain 12 / 31 / 47 identities;
candidate must have 8 / 27 / 43. Aspect mode invokes the project directly with
TRX logging. Fast/full use `tests/run.ps1`; WarmFull builds the solution with
`--no-restore` before full. Raw logs/TRX files remain ignored.

## Persistent testing criterion

[`docs/agents/testing.md`](../../../docs/agents/testing.md), linked from
AGENTS.md and README.md, specifies boundary selection, compatible grouping,
per-case diagnostic assertions, preservation of generated-code/runtime checks,
negative controls, inventory updates and reproducible performance measurements.
The 20% AspectTests median reduction target applies to this approved change.

## Review and final verification

A read-only reviewer assessed the implementation, coverage/control artifacts,
benchmark harness and policy against the approved specification and baseline.
No material correctness, policy or scope findings were reported. The final
measurement tables were completed after that static review and checked against
the 50 raw observations by the implementing agent.

Final verification confirmed all 24 expectation lines, unchanged original input
declarations and eight other baseline files. `git diff --check` passes. The final
clean-output build and full run verify current outputs and all 43 identities.
The [candidate source hashes](candidate-source-hashes.csv) identify the grouped
input, expected output and runner inventory used by this measurement.
The work remains uncommitted on the existing working branch; no publication or
merge was performed.
