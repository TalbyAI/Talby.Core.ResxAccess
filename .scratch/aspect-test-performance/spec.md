# Aspect snapshot consolidation

Status: implemented and verified

## Approved design

On 2026-10-04, the user approved consolidating five compatible diagnostic
snapshots into `ResourceValidationDiagnostics.cs`, with five named case groups.
Preserve all 24 diagnostic expectations and all other snapshot baselines. Keep
the production library, public API, dependencies, test runner and existing
parallelism unchanged.

The AspectTests inventory becomes eight tests; fast becomes 27 and full becomes
43. This intentionally changes four snapshot identities and compilation
isolation, while retaining every scenario and assertion. Resource Sets remain
isolated by the existing deterministic adapter's unique temporary directories.

Persist the testing criterion in `docs/agents/testing.md`, referenced by
`AGENTS.md` and `README.md`. Prefer UnitTests for isolated validation,
AspectTests for diagnostic mapping and introduced members, and IntegrationTests
for SDK embedding and runtime behavior. Group compatible diagnostic cases only
with explicit per-case assertions, coverage mapping and negative controls.

## Acceptance

- All 24 original diagnostic lines are compared, and remaining baselines are
  unchanged. A missing diagnostic must fail even when other errors remain.
- Release build, fast and full pass with their updated inventories.
- At least five alternating baseline/candidate pairs use external wall time,
  identical logging and separate output trees. Measure AspectTests, fast, full
  and warm build + full. Restore and warm-ups are outside timed samples.
- Include a clean-output build + full pair. Report all observations, medians,
  ranges, absolute and percentage changes, and any changed isolation.
- The target for this optimization is at least 20% lower median AspectTests
  command wall time, without a reproducible regression in the complete cycle.
  The target is specific to this change, not a rule for every new test.
- No publication, merge, production API change, custom runner or cache framework.

## Evidence

Diagnostic baseline: `c52b9c326a5a68ce8db971f5e1fe62c2dc4ad44a`.
The previous ignored diagnostic report measured 10.831 s for included helpers,
7.539 s for batching and 6.885 s for experimental precompiled helpers. These
three-sample probe results motivate the design; they are not the acceptance
measurements for the actual change.

Actual acceptance result: 21.2% lower median AspectTests command wall time and
19.3% lower median warm build + full cycle, with all retained assertions passing.
See [the result report](results/consolidation.md).
