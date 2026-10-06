# Integration test performance comparison

Status: **exploration completed on 2026-10-06**. All approved comparison modes,
clean-output pairs, strict summary and trace verification completed successfully.
See [the execution state](execution-state.md).

The user approved the A, B and A+B experiments and the 30% median IntegrationTests
improvement criterion. This report distinguishes prototype evidence from adoption:
the repository's original library and test implementation remain unchanged.

## Decision

**Recommend A alone for the next adoption change.** Bounded concurrency meets the
approved 30% goal with a 44.48% IntegrationTests reduction and a 41.74% warm
build-plus-full reduction. Its complete validation preserves the assertions,
inventories and shared library outputs, without adding dependency-graph state.

A+B is a successful prototype: it reduces IntegrationTests by 48.89% and warm
build-plus-full by 43.87% against its own baselines. Those results do not establish
B's incremental benefit over A: A and A+B were measured in separate baseline
series, and no direct A-versus-A+B pairs were collected. Their warm cycle candidate
medians are 56.755 s and 60.471 s respectively; this is not evidence of a causal
regression either. The clearer payoff is A's verified reduction with fewer
conditions to maintain. B alone reaches 11.98% in IntegrationTests and requires
restore-success markers, project fingerprints and graph invalidation. Retain B
as a documented optional follow-up rather than include it in the recommended
adoption. C, D and E need no additional investigation to meet the current goal.

This is a recommendation based on the approved experiments, not an implementation
or authorization to commit, open a pull request or merge.

The [independent final review](final-review.md) confirms the arithmetic, inventories,
frozen inputs and recommendation, with no material findings. Repository code and
Markdown formatting checks passed.

## Candidates and retained behavior

- **A:** two concurrent xUnit collection workers for independent consumer histories.
  Each history retains its sequential mutations, builds and assertions. Six thin
  wrapper facts expose the histories to scheduling; DesignTime has its own
  collection. The five diagnostic classes retain their shared fixture and cache.
- **B:** retain the first implicit restore, then use `--no-restore` for subsequent
  builds of the same successfully restored dependency graph. A consumer-local
  success marker and project fingerprint control eligibility; recognized graph
  writes invalidate that state.
- **A+B:** combine both changes without reducing consumer commands or assertions.

The [coverage map](coverage-map.md) records the six changed A/A+B identities and
29 unchanged IntegrationTests identities. The normalized six-history source hash
is identical across all four trees. A introduces no grouped diagnostic compilation.

## Source and measurement protocol

Frozen source: `71f1ab38f59866b35fa685cc58b031c80a6d09b0`, Windows x64,
.NET SDK 10.0.401, Release, 20 logical processors and the same NuGet cache.
See [environment metadata](results/environment.json).

The current baseline contains 35 IntegrationTests. The historical diagnosis used
36 tests at a different commit; its 103.15 s median is not a candidate baseline.
Each candidate is compared with its own five alternating, sequential baseline
pairs. Every variant/mode has an excluded warm-up. Restore and preparation builds
are outside measurement; fresh consumer restores during tests remain included.
External stopwatches measure wall time; overlapping test durations are never summed.

Required modes are IntegrationTests, AspectTests, fast, full and warm build plus
full. An additional clean-output build plus full pair is separate from the five
warm pairs. All samples, including outliers, remain in the evidence.

## IntegrationTests results

| Candidate | Baseline median | Candidate median | Reduction | Baseline range | Candidate range | Paired reduction range |
| --- | --- | --- | --- | --- | --- | --- |
| A | 96.245 s | 53.435 s | 44.48% | 94.898–97.892 s | 50.142–57.342 s | 41.12–47.16% |
| B | 97.968 s | 86.232 s | 11.98% | 90.327–99.067 s | 81.941–88.626 s | 9.28–11.98% |
| A+B | 92.753 s | 47.409 s | 48.89% | 92.318–98.176 s | 43.453–47.896 s | 48.36–53.02% |

All three comparisons preserve the same 35 passed canonical identities. A and A+B
meet the approved IntegrationTests threshold; B alone does not. The three baseline
series differ, so these percentage reductions are not additive and candidate-only
times do not establish the incremental contribution of B to A.

## Validation results

Every mode below has five alternating baseline/candidate pairs. Times are external
wall seconds. Ranges include every sample; percentage reduction is calculated from
the two medians, while paired ranges use each matched pair.

| Candidate | Mode | Baseline median (range) | Candidate median (range) | Reduction | Paired reduction range |
| --- | --- | --- | --- | --- | --- |
| A | Full | 94.925 (94.459–96.127) | 54.650 (51.884–55.020) | 42.43% | 42.04–45.07% |
| A | WarmFull | 97.423 (95.738–116.834) | 56.755 (53.541–65.170) | 41.74% | 31.93–54.17% |
| B | Full | 95.393 (93.299–105.646) | 85.673 (83.271–91.793) | 10.19% | 8.17–15.79% |
| B | WarmFull | 100.495 (96.698–105.940) | 91.227 (86.365–92.329) | 9.22% | 8.61–13.89% |
| A+B | Full | 111.711 (102.856–116.385) | 52.576 (49.046–55.424) | 52.94% | 49.96–53.65% |
| A+B | WarmFull | 107.737 (103.555–121.899) | 60.471 (57.656–65.928) | 43.87% | 38.80–52.70% |

WarmFull includes solution build and full test execution. Its median build/test
components are reported independently; their medians need not sum to the cycle
median because they can come from different samples.

| Candidate | Baseline median build / test | Candidate median build / test |
| --- | --- | --- |
| A | 2.389 / 95.033 | 2.407 / 54.348 |
| A+B | 2.934 / 105.402 | 3.060 / 57.820 |
| B | 2.437 / 97.987 | 2.472 / 87.627 |

AspectTests and fast do not execute the modified IntegrationTests helper or
history scheduling. Their observed time differences are not credited to A or B.

| Candidate | Mode | Baseline median (range) | Candidate median (range) | Observed reduction | Paired reduction range |
| --- | --- | --- | --- | --- | --- |
| A | Aspect | 15.734 (15.528–16.101) | 15.404 (15.225–15.446) | 2.10% | 0.56–5.31% |
| A | Fast | 18.286 (17.631–18.532) | 18.081 (17.914–18.229) | 1.12% | -2.55–2.04% |
| B | Aspect | 15.527 (14.991–15.782) | 15.035 (14.800–15.216) | 3.17% | -0.29–4.69% |
| B | Fast | 17.949 (17.454–18.293) | 17.848 (17.410–18.185) | 0.56% | -2.26–4.01% |
| A+B | Aspect | 16.192 (15.459–19.264) | 16.110 (15.878–16.891) | 0.51% | -9.26–16.37% |
| A+B | Fast | 21.982 (18.711–22.650) | 19.169 (19.004–27.893) | 12.80% | -23.15–15.62% |

Negative paired percentages are slower candidate observations. A fast contains
a pair 2.55% slower. A+B fast pair 1 is 23.15% slower (22.650 to 27.893 s),
and its Aspect pair 1 is also slower; all retain their exact passed inventories.
No cause was profiled. A WarmFull retains the 116.834 s baseline and 65.170 s
candidate maxima; A+B Full retains the 116.385 s baseline maximum. No sample
was discarded or replaced because of its timing.

### Additional clean-output pair

Each tree is cleaned outside the timer, then built and tested. These are single
observations, not warm medians or cold-cache guarantees.

| Candidate | Baseline build / test / cycle | Candidate build / test / cycle | Cycle reduction |
| --- | --- | --- | --- |
| A | 2.540 / 95.683 / 98.224 | 2.594 / 51.046 / 53.641 | 45.39% |
| B | 2.527 / 98.723 / 101.250 | 2.665 / 88.431 / 91.098 | 10.03% |
| A+B | 3.115 / 102.699 / 105.815 | 3.418 / 62.728 / 66.147 | 37.49% |

### Evidence verification

The final audit verified **188 completed observations**: 150 paired observations,
30 ordinary warm-ups, six clean observations and two supplemental resume warm-ups.
All recorded build/test exit codes are zero. Raw TRX verifies 8,204 passed identity
observations against the exact canonical inventories, not merely fixed counts:
[IntegrationTests](results/inventory-integration.csv) (35),
[AspectTests](results/inventory-aspect.csv) (12),
[fast](results/inventory-fast.csv) (32) and [full](results/inventory-full.csv) (67).
Every completed test execution preserved paths, lengths and UTC timestamps of
24 library output files; solution builds and cleans are outside that read-only
test interval. All 714 frozen source hashes still match, and the original
source/test diff is empty. See [the audit](results/final-verification.json).

## Restore and concurrency controls

The six excluded Integration warm-ups retain 70 child commands each: 36 builds,
seven DesignTime commands and 27 runtime invocations, including 12 fresh consumers
and 24 subsequent builds. B and A+B skip restore on exactly those 24 subsequent
builds; the first 12 retain restore. Baseline and A retain implicit restore on all
36 builds. Independent history concurrency changes from one to two with A/A+B.
Observed child-build concurrency is two in all warm-ups; the unchanged diagnostic
fixture starts its two isolated builds concurrently and could overlap another
history, so the theoretical child-build bound with A/A+B is three.

The [ten restore controls](results/restore-controls.csv) verify unchanged builds,
successful restore followed by failed compilation, corrected compilation, project
invalidation, malformed project failure, actual NU1101 RestoreTask failure and
recovery. Repeated failed restores remain ineligible for `--no-restore`.
See [the variant review](variant-review.md) for the resolved marker concern.

## Limits and reproduction

Results describe this local machine and SDK; CI performance was not measured.
B full pairs 1–2 were measured before a user-requested suspension; pairs 3–5
were measured after resuming about 8.5 hours later. Inputs, SDK and machine were
verified and two supplemental full warm-ups were excluded from statistics.
The interrupted sample was archived and repeated; completed pairs were retained.
B is scoped to the frozen dependency graph: project changes outside the helper
are fingerprinted, but external props/targets edits outside `ConsumerProject.Write`
are unsupported. Any later adoption must preserve this scope or broaden graph
invalidation deliberately. Clean-output results do not represent a cold OS or
NuGet cache. No profiler establishes the cause of individual timing outliers.

Prototype scripts live under `prototypes/integration-test-performance/`:
`New-Variants.ps1`, `Initialize-Experiment.ps1`, `Run-Comparison.ps1`,
`Run-Remainder.ps1`, `Summarize-Comparisons.ps1` and `Analyze-Traces.ps1`.
Reviewable results live in [the results directory](results/summary.csv), with
[all timings](results/timings.csv), [paired deltas](results/paired-deltas.csv),
[clean cycles](results/clean-cycle.csv) and [command traces](results/command-traces.csv).
Raw logs, TRX, traces, frozen source hashes and output metadata are retained in
ignored `test-results/integration-exploration-c891a32c/`.

No production behavior, installed package, original test source, commit or pull
request was changed by these experiments.
