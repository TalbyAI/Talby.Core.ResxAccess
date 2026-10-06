# Resource Access consumer guide

`GenerateResxAccess` validates a Resource Set at build time and introduces public
static methods on your non-generic static class. You choose the class name,
namespace and accessibility. Ordinary builds do not require `partial`; the
[recorded IDE limitations](#ide-evidence-and-deferred-requirements) apply separately.
The [glossary](../CONTEXT.md) defines the domain terms used here.

## Adopt the API

Use a .NET 10 SDK project with nullable reference types and implicit usings enabled.
For a source consumer at `consumer/Consumer.csproj` beneath the repository root,
use this project file. Adjust both relative paths if your project lives elsewhere.
The explicit targets import is required for `ProjectReference` consumers.

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ConsumerRoot</RootNamespace>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj" />
  </ItemGroup>
  <Import Project="../src/Talby.Core.ResxAccess/buildTransitive/Talby.Core.ResxAccess.targets" />
</Project>
```

A package built from this library includes these targets under `buildTransitive`,
so package consumers receive the import automatically. Keep Metalama enabled in
the consumer: it introduces the API during compilation. The repository's ordinary
test projects disable Metalama because they test separately compiled consumers.

The following four files form a runnable example. Resource paths in the attribute
are relative to the consumer project directory. The Reference Resource must be a
culture-neutral `.resx` file within that directory tree. Build and run the consumer
with `dotnet run --project consumer/Consumer.csproj --configuration Release` from
the repository root after installing the [repository prerequisites](../README.md#build-and-test).

### Texts.cs

```csharp
using Talby.Core.ResxAccess;

namespace Docs.Consumer;

[GenerateResxAccess(
    "Resources/Labels.resx",
    ExpectedCultures = new[] { "es" },
    InvalidKeyHandling = InvalidKeyHandling.Normalize)]
internal static class Texts
{
}
```

`ExpectedCultures` is optional; omit it to validate discovered Localized Resources
without requiring an additional list of cultures. `InvalidKeyHandling` is also
optional and defaults to `Warn`. Instance classes, generic classes, and classes
inside generic containing types are unsupported (`TRESX002`).

### Resources/Labels.resx

```xml
<root>
  <data name="Plain"><value>Hello</value></data>
  <data name="Welcome"><value>Hello {name@string}, {amount@decimal:N2}!</value></data>
  <data name="Summary"><value>{2} / {0:N2}</value></data>
  <data name="Mixed"><value>{name@string} {2} {0} {name}</value></data>
  <data name="Layout"><value>{{{0,6:N1}}} [{2,-5}]</value></data>
  <data name="Optional"><value>Optional [{note@string?}]</value></data>
  <data name="has-dash"><value>Dash text</value></data>
  <data name="class"><value>Keyword</value></data>
</root>
```

### Resources/Labels.es.resx

```xml
<root>
  <data name="Plain"><value>Hola</value></data>
  <data name="Welcome"><value>Hola {name}, {amount:N2}!</value></data>
  <data name="Summary"><value>{0:N2} / {2}</value></data>
  <data name="Mixed"><value>{0} {name} {2} {name}</value></data>
  <data name="Layout"><value>{{{0,6:N1}}} [{2,-5}]</value></data>
  <data name="Optional"><value>Opcional [{note}]</value></data>
  <data name="has-dash"><value>Texto con guion</value></data>
  <data name="class"><value>Palabra clave</value></data>
</root>
```

### Program.cs

```csharp
using System.Globalization;
using Docs.Consumer;

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
var es = CultureInfo.GetCultureInfo("es-AR");
var fr = CultureInfo.GetCultureInfo("fr-FR");
var invariant = CultureInfo.InvariantCulture;

Console.WriteLine(Texts.Plain());                              // Hello
Console.WriteLine(Texts.Plain(es));                            // Hola (parent fallback)
Console.WriteLine(Texts.Plain(CultureInfo.GetCultureInfo("de"))); // Hello (Reference fallback)
Console.WriteLine(Texts.Welcome(invariant)); // Hello {name@string}, {amount@decimal:N2}!
Console.WriteLine(Texts.Welcome(es));        // Hola {name}, {amount:N2}!

Console.WriteLine(Texts.FormatWelcome("Ada", 12.5m));         // Hello Ada, 12.50!
Console.WriteLine(Texts.FormatWelcome("Ada", 12.5m, es));     // Hola Ada, 12.50!
Console.WriteLine(Texts.FormatWelcome("Ada", 12.5m, es, fr)); // Hola Ada, 12,50!
Console.WriteLine(Texts.FormatSummary(12.5m, "second", invariant, invariant)); // second / 12.50
Console.WriteLine(Texts.FormatMixed("Ada", "first", "second", invariant)); // Ada second first Ada
Console.WriteLine(Texts.FormatMixed("Ada", "first", "second", es));        // first Ada second Ada
Console.WriteLine(Texts.FormatLayout(12.5m, null, invariant, invariant)); // {  12.5} [     ]
Console.WriteLine(Texts.FormatOptional(null, invariant)); // Optional []
Console.WriteLine(Texts.has_dash(invariant));             // Dash text
Console.WriteLine(Texts.@class(invariant));               // Keyword
```

These exact C# and resource snippets are compiled and invoked by
[`DocumentationConsumerTests`](../tests/Talby.Core.ResxAccess.IntegrationTests/DocumentationConsumerTests.cs)
through the existing SDK consumer boundary. The test checks the displayed output;
it does not emulate generation or resource lookup.

## Resource Set validation and discovery

The Reference Resource defines the complete, case-sensitive set of Resource Keys.
Only same-directory files with its base name and a recognized culture suffix are
Localized Resources: `Resources/Labels.es.resx` and `Resources/Labels.fr-CA.resx`
belong to `Resources/Labels.resx`; files elsewhere or with another base name do not.
Each discovered Localized Resource must contain exactly the Reference Resource's
Resource Keys. `Welcome` and `welcome` are different keys. Missing, additional or
duplicate keys, malformed XML, missing values and non-text values fail validation.
Use text entries; typed non-string values and file references are unsupported.
Empty and whitespace-only text is valid if its Placeholder Contract permits it,
and Raw Text preserves that content.

There can be only one Localized Resource per Resource Culture. Use canonical
filename suffix casing or its lowercase form (`es-MX` or `es-mx`); other casing,
such as `ES` or `Es-MX`, is rejected for portable satellite probing. Duplicate
cultures are rejected even if filenames differ only in casing on a case-sensitive
file system.

Every discovered culture is validated, including cultures outside `ExpectedCultures`.
The optional list adds required Localized Resources, matches recognized non-empty
culture names without regard to case, and reports an error when one is absent.
For example, `ExpectedCultures = new[] { "es", "fr" }` requires both files even if
runtime fallback could otherwise select the Reference Resource.

Validation also covers Resource Keys omitted by `Warn` or `Ignore`, including their
Placeholder Contracts. Omitting a generated member never exempts its Translation
from validation.

Runtime parent-culture and Reference Resource fallback still applies to requested
cultures without their own resource. It never excuses an incomplete Localized
Resource that is present. This preserves the
[complete Localized Resources decision](adr/0001-require-complete-localized-resources.md).

## Generated methods and cultures

Each representable Resource Key receives two Raw Text overloads returning `string`:
`Texts.Welcome()` and `Texts.Welcome(CultureInfo resourceCulture)`. They preserve
the selected Translation, including Formatting Placeholders, escaped braces and
whitespace; they do not substitute arguments. Keys such as `Plain` without
Formatting Placeholders receive only these overloads.

A key with Formatting Placeholders also receives three `Format` overloads.
For the example's `Welcome`, their parameter lists are:

| Method | Required parameters |
| --- | --- |
| `FormatWelcome` | `string name, decimal amount` |
| `FormatWelcome` | `string name, decimal amount, CultureInfo resourceCulture` |
| `FormatWelcome` | `string name, decimal amount, CultureInfo resourceCulture, CultureInfo formattingCulture` |

Resource Culture defaults to `CultureInfo.CurrentUICulture`. Formatting Culture
defaults independently to `CultureInfo.CurrentCulture`, including when Resource
Culture is explicit. Formatting Culture changes how arguments are rendered; it
does not select a Translation. Explicit cultures must be non-null or the call
throws `ArgumentNullException`. There is no Formatting Culture-only overload:
pass both cultures when overriding Formatting Culture.

Lookup uses standard .NET parent-culture fallback, then the Reference Resource.
`es-AR` selects `es` in this example; `de` and `InvariantCulture` select the
Reference Resource. Formatting then uses the selected Formatting Culture.
Standard formatting failures propagate, for example `FormatException` for an
Argument Format unsupported by the supplied value. Format strings are not checked
for compatibility with every possible runtime argument during compilation.

Missing runtime Resource Keys or resource manifests produce descriptive
`InvalidOperationException` messages identifying the resource, Resource Key and
requested Resource Culture. Missing manifest or satellite exceptions are retained
as inner exceptions. Rebuild and redeploy resources with the application; editing
resources in an already running application and resource hot reload are unsupported.

## Formatting Placeholders and Translation compatibility

Indexed Placeholders use `{index[,alignment][:format]}`, for example `{0:N2}` or
`{2,-5}`. Identities are numeric and only those used become required `object?`
parameters in numeric order. For `{2} / {0:N2}`, the parameters are `arg0`, `arg2`;
there is no `arg1`. Indexed Placeholders cannot declare an Argument Type.

Named Placeholders use `{name[@type][,alignment][:format]}`, for example
`{name@string}` or `{amount@decimal,10:N2}`. Untyped Named Placeholders accept
`object?`. The supported explicit spellings are case-sensitive:

| Argument Type | Nullable form |
| --- | --- |
| `string` | `string?` |
| `bool` | `bool?` |
| `int` | `int?` |
| `long` | `long?` |
| `double` | `double?` |
| `decimal` | `decimal?` |
| `DateTime` | `DateTime?` |
| `DateTimeOffset` | `DateTimeOffset?` |
| `Guid` | `Guid?` |

Other spellings, including explicit `object`, `float`, `Int32` and fully qualified
type names, are unsupported. `string` and `string?` retain distinct compiler
nullable annotations. Non-nullable reference arguments add no runtime null checks.
Nullable arguments remain required parameters: supply a value, possibly `null`;
the nullable identity must still occur in every Translation. Null arguments format
as empty text under standard composite formatting.

Repeated occurrences share a parameter. An explicit type on one occurrence
establishes the Argument Type even if another occurrence omits it. Conflicting
explicit types or nullability within a Translation are errors. Named identities
are case-sensitive valid C# identifiers; keywords are escaped in generated C#.

Positive alignment pads on the left, negative alignment pads on the right, and
width is a minimum rather than truncation. An Argument Format such as `N2` is
passed to standard composite formatting with the Formatting Culture. Use `{{`
and `}}` for literal braces. Raw Text keeps both braces; a formatting method
unescapes them. A Translation containing only escaped braces has no Formatting
Placeholders and therefore has no formatting method.

Every Translation must use all and only the Reference Resource's Named and
Indexed identities. Translations may reorder or repeat identities and change
alignment or Argument Formats. They may omit type declarations; any explicit
declaration must match the Reference Resource's Argument Type and nullability.
They cannot add an identity, drop a nullable identity, or change an explicit type.
Malformed syntax and incompatible Placeholder Contracts are compilation errors.

Prefer a single placeholder style per Resource Key for readability. Mixed styles
remain supported: distinct Named parameters appear first, in order of first
appearance in the Reference Resource, then Indexed parameters in numeric order.
For `{name@string} {2} {0} {name}`, the required parameters are `string name`,
`object? arg0`, `object? arg2`. Reordered Localized occurrences do not change the
signature. This preserves the
[mixed-placeholder parameter order decision](adr/0002-order-mixed-placeholder-parameters.md).

## Resource Key identifiers and diagnostics

| `InvalidKeyHandling` | Effect on invalid Resource Key identifiers |
| --- | --- |
| `Warn` (default) | Omits Raw Text and formatting members; reports `TRESX006` warnings. |
| `Ignore` | Omits the same members without identifier warnings. |
| `Normalize` | Replaces invalid identifier characters with underscores and assigns deterministic collision suffixes. |

Valid identifiers remain unchanged. Keywords are escaped (`class` is callable as
`Texts.@class()`). Under Normalize, `has-dash` becomes `has_dash` and `1Text`
becomes `_Text`: an invalid starting character is replaced, not prefixed.
Lookup always uses the original Resource Key.

Normalization reserves valid Resource Key identifiers first, then processes invalid
keys in ordinal order. Collisions receive `_2`, `_3` and subsequent suffixes.
With valid keys `a_b` and `a_b_2`, invalid keys `a b`, `a-b` and `a.b` become
`a_b_3`, `a_b_4` and `a_b_5`, regardless of entry order. C# identifier comparisons
ignore Unicode formatting characters.

Normalization does not rename conflicting member families. Existing target members,
the class name, reserved generated support members, and collisions between Raw Text
and formatting families cause `TRESX007`. For example, a `Welcome` key with
placeholders and a separate `FormatWelcome` key conflict. Named parameter collisions
with another argument, an Indexed parameter such as `arg0`, `resourceCulture` or
`formattingCulture` are errors; invalid argument identifiers are never normalized.

| Diagnostic | Meaning |
| --- | --- |
| `TRESX001` | Invalid Reference Resource, including invalid placeholders or parameter collisions. |
| `TRESX002` | Unsupported target class. |
| `TRESX003` | Unsupported Reference Resource embedding or unavailable consumer project context. |
| `TRESX004` | Invalid Localized Resource, including keys, Placeholder Contracts, culture casing or embedding. |
| `TRESX005` | Invalid or missing Expected Culture. |
| `TRESX006` | Warning for an omitted invalid Resource Key under Warn. |
| `TRESX007` | Generated member collision. |
| `TRESX008` | Unsupported `InvalidKeyHandling` value. |

## Embedding and incremental builds

Use standard SDK `EmbeddedResource` conventions. The SDK's effective manifest name
includes the resource location and `RootNamespace`, or the C# type associated by
implicit or explicit `DependentUpon`. The target class name does not determine the
manifest base name. Standard Localized Resources are embedded into satellite
assemblies for their Resource Cultures.

Explicit `LogicalName`, explicit `ManifestResourceName`, linked resources and
associated resources excluded from SDK embedding are diagnosed as unsupported.
Localized metadata must describe standard satellite embedding for the same Resource
Set. These restrictions apply to every discovered Localized Resource as well as the
Reference Resource. Do not rely on custom naming or embedding overrides.

With the targets import, ordinary incremental builds after resource-only changes
refresh Raw Text, Formatted Text, generated signatures and validation diagnostics.
Reference and Localized content edits, key and Placeholder Contract changes,
Localized additions/removals, Expected Culture removal/restoration and omitted-key
validation were verified without C# edits or cleaning. This requires a build;
automatic IDE refresh and updates inside a running application are separate concerns.

The [incremental build evidence](../.scratch/resource-access/results/06-incremental-builds.md)
records .NET SDK 10.0.401, Metalama 2026.1.28, dependency-isolated negative controls
and reproducible setup. After the [solution restore and Release build](../README.md#build-and-test),
run these checks from the repository root:

```powershell
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~DocumentationConsumerTests
dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~IncrementalBuildConsumerTests
```

## IDE evidence and deferred requirements

The recorded Windows probe used VS Code 1.140.0, C# 11.1.32, C# Dev Kit 11.1.3,
.NET SDK 10.0.401 and Metalama 2026.1.28. Analyzer and compiler diagnostic scopes
were `fullSolution`. SDK design-time compiler-input checks passed, but they do not
establish automatic editor refresh.

- C# Dev Kit failed project loading with `AddAdditionalFilesAsync` RPC errors.
- Standalone C# exposed the initial formatting signature for a `partial` target.
  Without `partial`, `LAMA0048` prevented recognition of introduced members even
  though ordinary compilation succeeded.
- After a Reference Resource-only edit, hover/completion stayed stale and no
  Placeholder Contract diagnostic appeared during the 90-second observation.
- A separate requested design-time build updated diagnostics. This was a manual
  control and does not satisfy automatic refresh acceptance.

Automatic API and diagnostic refresh, non-partial IDE support, C# Dev Kit
compatibility, subsequent editor-output scenarios and human verification remain
deferred. Issue 07 was resolved by the user's explicit scope deferral, not successful
IDE verification. No supported IDE policy has been established. The
[IDE evidence and probe instructions](../.scratch/resource-access/results/07-ide-refresh.md)
retain versions, traces, failure details and the human verification sequence.
The repository's VS Code diagnostic settings and `partial` workaround may help
initial recognition; they do not establish automatic resource refresh.
