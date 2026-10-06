# Integration test performance exploration preflight

Reviewed on 2026-10-05 at commit `71f1ab3`, using .NET SDK 10.0.401.
Scope: source inspection and experiment selection. No candidate changes,
benchmark executions, commits or pull requests were performed.

The current request authorizes using the exploration plan to determine which
option to apply. The plan explicitly leaves option selection and the 30%
acceptance threshold pending. Its required `auto-grill` approval gate remains
open for those decisions; a recommendation is not an adoption decision.

## Current baseline differs from the historical diagnosis

Compared with diagnostic commit `7abacfd`, `DocumentationConsumerTests.cs` has
been removed and `ConsumerProject.FindRepository` is now private. The current
source declares 35 `[Fact]` tests in seven IntegrationTests classes, compared
with the historical executed inventory of 36 tests in eight classes.

The removed documentation scenario accounts for one initial consumer build and
one runtime invocation. The six incremental histories, their 29 builds, the
DesignTime history and the 24 repeated implicit restores remain in source.
These are source observations, not a new executed inventory or timing result.

Use a fresh frozen baseline from the current source for every comparison.
The historical 103.15 s median explains the hypothesis but cannot establish
a candidate's percentage improvement. Preserve all current assertions;
the earlier documentation-test removal must not be credited to a candidate.

## Source findings

- Each `ConsumerProject` uses a GUID temporary directory and its own consumer
  `bin` and `obj` outputs. Operations within every history are awaited in order.
- Builds already pass `BuildProjectReferences=false` and
  `RestoreRecursive=false`. Read-only library reuse was verified historically
  and must be verified again during concurrency experiments.
- All current classes share the `SDK consumer builds` collection. The six
  incremental histories also share a class, so enabling collection parallelism
  alone cannot run those six methods concurrently.
- Five classes consume the collection's single `ConsumerDiagnosticsFixture`.
  Its lazy task performs exactly two isolated builds concurrently and caches
  their results. Keep those classes together to avoid duplicate diagnostic builds.
- NamedPlaceholder and IndexedPlaceholder runtime tests change the test host's
  current cultures and restore them in `finally`. Keep their existing shared
  collection. Incremental and ResourceKeyIdentifier culture changes in consumer
  source strings run in child processes instead.
- Repeated builds edit source/resource files, including additions and removals.
  Inspection found no test rewriting the consumer project, package references,
  props or targets between builds. Embedded Resource discovery changes must
  still execute; skipping restore must not skip evaluation or compilation.
- A nonzero build exit code is not evidence that restore failed. Option B must
  establish successful restore for the same dependency graph before skipping it,
  including histories containing expected compilation failures.

## Proposed selection and acceptance

1. Check **A** first: expose independent incremental histories to scheduling,
   starting with two concurrent histories and sequential operations within each.
   Keep the existing diagnostic fixture and culture-sensitive collection.
   Verify the actual child-build concurrency, including the fixture's internal
   pair, rather than assuming an xUnit thread setting bounds every process.
2. Check **B** independently against the same baseline: retain initial restore,
   then skip repeated restore only while successful restore and an unchanged
   dependency graph are established. Measure any explicit initial-restore cost.
3. Check **A+B** if their independent results justify the combination.
4. Use **at least 30% lower median IntegrationTests external wall time** as the
   proposed significant-improvement goal for the selected final candidate.
   Report isolated results even when they do not individually reach that goal.
5. Preserve all current scenarios, assertions and ordered mutations. Map any
   changed test identities before executing the candidate. Do not consolidate
   diagnostic compilations as part of A or B.
6. Follow `docs/agents/testing.md`: separate frozen output trees, restores outside
   timed runs, one untimed warm-up per variant and mode, at least five alternating
   sequential baseline/candidate pairs, and exact passed-test inventories.
   Cover IntegrationTests, AspectTests, fast, full and warm build plus full;
   add a clean-output build plus full pair. Record build/test/cycle times,
   medians, ranges, paired deltas, exit codes and library output metadata.
7. Reject reproducible full-cycle regressions, assertion loss or changed
   incremental contracts. A faster test-only result is insufficient.

## Alternatives and exclusions

**D** is the next investigation if fixed Metalama initialization cost remains
material after A/B. Historical target profiles support investigating it, but
no maintainable fix or dependency update has been established.

**C** requires a persistent MSBuild host and equivalence checks for evaluation,
diagnostics and invalidation. Its implementation cost and cache risks make it
secondary to A/B. **E** has limited scope because the dominant tests are mutation
histories; precompilation can displace cost into the solution build.

The experiments are reversible and do not authorize adoption, production changes,
removed coverage, package patching, disabling Metalama, commits or pull requests.
Store prototype source under `prototypes/`, temporary trees and raw outputs under
ignored `test-results/`, and reviewable comparison evidence beside this report.

Next action after approval: create the frozen baseline/candidate trees and the
paired measurement harness, then execute the selected comparisons. A/B selection
and the proposed 30% goal remain pending explicit approval or corrections.
