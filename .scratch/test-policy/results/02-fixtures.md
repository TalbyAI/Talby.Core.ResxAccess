# Precompiled consumer fixture experiment

Status: approved by the user for adoption on 2026-10-03; integration pending.

Origin: [issue 02](../issues/02-precompile-consumer-fixtures.md), following the
[parent measurement protocol](../spec.md).

## Candidate and baseline

Baseline: `f05e9fa3e7b903b13120e1fdcf8ac11df292ae52`, the synchronized
`main`/`origin/main` after merge of PR #1. It includes experiment 01's fast/full
entry points. This experiment uses that merged baseline without changing those
entry points, their inventory, or the permanent test classification policy.
Experiment 01's historical report remains unchanged.

Candidate: the working tree based on that baseline on
`test/precompile-consumer-fixtures`, subsequently committed with this report.
The staged executable snapshot tree is
`a3ac15ed2f12143706ad64ec904a9324d792945e`; its fixture subtree is
`7ef6e8da17d0412e53ae7c7b6838ee501bc4a33d`. Documentation and results were
added afterward. These Git object identities normalize checkout line endings.

Executable changes:

- Add one real SDK executable, `Talby.Core.ResxAccess.ConsumerFixture`, to the
  solution and reference it from the ordinary test project. Metalama processes
  aspects in the fixture; the ordinary project retains `MetalamaEnabled=false`.
- Move the successful consumer source and Resource Sets into checked-in fixture
  files. Keep `RootNamespace=ConsumerRoot` and the unrelated associated type in
  `Resources/Associated.cs` for SDK implicit `DependentUpon` naming.
- Give the two failure scenarios separate Resource Sets in the same fixture.
  Before `CoreCompile`, remove only `MissingManifest` from compiler resource
  inputs and copy the SDK-generated `Replacement.resources` over `Labels`.
  `SkipUnchangedFiles=true` avoids rewriting that output on unchanged builds.
  Positive Resources and localized satellite assemblies retain SDK embedding.
- Invoke the copied fixture assembly using the existing process helper, with
  no argument for success or `missing-manifest`/`missing-key` for runtime errors.
  Retain three fresh processes, output checks, redirected diagnostics, working
  directory outside the repository, and the two-minute process timeout.
- Keep the two failed-compilation tests and their temporary projects unchanged.
  Add no assembly cache, custom fixture framework, production hook, dependency,
  test identity, or permanent test classification rule.

## Reproducible commands

From the repository root with PowerShell 7 and SDK 10.0.401 or a permitted patch:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet clean Talby.Core.ResxAccess.slnx --configuration Release
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore

# Focused consumer verification.
dotnet test tests/Talby.Core.ResxAccess.Tests/Talby.Core.ResxAccess.Tests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~RawTextConsumerTests

# Complete verification, including all AspectTests.
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
pwsh -NoProfile -File tests/run.ps1 -Mode full

# Existing optional local route; unchanged selection.
pwsh -NoProfile -File tests/run.ps1 -Mode fast
```

Stop after a failed command. Rebuild after fixture source or resource changes;
`--no-build` intentionally exercises the previous build's output. The project
reference supplies the fixture assembly, runtime configuration, dependencies,
and localized satellite assemblies in the test output. A build failure now
fails the prerequisite solution build instead of an individual runtime Fact.

For baseline comparisons, create a detached worktree at the baseline revision,
restore/build it separately, and execute the same commands from its root. Both
revisions use the same global NuGet cache and installed SDK.

## Assertion mapping and isolation

All eight discovered identities remain unchanged. Full selects five ordinary
xUnit tests and three AspectTests; fast retains the setup test and all three
AspectTests and defers all four `RawTextConsumerTests`. The fixture is an
executable, not an additional test host or a source of discovered tests.

| Existing test | Retained assertions and new location | Temporary SDK builds, before -> after |
| --- | --- | ---: |
| `CanCreateAndQueryCompilation` | Unchanged unit test: single compiled type named `Sample`. | 0 -> 0 |
| `InvalidPaths` | Unchanged AspectTest and snapshot: exact TRESX001 messages for null, empty, whitespace, and wrong extension paths. | 0 -> 0 |
| `UnsupportedTargets` | Unchanged AspectTest and snapshot: exact TRESX002 messages for instance and generic targets. | 0 -> 0 |
| `UnavailableProjectContext` | Unchanged AspectTest and snapshot: exact TRESX003 unavailable-project-context message. | 0 -> 0 |
| `CanCompileAndInvokeIndependentResourceSets` | Successful SDK compilation moves to the solution build. `ConsumerFixture/Program.cs` retains the consumer checks; the original Fact retains invocation exit and exact multiline output. Three independent positive Resource Sets, internal `Customer.Api.Texts` identity and public `AssociatedTexts` accessibility, non-partial targets, four public static Basic methods and no Format methods, invalid identifier omission, parameterless/explicit culture, null rejection, CurrentUICulture independent of CurrentCulture, es-MX -> es, fr-CA -> fr, de-DE -> Reference Resource fallback, unrelated associated-type SDK manifest naming, RootNamespace, culture-named neutral `en.resx`, keyword/Unicode Resource Keys, empty/blank text, and placeholder/whitespace-preserving Raw Text remain checked. | 1 -> 0 |
| `ReportsEachInvalidResourceAndEmbeddingInOneBuild` | Unchanged fresh failed SDK build: all five exact target filename/code/message assertions, covering LogicalName, ManifestResourceName, Link, missing Reference Resource, and culture-specific Reference Resource. | 1 -> 1 |
| `ReportsMalformedReferenceResourceWithoutAspectCrash` | Unchanged fresh failed SDK build: nonzero exit, SDK MSB3103, and no LAMA0041 aspect crash. | 1 -> 1 |
| `DescribesMissingRuntimeManifestAndResourceKey` | Successful compilation moves to the solution build. Two fixture invocations retain nonzero-on-unexpected-behavior handling and exact `Descriptive failure` output. Each checks InvalidOperationException with Resource Key `Plain`, its manifest base name, and Resource Culture `de-DE`; missing manifest must retain MissingManifestResourceException as inner exception. | 2 -> 0 |

The positive fixture Resource Keys and Translations are the same as the original
in-memory resources. `Associated.cs` keeps its filename intentionally: matching
the `.resx` basename exercises implicit SDK type association with
`Unrelated.Namespace.ResourceAnchor`. The missing-key base name remains
`ConsumerRoot.Resources.Labels`; the missing-manifest base name changes to
`ConsumerRoot.Resources.MissingManifest` to isolate sabotage in one assembly.
Both error messages still require their exact scenario-specific base name.
The replacement Resource contains only `Other=Other text`, as before.

No runtime assertion is dropped or deferred from full. Per-scenario successful
compilation assertions are now a shared prerequisite build gate. Compile failures
therefore surface earlier, with solution-build diagnostics rather than Fact
output. Runtime assertion failures still include the child process output in the
Fact's assertion message. Both diagnostic tests retain their GUID-named temporary
directories, cleanup, and grouped failure reporting.

Fresh **project/build** isolation changes: the positive and two runtime failure
scenarios share one precompiled fixture, output directory, and SDK configuration.
They no longer prove a fresh restore/build in a new directory for each run.
Fresh **process** isolation remains: each scenario starts its own `dotnet`
process, preventing shared ResourceManager or culture state between scenarios.
The positive scenario keeps its original `try/finally` culture restoration.
Nothing relies on test execution order; all resource sabotage happens at build
time and each failure Resource Set is distinct from the positive Resources.

Existing gaps remain: AspectTests do not supply SDK resource-map context or
execute successful generated lookup. Fast success still does not verify that
boundary. The Translation control establishes this fixture's resource rebuild
behavior, not comprehensive incremental/IDE refresh or packaging coverage.
Localized Resource consistency and formatting are outside this experiment;
production sources, public attribute API, diagnostics, and ADRs are unchanged.

## Clean build and negative controls

A Release clean followed by a solution build succeeded before controls; the
focused consumer run passed 4/4. Controls then used the existing public generated
API and independent literal expectations, without changing production code:

1. Change only `Resources/Basic.resx` from `Basic text` to `Changed Translation`.
   Rebuild the solution without cleaning or changing C# source; SHA-256 checks
   confirmed every fixture C# file stayed unchanged. The positive Fact exits 1:
   expected `Basic text`, actual `Changed Translation`. Restore the original
   resource bytes, rebuild, and the same Fact exits 0. This also verifies copying
   the updated fixture to test output instead of using a stale assembly.
2. Temporarily remove only the `_CoreCompileResourceInputs Remove` entry for
   `MissingManifest` from the fixture project. Rebuild; the runtime failure Fact
   exits 1 with the fixture's `Expected failure` exception because real lookup
   now succeeds. Restore the original project bytes and rebuild.
3. After both controls are reverted, the focused consumer file passes 4/4. The
   final verification below and every measurement use the restored candidate.

Control logs, benchmark harness, temporary projects, build outputs, and TRX files
remain ignored under `test-results/02-fixtures/` or the runner's existing
`test-results/execution/` directory. Only the reviewable raw timings are checked in.

## Measurement protocol and environment

Measured 2026-10-03 on Windows 11 Pro x64 10.0.26200, Intel Core i9-10900K
(10 cores, 20 logical processors), 63.9 GiB RAM, SDK 10.0.401/MSBuild 18.9.11,
runtime 10.0.12, PowerShell 7.6.6, Metalama 2026.1.28, Microsoft.NET.Test.Sdk
17.14.1, xUnit 2.9.3, and adapter 3.1.4. Configuration: Release.

Baseline root: `E:\talby.net\Talby.Core.ResxAccess\test-results\02-fixtures\baseline`.
Candidate root: `E:\talby.net\Talby.Core.ResxAccess`. Both use separate outputs
and the same environment/NuGet cache. Initial solution restore and preparation
builds are excluded. The two remaining temporary consumer restores and builds
remain inside measured full test execution; the baseline includes all five.

Both revisions run sequentially using identical logging and commands:

```powershell
# Test-only: measure each route separately with an external stopwatch.
pwsh -NoProfile -File tests/run.ps1 -Mode full
pwsh -NoProfile -File tests/run.ps1 -Mode fast

# Warm build + test: prepend this and time build, test, and the whole interval.
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity minimal

# Additional clean-output observations: clean outside the timed interval.
dotnet clean Talby.Core.ResxAccess.slnx --configuration Release --verbosity minimal
```

Both runners use `--verbosity normal`, a TRX logger, fresh result directories,
and exact passed-identity validation. Logging/PowerShell startup/validation are
included in external wall time; benchmark TRX parsing happens afterward. Tests
and revisions never run concurrently with another measured route. Unchanged
defaults allow overlap between the two solution test hosts; xUnit class tests
remain sequential. No parallelism setting or inventory is changed.

One excluded warm-up runs for each revision, mode, and route. Each of the two
modes then has five baseline/candidate pairs for both full and fast. Odd pair
numbers run baseline first and full before fast; even pairs reverse both orders.
An additional before/after pair for each route follows its own Release clean.
These are clean-output measurements, not cold-cache benchmarks.

The CSV records selected TRX definition count, executed/passed identities, exits,
two test hosts, temporary SDK builds, fixture project visits, and three runtime
consumer processes for full (zero for fast). Temporary-build/process counts are
derived from the reviewed scenario inventory, not child-process instrumentation.
The candidate visits one additional fixture project for build + test modes;
warm visits can skip actual compilation. Test-only performs no fixture build.
Fast still builds the whole solution, including the fixture.

`OutsideTestRunEnvelopeSeconds` is external test wall time minus the interval
from the earliest TRX start to latest TRX finish across both hosts. It describes
work outside runner intervals, not isolated launch cost. Overlapping host
durations are not summed.

## Measurements

All times are external wall-clock seconds. [Raw CSV](02-timings.csv) contains
52 observations: eight excluded warm-ups, forty paired samples, and four
clean-output observations. Every exit is 0; every full run selects/executes/
passes 8/8/8 and every fast run 4/4/4. Warm-ups and clean-output observations
are excluded from the five-sample summaries below.

| Mode | Route | Revision | Build median [min, max] | Test median [min, max] | Combined median [min, max] |
| --- | --- | --- | ---: | ---: | ---: |
| test-only | full | baseline | 0.00 [0.00, 0.00] | 16.54 [16.16, 19.68] | 16.54 [16.16, 19.68] |
| test-only | full | candidate | 0.00 [0.00, 0.00] | 8.32 [7.93, 8.74] | 8.32 [7.93, 8.74] |
| test-only | fast | baseline | 0.00 [0.00, 0.00] | 6.17 [5.71, 6.89] | 6.17 [5.71, 6.89] |
| test-only | fast | candidate | 0.00 [0.00, 0.00] | 6.02 [5.71, 6.54] | 6.02 [5.71, 6.54] |
| warm-build-test | full | baseline | 1.93 [1.78, 2.12] | 15.96 [15.27, 16.73] | 17.74 [17.20, 18.85] |
| warm-build-test | full | candidate | 2.07 [1.97, 2.10] | 7.65 [7.32, 8.58] | 9.63 [9.40, 10.65] |
| warm-build-test | fast | baseline | 1.83 [1.81, 1.98] | 6.07 [5.72, 6.65] | 7.88 [7.53, 8.63] |
| warm-build-test | fast | candidate | 2.05 [1.93, 2.42] | 5.91 [5.83, 6.25] | 7.88 [7.85, 8.65] |

Combined medians come from measured combined intervals, rather than adding
separate medians. Positive deltas mean less time. Percentage delta is
`(baseline median - candidate median) / baseline median * 100`.

| Mode | Route | Absolute combined median delta | Percentage delta |
| --- | --- | ---: | ---: |
| test-only | full | 8.22 s | 49.67% |
| test-only | fast | 0.15 s | 2.36% |
| warm-build-test | full | 8.12 s | 45.75% |
| warm-build-test | fast | -0.00 s | -0.05% |

All paired combined observations and deltas:

| Mode | Route | Pair | Baseline | Candidate | Delta |
| --- | --- | ---: | ---: | ---: | ---: |
| test-only | full | 1 | 16.54 | 8.25 | 8.29 |
| test-only | full | 2 | 16.16 | 8.74 | 7.43 |
| test-only | full | 3 | 16.49 | 8.32 | 8.16 |
| test-only | full | 4 | 19.68 | 8.69 | 10.98 |
| test-only | full | 5 | 17.12 | 7.93 | 9.19 |
| test-only | fast | 1 | 6.41 | 5.79 | 0.62 |
| test-only | fast | 2 | 6.17 | 6.54 | -0.37 |
| test-only | fast | 3 | 5.71 | 6.04 | -0.32 |
| test-only | fast | 4 | 6.89 | 6.02 | 0.87 |
| test-only | fast | 5 | 5.79 | 5.71 | 0.08 |
| warm-build-test | full | 1 | 17.74 | 9.90 | 7.85 |
| warm-build-test | full | 2 | 17.20 | 9.40 | 7.80 |
| warm-build-test | full | 3 | 17.78 | 9.40 | 8.37 |
| warm-build-test | full | 4 | 18.85 | 9.63 | 9.23 |
| warm-build-test | full | 5 | 17.33 | 10.65 | 6.68 |
| warm-build-test | fast | 1 | 8.07 | 8.50 | -0.43 |
| warm-build-test | fast | 2 | 7.53 | 7.85 | -0.32 |
| warm-build-test | fast | 3 | 7.88 | 7.85 | 0.03 |
| warm-build-test | fast | 4 | 8.63 | 7.88 | 0.75 |
| warm-build-test | fast | 5 | 7.55 | 8.65 | -1.10 |

Outside-run overhead, measured as described above:

| Mode | Route | Revision | Overhead median [min, max] |
| --- | --- | --- | ---: |
| test-only | full | baseline | 1.52 [1.48, 2.07] |
| test-only | full | candidate | 1.60 [1.56, 1.70] |
| test-only | fast | baseline | 1.67 [1.54, 1.93] |
| test-only | fast | candidate | 1.62 [1.55, 1.77] |
| warm-build-test | full | baseline | 1.54 [1.50, 1.87] |
| warm-build-test | full | candidate | 1.54 [1.52, 1.67] |
| warm-build-test | fast | baseline | 1.54 [1.51, 1.95] |
| warm-build-test | fast | candidate | 1.57 [1.52, 1.77] |

Additional observations, each after its own untimed Release clean:

| Route | Revision | Build | Test | Combined | Selected/executed/passed |
| --- | --- | ---: | ---: | ---: | --- |
| full | baseline | 2.12 | 16.70 | 18.82 | 8/8/8 |
| full | candidate | 2.21 | 7.87 | 10.08 | 8/8/8 |
| fast | baseline | 1.96 | 6.13 | 8.09 | 4/4/4 |
| fast | candidate | 2.32 | 6.20 | 8.52 | 4/4/4 |

The full-route warm solution build median increases from 1.93 s to
2.07 s (+0.14 s). The clean-output full-route build
increases from 2.12 s to 2.21 s
(+0.08 s). Three former temporary SDK builds
move to one shared fixture build in the solution. That prerequisite cost is
paid by both full and fast; the three runtime process invocations remain in
full. Two failed-compilation SDK builds remain inside full test time.

Full saves time in every paired test-only and warm build + test observation.
The clean-output full pair also improves. The baseline includes a slower
test-only sample; it remains visible in the ranges and paired table instead
of being discarded. The separated build costs show that this is a complete
cycle improvement in this environment, not only a test-only gain.

Fast results have mixed paired deltas and overlapping ranges. A fast-route
improvement is inconclusive; the additional fixture is still a prerequisite
even though fast does not invoke it. A single clean-output pair is illustrative
rather than a statistical estimate. No fixed speedup is promised elsewhere.

## Final verification and recommendation

After reverting controls and completing measurements, the final Release solution
build succeeds with zero warnings/errors. Standard solution-level `dotnet test`
passes 8/8: five ordinary tests and three AspectTests, with no failures or skips.
Every measured full runner also validates all eight identities; every fast runner
validates four. Source and resource files match the measured executable snapshot.

Recommendation: adopt the precompiled fixture if the user accepts replacing
fresh per-scenario project/build isolation with a shared prerequisite build.
Full test-only and warm complete-cycle gains are consistent in these samples,
while build cost increases slightly and fast gains are inconclusive. Preserve
full verification before merge and rebuild after any fixture or resource change.
The coverage mapping and negative controls support reviewing this candidate;
passing tests do not constitute user approval.

The user approved candidate `bd9ce04` on 2026-10-03 after reviewing these results.
The review boundary for adoption is satisfied; integration remains pending.
This change set is independently revertible against merged main. Task 04 and
permanent test classification instructions remain outside this implementation.
No generated artifacts or temporary projects are included in the commit.

## Code review

The `code-review` skill reviewed the staged candidate against `f05e9fa` before
commit in independent Standards and Spec agents. Neither reviewer edited files
or ran builds/tests.

### Standards

Zero findings. The changed repository documentation and code comments are in
English, fixture code follows the documented C# formatting conventions, and
there is no conflict with `CONTEXT.md` or either ADR. No actionable baseline
smells found. `Resources/Associated.cs` intentionally retains that filename for
SDK resource association with `ResourceAnchor`.

### Spec

Zero findings. Task 02's acceptance criteria and shared protocol are covered:
the fixture replaces three temporary builds, the two failed-compilation builds
remain, the assertion mapping and isolation changes are reported, and the
measurements separate test time from build cost and include warm and clean-output
totals. The fixture preserves the listed positive and failure assertions, and
the report records both reverted negative controls.

Standards: 0 findings. Spec: 0 findings. No worst issue in either axis.
