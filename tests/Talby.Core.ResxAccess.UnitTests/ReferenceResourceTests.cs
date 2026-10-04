namespace Talby.Core.ResxAccess.UnitTests;

public class ReferenceResourceTests
{
    [Fact]
    public void ReadsTextEntriesAndSdkManifestName()
    {
        using var resource = new ResourceInput("<root><data name=\"Welcome\"><value> Hello {name} </value></data></root>");

        var result = ReferenceResourceReader.Read(resource.ReferencePath, resource.ProjectPath, resource.MapPath);

        Assert.Equal("ConsumerRoot.Resources.Labels", result.ManifestBaseName);
        Assert.Equal(new[] { "Welcome" }, result.Keys);
    }

    [Fact]
    public void AcceptsExplicitStringTypesEmptyValuesAndCaseSensitiveKeys()
    {
        using var resource = new ResourceInput("""
            <root>
              <data name="Text" type="System.String, mscorlib"><value /></data>
              <data name="text"><value>Raw {0:N2}</value></data>
            </root>
            """);
        Assert.Equal(new[] { "Text", "text" }, resource.Read().Keys);
    }

    [Fact]
    public void AcceptsAnEmptyReferenceResource()
    {
        using var resource = new ResourceInput("<root />");
        Assert.Empty(resource.Read().Keys);
    }

    [Fact]
    public void RejectsDuplicateLocalizedResourceCultures()
    {
        foreach (var duplicateName in new[] { "Labels.es-MX.RESX", "Labels.es-mx.resx" })
        {
            using var resource = new ResourceInput("<root />");
            var directory = Path.GetDirectoryName(resource.ResourcePath)!;
            var localizedPath = Path.Combine(directory, "Labels.es-MX.resx");
            File.WriteAllText(localizedPath, "<root />");
            File.AppendAllText(resource.MapPath, $"\n{localizedPath}|ConsumerRoot.Resources.Labels.es-MX||||true");
            Assert.Empty(resource.Read().Keys);

            var duplicatePath = Path.Combine(directory, duplicateName);
            File.WriteAllText(duplicatePath, "<root />");
            File.AppendAllText(resource.MapPath, $"\n{duplicatePath}|ConsumerRoot.Resources.Labels.{Path.GetFileNameWithoutExtension(duplicateName)["Labels.".Length..]}||||true");

            // Case-insensitive file systems retain one file when the spelling changes.
            if (Directory.GetFiles(directory).Length == 2)
            {
                Assert.Empty(resource.Read().Keys);
                continue;
            }

            var error = Assert.Throws<ResourceValidationException>(() => resource.Read());
            Assert.True(error.LocalizedResource);
            Assert.Contains("'Resources/Labels.es-", error.Message);
            Assert.Contains("duplicate Localized Resource for Resource Culture 'es-MX'", error.Message);
        }
    }

    [Fact]
    public void RejectsMalformedXmlWithoutAnUnhandledXmlException()
    {
        using var resource = new ResourceInput("<root><data>");
        var error = Assert.Throws<ResourceValidationException>(() => resource.Read());
        Assert.False(error.UnsupportedEmbedding);
        Assert.StartsWith("'Resources/Labels.resx' could not be read: ", error.Message);
    }

    [Fact]
    public void RejectsInvalidRootElements()
    {
        foreach (var xml in new[] { "<resources />", "<root xmlns=\"urn:other\" />" })
        {
            using var resource = new ResourceInput(xml);
            AssertInvalidReference(resource, "'Resources/Labels.resx' must contain a resx root element.");
        }
    }

    [Fact]
    public void RejectsDuplicateOrUnnamedResourceKeys()
    {
        foreach (var entries in new[]
        {
            "<data><value>Text</value></data>",
            "<data name=\"\"><value>Text</value></data>",
            "<data name=\"Same\"><value>One</value></data><data name=\"Same\"><value>Two</value></data>"
        })
        {
            using var resource = new ResourceInput($"<root>{entries}</root>");
            AssertInvalidReference(resource, "'Resources/Labels.resx' must contain unique, named text entries with one value each.");
        }
    }

    [Fact]
    public void RejectsInvalidValueAndTypeStructures()
    {
        foreach (var entry in new[]
        {
            "<data name=\"Text\" />",
            "<data name=\"Text\"><value>One</value><value>Two</value></data>",
            "<data name=\"Text\" mimetype=\"\"><value>Text</value></data>",
            "<data name=\"Text\" type=\"System.Int32, mscorlib\"><value>1</value></data>",
            "<data name=\"Text\" type=\"\"><value>Text</value></data>"
        })
        {
            using var resource = new ResourceInput($"<root>{entry}</root>");
            AssertInvalidReference(resource, "'Resources/Labels.resx' must contain unique, named text entries with one value each.");
        }
    }

    [Fact]
    public void RejectsInvalidPathsBeforeUnavailableProjectContext()
    {
        foreach (var path in new[] { null, "", " ", "Labels.txt" })
        {
            var error = Assert.Throws<ResourceValidationException>(() => ReferenceResourceReader.Read(path!, null, null));
            Assert.False(error.UnsupportedEmbedding);
            Assert.Equal("Specify a culture-neutral .resx path relative to the consumer project directory.", error.Message);
        }
    }

    [Fact]
    public void DistinguishesUnavailableProjectContextFromMissingFiles()
    {
        var contextError = Assert.Throws<ResourceValidationException>(() => ReferenceResourceReader.Read("Labels.resx", null, null));
        Assert.True(contextError.UnsupportedEmbedding);
        Assert.Equal("The consumer project directory is unavailable.", contextError.Message);

        using var resource = new ResourceInput("<root />");
        File.Delete(resource.ResourcePath);
        AssertInvalidReference(resource, "'Resources/Labels.resx' does not exist in the consumer project.");
    }

    [Fact]
    public void RejectsCultureSpecificReferenceResourcesBeforeMissingSdkMap()
    {
        using var resource = new ResourceInput("<root />", "Resources/Labels.es.resx");
        File.Delete(resource.MapPath);
        AssertInvalidReference(resource, "'Resources/Labels.es.resx' is culture-specific; select the culture-neutral Reference Resource.");
    }

    [Fact]
    public void RejectsUnavailableSdkMaps()
    {
        using var resource = new ResourceInput("<root />");
        foreach (var mapPath in new[] { null, "", Path.Combine(resource.DirectoryPath, "Missing.txt") })
        {
            var error = Assert.Throws<ResourceValidationException>(() => ReferenceResourceReader.Read(resource.ReferencePath, resource.ProjectPath, mapPath));
            Assert.True(error.UnsupportedEmbedding);
            Assert.Equal("The SDK resource map is unavailable. Import Talby.Core.ResxAccess.targets when using a ProjectReference.", error.Message);
        }
    }

    [Fact]
    public void RejectsMissingOrMalformedEmbeddedResourceMetadata()
    {
        using var resource = new ResourceInput("<root />");
        foreach (var map in new[] { "", "wrong|shape", $"{resource.ResourcePath}|||||false", "other.resx|Other||||false" })
        {
            File.WriteAllText(resource.MapPath, map);
            AssertUnsupportedEmbedding(resource, "'Resources/Labels.resx' must be an SDK EmbeddedResource.");
        }
    }

    [Fact]
    public void RejectsCustomNamesAndLinkedResourceMetadata()
    {
        using var resource = new ResourceInput("<root />");
        foreach (var metadata in new[] { "Name|Custom|||false", "Name||Custom||false", "Name|||Other/Labels.resx|false" })
        {
            File.WriteAllText(resource.MapPath, $"{resource.ResourcePath}|{metadata}");
            AssertUnsupportedEmbedding(resource, "'Resources/Labels.resx' uses LogicalName, ManifestResourceName, or linked-resource configuration.");
        }
    }

    [Fact]
    public void RejectsResourcesOutsideTheProjectDirectory()
    {
        using var resource = new ResourceInput("<root />");
        var projectPath = Path.Combine(resource.DirectoryPath, "Child", "Consumer.csproj");
        var error = Assert.Throws<ResourceValidationException>(() => ReferenceResourceReader.Read("../Resources/Labels.resx", projectPath, resource.MapPath));
        Assert.True(error.UnsupportedEmbedding);
        Assert.Equal("'../Resources/Labels.resx' uses LogicalName, ManifestResourceName, or linked-resource configuration.", error.Message);
    }

    [Fact]
    public void RejectsCultureSpecificSdkMetadata()
    {
        using var resource = new ResourceInput("<root />");
        File.WriteAllText(resource.MapPath, $"{resource.ResourcePath}|Name||||TRUE");
        AssertInvalidReference(resource, "'Resources/Labels.resx' is embedded as a culture-specific resource.");
    }

    [Fact]
    public void MatchesResourceMapPathsUsingPlatformComparison()
    {
        using var resource = new ResourceInput("<root />");
        File.WriteAllText(resource.MapPath, $"{resource.ResourcePath.ToUpperInvariant()}|Name||||false");
        if (Path.DirectorySeparatorChar == '\\')
        {
            Assert.Equal("Name", resource.Read().ManifestBaseName);
        }
        else
        {
            AssertUnsupportedEmbedding(resource, "'Resources/Labels.resx' must be an SDK EmbeddedResource.");
        }
    }

    [Fact]
    public void RecognizesKeywordAndUnicodeResourceKeyIdentifiers()
    {
        foreach (var key in new[] { "class", "_Text", "áéí", "漢字", "Text2", "A\u0301", "Ⅳ" })
        {
            Assert.True(ReferenceResourceReader.IsResourceKeyIdentifier(key), key);
        }
        foreach (var key in new[] { "", "two words", "has-dash", "1Text", "😀" })
        {
            Assert.False(ReferenceResourceReader.IsResourceKeyIdentifier(key), key);
        }
    }

    private static void AssertInvalidReference(ResourceInput resource, string message)
    {
        var error = Assert.Throws<ResourceValidationException>(() => resource.Read());
        Assert.False(error.UnsupportedEmbedding);
        Assert.Equal(message, error.Message);
    }

    private static void AssertUnsupportedEmbedding(ResourceInput resource, string message)
    {
        var error = Assert.Throws<ResourceValidationException>(() => resource.Read());
        Assert.True(error.UnsupportedEmbedding);
        Assert.Equal(message, error.Message);
    }

    private sealed class ResourceInput : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "ResxAccessUnitTests", Guid.NewGuid().ToString("N"));
        public string DirectoryPath => _directory;
        public string ReferencePath { get; }
        public string ResourcePath => Path.GetFullPath(Path.Combine(_directory, ReferencePath));
        public string ProjectPath => Path.Combine(_directory, "Consumer.csproj");
        public string MapPath => Path.Combine(_directory, "resources.txt");

        public ResourceInput(string xml, string referencePath = "Resources/Labels.resx")
        {
            ReferencePath = referencePath;
            Directory.CreateDirectory(Path.Combine(_directory, "Resources"));
            var path = Path.GetFullPath(Path.Combine(_directory, ReferencePath));
            File.WriteAllText(path, xml);
            File.WriteAllText(MapPath, $"{path}|ConsumerRoot.Resources.Labels||||false");
        }

        public (string ManifestBaseName, HashSet<string> Keys) Read()
        {
            var resource = ReferenceResourceReader.Read(ReferencePath, ProjectPath, MapPath);
            return (resource.ManifestBaseName, resource.Keys);
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }
}
