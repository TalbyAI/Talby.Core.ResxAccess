# Talby.Core.ResxAccess

Metalama generates Raw Text access methods on a consumer-declared, non-generic
static class. A `partial` declaration is not required.

```csharp
using System.Globalization;
using Talby.Core.ResxAccess;

[GenerateResxAccess("Resources/Labels.resx")]
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

This first slice generates Raw Text only. Localized Resource validation,
Formatted Text, configurable identifier policies, and verification of incremental
build and IDE refresh belong to the dependent issues.

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

## Build and test

Install .NET SDK 10.0.401 or a later patch in the 10.0.4xx feature band,
as selected by `global.json`. NuGet.org access is required for restore.

Run from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
dotnet test Talby.Core.ResxAccess.slnx --configuration Release --no-build --no-restore
```

### Fast/full execution experiment (pending review)

The candidate entry points require PowerShell 7 (`pwsh`) and the Release restore
and solution build above. Rebuild after changing source, resources, or fixtures;
both commands deliberately use `--no-build --no-restore`. Fast still requires
building both test projects, the library, and the consumer fixture in the
precompiled-fixture candidate below.

```powershell
# During local iteration: unit tests and all AspectTests.
pwsh -NoProfile -File tests/run.ps1 -Mode fast

# Required verification before merge: every test, including consumer integration.
pwsh -NoProfile -File tests/run.ps1 -Mode full
```

Fast uses `Category!=Integration` and defers all four `RawTextConsumerTests`:
`CanCompileAndInvokeIndependentResourceSets`,
`ReportsEachInvalidResourceAndEmbeddingInOneBuild`,
`ReportsMalformedReferenceResourceWithoutAspectCrash`, and
`DescribesMissingRuntimeManifestAndResourceKey`. Its output names the deferred
tests. Fast success does not verify generation or runtime lookup end to end.
Full applies no filter; the standard solution-level `dotnet test` command above
also continues to select every test.

Each entry point propagates test failures and checks the passed TRX identities
against the experiment's fixed inventory (four fast, eight full), rejecting
empty, skipped, missing, duplicate, or unexpected selections. Adding or renaming
tests requires reviewing and updating that inventory in `tests/run.ps1`.
Unique TRX directories under ignored `test-results/execution/` prevent stale
results from satisfying the check.

CI-ready full verification from the repository root:

```powershell
dotnet restore Talby.Core.ResxAccess.slnx
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
pwsh -NoProfile -File tests/run.ps1 -Mode full
exit $LASTEXITCODE
```

No CI provider is configured, so this requirement is documented rather than
automatically enforced. The [experiment report](.scratch/test-policy/results/01-execution.md)
contains measurements, selection checks, and the unchanged assertion inventory.
This candidate awaits user review before adoption; it does not establish the
final test classification policy.

The solution contains the library, ordinary xUnit tests, and a dedicated
`Metalama.Testing.AspectTesting` 2026.1.28 snapshot project with automatic
file-based discovery. Ordinary tests keep `MetalamaEnabled=false`; the library
keeps `MetalamaRemoveCompileTimeOnlyCode=false`.

### Precompiled consumer fixture experiment (approved)

`Talby.Core.ResxAccess.ConsumerFixture` is a real SDK executable built with the
solution. Metalama processes its aspects; ordinary tests still keep
`MetalamaEnabled=false`. The test project references the fixture so its assembly,
runtime configuration, and satellite assemblies are copied to the test output.
Runtime tests invoke that output in three fresh processes, retaining culture and
process isolation without rebuilding a consumer for each scenario.

Positive Resource Sets keep real SDK embedding. A fixture-only target removes
the missing-manifest Resource Set and replaces only the missing-Resource-Key
Resource Set. The grouped invalid-consumer and malformed XML tests still create
isolated temporary SDK projects and build from a different working directory.
Full execution now starts two temporary SDK builds instead of five, preserving
every test identity and assertion. Malformed XML is diagnosed by SDK resource
generation before the aspect executes (`MSB3103`).

Rebuild the solution after changing fixture source or `.resx` files before using
`--no-build`. A successful fixture build alone does not execute its runtime
assertions; run full to verify them. Fast/full selection and the required full
verification gate remain unchanged. The [fixture experiment report](.scratch/test-policy/results/02-fixtures.md)
records coverage, negative controls, build costs, and before/after measurements.
The user approved this candidate on 2026-10-03; integration is pending.

The stock snapshot runner does not forward the consumer project path or resource
map, even when resource files and the targets import are present in its project.
`UnavailableProjectContext` records this limitation. Diagnostics independent of
SDK context have reviewed `.t.cs` baselines; generation and context-dependent
diagnostics are verified through real consumer builds. No custom snapshot runner
or production test hook is required.

Relevant upstream documentation: [SDK resource manifest names](https://learn.microsoft.com/en-us/dotnet/core/resources/manifest-file-names)
and [Metalama aspect testing](https://doc.metalama.net/conceptual/aspects/testing/aspect-testing).
