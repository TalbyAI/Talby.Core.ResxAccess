# Talby.Core.ResxAccess

Generate strongly typed access to RESX Raw Text and Formatted Text with Metalama.
The Reference Resource defines Resource Keys and Placeholder Contracts; Localized
Resources are validated during compilation.

## Install

This beta targets `net10.0`. Use .NET SDK 10.0.401 or a later patch in the 10.0.4xx
feature band for the verified setup.

```powershell
dotnet add package Talby.Core.ResxAccess --version 0.1.0-beta.1
```

Metalama.Framework `2026.1.28` is a package dependency. Keep Metalama enabled in
your consumer project. The package imports its resource targets automatically
through `buildTransitive`; no explicit targets import is needed.

## Example

Create `Resources/Labels.resx`:

```xml
<root>
  <data name="Welcome"><value>Hello {name@string}, {amount@decimal:N2}!</value></data>
  <data name="Plain"><value>Hello</value></data>
</root>
```

Create `Resources/Labels.es.resx`:

```xml
<root>
  <data name="Welcome"><value>Hola {name}, {amount:N2}!</value></data>
  <data name="Plain"><value>Hola</value></data>
</root>
```

Declare your Resource Access class:

```csharp
using Talby.Core.ResxAccess;

[GenerateResxAccess("Resources/Labels.resx", ExpectedCultures = new[] { "es" })]
internal static class Texts
{
}
```

Use the generated methods:

```csharp
using System.Globalization;

CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var es = CultureInfo.GetCultureInfo("es-AR");
var fr = CultureInfo.GetCultureInfo("fr-FR");

Texts.Welcome();                              // Hello {name@string}, {amount@decimal:N2}!
Texts.Welcome(es);                            // Hola {name}, {amount:N2}!
Texts.FormatWelcome("Ada", 12.5m);            // Hello Ada, 12.50!
Texts.FormatWelcome("Ada", 12.5m, es, fr);    // Hola Ada, 12,50!
Texts.Plain(es);                              // Hola (parent culture fallback)
Texts.Plain(CultureInfo.GetCultureInfo("de")); // Hello (Reference Resource fallback)
```

Resource Culture defaults to `CurrentUICulture`; Formatting Culture defaults
independently to `CurrentCulture`. The example is compiled and invoked through a
real `PackageReference` consumer in the repository's IntegrationTests.

## Beta scope

- Text entries in culture-neutral Reference Resources and same-directory Localized Resources.
- Raw Text and formatting methods, indexed and named placeholders, nullable arguments and explicit culture overloads.
- Build-time validation of matching Resource Keys and Placeholder Contracts.
- Standard SDK resource embedding and satellite assemblies. Linked resources and custom `LogicalName` or `ManifestResourceName` metadata are unsupported.
- Ordinary builds do not require `partial`. Automatic IDE resource refresh is not verified; IDE-specific limitations remain documented.

Read the [consumer guide](https://github.com/TalbyAI/Talby.Core.ResxAccess/blob/main/docs/resource-access.md)
for supported Argument Types, identifier policies, diagnostics and IDE limitations.
This beta is intended for existing projects and early adopters; the API may change
before a stable release.

## License

MIT. The license text is included in the package. Metalama's core framework is MIT;
optional proprietary Metalama tooling has its own terms. See the
[Metalama package](https://www.nuget.org/packages/Metalama.Framework/2026.1.28).
