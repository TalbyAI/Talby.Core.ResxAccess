# Talby.Core.ResxAccess

Metalama generates Raw Text and formatting methods on a consumer-declared,
non-generic static class. The Reference Resource defines the API and every
Localized Resource must preserve its case-sensitive Resource Keys and Placeholder
Contracts. Ordinary builds do not require `partial`.

```csharp
using Talby.Core.ResxAccess;

[GenerateResxAccess(
    "Resources/Labels.resx",
    ExpectedCultures = new[] { "es" },
    InvalidKeyHandling = InvalidKeyHandling.Normalize)]
internal static class Texts
{
}
```

For a `Welcome` Resource Key with Translation `Hello {name@string}!`, use
`Texts.Welcome()` for Raw Text or `Texts.FormatWelcome("Ada")` for Formatted Text.
Resource Culture defaults to `CurrentUICulture`; Formatting Culture defaults
independently to `CurrentCulture`. Explicit culture overloads are also generated.

Read the [Resource Access consumer guide](docs/resource-access.md) for a complete,
compiled example, placeholder syntax and Argument Types, Translation validation,
identifier policies, diagnostics, supported SDK embedding and incremental builds.
Source consumers must import the library's targets in addition to their
`ProjectReference`; the guide includes the project setup. Packages built from the
library include the transitive targets automatically.

Automatic IDE resource refresh is not verified. The recorded VS Code setup has
C# Dev Kit project-loading failures, requires `partial` for initial standalone
C# recognition, and leaves the API stale after resource edits. These technical
requirements were explicitly deferred. See the guide's
[IDE evidence and deferred requirements](docs/resource-access.md#ide-evidence-and-deferred-requirements)
for the limitations and reproducible evidence.

## NuGet package

The first beta is `0.1.0-beta.1`, targeting `net10.0` with MIT licensing.
Once published to NuGet.org, install it in a .NET 10 consumer:

```powershell
dotnet add package Talby.Core.ResxAccess --version 0.1.0-beta.1
```

The package includes Metalama.Framework as a dependency and imports its resource
targets through `buildTransitive`. Keep Metalama enabled in the consumer.
The [package README](src/Talby.Core.ResxAccess/README.md) includes a runnable example.
The [manual release guide](docs/releasing.md) explains local validation, account
ownership checks and publication of the exact validated `.nupkg`.
Its [PowerShell release wizard](docs/releasing.md#powershell-release-wizard) reads
the version from the project XML and guides preparation, publication and verification:

```powershell
pwsh -NoProfile -File scripts/release.ps1 -Plan
pwsh -NoProfile -File scripts/release.ps1 -Mode Prepare
```

IntegrationTests packs the Release library and verifies an isolated local-feed
`PackageReference` consumer, including generated methods, satellite assemblies
and Resource Culture fallback. An optional `TALBY_TEST_PACKAGE` environment variable
selects an existing archive for the same checks.

## Command recipes

Install [just](https://just.systems/man/en/packages.html) and PowerShell 7 (`pwsh`)
to use the root `justfile`. Recipes run from the repository root, including when
invoked from a subdirectory. The .NET SDK, Node.js 24, Git and network prerequisites
in the sections below still apply. The direct commands remain available without
`just`.

```powershell
# List the available recipes.
just

# Restore solution dependencies, .NET tools, and npm tooling after cloning.
just restore

# Restore solution dependencies and build all projects in Release.
just build

# Build and run fast tests; these two commands are equivalent.
just test
just test fast

# Build and run every solution test with TRX validation before merge.
just test full

# Use the formatting tools installed by just restore.
just format
just format-check

# Build and pack a local archive into artifacts/nuget/.
just pack

# Inspect the release plan, then prepare the validated release archive.
just release-plan
just release
```

`build`, `test` and `pack` restore only solution dependencies; they do not repeat
`.NET` tool restore or `npm ci`. Run `just restore` again after the tool manifest
or npm lockfile changes. Formatting recipes require that tooling to be installed.
Commands stop on failure and propagate the failing command's exit code.

`pack` creates an archive without recording release validation evidence. Use
`just release` (equivalent to `just release Prepare`) for formatting checks, full
tests, exact archive validation and release evidence. The release wizard retains
its interactive confirmations. See the [release recipes](docs/releasing.md#release-recipes)
for publication modes and verification of the published package.

## Formatting and staged-file checks

Install Node.js 24 with npm, alongside the .NET SDK specified below. Tooling is
local to the repository: CSharpier is pinned in `.config/dotnet-tools.json`, and
markdownlint-cli2, Husky and lint-staged are pinned in `package.json` and
`package-lock.json`. No global formatter installation is required.

Run from the repository root after cloning:

```powershell
dotnet tool restore
npm ci
```

`npm ci` installs the pre-commit hook through Husky. Ensure Git, Node.js, npm and
dotnet are available on the PATH used by your Git client. Restore the tools again
after their manifest or lockfile changes.

```powershell
# Format C# and Project XML, and fix supported Markdown issues.
npm run format

# Verify formatting without changing files; also used by CI.
npm run format:check

# Run the pre-commit checks manually on staged files.
npm run format:staged
```

CSharpier uses four-space C# indentation, two-space Project XML indentation and
preserves existing line endings. markdownlint-cli2 applies the default Markdown
rules except line length (`MD013`), allowing long commands and tables. Some
Markdown errors require manual correction; `format` and the hook fail if errors
remain after automatic fixes.

The pre-commit hook formats staged C#/Project XML and Markdown files, stages its
fixes, and blocks the commit on remaining errors. lint-staged preserves unstaged
changes in partially staged files. Markdown checks use `--no-globs` in the hook
so unrelated documents are not scanned.

Both tools exclude the top-level `prototypes/` directory, dependencies and
generated output (`bin/`, `obj/`, `artifacts/`, `test-results/`). CSharpier also
excludes `.resx` and Metalama output snapshots (`*.t.cs`, `*.i.cs`), whose format
is owned by the snapshot runner. Keep all future prototypes under root
`prototypes/`; the IDE feasibility probe is kept there.
Repository Markdown includes `README.md`, `AGENTS.md`, `CONTEXT.md`, `docs/` and
the local issues/specifications in `.scratch/`.

The current npm dependency audit reports a high-severity advisory in `braces`,
a transitive dependency of markdownlint-cli2, with no patched release available:
[GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm).
It concerns stack exhaustion from deeply nested glob patterns. The commands
above use repository-controlled patterns; this is development tooling and is
not a runtime dependency of the library. Review the advisory when updating tools.

## Build and test

Install .NET SDK 10.0.401 or a later patch in the 10.0.4xx feature band,
as selected by `global.json`. NuGet.org access is required for restore.

Run from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

The solution contains three test projects:

- `Talby.Core.ResxAccess.UnitTests`: 20 tests of Metalama setup and shared Reference Resource validation.
- `Talby.Core.ResxAccess.IntegrationTests`: 38 SDK consumer tests, with the `ConsumerProject` helper, a reference to ConsumerFixture and NuGet package validation.
- `Talby.Core.ResxAccess.AspectTests`: 12 dedicated Metalama snapshot tests.

### Integration test scheduling

IntegrationTests uses at most two concurrent xUnit collection workers.
Each incremental consumer history has its own Fact class and collection;
mutations, builds and runtime checks within a history remain sequential.
DesignTime uses a separate collection. The five diagnostic classes retain
one shared collection and one lazy ConsumerDiagnosticsFixture.

Temporary consumers use isolated directories and reuse library outputs
without modifying them. The fixture's two builds can overlap another history,
so the worker limit is not a global two-process SDK limit.
The six changed identities are in the
[coverage map](.scratch/integration-test-performance/coverage-map.md).

Fast/full project selection stays unchanged. The
[performance comparison](.scratch/integration-test-performance/comparison.md)
records local experimental results, not CI timing guarantees.

The [testing criterion](docs/agents/testing.md) defines test placement,
compatible diagnostic grouping, assertion preservation, negative controls and
performance measurements. `ResourceValidationDiagnostics` groups 24 named
diagnostic cases into five labeled behavior groups with one shared compilation.
The generation and DesignTime snapshots remain separate. This reduces compiler
invocations while retaining every diagnostic expectation; filtering and failure
reporting now operate on the grouped snapshot. `NamedPlaceholderDiagnostics`
adds 20 targets in Reference and Localized Resource behavior groups; its
generation snapshot checks all supported Argument Types and mixed ordering.
`ResourceKeyIdentifiers` distinguishes default Warn from explicit Ignore;
`NormalizedResourceKeyGeneration` checks normalized Raw Text and formatting
members, original-key lookup and valid-name reservation.
`ResourceKeyIdentifierDiagnostics` groups 13 named cases for existing/generated
member collisions and validation of entries omitted by Warn or Ignore.

The five diagnostic classes share an xUnit collection fixture that caches one
build for twelve compatible diagnostic tests. Malformed XML uses a separate
consumer project because SDK resource generation fails before aspects execute.
The fixture starts these two builds concurrently, with isolated consumer bin/obj
directories. `ConsumerProject.Build()` sets `BuildProjectReferences=false` and
`RestoreRecursive=false`: it reuses the library's Release outputs and restores
only the fresh consumer. A current Release solution restore/build is required,
including after library or fixture changes; child builds do not check whether
the referenced library is up to date.

Run either ordinary test project independently after the solution build:

```powershell
dotnet test tests/Talby.Core.ResxAccess.UnitTests/Talby.Core.ResxAccess.UnitTests.csproj --configuration Release --no-build --no-restore
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore
```

### Fast/full execution

The entry points require PowerShell 7 (`pwsh`) and the Release restore
and solution build above. Rebuild after changing source, resources, or fixtures;
both commands deliberately use `--no-build --no-restore`. Fast still requires
the Release solution build above, including all three test projects, the library,
and the consumer fixture.

```powershell
# During local iteration: unit tests and all AspectTests.
pwsh -NoProfile -File tests/run.ps1 -Mode fast

# Required verification before merge: every test, including consumer integration.
pwsh -NoProfile -File tests/run.ps1 -Mode full
```

Fast selects the explicit `$fastProjects` list in `tests/run.ps1`, currently
UnitTests and AspectTests. Other solution test projects are deferred. IntegrationTests
covers SDK embedding, runtime lookup and culture selection, compiler diagnostics,
and incremental resource-only builds. Fast success does not verify generation or
runtime lookup end to end. See the
[incremental build report](.scratch/resource-access/results/06-incremental-builds.md)
for the resource dependency checks.

Full invokes the solution without a filter, including every test project registered
in it. The standard solution-level `dotnet test` command above also continues to
select every test. Add new test projects to the solution for full execution; update
`$fastProjects` only when changing fast project selection. Adding, grouping or
renaming tests within an existing project does not require a runner change.

Each entry point propagates test failures and validates the generated TRX results.
An empty fast project selection is rejected.
Every invocation must produce at least one TRX file, every file must contain test
results, and every recorded outcome must be `Passed`; skipped tests are rejected.
The runner reports the actual passed count without maintaining test names or fixed
counts. It does not detect individual tests that disappear from discovery.
Unique directories under ignored `test-results/execution/`, with a subdirectory
per invocation, prevent stale results from satisfying the check. The results path
is printed before execution so it is also available when a run fails.

CI-ready full verification from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
pwsh -NoProfile -File tests/run.ps1 -Mode full
exit $LASTEXITCODE
```

The [GitHub Actions workflow](.github/workflows/validate.yml) runs on pull
requests, pushes to `main` and manual dispatch. It sets up the SDK from
`global.json` and Node.js 24, restores .NET tools, runs `npm ci`, checks formatting,
restores the solution, builds Release and runs `tests/run.ps1 -Mode full`.
Every command must succeed; full execution validates the recorded test outcomes.
TRX results are retained as artifacts even when tests fail. Hooks are disabled
in CI; formatting checks do not modify files. Git hooks can be bypassed locally,
so require the `Format, build and test` check through branch protection when
merge blocking is needed. Deployment and branch protection settings are not
configured by this workflow.

The [experiment report](.scratch/test-policy/results/01-execution.md)
contains measurements, selection checks, and the unchanged assertion inventory.
Experiment 01 was merged in PR #1. The project split retained its optional local
fast route and complete verification gate; that historical experiment did not
establish a permanent test classification policy or demonstrate a performance improvement. The
[project split report](.scratch/test-policy/results/04-project-split.md) maps
test identities and assertions and records verification.

The solution contains the library, UnitTests, IntegrationTests, ConsumerFixture,
and a dedicated
`Metalama.Testing.AspectTesting` 2026.1.28 snapshot project with automatic
file-based discovery. Ordinary tests keep `MetalamaEnabled=false`; the library
keeps `MetalamaRemoveCompileTimeOnlyCode=false`.

### Precompiled consumer fixture experiment (approved)

`Talby.Core.ResxAccess.ConsumerFixture` is a real SDK executable built with the
solution. Metalama processes its aspects; ordinary tests still keep
`MetalamaEnabled=false`. IntegrationTests references the fixture so its assembly,
runtime configuration, and satellite assemblies are copied to the test output.
Runtime tests invoke that output in fresh processes, retaining culture and
process isolation without rebuilding a consumer for each scenario.

Positive Resource Sets keep real SDK embedding. A fixture-only target removes
the missing-manifest Resource Set and replaces only the missing-Resource-Key
Resource Set. Canonical and lowercase Localized Resource cultures use two
independent fixture Resource Sets, with `es-MX` and `es-mx` satellite suffixes
and uppercase `ExpectedCultures`. Their runtime test invokes the `culture-casing`
scenario and requires distinct Translations from both Resource Sets.

Diagnostic tests still create isolated temporary SDK projects and build from a
different working directory. Twelve compatible aspect diagnostic tests share one
temporary SDK compilation; the malformed XML test uses another. Named Argument Type,
reference nullability and required-argument compiler diagnostics use a third
consumer that references the compiled fixture API. The original integration
refactor preserved every test identity. Named Placeholder coverage brings the
inventory to 30 fast / 53 full at that point. Resource Key identifier policies
add two AspectTests and five IntegrationTests, bringing that inventory to
32 fast / 60 full. Incremental build coverage adds six IntegrationTests, bringing
that inventory to 32 fast / 66 full at that point. Grouped assertions
require the target source file, diagnostic code, and expected message on the
same output line. Malformed XML is diagnosed by SDK resource generation before
the aspect executes (`MSB3103`). The shared builds start lazily: runtime-only
filtered runs do not compile temporary consumers. Filtering a shared aspect diagnostic test
starts both builds, and its test duration includes their shared setup cost.

Rebuild the solution after changing fixture source or `.resx` files before using
`--no-build`. A successful fixture build alone does not execute its runtime
assertions; run full to verify them. Fast/full selection and the required full
verification gate remain unchanged. The [fixture experiment report](.scratch/test-policy/results/02-fixtures.md)
records coverage, negative controls, build costs, and before/after measurements.
The user approved this candidate on 2026-10-03; it was merged in PR #2.

The stock snapshot runner does not forward the consumer project path or resource
map, even when resource files and the targets import are present in its project.
`UnavailableProjectContext` records this limitation. Experiment 03, merged in
PR #3, added seventeen unit tests of internal production helpers and four AspectTests
using deterministic XML/resource-map inputs. The adapter delegates to the
original production helper source through supported `@Include` directives;
generated Raw Text snapshots use the attribute's compiled templates. A
DesignTime `.i.cs` snapshot checks reserved-keyword signatures, while the SDK
fixture retains their method-body/runtime checks.

All original consumer assertions remain in full. Fast checks shared validation
and generated templates, but cannot establish SDK wiring, embedding, associated
manifest naming, or runtime lookup. The [shared-logic report](.scratch/test-policy/results/03-aspect-logic.md)
records adapter limitations, assertion mapping, negative controls, and paired
timings. The current [testing criterion](docs/agents/testing.md) was approved
separately from those historical experiments. No custom snapshot runner or
public testing API is required. The [snapshot consolidation report](.scratch/aspect-test-performance/results/consolidation.md)
records the retained assertions, negative controls and baseline/candidate timings.

Relevant upstream documentation: [SDK resource manifest names](https://learn.microsoft.com/en-us/dotnet/core/resources/manifest-file-names)
and [Metalama aspect testing](https://doc.metalama.net/conceptual/aspects/testing/aspect-testing).
