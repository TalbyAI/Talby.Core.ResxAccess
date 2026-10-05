# Shared aspect logic experiment

Status: candidate for user review; adoption pending.

Origin: [issue 03](../issues/03-share-testable-aspect-logic.md), following the
[parent measurement protocol](../spec.md).

## Baseline and production changes

Baseline: `9a0aa8b9cd4fe13213c4826acc0518bea25b4fbd`, synchronized
`main`/`origin/main` after PR #2. The approved precompiled consumer fixture is
already part of this baseline. The working tree was clean before creating
`test/share-testable-aspect-logic`. No earlier unapproved experiment is included.

Candidate: the working tree on that branch, subsequently committed with this
report. The measured executable snapshot tree is
`9f34fc0ecc664ccfd7c3e776687d6a309099f016`; documentation, measurement script,
and CSV are added afterward. This Git tree records every source, project,
fixture, snapshot, and execution-runner input, with normalized line endings.

The production diff extracts the existing decisions into three internal
compile-time types:

- `ReferenceResourceReader` reads the same SDK resource-map file and Reference
  Resource, preserving validation order, exact messages, platform path
  comparison, case-sensitive Resource Keys, and the identifier expression.
  Its concrete inputs are Reference Resource path, consumer project path, and
  SDK resource-map path; its result contains the manifest base name and keys.
- `ResourceValidationException` carries a validation message and the distinction
  between invalid Reference Resource and unsupported embedding. Expected read
  exceptions retain their diagnostic message through the reader.
- `ResxAccessImplementation` retains target checks, diagnostic definitions and
  mapping, and member introduction. The real attribute acquires the consumer
  project/path/property and delegates to it. Member introduction uses the
  attribute's existing templates through `WithTemplateProvider`.

The public attribute constructor, AttributeUsage, and public template signatures
remain unchanged. The ResourceManager field and both Raw Text templates remain
in the attribute and retain their bodies. A catch around member introduction
also retains the original IO, access, XML, and argument-error mapping. Project
property acquisition now precedes shared validation; querying that property
does not supply a fallback or suppress unavailable-project diagnostics.

Only the two test assemblies receive `InternalsVisibleTo`. Ordinary tests retain
`MetalamaEnabled=false`; the library retains
`MetalamaRemoveCompileTimeOnlyCode=false`. No new package, production fallback,
public testing API, custom runner, framework, or Resource Access behavior is
introduced. Both ADRs and Task 04's project layout remain unchanged.

## Adapter and supported Metalama facilities

The minimal test aspect writes a unique temporary Reference Resource and
resource-map file from literal XML/metadata, calls `ResxAccessImplementation.Build`,
then removes its files in `finally`. It supplies inputs; it contains no validation,
identifier, diagnostic, generation, or Raw Text algorithm.

Metalama 2026.1.28's generated compile-time assemblies cannot access these
internal helpers through the ordinary test assembly's friendship. The new
snapshots therefore use the supported `@Include` facility to compile the three
**original production helper files**, without copied helper implementations.
The production templates come from the referenced compiled library. Ordinary
unit tests execute the compiled library helpers. Changes to either production
source or compiled templates are consequently visible to these tests.

The adapter bypasses precisely:

1. Reading the real consumer project path from the Metalama compilation.
2. Reading `TalbyResxResourceMap` as a compiler-visible MSBuild property.
3. MSBuild metadata capture, `CreateManifestResourceNames`, resource-map writing,
   embedding, and satellite assembly generation.

It still executes production path resolution, file existence/culture checks,
resource-map reading and metadata validation, XML validation, Resource Key
selection, diagnostic mapping, and member introduction. It does not verify that
the supplied manifest name matches an actual embedded resource. The real SDK
fixture and temporary failed consumers retain those boundaries.

`RawTextGeneration` and `ResourceKeyIdentifiers` snapshot actual templates and
compile their generated output. `ResourceValidationDiagnostics` snapshots the
existing TRESX001/002/003 mapping and target locations.

The runner incorrectly renders the parameterless reserved-keyword invocation
as `.class(...)` when the adapter uses the compiled attribute templates; accepting
that compiler failure as a success snapshot would hide a gap. Instead,
`KeywordResourceKey` uses the supported `@TestScenario(DesignTime)` and checks
both generated `@class` signatures in its `.0.i.cs` snapshot. Its `.t.cs` checks
the target. The `.i.cs` is a compared baseline, not merely an output artifact.
DesignTime intentionally uses placeholder bodies and emits their existing
nullable/unused-field warnings. It does **not** check the reserved-keyword
method bodies. The unchanged SDK fixture compiles and invokes `EdgeTexts.@class()`
and asserts `Keyword`, covering that gap. Default snapshots check the contextual
keyword `record`, Unicode methods, invalid identifiers, and the real Raw Text
method bodies. No output-compilation checks are disabled.

These are documented facilities in
[Metalama snapshot testing](https://doc.metalama.net/conceptual/aspects/testing/snapshot-testing)
and the installed package's `TestOptions` XML documentation. Template delegation
uses the installed `AdviserExtensions.WithTemplateProvider` API, also described
in the [template-provider example](https://doc.metalama.net/src/builder/builder-3).

## Assertion mapping and isolation

No original test, scenario, or assertion is removed, moved, or replaced. The
baseline's eight identities remain; seventeen ordinary unit tests and four
AspectTests are added. Full therefore selects 29 tests; fast selects 25 and
continues to defer exactly the same four integration identities.

| Existing assertion/scenario | Candidate protection | Fast/full boundary |
| --- | --- | --- |
| Metalama compilation can be created and queried | Unchanged `MetalamaSetupTests.CanCreateAndQueryCompilation` | Both |
| Null, empty, whitespace, and non-resx paths produce exact TRESX001 diagnostics | Unchanged `InvalidPaths`; additional unit precedence checks | Both |
| Instance and generic targets produce exact TRESX002 diagnostics | Unchanged `UnsupportedTargets`; added nested generic-container diagnostic | Both |
| Missing real project context produces the same TRESX003 message | Unchanged `UnavailableProjectContext` using the real attribute | Both |
| SDK map acquisition, RootNamespace manifest naming, independent Resource Sets, implicit associated-type naming, and embedded/localized resources work | Unchanged `CanCompileAndInvokeIndependentResourceSets` and SDK fixture | Full |
| Raw Text preserves whitespace/placeholders, empty/blank values, Unicode and reserved-keyword lookup; no Format methods are introduced; class identity/accessibility are retained | Same fixture source checks and exact process output assertions | Full; default snapshots additionally check ordinary generated signatures/templates |
| CurrentUICulture, explicit Resource Culture, parent-culture and neutral fallback, CurrentCulture independence, and null culture rejection | Same fixture checks and output assertions; default Raw Text snapshot also checks CurrentUICulture and null guard | Runtime behavior full |
| LogicalName, ManifestResourceName, linked resource, missing file, and localized Reference Resource each report the original diagnostic/message on the correct target file | Unchanged `ReportsEachInvalidResourceAndEmbeddingInOneBuild`, with all five cases | Full; helpers/diagnostic snapshots additionally cover relevant decisions |
| Malformed XML fails the real SDK build with MSB3103 and no LAMA0041 aspect crash | Unchanged `ReportsMalformedReferenceResourceWithoutAspectCrash` | Full; helper test separately checks malformed XML read handling |
| Missing manifest/key messages name the Resource Key, manifest, and Resource Culture; missing manifest retains its exception cause | Unchanged `DescribesMissingRuntimeManifestAndResourceKey`, with two fresh runtime processes | Full; default snapshots additionally check exception templates |

New helper coverage:

| Unit tests | Independently expected behavior |
| --- | --- |
| `ReadsTextEntriesAndSdkManifestName` | Literal SDK manifest name and Resource Key survive reading |
| `AcceptsExplicitStringTypesEmptyValuesAndCaseSensitiveKeys`, `AcceptsAnEmptyReferenceResource` | Explicit System.String metadata, empty values, distinct case-sensitive keys, empty root remain accepted |
| `RejectsMalformedXmlWithoutAnUnhandledXmlException`, `RejectsInvalidRootElements` | Malformed XML becomes a read failure; wrong or namespaced roots are rejected |
| `RejectsDuplicateOrUnnamedResourceKeys` | Missing/empty names and duplicates are rejected |
| `RejectsInvalidValueAndTypeStructures` | Zero/two values, mimetype presence, non-string and empty types are rejected |
| `RejectsInvalidPathsBeforeUnavailableProjectContext`, `DistinguishesUnavailableProjectContextFromMissingFiles` | Existing path/context/file error identity and precedence |
| `RejectsCultureSpecificReferenceResourcesBeforeMissingSdkMap`, `RejectsCultureSpecificSdkMetadata` | Filename and WithCulture decisions retain their separate messages and precedence |
| `RejectsUnavailableSdkMaps`, `RejectsMissingOrMalformedEmbeddedResourceMetadata` | Absent property/file and malformed/unmatched/empty-name map rows remain distinct embedding errors |
| `RejectsCustomNamesAndLinkedResourceMetadata`, `RejectsResourcesOutsideTheProjectDirectory` | All three metadata columns and outside-project path are rejected |
| `MatchesResourceMapPathsUsingPlatformComparison` | Windows ignores path case; other platforms use ordinal comparison |
| `RecognizesKeywordAndUnicodeResourceKeyIdentifiers` | Keywords, letters, combining marks, digits after initial letters, and letter numbers accepted; spaces, dashes, leading digits and emoji rejected |

New generated-output coverage checks the literal ResourceManager base name and
target assembly, public static string overloads, CurrentUICulture dispatch,
null guard, GetString arguments, missing-key/manifest/satellite exception bodies,
Unicode/contextual/reserved keyword APIs, invalid-key omission, and six mapped
diagnostics. Snapshots omit auxiliary helper layout, preserving an observable
generated API/template boundary.

The new unit/adapter inputs have unique directories and cleanup; they do not
share Resource Sets or mutable metadata. Existing integration isolation is
unchanged: one shared prerequisite SDK fixture, three fresh runtime processes,
and two separate temporary failed-consumer projects/builds. A unit failure names
its validation branch; snapshot failures show generated or diagnostic differences;
SDK failures retain build output and target locations.

Known gaps: deterministic inputs cannot establish SDK property/metadata accuracy,
embedding, associated-type naming, or runtime fallback. DesignTime cannot
establish reserved-keyword bodies. IO/permission failure injection and every
possible Unicode character are not exhaustively tested; the unchanged catch and
identifier expression remain reviewed production code. Existing formatting,
localized-key consistency, packaging expansion, and IDE integration remain
outside the implementation scope.

## Negative controls

All controls were reverted before measurements. Ignored evidence is under
`test-results/03-aspect-logic/controls/`.

| Temporary production regression | Observed detection |
| --- | --- |
| Replace `document.Root?.Name != "root"` with a null-root-only check | Release build succeeds; `RejectsInvalidRootElements` fails because `<resources />` is accepted (`validation-test.log`) |
| Make the parameterless Raw Text overload pass InvariantCulture instead of CurrentUICulture | Release build succeeds; `RawTextGeneration` snapshot fails on the dispatch expression (`generation-test.log`) |
| Discard the acquired SDK map before calling shared production logic | Real solution/consumer-fixture build fails with the existing TRESX003 SDK-map-unavailable message (`sdk-build.log`); independently building AspectTests succeeds and all four deterministic snapshots pass (`sdk-aspect-test.log`) |

The SDK control demonstrates that adapter success is insufficient for production
wiring. After restoration, the Release build has zero warnings/errors and the
full runner verifies 29 passed identities (`restored-build.log`,
`restored-full.log`). The initial TDD tracer failed to compile because the
reader did not exist, then passed after extracting production logic. Snapshot
outputs were inspected before accepting their baselines; negative controls
subsequently establish their sensitivity to real production changes.

## Commands and measurement environment

From the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
pwsh -NoProfile -File tests/run.ps1 -Mode fast
pwsh -NoProfile -File tests/run.ps1 -Mode full
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
pwsh -NoProfile -File .scratch/test-policy/results/03-measure.ps1
```

[Measurement script](03-measure.ps1) archives the pinned baseline into the ignored
results directory. Both revisions run sequentially, using the same machine,
NuGet cache, SDK, runtime, configuration, logging, and default parallelism.
The script changes no production/test inputs. Re-running it intentionally
replaces the raw CSV and ignored measurement logs with a new complete run.

Measured on 2026-10-03 on `DESKTOP-34DS59F`: Windows 11 Pro 10.0.26200,
Intel Core i9-10900K at 3.70 GHz, 20 logical processors, 63.92 GiB RAM,
PowerShell 7.6.6, .NET SDK 10.0.401, runtime 10.0.12, MSBuild 18.9.11,
Metalama 2026.1.28. No project parallelism/xUnit parallelism override was added.
The solution has the same four projects and two test-host assemblies throughout.

Initial solution restores/builds are untimed and excluded. Temporary consumer
restore/build work remains in full test time. Build samples use quiet verbosity;
both revisions' unchanged underlying test command uses normal verbosity, TRX
logging, and no-build/no-restore. External Stopwatches measure build, runner,
and combined intervals; combined medians are not sums of separate medians.
There is one excluded warm-up per revision/mode/route and five paired samples,
alternating which revision runs first. Additional observations follow their own
untimed Release clean; these are clean-output measurements with warm caches.

Two earlier campaigns were interrupted by CS0009 when a compiler could not read
managed metadata from a reference assembly. Campaign 1 had 34 successful
observations, then its next baseline warm build failed before running tests.
An untimed repeat build succeeded without source changes. Campaign 2 had seven
successful observations, then a candidate full run passed 28/29: the grouped
invalid-consumer test failed because its SDK build encountered the same metadata
error instead of its expected resource diagnostics. All seven AspectTests passed.

After `dotnet build-server shutdown` and a successful Release build, five
consecutive untimed candidate full runs passed without any code/configuration
change. Compiler/build-server metadata state is a plausible explanation, not a
proven root cause. The final campaign uses the original default options; no
retry or server shutdown is inserted into its timed samples.

**All** successful observations from both interrupted campaigns are excluded
from primary statistics, rather than retaining favorable ones. Their 41 rows
remain in [interrupted raw timings](03-interrupted-timings.csv), identified by
Campaign. [Interruption inventory](03-interruptions.csv) records the failures
and execution counts. Failed-attempt external elapsed time was not exported by
the aborted script and is explicitly marked NotCaptured. Full logs/TRX remain
under the two ignored interrupted-measurement directories and
`test-results/execution/`; five successful post-shutdown runs remain under
`test-results/03-aspect-logic/diagnosis/`. These tooling failures limit
reproducibility and do not establish any candidate speedup.

The raw CSV separately records two temporary SDK builds per full run and zero
per fast run. Each timed solution build schedules one consumer fixture project;
warm builds may skip its compilation when inputs are unchanged. No SDK build is
removed in this experiment. Each route still launches two test hosts through
one solution-level dotnet test command and one PowerShell runner. Full additionally
launches three runtime processes and the two temporary SDK build processes.

`TestHostSpanSeconds` spans the earliest TRX TestRun start to the latest finish
across hosts. `OutsideTestHostSpanSeconds` subtracts that span from runner wall
time, describing launch/teardown/runner work outside the host intervals, rather
than a separately isolated process-launch benchmark. Overlapping durations are
not summed. Every observation records exits and selected/executed/passed counts.

## Measurements

All times are external wall-clock seconds. The primary [raw CSV](03-timings.csv)
contains 52 observations: eight excluded warm-ups, forty paired samples, and
four clean-output observations. Every exit is 0. Baseline fast/full selects,
executes and passes 4/8 tests; candidate fast/full selects, executes and passes
25/29 tests. No failure or skip is hidden in these inventories. The interrupted
campaigns and their failures are separately retained as described above.

| Mode | Route | Revision | Build median [min, max] | Test median [min, max] | Combined median [min, max] |
| --- | --- | --- | ---: | ---: | ---: |
| test-only | fast | baseline | 0.00 [0.00, 0.00] | 5.72 [5.61, 5.84] | 5.72 [5.61, 5.84] |
| test-only | fast | candidate | 0.00 [0.00, 0.00] | 7.12 [6.81, 7.51] | 7.12 [6.81, 7.51] |
| test-only | full | baseline | 0.00 [0.00, 0.00] | 8.29 [7.81, 12.71] | 8.29 [7.81, 12.71] |
| test-only | full | candidate | 0.00 [0.00, 0.00] | 8.54 [8.01, 8.89] | 8.54 [8.01, 8.89] |
| warm-build-test | fast | baseline | 2.01 [1.95, 3.11] | 5.64 [5.60, 6.45] | 7.65 [7.62, 9.02] |
| warm-build-test | fast | candidate | 1.99 [1.95, 2.61] | 7.20 [6.85, 7.47] | 9.18 [8.84, 10.08] |
| warm-build-test | full | baseline | 2.01 [1.99, 2.49] | 7.80 [7.34, 8.02] | 10.00 [9.34, 10.15] |
| warm-build-test | full | candidate | 2.00 [1.98, 2.05] | 7.56 [7.55, 8.52] | 9.60 [9.54, 10.53] |

Positive deltas mean less time. Percentage delta uses
`(baseline median - candidate median) / baseline median * 100`.

| Mode | Route | Combined median delta | Percentage delta |
| --- | --- | ---: | ---: |
| test-only | fast | -1.40 s | -24.53% |
| test-only | full | -0.25 s | -2.96% |
| warm-build-test | fast | -1.53 s | -20.04% |
| warm-build-test | full | 0.40 s | 3.97% |

All paired combined observations and deltas, including slower baseline samples:

| Mode | Route | Pair | Baseline | Candidate | Delta |
| --- | --- | ---: | ---: | ---: | ---: |
| test-only | fast | 1 | 5.72 | 7.51 | -1.79 |
| test-only | fast | 2 | 5.76 | 7.09 | -1.33 |
| test-only | fast | 3 | 5.61 | 7.12 | -1.51 |
| test-only | fast | 4 | 5.84 | 6.81 | -0.96 |
| test-only | fast | 5 | 5.70 | 7.51 | -1.81 |
| test-only | full | 1 | 8.29 | 8.54 | -0.25 |
| test-only | full | 2 | 7.93 | 8.01 | -0.08 |
| test-only | full | 3 | 12.71 | 8.72 | 3.99 |
| test-only | full | 4 | 8.31 | 8.27 | 0.04 |
| test-only | full | 5 | 7.81 | 8.89 | -1.08 |
| warm-build-test | fast | 1 | 8.39 | 10.08 | -1.69 |
| warm-build-test | fast | 2 | 7.63 | 9.18 | -1.55 |
| warm-build-test | fast | 3 | 9.02 | 9.12 | -0.10 |
| warm-build-test | fast | 4 | 7.65 | 8.84 | -1.19 |
| warm-build-test | fast | 5 | 7.62 | 9.21 | -1.59 |
| warm-build-test | full | 1 | 10.15 | 10.53 | -0.38 |
| warm-build-test | full | 2 | 9.79 | 9.60 | 0.19 |
| warm-build-test | full | 3 | 10.02 | 9.55 | 0.48 |
| warm-build-test | full | 4 | 10.00 | 9.76 | 0.24 |
| warm-build-test | full | 5 | 9.34 | 9.54 | -0.20 |

Outside-test-host overhead, measured as described above:

| Mode | Route | Revision | Overhead median [min, max] |
| --- | --- | --- | ---: |
| test-only | fast | baseline | 1.54 [1.52, 1.61] |
| test-only | fast | candidate | 1.57 [1.53, 1.71] |
| test-only | full | baseline | 1.54 [1.51, 1.66] |
| test-only | full | candidate | 1.55 [1.51, 1.73] |
| warm-build-test | fast | baseline | 1.53 [1.53, 1.60] |
| warm-build-test | fast | candidate | 1.57 [1.50, 1.73] |
| warm-build-test | full | baseline | 1.57 [1.53, 1.65] |
| warm-build-test | full | candidate | 1.55 [1.50, 1.80] |

Additional observations after an untimed Release clean (excluded from medians):

| Route | Revision | Build | Test | Combined | Selected/executed/passed |
| --- | --- | ---: | ---: | ---: | --- |
| full | baseline | 2.20 | 7.51 | 9.71 | 8/8/8 |
| full | candidate | 2.22 | 7.74 | 9.95 | 29/29/29 |
| fast | baseline | 2.14 | 5.88 | 8.01 | 4/4/4 |
| fast | candidate | 2.11 | 7.28 | 9.38 | 25/25/25 |

Fast is slower in every paired test-only and warm build + test sample. Its new
unit tests cover validation decisions cheaply, but four additional snapshot
scenarios compile shared production source and generate or check API/templates.
Those scenarios add work rather than removing SDK builds. Median fast latency
increases by 1.40 s (24.53%) test-only and 1.53 s (20.04%) for the complete warm
cycle. The clean-output fast pair also increases by 1.37 s.

Full has mixed paired deltas and overlapping ranges. Its test-only median is
0.25 s worse; its warm complete-cycle median is 0.40 s better. The clean-output
full pair is 0.24 s worse. The slower 12.71 s baseline test-only sample remains
in the ranges and paired table; it is not discarded. A full-route performance
improvement is **inconclusive**. Build medians remain close (about 2 s on both
revisions); no meaningful build-cost reduction is established. The additional
clean-output pair is illustrative, not a cold-cache or statistical benchmark.

The increased fast count (4 to 25) and full count (8 to 29) represents new
assertions and scenarios, not replacing equivalent protection with fewer tests.
Two temporary SDK builds remain per full run, with the same fixture prerequisite
and runtime process counts. No fixed speedup is promised.

## Final verification and recommendation

After measurements, the final Release solution build succeeds with zero
warnings/errors. Standard solution-level execution passes 29/29 (22 ordinary
xUnit tests and seven AspectTests), with no skips or failures. The fast/full
runner verifies its exact identities in every primary observation. Independent
`--list-tests` checks confirm baseline fast/full discovery of 4/8 and candidate
fast/full discovery of 25/29. Logs are under
`test-results/03-aspect-logic/final-*.log` and `discovery-*.log`.

The measured source/test subtrees remain identical to the pinned executable
snapshot. The original attribute templates, integration tests/helper, and all
three original AspectTest inputs/baselines were also compared with `9a0aa8b` and
remain unchanged. No mutation, generated binary, TRX, log, or temporary project
is included in the commit.

Recommendation: **reject as a speed optimization**. Fast consistently adds
latency and a full-route speed gain is inconclusive. **Consider adoption as a
coverage improvement** only if the user accepts approximately 1.4 s more
test-only fast feedback and the production/helper/snapshot tradeoffs in this
report. The new fast assertions make representative validation and generation
regressions visible locally; they do not replace SDK smoke verification. Retain
the full gate and rebuild after source or fixture changes.

The two interrupted campaigns also show tooling instability under repeated
builds. Their failures are retained, their entire timing sets are excluded from
primary statistics, and the exact cause remains unproven. Do not interpret
successful final checks as proof that this tooling issue is fixed.

This is an independently reviewable candidate, not adopted policy. No merge or
experiment 04 implementation is performed. User review remains the adoption
boundary; passing tests and code review do not constitute approval.

## Code review

The `code-review` skill reviewed the staged candidate against `9a0aa8b` before
commit using independent Standards and Spec agents. Both read files/logs only;
neither edited files or ran builds/tests. The Spec reviewer additionally checked
the completed measurements, final verification logs, and discovery inventories.

## Standards

No findings. The changed C# and project XML follow the documented naming,
indentation, namespace, and brace conventions; repository documentation is in
English. No actionable baseline smells were found in the executable changes.

## Spec

No findings. Shared production helpers, deterministic-input AspectTests, retained
SDK consumer assertions, adapter limitations, and negative controls match issue
03. No unrequested behavior was found. The primary 52-row CSV has eight warm-ups,
forty samples, and four clean-output observations; every mode/route has five
alternating pairs with successful exits and passing counts. Medians, ranges,
paired deltas, coverage counts, and the inconclusive full-route verdict agree
with the CSV. The 41 interrupted observations and two failures are retained and
described, including uncaptured elapsed time and the unproven cause. Final build,
test, and discovery logs match the reported results.

Standards: 0 findings. Spec: 0 findings. No worst issue in either axis.
