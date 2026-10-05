# Talby.Core.ResxAccess

Metalama generates Raw Text and formatting methods on a consumer-declared, non-generic
static class. Builds do not require a `partial` declaration. Metalama 2026.1.28
requires `partial` for IDE recognition of introduced members (`LAMA0048`).

```csharp
using System.Globalization;
using Talby.Core.ResxAccess;

[GenerateResxAccess("Resources/Labels.resx", ExpectedCultures = new[] { "es", "fr" })]
internal static class Texts
{
}

// For the Resource Key "Welcome":
// Texts.Welcome();
// Texts.Welcome(CultureInfo.GetCultureInfo("es"));
```

Each representable Resource Key receives two public static methods returning
`string`. The parameterless method selects `CurrentUICulture`; the other requires
a non-null `CultureInfo resourceCulture`. Raw Text is returned unchanged,
including Formatting Placeholders and whitespace. Lookup uses standard .NET
parent-culture and Reference Resource fallback. The target class retains its
name, namespace, and accessibility.

Reference Resource paths are resolved relative to the consumer project directory.
Resources must be text `.resx` files embedded using standard SDK conventions.
The manifest base name comes from SDK metadata, including resource location,
`RootNamespace`, and implicit or explicit `DependentUpon` C# type association.
Explicit `LogicalName`, explicit `ManifestResourceName`, and linked resources
are rejected. Missing runtime resources throw descriptive `InvalidOperationException`
instances; missing manifest or satellite exceptions are retained as inner exceptions.

Localized Resources are discovered only in the Reference Resource's directory,
using its base name followed by a recognized culture suffix (for example,
`Labels.es.resx`). Every discovered Localized Resource must contain exactly the
Reference Resource's case-sensitive Resource Keys and use standard SDK satellite
embedding. Duplicate keys and non-text entries are rejected in both kinds of
resource, including keys that do not receive generated methods. A Resource Set
cannot contain multiple Localized Resources for the same Resource Culture,
including filenames differing only in casing on a case-sensitive file system. Empty and
whitespace-only text remains valid when its Placeholder Contract permits it and is returned unchanged.

Localized Resource filename suffixes must use canonical Resource Culture casing
or its lowercase form (for example, `es-MX` or `es-mx`). Other spellings such as
`ES` or `Es-MX` receive `TRESX004`, because standard runtime satellite probing
on Linux and macOS requires canonical or lowercase directory names. See
[satellite assembly loading](https://learn.microsoft.com/en-us/dotnet/core/dependency-loading/loading-resources).

`ExpectedCultures` is optional. When supplied, each name must identify a non-empty
Resource Culture with an associated Localized Resource. Culture names are matched
without regard to case. The list supplements discovery: cultures outside it are
still validated. Parent-culture and Reference Resource fallback apply to requested
cultures without their own resource; they never excuse an incomplete resource
that is present.

`TRESX004` reports invalid Localized Resources; `TRESX005` reports invalid or
missing Expected Cultures. Verification of incremental build and IDE refresh
belongs to dependent issues.

`InvalidKeyHandling` controls Resource Keys that cannot become C# method identifiers:

- `Warn` (the default) omits their Raw Text and formatting methods and reports `TRESX006` warnings.
- `Ignore` omits the same methods without identifier warnings.
- `Normalize` replaces invalid identifier characters with underscores. A character invalid at the start is also replaced, so `1Text` becomes `_Text`.

All policies validate the complete Resource Set and every Placeholder Contract,
including omitted entries. Valid identifiers are preserved; C# keywords are
escaped and can be called with syntax such as `Texts.@class()`.

```csharp
[GenerateResxAccess("Resources/Labels.resx", InvalidKeyHandling = InvalidKeyHandling.Normalize)]
internal static class NormalizedTexts
{
}

// Resource Key "has-dash": NormalizedTexts.has_dash();
// With placeholders: NormalizedTexts.Formathas_dash(arguments);
```

Normalization reserves all valid Resource Key identifiers first, then processes
invalid keys in ordinal order. Collisions receive `_2`, `_3`, and subsequent
suffixes. For example, valid `a_b` and `a_b_2` reserve those names; `a b`, `a-b`,
and `a.b` become `a_b_3`, `a_b_4`, and `a_b_5` regardless of resource entry order.
Lookup always uses the original Resource Key and preserves its Translation.
C# identifier comparisons ignore Unicode formatting characters.

`TRESX007` rejects collisions with existing target-class members, the class name,
the generated ResourceManager field, or other generated Raw Text and formatting
member families. Normalization does not rename these conflicting families.
Unsupported `InvalidKeyHandling` values receive `TRESX008` instead of selecting
an omission policy.

For a Reference Resource Translation such as `"{2} / {0:N2}"`, the generated API is:

```csharp
Texts.FormatSummary(arg0, arg2);
Texts.FormatSummary(arg0, arg2, resourceCulture);
Texts.FormatSummary(arg0, arg2, resourceCulture, formattingCulture);
```

The Resource Key in this example is `Summary`. Only Indexed Placeholder identities
actually used in the Reference Resource become parameters, in numeric order;
`arg0` and `arg2` are required `object?` arguments. Formatting Culture defaults to
`CurrentCulture` independently of Resource Culture, even when Resource Culture is
explicit. Explicit cultures must be non-null. There is no formatting-culture-only
overload. Keys without Formatting Placeholders retain only Raw Text methods.

Named Placeholders use `{name[@type][,alignment][:format]}`. Supported explicit
Argument Types are `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`,
`DateTimeOffset`, and `Guid`, each with an optional nullable `?` suffix. Untyped
Named Placeholders use `object?`. Explicit `string` and `string?` retain different
compiler annotations; non-nullable reference arguments add no runtime null checks.
Nullable arguments remain required parameters and required placeholder identities.

For `{name@string} {2} {0}`, `FormatSummary` accepts `string name`, `object? arg0`,
and `object? arg2`, followed by the same optional culture overloads shown above.
Distinct Named parameters appear first in order of first appearance in the
Reference Resource; Indexed parameters follow in numeric order. Repeated
occurrences share one parameter. An explicit declaration supplies the Argument
Type even when another occurrence omits it; conflicting explicit declarations
are errors. Prefer one placeholder style per Resource Key for readability;
mixed styles are supported.

Named argument identifiers must be valid C# identifiers. Keywords are escaped in
generated C# without changing their identities. Collisions with other parameters,
including Indexed parameter names, `resourceCulture`, and `formattingCulture`,
are compilation errors; arguments are never silently renamed.

Validated Translations are converted to composite formats at compile time.
Each formatting call allocates one argument-array slot per public argument;
Indexed identity gaps do not allocate additional slots.

Every Translation must use exactly the Reference Resource's Named and Indexed
identities, including entries omitted because their Resource Keys are invalid
identifiers. Translations may reorder or repeat identities and change alignment or
Argument Formats. Translations may omit type declarations; any explicit declaration
must match the Reference Resource's Argument Type and nullability. Standard composite formatting supports null arguments, alignment,
formats, and escaped braces (`{{` and `}}`); standard formatting failures propagate.
Malformed syntax receives `TRESX001` in a Reference Resource or `TRESX004` in a
Localized Resource. Raw Text remains unchanged.

## Consumer build integration

The NuGet package includes `buildTransitive/Talby.Core.ResxAccess.targets`, imported
automatically for package consumers. It records the SDK's effective resource names
and exposes the resource map to the aspect through a compiler-visible property.

When referencing the source library with `ProjectReference`, also import its targets
in the consumer project (adjust paths to your layout):

```xml
<ItemGroup>
  <ProjectReference Include="../src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj" />
</ItemGroup>
<Import Project="../src/Talby.Core.ResxAccess/buildTransitive/Talby.Core.ResxAccess.targets" />
```

The targets register project `.resx` files and the resource map as compiler
`AdditionalFiles`. Content edits invalidate compilation; the map also records
discovered paths so additions and removals invalidate generation and validation.
Discovery includes associated resources excluded from SDK embedding, which must
still fail validation. The map is written only when its contents change.
Ordinary incremental consumer builds refresh Raw Text, Formatted Text, generated
signatures and diagnostics without C# edits or cleaning. These are build-level
guarantees; supported IDE refresh requires the separate
[IDE verification issue](.scratch/resource-access/issues/07-refresh-ide-resource-access.md).

Design-time builds prepare SDK resource names, watch resource content through
`AdditionalDesignTimeBuildInput`, and write a stable generated C# dependency under
`obj/`. The aspect reads that declaration before validation so Metalama can track
changes even for invalid Resource Sets. The dependency includes content hashes
and discovery membership and is absent from ordinary compilation inputs.

For VS Code, enable C# analyzer diagnostics and use `partial` consumer classes.
The repository's `.vscode/settings.json` enables analyzer and compiler diagnostics
for the full solution, following [Metalama's VS Code configuration](https://doc.metalama.net/conceptual/using/ide/vs-code).
Automatic resource refresh is **not established** in the installed VS Code setup:
C# Dev Kit fails project loading, and the standalone C# language server leaves
the generated API stale after a resource edit. See the
[IDE refresh evidence](.scratch/resource-access/results/07-ide-refresh.md)
for versions, observed failures, the reproducible probe and human verification steps.

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
`prototypes/`; no prototype directory or implementation is currently required.
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
- `Talby.Core.ResxAccess.IntegrationTests`: 35 SDK consumer tests, with the `ConsumerProject` helper and a reference to ConsumerFixture.
- `Talby.Core.ResxAccess.AspectTests`: 12 dedicated Metalama snapshot tests.

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

The integration test classes share an xUnit collection fixture that caches one
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
the current inventory to 32 fast / 66 full. Grouped assertions
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
