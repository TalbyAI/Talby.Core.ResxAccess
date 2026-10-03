using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using Metalama.Framework.Aspects;

namespace Talby.Core.ResxAccess;

[CompileTime]
internal static class ReferenceResourceReader
{
    public static (string ManifestBaseName, HashSet<string> Keys) Read(string referenceResource, string? projectPath, string? resourceMap, string[]? expectedCultures = null)
    {
        try
        {
            return ReadCore(referenceResource, projectPath, resourceMap, expectedCultures);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
        {
            throw new ResourceValidationException($"'{referenceResource}' could not be read: {exception.Message}");
        }
    }

    private static (string ManifestBaseName, HashSet<string> Keys) ReadCore(string referenceResource, string? projectPath, string? resourceMap, string[]? expectedCultures)
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
        var resourceMetadata = File.ReadLines(resourceMap).Select(line => line.Split(new[] { '|' })).Where(parts => parts.Length == 6).ToArray();
        var metadata = resourceMetadata.FirstOrDefault(parts => string.Equals(parts[0], resourcePath, pathComparison));
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

        var keys = ReadKeys(resourcePath, referenceResource);
        var cultureNames = CultureInfo.GetCultures(CultureTypes.AllCultures).Where(c => c.Name.Length > 0).Select(c => c.Name).ToDictionary(name => name, StringComparer.OrdinalIgnoreCase);
        foreach (var culture in expectedCultures ?? Array.Empty<string>())
        {
            if (culture is null || !cultureNames.ContainsKey(culture))
            {
                throw new ResourceValidationException($"ExpectedCultures contains invalid Resource Culture '{culture ?? "(null)"}'. Specify a non-empty culture name.", invalidExpectedCultures: true);
            }
        }

        var discoveredCultures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var prefix = Path.GetFileNameWithoutExtension(resourcePath) + ".";
        foreach (var localizedPath in Directory.EnumerateFiles(Path.GetDirectoryName(resourcePath)!).OrderBy(path => path, StringComparer.Ordinal))
        {
            var name = Path.GetFileNameWithoutExtension(localizedPath);
            if (!string.Equals(Path.GetExtension(localizedPath), ".resx", StringComparison.OrdinalIgnoreCase) ||
                !name.StartsWith(prefix, pathComparison) || !cultureNames.TryGetValue(name.Substring(prefix.Length), out var canonicalCulture))
            {
                continue;
            }

            var culture = name.Substring(prefix.Length);
            discoveredCultures.Add(culture);
            var localizedResource = Path.Combine(Path.GetDirectoryName(referenceResource) ?? "", Path.GetFileName(localizedPath)).Replace('\\', '/');
            try
            {
                if (!string.Equals(culture, canonicalCulture, StringComparison.Ordinal) &&
                    !string.Equals(culture, canonicalCulture.ToLowerInvariant(), StringComparison.Ordinal))
                {
                    throw new ResourceValidationException($"'{localizedResource}' must use the canonical Resource Culture suffix '{canonicalCulture}' or its lowercase form for runtime satellite probing.");
                }

                var localizedKeys = ReadKeys(localizedPath, localizedResource);
                if (!keys.SetEquals(localizedKeys))
                {
                    throw new ResourceValidationException($"'{localizedResource}' must contain exactly the Reference Resource's case-sensitive Resource Keys. Missing: {DescribeKeys(keys.Except(localizedKeys))}. Additional: {DescribeKeys(localizedKeys.Except(keys))}.");
                }

                var localizedMetadata = resourceMetadata.FirstOrDefault(parts => string.Equals(parts[0], localizedPath, pathComparison));
                if (localizedMetadata is null || localizedMetadata[1] != metadata[1] + "." + culture ||
                    localizedMetadata[2].Length > 0 || localizedMetadata[3].Length > 0 || localizedMetadata[4].Length > 0 ||
                    !string.Equals(localizedMetadata[5], "true", StringComparison.OrdinalIgnoreCase))
                {
                    throw new ResourceValidationException($"'{localizedResource}' must use standard SDK satellite embedding for this Resource Set.");
                }
            }
            catch (ResourceValidationException exception)
            {
                throw new ResourceValidationException(exception.Message, localizedResource: true);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or XmlException or ArgumentException)
            {
                throw new ResourceValidationException($"'{localizedResource}' could not be read: {exception.Message}", localizedResource: true);
            }
        }

        foreach (var culture in expectedCultures ?? Array.Empty<string>())
        {
            if (!discoveredCultures.Contains(culture))
            {
                throw new ResourceValidationException($"Expected Culture '{culture}' requires an associated Localized Resource for '{referenceResource}'.", invalidExpectedCultures: true);
            }
        }

        return (metadata[1], keys);
    }

    private static string DescribeKeys(IEnumerable<string> keys)
    {
        var names = keys.OrderBy(key => key, StringComparer.Ordinal).Select(key => $"'{key}'").ToArray();
        return names.Length == 0 ? "(none)" : string.Join(", ", names);
    }

    private static HashSet<string> ReadKeys(string resourcePath, string resourceName)
    {
        var document = XDocument.Load(resourcePath, LoadOptions.PreserveWhitespace);
        if (document.Root?.Name != "root")
        {
            throw new ResourceValidationException($"'{resourceName}' must contain a resx root element.");
        }

        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in document.Root.Elements("data"))
        {
            var key = (string?)entry.Attribute("name");
            var resourceType = (string?)entry.Attribute("type");
            if (string.IsNullOrEmpty(key) || entry.Elements("value").Count() != 1 || entry.Attribute("mimetype") is not null ||
                (resourceType is not null && resourceType.Split(new[] { ',' })[0].Trim() != "System.String") || !keys.Add(key))
            {
                throw new ResourceValidationException($"'{resourceName}' must contain unique, named text entries with one value each.");
            }
        }

        return keys;
    }

    public static bool IsResourceKeyIdentifier(string key)
        => Regex.IsMatch(key, @"^[_\p{L}\p{Nl}][_\p{L}\p{Nl}\p{Nd}\p{Pc}\p{Mn}\p{Mc}\p{Cf}]*$");
}
