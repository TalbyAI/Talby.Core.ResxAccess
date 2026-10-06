# Integration test performance exploration plan

Saved on 2026-10-05 for continuation in another session.

Exploration state: awaiting the user's selection of options and acceptance criteria.
No optimization option has been approved, implemented or practically checked.
The user authorized saving this plan; that authorization does not select an option.
The earlier diagnostic measurements remain valid evidence for the recorded source
version, not proof of any candidate's improvement.

## Goal and conversation state

Find several ways to significantly reduce IntegrationTests execution time, explain
their advantages and disadvantages, and let the user choose which to check before
performing practical experiments. The user explicitly invoked `auto-grill` and
then requested that the exploration plan be saved for another session.

The discovery summary proposed checking A and B separately, then together. This
is a recommendation, not an accepted decision. The proposed 30% improvement
threshold also remains unapproved. Do not infer approval from this document or
from the request to save it. An explicit next-session instruction selecting
options and criteria can supply approval; do not ask again for decisions already
made in that instruction.

Read [the diagnosis](diagnosis.md) and this plan to recover context. Do not rerun
the measurements merely to recover context. Read repository `AGENTS.md` and
[the testing policy](../../docs/agents/testing.md) before any future test changes
or execution optimization. Use `auto-grill` to reconcile corrections or remaining
authority-dependent decisions before starting the selected experiments.

## Evidence already available

The diagnostic source version is commit `7abacfd`, using Windows x64, .NET SDK
10.0.401, Metalama.Framework 2026.1.28 and Metalama.Compiler 2026.1.18.

- IntegrationTests median external wall time: **103.15 s** from three measured
  samples of 102.71, 105.30 and 103.15 s, after a 95.15 s warm-up.
- All four executions passed the same **36-test inventory**, with no overlapping
  test intervals. This is historical evidence, not a hardcoded future test count.
- IncrementalBuildConsumerTests: six histories, **29 builds**, median summed test
  duration **71.48 s**.
- DesignTimeResourceConsumerTests: one initial build and **seven DesignTime
  commands**, median test duration **13.53 s**.
- Together, these classes account for **80.4–82.8%** of measured suite wall time.
- The whole suite launches **37 consumer builds, seven DesignTime commands and
  28 child runtime invocations**. All eight classes share one xUnit collection.
  The two builds inside ConsumerDiagnosticsFixture already run concurrently.
- Thirteen fresh consumer projects account for **24 subsequent builds** that
  currently repeat implicit restore.
- Unchanged-consumer probes: median **2.26 s** with implicit restore and **1.71 s**
  with `--no-restore`. Paired differences: 0.80, 0.59 and 0.28 s. All six builds
  preserved the assembly timestamp.
- The Metalama `MetalamaSetDotnetRootHint` target took **566–749 ms** in isolated
  profiles, including skipped compilation and DesignTime. The inline task body
  itself took 1 ms; its task-factory setup is the substantial cost.
- Library reuse already works: all 46 files under its `bin/Release` and `obj`
  preserved paths, sizes and timestamps across repeated measurements and probes.
- Twelve precompiled-fixture runtime tests passed in **3.07 s**, including host
  startup. A representative child runtime invocation took approximately 58 ms.

Reviewable timing CSVs and the call-site inventory are beside this plan. Raw TRX
files, logs and library metadata snapshots are in ignored
`test-results/integration-diagnosis/`. Those ignored artifacts may be unavailable
in another checkout; the committed diagnosis and CSVs preserve the findings.

## Options presented to the user

Potential is qualitative. None of these estimates is a measured improvement,
and benefits are not necessarily additive.

| Option | Potential | Advantages | Disadvantages and risks |
| --- | --- | --- | --- |
| **A. Bounded concurrency between independent consumer histories** | High | Preserves builds and assertions; consumer directories are isolated and library outputs are already reused without writes. | Requires reorganizing classes/collections. Enabling xUnit parallelism alone is insufficient. More CPU, disk and compiler-server contention could offset the gain. |
| **B. Initial restore followed by `--no-restore` for subsequent builds** | Medium | Small, reversible change targeting 24 repeated restores. Existing isolated probes support the hypothesis. | Valid only while the dependency graph is unchanged. Does not eliminate MSBuild startup or Metalama initialization. Fresh consumers still require restore. |
| **C. Reuse an MSBuild process** | Medium to high, uncertain | Could amortize process startup, evaluation and task initialization across the 44 SDK/MSBuild commands, retaining separate consumer directories. | Greater complexity. Caches could conceal resource discovery or incremental invalidation defects. Changes the current `dotnet` CLI boundary and requires equivalence evidence. |
| **D. Reduce Metalama inline-task initialization** | Medium to high, uncertain | Targets the observed 566–749 ms setup cost, including skipped compilation and DesignTime. | Depends on third-party behavior. A temporary equivalent control is only diagnostic; a permanent solution needs a maintainable integration or suitable dependency update. |
| **E. Precompile additional static consumer scenarios** | Low to medium | Extends the existing ConsumerFixture pattern and reduces builds during test execution. | Can move cost into the solution build without improving the full cycle. Unsuitable for mutation histories; compiler and documentation contracts must remain covered. |

Recommendation pending approval: **A first, B second, then A+B** if their isolated
results justify a combined comparison. D is the next investigation if substantial
fixed cost remains. C is a more expensive alternative. E is secondary.

## Proposed acceptance criteria and exclusions

These are proposed criteria for the user to approve or correct:

- A significant improvement means **at least 30% lower median IntegrationTests
  external wall time**, relative to a controlled baseline of the current source.
- Preserve every scenario, assertion and required resource mutation sequence.
  Changed test identities require an explicit old-to-new coverage map.
- Verify build plus full execution as well as test-only execution. Reject
  reproducible full-cycle regressions or concealed work shifted into builds.
- Keep experiments isolated and reversible. Adopt a candidate only after the
  assertion-preservation and measurement requirements are met.

Out of scope: deleting coverage, disabling Metalama in real consumer tests,
patching installed packages, altering production behavior merely to accelerate
tests, and treating a temporary task override as an approved permanent solution.
This plan does not authorize commits, pull requests or merging changes.

## Checks to perform after selection

### A: bounded concurrency

1. Map shared state, fixture lifetime, culture-sensitive runtime checks and
   writable build outputs before changing scheduling.
2. Keep operations within each consumer history sequential. Start with a bounded
   concurrency of two independent histories rather than unrestricted parallelism.
3. Determine the smallest class/collection reorganization that makes those
   histories eligible for concurrent execution. Preserve the shared diagnostic
   cache and prevent accidental duplication of its two builds.
4. Verify that library outputs remain read-only, temporary projects remain
   isolated, and every existing assertion still executes.
5. Compare against the baseline under the measurement protocol. Consider a higher
   concurrency bound only if the first result and resource contention justify it.

### B: restore once per unchanged dependency graph

1. Map initial and subsequent builds and identify any project/dependency changes.
2. Preserve restore for fresh projects. Skip it only after a successful restore
   for the same graph; a failed compilation can still follow a successful restore.
3. Keep ordinary incremental compilation and all diagnostic/runtime assertions.
   Do not apply `--no-restore` unconditionally or introduce shared writable assets
   between isolated consumers.
4. Compare B against the same frozen baseline, then compare A+B separately if both
   options were selected and their isolated results support combination.

### C, D and E: conditional investigations

- **C:** prototype repeated SDK/MSBuild execution in a persistent process, keeping
  fresh evaluation where resource discovery requires it. Compare compiler inputs,
  diagnostics, manifests, satellite assemblies, invalidation and runtime behavior
  with the existing CLI path before drawing performance conclusions.
- **D:** establish an equivalent, temporary task control that supplies the correct
  environment hint and retains analyzer resolution. Measure its effect without
  modifying installed packages. Separately investigate maintainable dependency
  support; do not assume an available update fixes the measured cost.
- **E:** identify truly static consumer scenarios and map all retained assertions.
  Keep mutation histories, compiler failures and compilation of documentation at
  the boundary that exercises their real contract. Measure build plus full to
  distinguish removed work from displaced work.

Keep future prototype source under top-level `prototypes/`. Keep temporary outputs,
baseline copies, build logs, TRX and controls under ignored `test-results/`.

## Measurement and continuation protocol

1. Obtain the user's selection and resolve the proposed criteria. If the next
   session only requests resumption without selecting options, present the pending
   recommendation and await a decision before practical checks.
2. Inspect the current source and pending changes. If source, SDK or dependencies
   differ from the diagnostic version, establish a fresh baseline after approval;
   do not compare a candidate solely against the old 103.15 s median.
3. Freeze baseline/candidate inputs with separate output trees, on the same machine
   with identical SDK, NuGet cache, configuration, logging and parallelism, except
   for the scheduling variable intentionally changed by A. Restore outside timed
   measurements and perform one untimed warm-up per variant and mode.
4. Follow the repository testing policy: at least five baseline/candidate pairs,
   alternating order and running variants sequentially. Include IntegrationTests
   as the affected boundary, the policy's required suite modes and complete-cycle
   checks, plus an additional clean-output build plus full pair.
5. Record external wall time, build/test time separately and together, exit codes
   and exact passed-test inventories. Preserve samples and report medians, ranges,
   paired deltas and percentage changes. Do not sum overlapping test intervals.
6. Preserve assertions before accepting an improvement. If cases are grouped,
   follow the policy's coverage mapping and temporary negative-control requirements.
7. Save the comparison, limitations and recommendation in this directory. Distinguish
   successful experiment results from any later decision to adopt a candidate.

Useful starting points after approval:

- [ConsumerProject](../../tests/Talby.Core.ResxAccess.IntegrationTests/ConsumerProject.cs)
- [ConsumerBuildCollection](../../tests/Talby.Core.ResxAccess.IntegrationTests/ConsumerBuildCollection.cs)
- [ConsumerDiagnosticsFixture](../../tests/Talby.Core.ResxAccess.IntegrationTests/ConsumerDiagnosticsFixture.cs)
- [IncrementalBuildConsumerTests](../../tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerTests.cs)
- [DesignTimeResourceConsumerTests](../../tests/Talby.Core.ResxAccess.IntegrationTests/DesignTimeResourceConsumerTests.cs)
- [Measurement script](measure.ps1), which currently measures one tree rather than
  a controlled baseline/candidate pair and must not be treated as sufficient for
  accepting an optimization by itself.

## Conversation record

- The user requested diagnosis of slow IntegrationTests. The diagnosis and its
  original probes were completed before the option-selection request.
- The user then requested `auto-grill`, several improvement options with pros and
  cons, and a decision before any new practical checks.
- The agent presented A–E, recommended A and B followed by their combination, and
  proposed a 30% threshold with preserved coverage and full-cycle verification.
- The user requested saving the plan to continue checking in another session.
  No option selection or approval of the proposed threshold was given.
