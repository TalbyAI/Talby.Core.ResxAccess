# Aspect snapshot consolidation implementation plan

> For agentic workers: execute this approved plan inline using the existing
> repository test infrastructure. Keep source edits sequential; request a
> read-only review after implementation and verification.

**Goal:** Simplify AspectTests and persist a testing criterion while targeting
at least 20% lower median AspectTests command wall time.

**Architecture:** One diagnostic snapshot includes the existing production
helpers once and checks 24 named target diagnostics. Generation, DesignTime,
UnitTests, IntegrationTests and production code retain their existing roles.

**Tech stack:** .NET SDK 10.0.401, Metalama 2026.1.28, xUnit, PowerShell 7.

## Global constraints

- Preserve all scenarios, exact diagnostic messages and other baselines.
- Keep nullable settings, dependencies, public API and compile-time settings.
- Use English in repository documents and the existing Resource Access glossary.
- Keep generated measurements and baseline copies under ignored test-results/.
- No publication or merge.

## Task 1: Consolidate snapshots and persist policy

- [x] Archive the clean baseline and restore/build it separately.
- [x] Keep the existing @Include header and namespace in
  `tests/Talby.Core.ResxAccess.AspectTests/ResourceValidationDiagnostics.cs`.
  Append the unchanged declarations from ResourceEntryValidationDiagnostics,
  LocalizedResourceDiagnostics, ExpectedCultureDiagnostics and
  IndexedPlaceholderDiagnostics, with one descriptive comment per group.
- [x] Concatenate the five original `.t.cs` baselines in source-group order.
  Remove the four redundant input/baseline pairs. Verify that all 24 lines
  are retained and that all eight other baseline files match the baseline.
- [x] Remove the four absorbed identities from `tests/run.ps1`.
- [x] Write `docs/agents/testing.md` with layer selection, grouping criteria,
  exact assertions, negative controls and performance-measurement rules.
  Link it from AGENTS.md and README.md. Update current counts to 8 / 27 / 43;
  preserve the meaning of historical experiment results.
- [x] Run `dotnet restore Talby.Core.ResxAccess.slnx`, then
  `dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore`,
  then fast and full. Expected: zero build errors, 27 and 43 passed identities.
- [x] Temporarily correct one invalid input in each of the five diagnostic
  groups; run only ResourceValidationDiagnostics and require an assertion
  failure for the missing diagnostic while other diagnostics remain. Revert
  each control, then rebuild and run all eight AspectTests successfully.

## Task 2: Verify performance and review

- [x] Restore/build both variants outside test-only measurements. Run one
  untimed warm-up per variant and mode. Use default parallelism and the same
  verbosity/TRX logging in both variants.
- [x] Collect five alternating pairs for AspectTests, fast, full, and warm
  build + full. Record stopwatch wall times, build/test exits, and exact passed
  identities. Measure one additional clean-output build + full pair.
- [x] Publish raw timings, coverage mapping, controls, command recipes and
  medians/ranges under `.scratch/aspect-test-performance/results/`.
- [x] Request a read-only review of the diff against the approved spec and
  fix material findings. Verify the final source diff, inventories and result
  evidence. Report the achieved reduction and any limitations.
