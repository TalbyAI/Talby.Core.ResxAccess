# Integration performance exploration execution state

Current status: **exploration and recommendation completed on 2026-10-06**.
Unified exec session `1521` finished successfully. Do not resume benchmark work.

User approval received on 2026-10-05 for A, B and A+B, with a 30% median
IntegrationTests improvement goal, preserved assertions and no reproducible
build-plus-full regression. Approval covers experiments and a recommendation,
not adoption, commits or pull requests.

- [x] Review current source and historical evidence.
- [x] Resolve selection and acceptance criteria through explicit approval.
- [x] Generate frozen baseline, A, B and A+B source trees with independent outputs.
- [x] Verify assertion mapping, diagnostic fixture lifetime and restore handling.
- [x] Restore and build all trees outside measured commands.
- [x] Compare A against baseline: warm-ups and five alternating Integration pairs.
- [x] Compare B against baseline: warm-ups and five alternating Integration pairs.
- [x] Compare A+B if independent results justify combination.
- [x] Complete five-pair AspectTests, fast, full and warm build plus full checks.
- [x] Complete additional clean-output build plus full comparisons.
- [x] Review samples, inventories, shared-output metadata and command traces.
- [x] Publish reviewable CSVs, limitations and final recommendation.

Prototype generators and harness live under `prototypes/integration-test-performance/`.
Frozen source trees, raw logs, traces and TRX files live under a fresh ignored
`test-results/integration-exploration-<id>/` directory. The active path is recorded
in `test-results/integration-exploration-latest.txt` when preparation completes.

This approval supersedes the pending-selection state recorded in the saved
`exploration-plan.md` and `exploration-preflight.md`; their original approval
notes describe the state before the user's explicit approval.

Active experiment: `test-results/integration-exploration-c891a32c/`, frozen from
`71f1ab38f59866b35fa685cc58b031c80a6d09b0`. All four solution builds passed with the
two expected ConsumerFixture identifier warnings. The normalized six-history
source hash is identical across variants. Eight temporary B controls passed,
including an unchanged assembly, expected compilation failure, project-file
invalidation and failed restore followed by recovery; command traces confirm
fresh/invalidated consumers retain restore and only restored graphs skip it.

A Integration comparison completed in unified exec session `26920`.
Its excluded warm-ups passed in 102.888 s (baseline) and 56.332 s (A).
Four candidate observations are 57.342, 53.352, 53.435 and 53.839 s; all
passed 35 tests and preserved 24 library output files. Warm-up traces show
the same 70 child commands (36 builds, seven DesignTime commands, 27 runtime
invocations), 12 fresh consumers and 24 subsequent builds. Peak independent
history concurrency is one for baseline and two for A; observed child-build
command concurrency is two for both warm-ups.

Before starting B: rerun the extended `RestoreControl` source, which now adds
an actual RestoreTask NU1101 failure through an empty local feed plus a repeated
build. Earlier malformed-XML controls fail before Restore executes, so these
additional controls close the static-review uncertainty about an unconditional
AfterTargets=Restore marker after restore failure. Do not run this probe while
the A paired series is active. Copy updated control source into the existing
B/prototypes/restore-control directory, rebuild only that standalone control,
and invoke it with B's IntegrationTests DLL, a fresh trace directory and JSON
result path. Expect ten sequential builds: skipRestore flags
false,true,true,true,false,false,false,false,false,false; exit codes
0,0,1,0,0,1,1,1,1,0. If this succeeds, proceed with B Integration, then AB if
their isolated results support combining them. Validation and clean comparisons
are still pending for all three candidates.

The extended ten B controls completed successfully. Both actual NU1101 restore
failure commands retained implicit restore, closing the AfterTargets-marker
review risk for SDK 10.0.401. Updated control evidence is in
`results/restore-controls.csv`; raw traces use `restore-control-v2-trace/`.

A Integration median: baseline 96.245 s, candidate 53.435 s, 44.48% lower.
Ranges: baseline 94.898–97.892 s; A 50.142–57.342 s. All five pairs preserved
35 passed identities and 24 shared library files. Partial summary is published
under `results/`; full acceptance validation is still pending.

B Integration completed in session `50769`: baseline median 97.968 s,
candidate median 86.232 s, approximately 11.98% lower. All paired observations
passed the same 35 identities and preserved 24 library output files. A and B
both show positive isolated improvements, supporting the approved A+B comparison.

Next automated orchestration is `prototypes/integration-test-performance/Run-Remainder.ps1`:
A+B Integration, then A/B/A+B Validation (Aspect, fast, full, warm build plus full,
five pairs per mode plus one clean-output pair each), followed by strict summary
and trace verification. All native runs remain sequential. Do not adopt code,
commit or create a pull request. After execution, review all results, publish the
comparison report and recommend the selected option. Original source remains intact.

Active orchestration session: `82249`. It started A+B Integration warm-up on
2026-10-05 at 23:51 Europe/Madrid, then will run all validation modes automatically.
Poll this session before attempting any resume to avoid concurrent native runs.

A+B Integration completed five pairs on 2026-10-06. Candidate samples were
47.415, 47.409, 43.453, 45.275 and 47.896 s; paired baseline samples were
98.176, 94.884, 92.503, 92.318 and 92.753 s. All passed the same 35 identities
and preserved 24 library files. Orchestration session `82249` has moved to
A Validation, starting with AspectTests (12 passed identities, approximately
16 s per excluded warm-up). It will automatically continue A fast/full/warm
build+full/clean, then all B validation modes, then all A+B validation modes.
No additional build/test process may overlap this orchestration.

On 2026-10-06 at 00:27 Europe/Madrid, A has completed Aspect (12 identities),
fast (32 identities) and full (67 identities), each with five alternating pairs
and excluded warm-ups. A full candidate samples were 53.981, 51.884, 55.020,
54.651 and 54.650 s; all retained 67 passed identities and 24 shared output files.
Session `82249` is now in A WarmFull (build plus full) warm-up. A's clean pair,
then all B and A+B validation modes remain pending and will run automatically.

A Validation completed at 00:45 Europe/Madrid on 2026-10-06: all five modes
have five alternating pairs and excluded warm-ups, plus the clean-output pair.
There are 62 A comparison observations. Full retained 67 passed identities;
every test execution preserved all 24 captured shared library files.
A clean-output cycle: baseline 98.224 s, candidate 53.641 s.
Session `82249` is now executing B Validation (Aspect, fast, full, WarmFull,
clean), then A+B Validation, then strict summary/trace verification.

## Paused session and resumption

The user requested an immediate stop to suspend the session and continue tomorrow.
Unified exec session `82249` was interrupted with Ctrl+C and exited with code 1.
No experiment runner, active build or testhost remained; the Metalama compiler
server remained idle. Do not poll or reuse session `82249`.

There are **116 completed observations**: A 62, B 42, A+B 12. Every completed
observation has zero build/test exit codes. A has completed every required mode
and its clean pair. B has completed Integration, Aspect and fast, plus the full
warm-ups and full pairs 1 and 2. A+B has completed Integration only.
There are **70 protocol observations remaining**, including the interrupted sample.

Interrupted sample: `B/paired-Full-baseline-3`, started at
2026-10-06T01:01:18.6607917+02:00. It had not produced a timing row or accepted
inventory. Its original directory, containing the pre-test library snapshot, was
preserved at `test-results/integration-exploration-c891a32c/interrupted/2026-10-06-paired-Full-baseline-3/`
with `interruption.json`. It was moved out of `comparisons/B/` so the harness can
repeat that sample without overwriting interrupted evidence. Pair 3's candidate
had not started. All 714 frozen source file hashes were verified after stopping.

Progress CSVs were refreshed using explicit `-AllowPartial`. They are **not final
acceptance evidence**; B full is incomplete and omitted from `summary.csv`.
The [comparison draft](comparison.md) deliberately has no final recommendation.

To resume:

1. Read this ledger, the approved plan, testing policy and comparison draft.
   Preserve original source, all accepted observations and the interrupted archive.
2. Confirm the same machine, .NET SDK 10.0.401, source hashes and NuGet cache.
   Verify no experiment build/test process is running. Record the resumption time
   and the overnight interruption as a measurement limitation.
3. Before the resumed B full pairs, perform supplemental excluded baseline and B
   full warm-ups after suspension, storing them separately from accepted timings.
   Do not replace prior samples or include supplemental warm-ups in five-pair
   statistics. Pending modes receive their normal harness warm-ups.
4. Resume the existing orchestration against the same frozen experiment:

   ```powershell
   $experiment = Get-Content test-results/integration-exploration-latest.txt
   pwsh -NoProfile -File prototypes/integration-test-performance/Run-Remainder.ps1 -ExperimentRoot $experiment
   ```

   The harness revalidates and skips accepted rows. It resumes B full at baseline
   pair 3, finishes B full/WarmFull/clean, then A+B validation, strict summary and
   trace checks. It also revalidates completed A and A+B Integration evidence.
5. After all modes complete, verify strict completeness, exact inventories,
   library metadata, command traces and restore controls. Publish all-mode and
   clean-cycle tables, ranges, paired deltas, outliers and limitations.
6. Request the existing `/root/review_harness` agent's read-only final review if
   available, then run repository formatting checks. No benchmark reruns are
   needed unless a concrete new failure requires them.

Read-only draft review favors A as the simpler evidence-backed default: it meets
the goal without restore-state machinery. A+B's lower Integration median remains
suggestive of extra benefit, but separate A and A+B baseline series do not prove
that incremental benefit. Decide after complete full-cycle evidence; do not
automatically adopt A+B merely because its raw candidate median is lowest.

No code adoption, commit, pull request or production/package change is authorized
by the experiment approval. The user's stop request pauses all benchmark work.

## Resumed measurement session

The user explicitly requested continuation on 2026-10-06. The same machine,
SDK 10.0.401, NuGet cache and all 714 frozen hashes were verified; the 116 accepted
observations were retained. No experiment process was active before resumption.
Machine/environment details and UTC resumption time are in the raw experiment's
`resumption.json`. The interruption spans approximately 8.5 hours and will be
reported as a measurement limitation for B full, whose first two pairs predate it.

`Run-Comparison.ps1 -Phase ResumeFullWarmup` reuses the normal inventory and
library-output checks to record two supplemental B full warm-ups with stage
`resume-warmup`. They are excluded from paired and clean statistics. Expected
final count: 186 protocol observations plus these two supplemental observations.
The frozen source trees are unchanged by this harness-only phase.

Active unified exec session `1521` started at 09:33 Europe/Madrid. It runs the
two supplemental warm-ups, then `Run-Remainder.ps1`, which revalidates accepted
samples and completes the 70 outstanding protocol observations. Do not overlap
SDK/test/formatter processes with this session. After completion, publish the
strict summary, report, final read-only review and formatting validation.

At 09:47 Europe/Madrid, B full completed all five pairs with 67 passed canonical
identities and 24 unchanged library files. Baseline samples are 93.299, 95.393,
105.646, 104.864 and 94.596 s; B samples are 85.673, 85.138, 91.793, 88.304 and
83.271 s. Session `1521` moved to B WarmFull warm-up. B WarmFull/clean and all
A+B validation modes remain pending. The two supplemental full warm-ups passed
67 identities and retained the shared library metadata.

B Validation completed at 10:09 Europe/Madrid with all required pairs and clean
observations. B has 62 protocol observations plus two supplemental warm-ups.
WarmFull medians: baseline 100.495 s, B 91.227 s (9.22% lower). Clean cycle:
baseline build 2.527 s, test 98.723 s, total 101.250 s; B build 2.665 s, test
88.431 s, total 91.098 s. All full observations preserve 67 passed identities
and 24 library files. Session `1521` is now running A+B Validation; its 50
validation observations are the remaining protocol work before strict checks.

At 10:17 Europe/Madrid, A+B Aspect and fast completed all five pairs and warm-ups.
They retained 12 and 32 passed identities respectively. A+B fast pair 1 was
slower than its baseline (22.650 to 27.893 s); keep this observation and its paired
regression in the report rather than interpreting unrelated mode fluctuations as
causal improvements. Session `1521` is now in A+B full warm-up. There are 26
protocol observations remaining: full, WarmFull and the clean-output pair.

At 10:33 Europe/Madrid, A+B full completed five pairs and excluded warm-ups.
All samples retained 67 passed identities and 24 shared library files. Candidate
samples were 55.424, 49.046, 51.829, 52.576 and 53.941 s; baseline samples were
111.711, 102.856, 103.576, 112.805 and 116.385 s. Session `1521` moved to A+B
WarmFull warm-up, leaving 14 protocol observations before strict final checks.

## Completed measurement evidence

Session `1521` completed with exit code 0 at approximately 10:53 Europe/Madrid.
All required A/B/A+B modes and clean pairs completed; strict summary and trace
verification passed. There are 186 protocol observations plus two supplemental
resume warm-ups. The final read-only audit reparsed raw TRX for all 188 rows,
verified 8,204 passed canonical identity observations, all zero build/test exits,
24 unchanged library files per test interval, and all 714 frozen source hashes.
Original source/test diff remains empty. The interrupted attempt is archived and
excluded from accepted timings.

[The comparison](comparison.md) recommends A alone for next adoption: it meets
the approved goal and full-cycle requirements without B's graph-state machinery.
A+B also passes, but separately paired baseline series cannot quantify its
incremental benefit over A. A+B warm cycle median is 60.471 s against 107.737 s;
A's is 56.755 s against 97.423 s. These are not direct A-versus-A+B comparisons.

Final independent read-only review is assigned to `/root/final_exploration_review`.
Its [written verdict](final-review.md) confirms specification and quality compliance
with no material findings. It independently checked the published arithmetic,
all 188 inventories and all 714 frozen source hashes.
Repository `npm run format:check` passed (50 CSharpier files, 46 Markdown files,
zero issues). The closing validation command is `npm run format:markdown:check`
to include the final review document and completed ledger. No further performance
measurements, code adoption, commits or pull requests are needed for this task.

## Implementation handoff

At the user's request, [the Option A implementation plan](implementation-plan-a.md)
was prepared on 2026-10-06 for execution in another session. It covers the helper
declaration conversion, six independent wrapper Facts, the DesignTime collection,
two-worker assembly bound, assertion/inventory preservation, correctness checks,
documentation and conditional A-only measurements. Implementation has not started.
The exploration evidence remains unchanged; B is outside the planned adoption.
