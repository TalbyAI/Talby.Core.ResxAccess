# Integration test performance diagnosis

Measured on 2026-10-05 at commit `7abacfd`, on Windows x64 with .NET SDK
10.0.401, Metalama.Framework 2026.1.28 and Metalama.Compiler 2026.1.18.
Scope: diagnosis only. No production code, test code, assertions, test identities
or execution settings were changed.

## Finding

IntegrationTests take approximately **103 seconds** because they launch many
independent SDK/MSBuild processes and execute all eight test classes sequentially.
The six IncrementalBuild tests and the DesignTime test account for **80.4–82.8%**
of measured suite wall time. Runtime resource access is a small part of the cost.

The existing precompiled ConsumerFixture and shared diagnostic builds still work.
The earlier approximately five-second integration results in
[the previous diagnosis](../test-performance/diagnosis.md#follow-up-reuse-and-share-consumer-builds-2026-10-04)
describe a smaller, 16-test suite. The current suite contains 36 tests, including
the new incremental resource mutation histories, DesignTime inputs and consumer
documentation checks. Those historical results are not a controlled baseline
for the current source tree.

## Measurements

Restore and Release solution build completed before the timed tests, taking
0.96 s and 2.34 s respectively. Runs used identical inputs, configuration,
logging and parallelism, and were executed sequentially on the same machine.
The solution build had the two intentional ConsumerFixture identifier warnings.

| Run | External wall time | Passed | Overlapping test intervals |
| --- | ---: | ---: | ---: |
| Warm-up, excluded from median | 95.15 s | 36/36 | 0 |
| Sample 1 | 102.71 s | 36/36 | 0 |
| Sample 2 | 105.30 s | 36/36 | 0 |
| Sample 3 | 103.15 s | 36/36 | 0 |

Median: **103.15 s**; measured range: **102.71–105.30 s**. Every run has exactly
the same passed-test inventory. The warm-up also reproduces the slow behavior.

| Test class | Tests | Median sum of test durations | Range |
| --- | ---: | ---: | ---: |
| IncrementalBuildConsumerTests | 6 | 71.48 s | 68.98–72.72 s |
| DesignTimeResourceConsumerTests | 1 | 13.53 s | 13.28–13.98 s |
| ResourceKeyIdentifierConsumerTests | 5 | 11.59 s | 10.73–12.46 s |
| DocumentationConsumerTests | 1 | 2.71 s | 2.53–2.99 s |
| NamedPlaceholderConsumerTests | 7 | 2.61 s | 2.50–3.06 s |
| RawTextConsumerTests | 4 | 0.26 s | 0.25–0.28 s |
| LocalizedResourceConsumerTests | 6 | 0.17 s | 0.17–0.17 s |
| IndexedPlaceholderConsumerTests | 6 | 0.01 s | 0.01–0.01 s |

TRX start/end timestamps verify that these test intervals do not overlap, so
summing test durations within each run is valid here. Medians across classes
should not be added to reconstruct a particular run. Lazy diagnostic setup is
charged to whichever diagnostic test requests it first; in these runs that is
`RejectsUnsupportedIdentifierPolicies`. Its duration is not the standalone cost
of checking that policy. The thirteen diagnostic tests share two builds.

## Confirmed causes

1. **Many child commands remain despite the outer `--no-build --no-restore`.**
   [ConsumerProject.Build](../../tests/Talby.Core.ResxAccess.IntegrationTests/ConsumerProject.cs)
   explicitly launches `dotnet build` with implicit restore. Those outer flags
   apply to the test project; they do not propagate to child processes. The
   current source contains **37 consumer builds, seven DesignTime builds and
   28 child runtime invocations**, or 72 child `dotnet` commands per suite.
   [The inventory](build-inventory.csv) records the call-site breakdown.

2. **IncrementalBuild histories are sequential and require repeated builds.**
   Their six histories contain 4, 5, 3, 4, 7 and 6 builds respectively: **29** in
   total. Each history needs ordered resource edits, additions, removals,
   diagnostic checks and successful runtime checks. Combining unrelated
   assertions or dropping intermediate builds would change regression coverage.
   All eight classes use `[Collection("SDK consumer builds")]`, serializing
   independent histories as well. Only the two builds inside the shared
   ConsumerDiagnosticsFixture run concurrently through `Task.WhenAll`.

3. **Each MSBuild process pays significant fixed setup cost.**
   A representative consumer copied the same project reference, resource-map
   targets and `IsolateResourceInputs` target used by IncrementalBuild tests.
   Diagnostic performance summaries show the following nested target/task costs:

   | Probe | MetalamaSetDotnetRootHint | CoreCompile | Resource-map target |
   | --- | ---: | ---: | ---: |
   | Unchanged, no restore | 566 ms | 2 ms, skipped | 1 ms |
   | Reference Resource edit | 749 ms | 163 ms | 2 ms |
   | DesignTime | 634 ms | 31 ms, compiler execution disabled | 2 ms |

   The installed Metalama.Compiler target initializes a `RoslynCodeTaskFactory`
   inline task before CoreCompile. The environment-setting task body itself is
   reported as 1 ms in all three profiles. This identifies task-factory setup as
   the substantial cost in that target. It runs even when CoreCompile is skipped
   or `SkipCompilerExecution=true`. DesignTime hashing and dependency generation
   take only 4 ms and 7 ms respectively in the representative profile.
   These target/task timings overlap and must not be added together. These are
   isolated profiles, not a full-suite allocation of every millisecond.

4. **Implicit restore adds avoidable work to repeated builds.**
   Three paired unchanged-consumer probes alternated which restore variant ran
   first. Assembly timestamps stayed unchanged in all six probes.

   | Probe | Median external wall time | Range |
   | --- | ---: | ---: |
   | Unchanged with implicit restore | 2.26 s | 1.99–2.57 s |
   | Unchanged with `--no-restore` | 1.71 s | 1.67–1.77 s |
   | Reference Resource edit with implicit restore | 2.28 s | 2.18–2.40 s |
   | Runtime invocation | 0.058 s | 0.058–0.062 s |

   Paired restore/no-restore differences are 0.80, 0.59 and 0.28 seconds.
   The profiled edited build attributes 290 ms to Restore and 163 ms to
   CoreCompile. Fresh consumers still require an initial restore. There are
   thirteen fresh consumer projects and 24 subsequent builds in the suite;
   no suite-level optimization was implemented or benchmarked.

## Explanations excluded or limited

- **Library rebuilds:** `BuildProjectReferences=false` and
  `RestoreRecursive=false` already reuse library outputs. All 46 files under
  library `bin/Release` and `obj` retained their paths, sizes and modification
  timestamps from before repeated measurements through the completed probes.
- **Runtime lookup:** a separate filter selecting twelve precompiled-fixture
  runtime tests passed 12/12 in **3.07 s**, including test host startup. Direct
  formatting/reflection checks generally take milliseconds.
- **Failed incremental behavior:** unchanged probe builds preserve the consumer
  assembly timestamp, and all six real IncrementalBuild histories pass. Slow
  execution does not itself indicate incorrect resource invalidation.
- **Resource-map overhead:** the isolated target takes 1–2 ms. It does not
  explain the observed minute-long suite.
- **Discovery and host startup:** the runtime-only command bounds their combined
  contribution for that filter. No full test-host CPU profile was collected.
- **System variance:** the warm-up and measured samples vary. These measurements
  do not identify every source of OS scheduling, filesystem or security-scanning
  variance, and they do not establish a guaranteed execution time.

## Recommended next work

1. Investigate bounded concurrency between independent consumer histories while
   preserving sequential operations within each history. Keep culture-sensitive
   runtime checks isolated and retain the shared diagnostic cache. Simply
   enabling xUnit parallelism will not help while all tests share one collection;
   the six IncrementalBuild methods also share one class. The existing read-only
   library reuse removes the previous reason to serialize shared library writes,
   but concurrency still needs verification for the complete suite.
2. Restore each new consumer once, then use `--no-restore` for subsequent builds
   whose project/dependency graph is unchanged. Preserve initial restore and all
   failed-build diagnostic assertions. Do not add `--no-restore` unconditionally.
3. Investigate or report the repeated Metalama inline-task initialization cost
   upstream, particularly its execution during skipped compilation and DesignTime.
   Avoid patching installed packages or disabling Metalama in real consumers.
4. Consider moving stable, successful consumer scenarios into ConsumerFixture
   where their actual SDK contract remains covered. Keep mutation histories,
   compiler diagnostics and compilation of documentation at their real boundary.
   Measure build plus test time so moving work into the solution build is visible.

No speed improvement is claimed. Any execution change should follow the
[repository measurement and assertion-preservation policy](../../docs/agents/testing.md),
including controlled baseline/candidate pairs and full-cycle validation.

## Reproduction and evidence

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore

# Warm-up, then three identical timed IntegrationTests runs.
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore
pwsh -NoProfile -File .scratch/integration-test-performance/measure.ps1 -Mode suite

# Run sequentially after the suite measurements.
pwsh -NoProfile -File .scratch/integration-test-performance/measure.ps1 -Mode probes
```

The [measurement script](measure.ps1) records external Stopwatch times and child
exit codes. Reviewable evidence is retained in [suite-times.csv](suite-times.csv),
[class-summary.csv](class-summary.csv), [class-times.csv](class-times.csv),
[test-times.csv](test-times.csv), [probe-times.csv](probe-times.csv),
[probe-timestamps.csv](probe-timestamps.csv) and [runtime-only.csv](runtime-only.csv).
Raw TRX files, the exact inventory, MSBuild diagnostic logs and library metadata
snapshots remain under ignored `test-results/integration-diagnosis/`.
The probe consumer's path is recorded in `probe-path.txt` in that directory.
It is deliberately retained for inspection and is outside the solution.
