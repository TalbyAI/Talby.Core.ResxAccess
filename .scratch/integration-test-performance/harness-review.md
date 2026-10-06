# Integration test performance harness review

Reviewed on 2026-10-05. Scope: `prototypes/integration-test-performance/Run-Comparison.ps1` against `AGENTS.md`, `docs/agents/testing.md`, and `.scratch/integration-test-performance/exploration-preflight.md`. No build or benchmark was run. PowerShell parser-only validation passed.

## Findings

1. **High — Failed samples lose their exit-code record and metadata check** (`Run-Comparison.ps1:89-90, 109-121, 140-147`). A failed build or test throws before the sample is appended to `timings.csv`; the error also omits the captured exit code. For test failures, the throw at line 114 occurs before the post-test library metadata snapshot, so that execution cannot establish whether `bin/Release` or `obj` changed. The result directory remains, and a retry then stops at line 71. Persist the sample status, elapsed times, and exit codes before reporting failure, and take the post-test metadata snapshot before branching on the test exit code.

2. **Medium — Resume trusts timing rows without checking their evidence** (`Run-Comparison.ps1:65-72`). A matching row returns immediately even if the sample directory, `inventory.csv`, logs, or its TRX directory has been removed. The harness can therefore report completion with missing per-sample evidence. Before skipping a row, confirm its required artifacts still exist and correspond to the recorded sample.

3. **Medium — The harness does not produce the required paired summary** (`Run-Comparison.ps1:140-148, 190`). It saves raw timings and inventories, but does not report medians, ranges, paired deltas, or percentage changes required by `docs/agents/testing.md` under “Measure execution changes.” Add a summary from the five completed pairs, keeping the clean pair separate from the warm pairs.

4. **Low — The extra clean pair repeats baseline-first order** (`Run-Comparison.ps1:175-187`). The five warm pairs alternate order, but the clean pair always runs baseline before the candidate. Run the clean pair candidate-first so it continues the alternating sequence.

5. **Low — Duplicate identity detection is case-insensitive** (`Run-Comparison.ps1:58`). The final inventory comparison is case-sensitive at line 139, but `Sort-Object -Unique` uses its default case-insensitive comparison here. Distinct canonical identities that differ only by case can be rejected as duplicates. Use a case-sensitive uniqueness check to preserve exact test identities.

## Checks that passed

- PowerShell parser validation found no syntax errors.
- `Stopwatch.Elapsed.TotalSeconds` is used for build, test, and total durations, and the CSV labels those values in seconds.
- The harness reads the raw TRX `testName`, applies each tree's `identity-map.json`, derives assembly identity from the matching test definition's `storage`, and writes an inventory for each successful sample. This matches the expected per-tree canonical map flow.
- `tests/run.ps1` currently emits `Verified fast/full results: <count> passed. Results: <path>`; the harness regex at line 126 matches that output.
- Each selected mode has an untimed baseline and candidate warm-up, then five sequential pairs with alternating order. `All` covers Integration, Aspect, Fast, Full, and WarmFull; `Integration` and `Validation` allow the two groups to be run in phases.
- The clean path invokes `dotnet clean`, then measures the `WarmFull` build and full test run. Restores are not invoked by this harness; the measured test code remains responsible for any B-specific ephemeral child restore.
- Successful consumer test executions compare file paths, lengths, and last-write timestamps under the library's `bin/Release` and `obj` trees before and after the tests.

## Follow-up review

Reviewed the updated `Run-Comparison.ps1`, `Summarize-Comparisons.ps1`, and `Initialize-Experiment.ps1` on 2026-10-05. Parser-only validation passed for all three scripts; no builds or tests were run.

- The earlier failure-recording issue is resolved: the harness writes failed samples to `failures.csv`, captures elapsed time and available exit codes, and takes the post-test library metadata snapshot before checking the test exit code. Failed samples remain out of `timings.csv` and the summary by design. Their directories block automatic retries, preserving the evidence for inspection.
- Resume now rejects nonzero recorded exit codes, checks required sample artifacts, and reconstructs the inventory from the recorded TRX directory before reusing a timing row. Missing TRX evidence fails closed through `Read-Inventory`; this protects reuse even though the TRX path is not checked with a separate existence test.
- Canonical identity uniqueness is case-sensitive, and the clean pair now runs candidate first as pair 6, continuing the alternating order.
- **Remaining medium finding — incomplete comparisons can yield a partial summary without an error** (`Summarize-Comparisons.ps1:25-35, 69-71`). A group with other than five baseline or candidate rows is silently skipped, and the clean-cycle export does not verify that both variants are present. The script can therefore write a successful-looking summary that omits a mode or candidate after an interrupted run. Fail loudly for incomplete or unbalanced pair sets and require the baseline/candidate clean pair before writing final evidence.
- `Initialize-Experiment.ps1` records the source commit, SDK, OS, architecture, processor count, NuGet package path, temp directory, configuration and logging, then restores and builds each baseline/candidate tree before timing; both command exit codes are checked. This matches the preflight's preparation boundary. No additional initializer finding.

## Summary completeness resolution

The earlier partial-summary finding is resolved in the updated `Summarize-Comparisons.ps1`. By default it requires pair IDs 1–5 for both baseline and each of A, B, and AB in Integration, Aspect, Fast, Full, and WarmFull, plus exactly one clean sample for each baseline/candidate pair. It rejects nonzero timing-row exit codes. `-AllowPartial` is an explicit progress-only path and labels its output as not acceptance evidence. Parser-only validation passed; no .NET commands were run during this follow-up review.
