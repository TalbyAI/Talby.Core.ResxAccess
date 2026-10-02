# Separate fast and full test execution

Status: ready-for-human
Type: HITL
Previous option: 2

## Parent

[Test execution policy experiments](../spec.md)

## What to build

Provide explicit fast and full test commands using the existing test projects and xUnit filtering. Mark the temporary consumer tests as integration tests; the fast path runs the remaining unit tests and all AspectTests. The full path retains every test and remains the behavior of the standard solution-level `dotnet test` command.

Use the smallest reproducible entry points with correct exit-code propagation. Document when each command runs and make the full suite the required verification before merge. No CI provider is configured in the repository: provide CI-ready commands without introducing a provider or claiming enforcement that does not exist.

This changes execution cadence, not test coverage or production behavior. Deferring integration tests must be visible in the command documentation and results.

## Acceptance criteria

- [x] Deliver working fast and full commands, including prerequisites for fresh or changed source. Reuse existing tooling; introduce no test dependency or general runner framework.
- [x] Keep the complete suite selected by the standard solution command. Explicitly identify the tests omitted by fast execution.
- [x] Verify discovered test identities: fast and integration selections are disjoint and their union equals the full suite. Reject empty or unexpectedly missing selections rather than treating them as a successful fast check.
- [x] As a temporary negative control, break an integration assertion and show that the full command fails while the documented fast selection excludes that test. Revert the mutation before final verification.
- [x] Follow the parent measurement protocol. Compare baseline full execution with candidate fast execution as reduced local work, and baseline full execution with candidate full execution as equivalent work. Report both test-only and build + test timings, including the cost of building projects omitted from fast execution if applicable.
- [x] Record the unchanged assertion inventory, deferred integration failures, and the remaining gaps in current AspectTests; do not imply that fast success verifies generation and runtime lookup end to end.
- [x] Publish `results/01-execution.md` with measurements, commands, checks, diff summary, and recommendation. Present this change set for user review before adoption.

## Blocked by

None technically. Implementation begins only when requested; this planning issue does not start it.

## Comments

Human review is required because the experiment changes when integration regressions become visible. It is not expected to make the full suite substantially faster, and it does not delete any tests or establish the final classification policy.

2026-10-02: Implemented on `test/separate-fast-and-full-execution` from approved
baseline `7fa01ab`. [Results and review material](../results/01-execution.md)
include 39 observations, unchanged assertion mapping, discovery set checks,
selection guard controls, and the integration negative control. The candidate
is ready for human review; it has not been adopted or merged. Status records
the review handoff rather than remaining implementation work.
