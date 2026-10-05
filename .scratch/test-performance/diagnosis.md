# Test execution performance diagnosis

Measured on 2026-10-02, Windows x64, .NET SDK 10.0.401, runtime 10.0.12,
Metalama.Framework 2026.1.28 and Metalama.Compiler 2026.1.18.
Initial scope: diagnosis only; production code and tests were not changed during
the measurements above. The subsequent test refactor is recorded below.

## Reproduction

```powershell
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore --logger 'trx;LogFilePrefix=diagnosis' --results-directory .scratch/test-performance/test-results --verbosity normal
```

Two complete executions took **44.69 s** and **54.91 s** of wall time.
All 11 tests passed in both runs. The second run followed an explicit Release
solution build, which passed with zero warnings/errors and took 2.08 s separately.
This excludes the outer solution build and restore as the explanation for the
reported test duration. It does not exclude builds launched inside the tests.

| Test in RawTextConsumerTests | Consumer builds | First run | Second run |
| --- | ---: | ---: | ---: |
| CanCompileAndInvokeRawTextOnNonPartialStaticClass | 1 | 3.58 s | 3.14 s |
| CultureNamedNeutralFilePreservesRepresentableKeysAndBlankText | 1 | 2.82 s | 5.76 s |
| UsesSdkAssociatedTypeNameAndIndependentResourceCulture | 1 | 2.72 s | 5.85 s |
| ReportsUnsupportedEmbeddingMetadata | 3 | 8.08 s | 10.53 s |
| ReportsMalformedReferenceResourceWithoutAspectCrash | 1 | 2.43 s | 3.00 s |
| DescribesMissingRuntimeManifestAndResourceKey | 2 | 5.29 s | 5.54 s |
| ReportsInvalidReferenceInputsAndTargetsInRealBuilds | 7 | 18.03 s | 19.14 s |
| Total | 16 | 42.95 s | 52.97 s |

TRX timestamps confirm these seven tests execute sequentially. The foreach
loops also await each build before starting the next case. The two diagnostic
tests with 10 builds consume 26.11 s in the first run, about 61% of this class.

## Confirmed causes

1. **Sixteen independent SDK builds dominate the suite.**
   `ConsumerProject.DirectoryPath` creates a new GUID directory for every case,
   and `Dispose` deletes it. Every case starts without consumer build outputs or
   restore assets. `Build` launches a new `dotnet build` process. There are also
   five assembly invocations. The outer `--no-build --no-restore` flags are not
   forwarded to these child commands.

2. **Every consumer build performs an implicit restore.**
   `ConsumerProject.Build` supplies neither `--no-restore` nor an existing assets
   file. A representative fresh consumer build took 2.72 s; a separate fresh,
   pre-restored equivalent took 2.08 s for its build alone. RestoreTask took
   0.36 s in the combined build, with additional restore graph work. Explicitly
   restoring the second project took 1.17 s including process startup, so
   splitting restore and build into two processes is not itself an improvement.
   Adding `--no-restore` alone would break genuinely fresh consumers.

3. **Metalama adds a repeated fixed initialization cost.**
   Its installed `Metalama.Compiler.targets` defines
   `MetalamaSetEnvironmentVariable` through `RoslynCodeTaskFactory`.
   `MetalamaSetDotnetRootHint` took 0.535-0.585 s per measured build process;
   the environment-variable task execution itself took only 1-2 ms. Diagnostic
   timestamps locate the delay in the initial task factory initialization.
   At 16 processes, this represents roughly 8.6-9.4 s if the measured cost holds
   across the suite; this is an extrapolation, not a full-suite profiler total.

4. **The referenced library recompiles even with no source changes.**
   Installed `Metalama.Framework.targets` calls `Touch` with
   `AlwaysCreate=true` on `MetalamaBuild.touch` before compilation and registers
   that file as an `AdditionalFiles` input. MSBuild diagnostic output explicitly
   reports this input is newer than the library PDB and reruns `CoreCompile`.
   The rebuilt reference also invalidates consumer resource generation and
   compilation. A repeated, unchanged consumer build with `--no-restore` still
   took 1.88 s. Probes with `BuildProjectReferences=false` took 1.72-1.80 s,
   compared with 2.02-2.40 s with reference builds enabled in alternating runs.
   This reduces work but still leaves consumer compilation and fixed startup.

## Costs that do not explain the delay

- Discovery started within 0.33 s of each adapter starting.
- MetalamaSetupTests took 0.70 s and 0.83 s and overlapped consumer tests.
- The complete AspectTests runner took 4.46 s and 4.78 s and overlapped the
  consumer runner. Its reported 1 ms per test is not the total pipeline cost;
  the TRX start/end timestamps include substantially more work.
- A representative consumer assembly invocation took 0.057 s.
- `TalbyWriteResourceMap` took 0-2 ms in the profiled builds; CoreCompile took
  0.27-0.35 s combined for library and consumer. These target summaries are
  nested and must not be added as if every entry were disjoint.

The additional approximately 10 s in the second suite run is observed timing
variation. These probes do not identify its precise system-level cause. The
16-build architecture already accounts for the original greater-than-40 s
symptom without assuming a network outage or slow resource lookup.

## Recommended order of changes

1. Reduce the number of full consumer builds. Batch compatible diagnostic cases
   into one consumer compilation and retain distinct diagnostic assertions.
   Consider avoiding duplicate real-build checks for invalid paths and targets
   already covered by AspectTests. Preserve real builds for SDK metadata,
   resource embedding, and runtime behavior: the snapshot runner does not
   forward the project path/resource map needed for those checks.
2. Build the library once and reuse its outputs in consumer builds, with an
   explicit prerequisite preventing stale outputs. The measured
   `BuildProjectReferences=false` probe demonstrates the smaller, secondary gain.
3. Reuse restore assets only across compatible consumer project graphs, keeping
   resource files and build outputs isolated. A shared cache is unnecessary
   unless reducing the build count leaves restore as a material bottleneck.
4. Investigate the installed Metalama touch-file incremental-build behavior and
   task factory cost upstream if they remain significant. Do not disable
   Metalama for consumers: that would stop exercising aspect generation.

Parallelizing the existing tests first would allow concurrent builds against
the same library bin/obj directories. Resolve the shared reference build work
before introducing concurrency. Converting foreach loops to Theory cases alone
does not remove any of the 16 builds.

## Evidence

Raw TRX files, build performance summaries, an MSBuild diagnostic log, and a
small standalone consumer probe remain under the Git-ignored
`.scratch/test-performance/test-results/` directory. The relevant source is
`tests/Talby.Core.ResxAccess.Tests/ConsumerProject.cs` and
`tests/Talby.Core.ResxAccess.Tests/RawTextConsumerTests.cs`.

## Follow-up: remove duplicate consumer builds

The authorized refactor changes three test files. Production code, project
configuration, ConsumerProject, and agent instructions remain unchanged.

- Add WhitespacePath to InvalidPaths and its reviewed diagnostic snapshot.
- Remove five cases from the consumer integration loop and rename its test to
  ReportsInvalidReferenceResourcesInRealBuilds.
- Keep the missing-file and Localized Resource cases in the real-build loop.
  Keep all other consumer tests and MetalamaSetupTests.

| Removed consumer build case | Remaining aspect assertion |
| --- | --- |
| null Reference Resource path | InvalidPaths.NullPath: TRESX001 and message |
| whitespace Reference Resource path | InvalidPaths.WhitespacePath: TRESX001 and message (added) |
| .txt extension | InvalidPaths.WrongExtension: TRESX001 and message |
| non-static target | UnsupportedTargets.InstanceTarget: TRESX002 and message |
| generic target | UnsupportedTargets.GenericTarget: TRESX002 and message |

Before updating the expected snapshot, the InvalidPaths test failed explicitly
on the additional WhitespacePath diagnostic. After reviewing and adding that
diagnostic to the snapshot, the focused test passed. The five integration cases
were then removed, and the Release solution build passed with zero warnings
and errors.

### Performance comparison

The same full-solution test command was measured immediately before the refactor
and twice after it, using Release, --no-build, --no-restore, normal verbosity,
and a TRX logger. The explicit solution builds were outside the timed intervals.
The only logger difference was the output filename prefix.

| Measurement | Full command wall time | Consumer diagnostic loop | Consumer builds | Passed tests |
| --- | ---: | ---: | ---: | ---: |
| Before refactor | 53.80 s | 19.36 s (7 cases) | 16 | 11/11 |
| After, run 1 | 34.11 s | 5.68 s (2 cases) | 11 | 11/11 |
| After, run 2 | 36.68 s | 5.78 s (2 cases) | 11 | 11/11 |

The two post-refactor runs average 35.40 s: an observed reduction of 18.40 s,
or 34.2%, against the immediate pre-refactor run. The changed diagnostic loop
accounts for approximately 13.63 s of reduction. The rest of the observed
difference includes variation in unchanged tests and should not be attributed
entirely to the refactor. The earlier baseline range of 44.69-54.91 s also
demonstrates environmental variation. These runs establish a measured benefit,
not a guarantee that every future execution will finish below 40 s.

### Review boundary

All five validation conditions still have aspect assertions. The complete SDK
build failure path for those five exact inputs is no longer checked. Passing
the same 11 discovered tests does not by itself prove equivalent regression
protection: the removed cases were iterations inside one Fact, and whitespace
is an additional assertion inside an existing aspect test.

This section records the change for the user's subsequent regression-protection
analysis. No test classification policy has been introduced.

Detailed evidence is in the ignored test-results directory: before-refactor and
after-refactor-1/2 TRX files and refactor-timings.csv.

## Follow-up: group compatible consumer scenarios

Baseline commit: b1f28ed (test: reduce redundant consumer build cases).
The refactor changes only RawTextConsumerTests.cs, plus this report.

- CanCompileAndInvokeIndependentResourceSets builds one consumer containing
  three independent Resource Sets and aspect targets. It retains the API,
  accessibility, invalid identifier, SDK associated type naming, culture
  selection/fallback, null culture, culture-named Reference Resource, Unicode,
  keyword, empty text, whitespace, and Raw Text assertions from the three
  previous tests. Basic, Associated, and Edge use distinct Translations so an
  incorrect Resource Set cannot satisfy the same expected text. CurrentCulture
  and CurrentUICulture are restored between scenarios and in finally.
- ReportsEachInvalidResourceAndEmbeddingInOneBuild builds one consumer with five
  invalid targets in separate source files. Each assertion requires its target
  filename, diagnostic code, and complete expected message on the same output
  line. All three embedding metadata errors and both invalid Reference Resource
  errors must appear; a generic unsuccessful build is insufficient.
- The malformed XML test and the two runtime failure builds remain separate and
  unchanged. ConsumerProject, production code, project settings, AspectTests,
  and agent instructions remain unchanged.

### Assertion checks

Both grouped tests passed initially. Two temporary input mutations then produced
two expected test failures:

1. Pointing the Basic target at the Associated Reference Resource compiled and
   invoked successfully, but failed the expected Translation output assertion.
2. Removing the LogicalName override left the other four invalid cases in place
   and the build still failed. The grouped diagnostic assertion nevertheless
   failed because the LogicalNameTarget diagnostic was absent.

Both mutations were reverted before the final Release build and full-suite
measurements. No production mutation or additional test dependency was needed.

### Measured results

Measurements use the same Release full-solution test command, --no-build,
--no-restore, normal verbosity, and TRX logging. Explicit solution builds are
excluded from wall time. Both builds passed with zero warnings/errors.

| Scenario group | Before grouping | After run 1 | After run 2 | Builds before -> after |
| --- | ---: | ---: | ---: | ---: |
| Successful generation and lookup | 8.61 s | 3.09 s | 3.73 s | 3 -> 1 |
| Invalid resources and embedding | 14.38 s | 2.72 s | 3.31 s | 5 -> 1 |
| Malformed XML (unchanged) | 2.44 s | 2.43 s | 10.25 s | 1 -> 1 |
| Runtime failures (unchanged) | 5.99 s | 5.88 s | 7.72 s | 2 -> 2 |
| Full command wall time | 33.12 s | 16.02 s | 26.81 s | 11 -> 5 |
| Discovered tests passed | 11/11 | 8/8 | 8/8 | |

The grouped scenarios together decreased from 22.99 s to 5.81-7.04 s. The full
command averages 21.41 s after grouping, an observed reduction of 35.3% versus
the immediate 33.12 s baseline. The unchanged malformed XML and runtime failure
tests explain most of the difference between the two post-grouping runs; the
precise source of that timing variation was not profiled in this refactor.

Three fewer Facts are discovered because three successful Facts became one and
two diagnostic Facts became one. Their scenario assertions remain in the two
grouped Facts. Grouping reduces independent failure reporting and project
isolation; the negative controls demonstrate detection of two masking risks,
not a proof of equivalence for every possible future regression.

Evidence: before-grouping and after-grouping-1/2 TRX files,
grouping-negative-controls TRX, and grouping-timings.csv under the ignored
test-results directory. No test classification policy has been introduced.

## Follow-up: reuse and share consumer builds (2026-10-04)

The current suite contains 47 tests: 19 UnitTests, 16 IntegrationTests, and 12
AspectTests. Before this change, IntegrationTests started nine temporary SDK
consumer builds, including seven compatible diagnostic groups, malformed XML,
and the successful canonical/lowercase Localized Resource culture scenario.
Measurements used Windows x64, SDK 10.0.401, runtime 10.0.12, and the existing
Metalama package versions. Production code and dependencies are unchanged.

### Changes and coverage

- `ConsumerProject.Build()` passes `BuildProjectReferences=false` to reuse the
  library's current Release outputs. MSBuild documents this property in its
  [common project properties reference](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-properties).
  The solution must be restored and built in Release after library or fixture
  changes. Child builds no longer check whether the library is up to date.
- Canonical and lowercase culture scenarios moved to ConsumerFixture, preserving
  two independent Resource Sets, `ES-MX` ExpectedCultures, `es-MX`/`es-mx`
  satellite suffixes, and the distinct `Canonical`/`Lowercase` runtime outputs.
- An xUnit collection fixture caches one build across the seven diagnostic
  tests. Input setup stays beside each test's assertions. Indexed Placeholder
  groups use distinct `Malformed` and `Changed` target/resource names to prevent
  collisions. Their assertions now also require the target filename on the same
  line as the diagnostic code, resource path, Resource Key, and message fragment.
  Existing raw/localized diagnostic assertions are retained.
- Malformed XML stays in its own consumer project: SDK resource generation emits
  MSB3103 before aspects execute. The two consumer builds start together through
  `Task.WhenAll`, limiting concurrency to exactly two isolated projects.
- `RestoreRecursive=false` limits restore to the consumer. The installed SDK's
  `NuGet.targets` selects only top-level restore entries for this value, while
  still evaluating dependency metadata. Library bin/Release and obj file paths,
  sizes, and modification timestamps were compared before/after three concurrent
  integration runs and both final full runs; none changed.
- Build setup is lazy and temporary projects are disposed after both processes
  finish. Runtime-only filters do not trigger consumer builds. A diagnostic
  filter starts both builds; the first diagnostic test includes shared setup in
  its reported duration. Per-test timings no longer represent independent builds.

Every existing test identity remains unchanged. `tests/run.ps1` therefore keeps
its 31 fast / 47 full inventory, verified by both final full runs.

### Measurements

Solution restore completed successfully before the baseline. The full command
is `pwsh -NoProfile -File tests/run.ps1 -Mode full`; the preceding build command
is `dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore`.
Stopwatch wall times include command startup and, for full, inventory validation.
All solution builds passed with zero warnings/errors.

| Stage | Temporary builds | Solution build | Full wall time | Build + full | Passed |
| --- | ---: | ---: | ---: | ---: | ---: |
| Original baseline | 9 | 7.60 s | 31.48 s | 39.08 s | 47/47 |
| Reference reuse only | 9 | 2.78 s | 28.55 s | 31.33 s | 47/47 |
| Final, run 1 | 2 | 2.16 s | 14.13 s | 16.29 s | 47/47 |
| Final, run 2 | 2 | 2.14 s | 12.20 s | 14.34 s | 47/47 |

The final full wall-time mean is 13.17 s, 58.2% below the single original
baseline. Build + full also decreases, so the observed savings are not limited
to moving work into ConsumerFixture. The original solution build was slower
than subsequent builds; its difference includes warm-up/incremental effects
and must not be attributed entirely to this refactor. These are local samples,
not a guaranteed timing bound or a controlled benchmark.

The final IntegrationTests runner reported 5.23 s and 5.01 s, versus 27.80 s
before the change. AspectTests reported 10.75 s and 10.55 s, versus 11.21 s
before; AspectTests now dominate the full suite. Separate integration command
wall times were 6.03 s with grouped serial builds and consumer-only restore,
then 4.65 s, 5.10 s, and 4.70 s with two concurrent builds. All three concurrent
runs passed 16/16 tests.

### Negative controls and limits

Two temporary mutations were built and tested individually, then restored:

1. Removing only the LogicalName override left the other diagnostic failures
   present. `ReportsEachInvalidResourceAndEmbeddingInOneBuild` failed because
   its required LogicalNameTarget diagnostic was absent.
2. Pointing CanonicalTexts at the Lowercase Resource Set built successfully but
   failed `CanInvokeCanonicalAndLowercaseLocalizedResourceCultures`: the output
   became `Lowercase`/`Lowercase` instead of `Canonical`/`Lowercase`.

The final Release builds and full runs occurred after restoring both mutations.
Grouping retains individual Facts and assertions but reduces project isolation:
an unexpected failure before aspect execution can affect all seven diagnostic
tests. The filename/code/message assertions detect missing diagnostics even
when the shared build fails for other reasons. The two negative controls check
specific masking risks and do not prove equivalence for every regression.
Concurrent execution was verified on the stated Windows/SDK environment.

Final full logs and negative-control logs are under the ignored
`.scratch/test-performance/test-results/` directory. Final TRX inventories are
under `test-results/execution/full-bbe2f733afa542098b07a1737bf9ee8e/` and
`test-results/execution/full-d988bc4118c3451aa9fab0aaf558d008/`.
