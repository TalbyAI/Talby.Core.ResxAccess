# Testing criterion

Read this before adding tests, grouping scenarios, or optimizing test execution.

## Choose the boundary

| Project | Behavior to verify |
| --- | --- |
| UnitTests | Isolated Reference Resource, Localized Resource and Placeholder Contract validation through production helpers. |
| AspectTests | Diagnostic code, target and message mapping; introduced members and transformed code through Metalama snapshots. |
| IntegrationTests | Real SDK project properties, manifest naming, resource embedding, satellite assemblies and generated runtime behavior. |

Use the narrowest boundary that reaches the real behavior. Preserve IntegrationTests
for SDK and runtime contracts that deterministic snapshot inputs bypass. Fast runs
the explicit `$fastProjects` list in `tests/run.ps1` (currently UnitTests and
AspectTests); full runs every test project in the solution and is required
before merge. Add new test projects to the solution, and update `$fastProjects`
only if they should also run in fast. The runner uses automatic test discovery
and validates TRX results, without a fixed test-name inventory. Rebuild Release
outputs after source or resource changes.

## Group compatible diagnostic cases

Reduce independent compiler invocations by grouping cases that use the same
compiler settings and test adapter. Keep each case as a descriptively named
target, with its input beside the other cases in a clearly labeled behavior
group. Retain distinct Resource Sets or unique temporary directories per case.
Keep cases with different compiler settings or failure stages in separate
compilations; SDK malformed-XML failures occur before aspects execute.

Before replacing tests, map every original scenario and assertion to its retained
case. Compare every expected diagnostic's code, target and message, including
Resource Keys and resource paths when present. A failed compilation alone is
insufficient. SDK diagnostic assertions require the case's source filename,
diagnostic code and expected message on the same output line.

Keep generation and DesignTime baselines that cover distinct output contracts.
Preserve output-compilation checks and real runtime assertions. Report changed
test identities, compilation isolation and failure granularity; update current
documentation and any reported inventory counts together. Adding, grouping or
renaming tests within an existing project does not require a runner change.

When introducing a grouped diagnostic compilation, run at least one temporary
negative control per behavior group: correct one invalid input so its expected
diagnostic disappears while other errors remain. Require an assertion failure
for that missing diagnostic. Revert all controls, rebuild, and verify the complete
suite. This demonstrates that shared compilation errors cannot mask an omitted
case. Keep the coverage mapping and control results in the change report.

## Measure execution changes

1. Freeze baseline and candidate inputs. Use separate output trees, the same
   machine, SDK, NuGet cache, configuration, logging and parallelism. Restore
   outside timed measurements, build before test-only runs, and run one untimed
   warm-up per variant and mode.
2. Collect at least five baseline/candidate pairs, alternating their order and
   running variants sequentially. Measure affected AspectTests, fast, full, and
   warm build + full. Record build and test wall times separately and together.
   Include one additional clean-output build + full pair.
3. Measure external wall time with a stopwatch. Record exit codes and exact
   passed-test inventories for each sample. Keep all samples and report medians,
   ranges, paired deltas, and percentage change. Overlapping test durations must
   not be summed; Metalama 2026.1.28's reported 1 ms durations are not usable.
4. Verify assertion preservation and the complete cycle before accepting a
   speed improvement. Describe variance and reproducible regressions. A smaller
   test count or a faster `--no-build` run alone does not establish the result.

The 20% median AspectTests wall-time target belongs to the approved
[snapshot consolidation](../../.scratch/aspect-test-performance/spec.md).
It is not a required improvement for every new test. Preserve correctness and
measure any future optimization against its own stated goal.

Store reviewable coverage maps and timing CSVs under `.scratch/<feature>/`.
Keep baseline copies, TRX files, logs, temporary controls and build outputs in
ignored `test-results/` directories.
