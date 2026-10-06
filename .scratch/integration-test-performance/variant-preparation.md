# Reversible IntegrationTests variants

Prepared a generator for four independent source trees: baseline, A, B and AB.
Each tree starts from the same git archive of HEAD. The source archive and a
per-file SHA-256 manifest are written under the ignored ExperimentRoot. Each
tree also receives an identity-map.json and coverage-verification.json.
Baseline and B identity maps are empty; A and AB map the six wrapper test
identities back to their original baseline identities.

Run prototypes/integration-test-performance/New-Variants.ps1 with mandatory
RepositoryRoot and ExperimentRoot arguments. ExperimentRoot must be a fresh,
git-ignored child of RepositoryRoot. The generator checks all tree, archive
and manifest destinations before writing and fails if one already exists.
It does not delete prior output. It archives HEAD, expands baseline, copies
the source trees sequentially, then applies the patches.

## A: schedule incremental histories

The original IncrementalBuildConsumerTests source becomes the internal static
IncrementalBuildConsumerHistories helper. The six history methods keep their
ordered, awaited operations and existing helpers/assertions. Six thin xUnit
test classes expose those histories in distinct collections. The test
assembly allows at most two collection workers. The five classes that use
ConsumerDiagnosticsFixture stay together in SDK consumer builds, preserving
one fixture instance. DesignTimeResourceConsumerTests gets a separate
collection.

The generator reverses the helper's mechanical class, Fact and static-method
declaration edits, normalizes line endings, and compares the resulting SHA-256
with the archived original. It aborts if that source check differs. The
coverage map lists every original method, its retained identity, and its
scenario/assertion contract.

## B: skip repeated restore using a successful-restore marker

B keeps the existing first dotnet build invocation and its implicit restore.
ConsumerProject adds one target to each generated consumer project:
RecordConsumerRestoreSuccess runs AfterTargets=Restore, creates the consumer's
obj directory if needed, and writes ResxAccess.RestoreSucceeded there. The
marker is local to the temporary consumer and does not touch the referenced
library's obj directory.

After a build, ConsumerProject records successful restore state only if that
target-created marker exists. It does not use the build exit code or
project.assets.json as evidence. Later builds add --no-restore only while the
marker exists and the Consumer.csproj SHA-256 still matches. Writes through
ConsumerProject.Write to project, props, targets, solution, NuGet config,
package config or lock files delete the marker and reset the state. Source and
resource writes do not invalidate it. A compile failure after restore leaves
the successful marker in place, so the next operation can skip restore.

This keeps the original build-process count and diagnostic output shape. It
adds the marker target to B and AB, which is part of the measured candidate
cost. If a failed restore leaves the marker absent, the next Build retries
the original implicit-restore path.

## Shared opt-in traces

The same lightweight trace hook is present in all four trees. With
TALBY_CONSUMER_TRACE_DIR set, each child dotnet command writes a unique JSON
file containing args, startTimeUtc, endTimeUtc, exitCode and projectPath.
Without the environment variable it writes no files. Use it only for excluded
warmups or diagnostic probes; leave it unset for timed pairs. This lets the
owner verify the first implicit restore, subsequent --no-restore commands,
and actual overlapping child processes.

## Acceptance and unresolved checks

The approved comparison goal is at least 30% lower median external wall time
for IntegrationTests. Compare A and B independently with the same frozen
baseline, then AB if justified. Retain the full-cycle and assertion-preserving
requirements from exploration-preflight.md and docs/agents/testing.md.

The source generator and coverage map are prepared; no build, test or
benchmark was run while preparing them. Before interpreting timings, the
experiment owner should confirm:

- xUnit schedules the six one-test collections with no more than two active
  test collections at a time. The fixture's two internal builds can overlap
  one incremental history, so child-process concurrency can reach three.
- The AfterTargets=Restore target runs during the first implicit restore and
  writes its marker only after a successful restore. Confirm this from an
  excluded probe trace and the marker file. If it does not, reject B until the
  marker mechanism is corrected.
- Current tests do not change the dependency graph after construction.
  Project-file edits outside ConsumerProject.Write are detected by the
  fingerprint; props, targets and package configuration invalidation relies
  on writes through ConsumerProject.Write. The preflight found no graph edits
  during the existing histories.
- The common trace hook stays disabled during timing samples.

The first initializer attempt exposed Windows line-ending differences in
template replacement. The generator's replacement helpers were updated to
normalize source, marker text and replacement text to LF before matching;
the experiment owner should rerun initialization into a fresh ignored
ExperimentRoot after that correction.
