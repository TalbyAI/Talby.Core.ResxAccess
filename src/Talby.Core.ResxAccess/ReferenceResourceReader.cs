using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal static class ReferenceResourceReader
{
    public static (string ManifestBaseName, HashSet<string> Keys) Read(string referenceResource, string? projectPath, string? resourceMap)
    {
        try
        {
            return ReadCore(referenceResource, projectPath, resourceMap);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
        {
            throw new ResourceValidationException($"'{referenceResource}' could not be read: {exception.Message}");
        }
    }

    private static (string ManifestBaseName, HashSet<string> Keys) ReadCore(string referenceResource, string? projectPath, string? resourceMap)
    {
        if (string.IsNullOrWhiteSpace(referenceResource) || !string.Equals(Path.GetExtension(referenceResource), ".resx", StringComparison.OrdinalIgnoreCase))
        {
            throw new ResourceValidationException("Specify a culture-neutral .resx path relative to the consumer project directory.");
        }

        var projectDirectory = Path.GetDirectoryName(projectPath);
        if (string.IsNullOrEmpty(projectDirectory))
        {
            throw new ResourceValidationException("The consumer project directory is unavailable.", unsupportedEmbedding: true);
        }

        var resourcePath = Path.GetFullPath(Path.Combine(projectDirectory, referenceResource));
        if (!File.Exists(resourcePath))
        {
            throw new ResourceValidationException($"'{referenceResource}' does not exist in the consumer project.");
        }

        var nameParts = Path.GetFileNameWithoutExtension(resourcePath).Split(new[] { '.' });
        if (nameParts.Length > 1 && CultureInfo.GetCultures(CultureTypes.AllCultures).Any(c => c.Name.Length > 0 && string.Equals(c.Name, nameParts.Last(), StringComparison.OrdinalIgnoreCase)))
        {
            throw new ResourceValidationException($"'{referenceResource}' is culture-specific; select the culture-neutral Reference Resource.");
        }

        if (string.IsNullOrEmpty(resourceMap) || !File.Exists(resourceMap))
        {
            throw new ResourceValidationException("The SDK resource map is unavailable. Import Talby.Core.ResxAccess.targets when using a ProjectReference.", unsupportedEmbedding: true);
        }

        var pathComparison = Path.DirectorySeparatorChar == '\\' ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var metadata = File.ReadLines(resourceMap).Select(line => line.Split(new[] { '|' }))
            .FirstOrDefault(parts => parts.Length == 6 && string.Equals(parts[0], resourcePath, pathComparison));
        if (metadata is null || string.IsNullOrEmpty(metadata[1]))
        {
            throw new ResourceValidationException($"'{referenceResource}' must be an SDK EmbeddedResource.", unsupportedEmbedding: true);
        }

        if (metadata[2].Length > 0 || metadata[3].Length > 0 || metadata[4].Length > 0 ||
            !resourcePath.StartsWith(projectDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar, pathComparison))
        {
            throw new ResourceValidationException($"'{referenceResource}' uses LogicalName, ManifestResourceName, or linked-resource configuration.", unsupportedEmbedding: true);
        }

        if (string.Equals(metadata[5], "true", StringComparison.OrdinalIgnoreCase))
        {
            throw new ResourceValidationException($"'{referenceResource}' is embedded as a culture-specific resource.");
        }

        var document = XDocument.Load(resourcePath, LoadOptions.PreserveWhitespace);
        if (document.Root?.Name != "root")
        {
            throw new ResourceValidationException($"'{referenceResource}' must contain a resx root element.");
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in document.Root.Elements("data"))
        {
            var key = (string?)entry.Attribute("name");
            var resourceType = (string?)entry.Attribute("type");
            if (string.IsNullOrEmpty(key) || entry.Elements("value").Count() != 1 || entry.Attribute("mimetype") is not null ||
                (resourceType is not null && resourceType.Split(new[] { ',' })[0].Trim() != "System.String") || !keys.Add(key))
            {
                throw new ResourceValidationException($"'{referenceResource}' must contain unique, named text entries with one value each.");
            }
        }

        return (metadata[1], keys);
    }

    public static bool IsResourceKeyIdentifier(string key)
        => Regex.IsMatch(key, @"^[_\p{L}\p{Nl}][_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*$");
}
