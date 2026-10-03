# UnitTests and IntegrationTests project split

Date: 2026-10-03
Status: implemented for review
Issue: [04](../issues/04-separate-unit-and-integration-test-projects.md)
Approved baseline: `f10267545ab7526082d7afabc3d5ae30ee34ba3a` (PR #3 merged into main)
Working branch: `test/separate-unit-and-integration-projects`
Environment: Windows, .NET SDK 10.0.401, Release, PowerShell 7

## Changes

The former ordinary test project is now `Talby.Core.ResxAccess.UnitTests`,
including its directory, project filename, namespace, and default assembly name.
It retains MetalamaSetupTests and the seventeen ReferenceResourceTests added by
approved experiment 03. The library grants internal access to its new assembly
identity; the Metalama UnitTesting dependency stays with UnitTests.

The new `Talby.Core.ResxAccess.IntegrationTests` project contains all four
RawTextConsumerTests and ConsumerProject. The ConsumerFixture reference moves
here, copying its executable, runtime configuration, and satellite assemblies
into this project's output. Both ordinary projects retain the library reference,
.NET 10, nullable reference types, implicit usings, and `MetalamaEnabled=false`.
Package versions are unchanged. The library retains
`MetalamaRemoveCompileTimeOnlyCode=false`.

The dedicated AspectTests project, its snapshots, the SDK fixture, and resource
assets are unchanged. The solution now contains five projects, three of which
are test projects. README and AGENTS.md document the new paths and independent
commands. The parent spec records PR #3's merge and this task's review handoff;
historical experiment reports, scripts, and measured identities are untouched.

Fast invokes UnitTests and AspectTests explicitly, without visiting the
IntegrationTests project. Full invokes the unfiltered solution-level test
command. The inventory changes only the ordinary test namespace prefixes:
fast retains 25 identities and full retains all 29.

## Test identity and assertion mapping

Baseline solution discovery (`--list-tests`) listed 29 tests. Final execution
also discovered and passed 29 tests. The complete one-to-one map, including all
method suffixes and unchanged AspectTests identities, is in
[04-identities.csv](04-identities.csv), derived from final passed TRX results.

| Baseline identity prefix | Candidate identity prefix | Count | Preserved assertions |
| --- | --- | ---: | --- |
| `Talby.Core.ResxAccess.Tests.MetalamaSetupTests.` | `Talby.Core.ResxAccess.UnitTests.MetalamaSetupTests.` | 1 | Creating a Metalama compilation and querying its single type |
| `Talby.Core.ResxAccess.Tests.ReferenceResourceTests.` | `Talby.Core.ResxAccess.UnitTests.ReferenceResourceTests.` | 17 | Text entries, SDK manifest names, string types, empty values/resources, case-sensitive keys, malformed XML, root/value/type structure, duplicate/unnamed keys, paths and project context, culture-neutral references, SDK map availability/shape/path comparison, custom/linked metadata, containment, and keyword/Unicode identifiers |
| `Talby.Core.ResxAccess.Tests.RawTextConsumerTests.` | `Talby.Core.ResxAccess.IntegrationTests.RawTextConsumerTests.` | 4 | SDK compilation and embedding, generated Raw Text and independent Resource Sets, associated naming, culture selection and fallback, invalid resources/embedding, malformed XML, and runtime missing-manifest/missing-key failures |
| AspectTests displayed names | Unchanged | 7 | InvalidPaths, UnavailableProjectContext, UnsupportedTargets, RawTextGeneration, ResourceKeyIdentifiers, ResourceValidationDiagnostics, KeywordResourceKey snapshots |

Each moved C# file was compared with the baseline after normalizing line endings
and replacing its new namespace with the old namespace. All four files were
otherwise identical. No test, assertion, scenario, or helper behavior was added
or removed; TDD required no new behavioral test for this project organization
change. Existing tests provide the verification boundary.

Integration assertion mapping:

- `CanCompileAndInvokeIndependentResourceSets`: preserves successful process exit
  and exact multiline output, including whitespace, Formatting Placeholders,
  parent-culture and Reference Resource fallback, associated resources, and
  generated identifier behavior.
- `ReportsEachInvalidResourceAndEmbeddingInOneBuild`: preserves failed compilation
  and each target-specific diagnostic code/message for LogicalName,
  ManifestResourceName, linked metadata, missing resources, and localized references.
- `ReportsMalformedReferenceResourceWithoutAspectCrash`: preserves failed compilation,
  `MSB3103`, and absence of `LAMA0041`.
- `DescribesMissingRuntimeManifestAndResourceKey`: preserves independent process
  invocations for both failures, successful exits, and descriptive output.

ConsumerProject retains fresh GUID-based temporary projects, process timeouts,
captured stdout/stderr, a different build working directory, cleanup, and failure
reporting. ConsumerFixture retains fresh runtime processes and culture restoration.
Full retains two failed temporary SDK builds and three fresh runtime invocations.
Ordinary unit and integration tests now have separate test hosts and output
directories. Fast runs its two projects sequentially; full retains solution-level
scheduling. No ordering dependency is introduced.

## Verification

Commands ran from the repository root; all successful commands exited zero:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet clean Talby.Core.ResxAccess.slnx --configuration Release --verbosity quiet
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test tests/Talby.Core.ResxAccess.UnitTests/Talby.Core.ResxAccess.UnitTests.csproj --configuration Release --no-build --no-restore --logger 'trx;LogFileName=unit.trx' --results-directory test-results/04-project-split
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --logger 'trx;LogFileName=integration.trx' --results-directory test-results/04-project-split
pwsh -NoProfile -File tests/run.ps1 -Mode fast
pwsh -NoProfile -File tests/run.ps1 -Mode full
```

The full runner invokes the standard solution-level command, adding verbosity
and TRX logging only:

```powershell
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

| Check | Result |
| --- | --- |
| Restore and clean Release solution build | Passed; zero warnings and errors |
| Independent UnitTests | 18 passed, zero failed/skipped |
| Independent IntegrationTests | 4 passed, zero failed/skipped |
| Fast route | 25 passed; four IntegrationTests deferred; two test projects invoked |
| Full solution route | 29 passed, zero failed/skipped; all three test projects invoked |
| Missing selection control | Replacing the expected setup identity with a nonexistent identity made fast exit 1 with an inventory mismatch |
| Unexpected selection control | Removing InvalidPaths from the expected inventory made fast exit 1 with an inventory mismatch |
| Control cleanup | Runner restored exactly before final full execution |
| Source/assertion preservation | Namespace-only changes in all four moved files; fixture and AspectTests unchanged |

Both negative controls retained successful test execution and demonstrated that
the inventory guard itself rejected incorrect selections. Logs and generated TRX
files are under ignored `test-results/`; none is included in the change set.

## Review boundary

Independent code-review axes reviewed the complete staged diff against approved
baseline `f102675` before commit:

- Standards: zero findings; documented repository/domain standards are followed,
  with no baseline code smells identified.
- Spec: zero findings; Task 04's project boundaries, dependencies, execution
  selection, preservation requirements, and verification evidence are satisfied.

This task makes the current execution boundaries explicit while preserving every
scenario. It does not measure or claim a performance improvement. The experiment
01–03 measurement protocol is outside Task 04's project organization scope.
Fast continues to defer SDK wiring, embedding, and runtime lookup feedback;
full remains the complete verification gate. Permanent classification policy
and candidate integration remain user decisions.
