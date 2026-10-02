using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Metalama.Framework.Aspects;
using Metalama.Framework.Code;
using Metalama.Framework.Diagnostics;

namespace Talby.Core.ResxAccess;

/// <summary>Introduces Raw Text methods for a culture-neutral, SDK-embedded .resx file.</summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class GenerateResxAccessAttribute : TypeAspect
{
    private static readonly DiagnosticDefinition<string> InvalidReference = new(
        "TRESX001", Severity.Error, "Invalid Reference Resource: {0}", "Invalid Reference Resource");

    private static readonly DiagnosticDefinition<string> UnsupportedTarget = new(
        "TRESX002", Severity.Error, "Resource Access requires a non-generic static class: {0}", "Unsupported Resource Access target");

    private static readonly DiagnosticDefinition<string> UnsupportedEmbedding = new(
        "TRESX003", Severity.Error, "Unsupported Reference Resource embedding: {0}", "Unsupported resource embedding");

    private readonly string _referenceResource;

    public GenerateResxAccessAttribute(string referenceResource)
    {
        _referenceResource = referenceResource;
    }

    public override void BuildAspect(IAspectBuilder<INamedType> builder)
    {
        for (var type = builder.Target; type is not null; type = type.DeclaringType)
        {
            if (type.TypeParameters.Count > 0 || (type == builder.Target && (!type.IsStatic || type.TypeKind != TypeKind.Class)))
            {
                builder.Diagnostics.Report(UnsupportedTarget.WithArguments(builder.Target.ToDisplayString()));
                return;
            }
        }

        try
        {
            var referenceResource = _referenceResource;
            if (string.IsNullOrWhiteSpace(referenceResource) || !string.Equals(Path.GetExtension(referenceResource), ".resx", StringComparison.OrdinalIgnoreCase))
            {
                builder.Diagnostics.Report(InvalidReference.WithArguments("Specify a culture-neutral .resx path relative to the consumer project directory."));
                return;
            }

            var projectDirectory = Path.GetDirectoryName(builder.Target.Compilation.Project.Path);
            if (string.IsNullOrEmpty(projectDirectory))
            {
                builder.Diagnostics.Report(UnsupportedEmbedding.WithArguments("The consumer project directory is unavailable."));
                return;
            }

            var resourcePath = Path.GetFullPath(Path.Combine(projectDirectory, referenceResource));
            if (!File.Exists(resourcePath))
            {
                builder.Diagnostics.Report(InvalidReference.WithArguments($"'{referenceResource}' does not exist in the consumer project."));
                return;
            }

            var nameParts = Path.GetFileNameWithoutExtension(resourcePath).Split(new[] { '.' });
            if (nameParts.Length > 1 && CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name.Length > 0 && string.Equals(c.Name, nameParts.Last(), StringComparison.OrdinalIgnoreCase)))
            {
                builder.Diagnostics.Report(InvalidReference.WithArguments($"'{referenceResource}' is culture-specific; select the culture-neutral Reference Resource."));
                return;
            }

            if (!builder.Target.Compilation.Project.TryGetProperty("TalbyResxResourceMap", out var resourceMap) || string.IsNullOrEmpty(resourceMap) || !File.Exists(resourceMap))
            {
                builder.Diagnostics.Report(UnsupportedEmbedding.WithArguments("The SDK resource map is unavailable. Import Talby.Core.ResxAccess.targets when using a ProjectReference."));
                return;
            }

            var pathComparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
            var metadata = File.ReadLines(resourceMap).Select(line => line.Split(new[] { '|' }))
                .FirstOrDefault(parts => parts.Length == 6 && string.Equals(parts[0], resourcePath, pathComparison));
            if (metadata is null || string.IsNullOrEmpty(metadata[1]))
            {
                builder.Diagnostics.Report(UnsupportedEmbedding.WithArguments($"'{referenceResource}' must be an SDK EmbeddedResource."));
                return;
            }

            if (metadata[2].Length > 0 || metadata[3].Length > 0 || metadata[4].Length > 0 ||
                !resourcePath.StartsWith(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, pathComparison))
            {
                builder.Diagnostics.Report(UnsupportedEmbedding.WithArguments($"'{referenceResource}' uses LogicalName, ManifestResourceName, or linked-resource configuration."));
                return;
            }

            if (string.Equals(metadata[5], "true", StringComparison.OrdinalIgnoreCase))
            {
                builder.Diagnostics.Report(InvalidReference.WithArguments($"'{referenceResource}' is embedded as a culture-specific resource."));
                return;
            }

            var document = XDocument.Load(resourcePath, LoadOptions.PreserveWhitespace);
            if (document.Root?.Name != "root")
            {
                builder.Diagnostics.Report(InvalidReference.WithArguments($"'{referenceResource}' must contain a resx root element."));
                return;
            }

            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in document.Root.Elements("data"))
            {
                var key = (string?)entry.Attribute("name");
                var resourceType = (string?)entry.Attribute("type");
                if (string.IsNullOrEmpty(key) || entry.Elements("value").Count() != 1 || entry.Attribute("mimetype") is not null ||
                    (resourceType is not null && resourceType.Split(new[] { ',' })[0].Trim() != "System.String") || !keys.Add(key))
                {
                    builder.Diagnostics.Report(InvalidReference.WithArguments($"'{referenceResource}' must contain unique, named text entries with one value each."));
                    return;
                }
            }

            var resourceManagerField = builder.IntroduceField(nameof(__resxResourceManager), tags: new { ManifestBaseName = metadata[1] }).Declaration;
            foreach (var key in keys)
            {
                if (!Regex.IsMatch(key, @"^[_\p{L}\p{Nl}][_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*$"))
                {
                    continue;
                }

                var cultureMethod = builder.IntroduceMethod(nameof(RawTextWithCulture), buildMethod: method => method.Name = key, args: new { key, resourceManagerField }).Declaration;
                builder.IntroduceMethod(nameof(RawText), buildMethod: method => method.Name = key, args: new { cultureMethod });
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
        {
            builder.Diagnostics.Report(InvalidReference.WithArguments($"'{_referenceResource}' could not be read: {exception.Message}"));
        }
    }

    [Template]
    private static readonly ResourceManager __resxResourceManager = new((string)meta.Tags["ManifestBaseName"]!, meta.Target.Type.ToType().Assembly);

    [Template]
    public static string RawText([CompileTime] IMethod cultureMethod)
        => cultureMethod.Invoke(CultureInfo.CurrentUICulture)!;

    [Template]
    public static string RawTextWithCulture(CultureInfo resourceCulture, [CompileTime] string key, [CompileTime] IField resourceManagerField)
    {
        ArgumentNullException.ThrowIfNull(resourceCulture);
        var resourceManager = (ResourceManager)resourceManagerField.Value!;
        try
        {
            return resourceManager.GetString(key, resourceCulture)
                ?? throw new InvalidOperationException($"Resource Key '{key}' was not found in '{resourceManager.BaseName}' for Resource Culture '{resourceCulture.Name}'.");
        }
        catch (MissingManifestResourceException exception)
        {
            throw new InvalidOperationException($"Reference Resource '{resourceManager.BaseName}' could not be loaded for Resource Key '{key}' and Resource Culture '{resourceCulture.Name}'.", exception);
        }
        catch (MissingSatelliteAssemblyException exception)
        {
            throw new InvalidOperationException($"Satellite resources for '{resourceManager.BaseName}' could not be loaded for Resource Key '{key}' and Resource Culture '{resourceCulture.Name}'.", exception);
        }
    }
}
