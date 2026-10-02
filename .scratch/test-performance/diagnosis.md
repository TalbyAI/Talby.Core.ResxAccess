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
