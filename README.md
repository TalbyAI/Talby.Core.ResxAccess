# Talby.Core.ResxAccess

Metalama generates Raw Text and Indexed Placeholder formatting methods on a consumer-declared, non-generic
static class. A `partial` declaration is not required.

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
missing Expected Cultures. Named and mixed Formatting Placeholders, configurable
identifier policies, and verification of incremental build and IDE refresh belong
to dependent issues.

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

Every Translation must use exactly the Reference Resource's Indexed Placeholder
identities, including entries omitted because their Resource Keys are invalid
identifiers. Translations may reorder or repeat identities and change alignment or
Argument Formats. Standard composite formatting supports null arguments, alignment,
formats, and escaped braces (`{{` and `}}`); standard formatting failures propagate.
Malformed syntax receives `TRESX001` in a Reference Resource or `TRESX004` in a
Localized Resource. Raw Text remains unchanged. Named and mixed placeholders retain
Raw Text access while their formatting and contract validation await ticket 04.

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

The solution contains three test projects:

- `Talby.Core.ResxAccess.UnitTests`: 19 tests of Metalama setup and shared Reference Resource validation.
- `Talby.Core.ResxAccess.IntegrationTests`: sixteen SDK consumer tests, with the `ConsumerProject` helper and a reference to ConsumerFixture.
- `Talby.Core.ResxAccess.AspectTests`: twelve dedicated Metalama snapshot tests.

The integration test classes share an xUnit collection so their temporary SDK
consumer builds cannot concurrently overwrite the referenced library's outputs.

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

Fast selects the UnitTests and AspectTests projects and defers the IntegrationTests
project with four `RawTextConsumerTests`:
`CanCompileAndInvokeIndependentResourceSets`,
`ReportsEachInvalidResourceAndEmbeddingInOneBuild`,
`ReportsMalformedReferenceResourceWithoutAspectCrash`, and
`DescribesMissingRuntimeManifestAndResourceKey`, plus six `LocalizedResourceConsumerTests`:
`CanInvokeSatelliteResourcesWithDefaultAndExplicitCulture`,
`RejectsUnsupportedLocalizedResourceCultureCasing`,
`CanInvokeCanonicalAndLowercaseLocalizedResourceCultures`,
`RejectsInconsistentLocalizedResourcesOutsideExpectedCultures`,
`ReportsMissingAndInvalidExpectedCultures`, and
`RejectsLocalizedResourcesWithoutStandardSatelliteEmbedding`, plus six `IndexedPlaceholderConsumerTests`
covering generated signatures, independent cultures, composite formatting, runtime
failures, malformed syntax, and Localized Resource Placeholder Contracts. Its output names the deferred
tests. Fast success does not verify generation or runtime lookup end to end.
Full applies no filter; the standard solution-level `dotnet test` command above
also continues to select every test.

Each entry point propagates test failures and checks the passed TRX identities
against the fixed inventory (31 fast, 47 full), rejecting
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
Experiment 01 was merged in PR #1. The project split retains its optional local
fast route and complete verification gate; it does not establish a permanent
test classification policy or demonstrate a performance improvement. The
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
timings. Its historical evidence does not establish a permanent test
classification policy. No custom snapshot runner or public testing API is
required.

Relevant upstream documentation: [SDK resource manifest names](https://learn.microsoft.com/en-us/dotnet/core/resources/manifest-file-names)
and [Metalama aspect testing](https://doc.metalama.net/conceptual/aspects/testing/aspect-testing).
