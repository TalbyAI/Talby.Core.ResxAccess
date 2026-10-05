namespace Talby.Core.ResxAccess.IntegrationTests;

public sealed class ConsumerDiagnosticsFixture
{
    private readonly Lazy<Task<((int ExitCode, string Output) Diagnostics, (int ExitCode, string Output) Malformed)>> builds = new(Build);

    public async Task<(int ExitCode, string Output)> BuildDiagnostics()
        => (await builds.Value).Diagnostics;

    public async Task<(int ExitCode, string Output)> BuildMalformedResource()
        => (await builds.Value).Malformed;

    private static async Task<((int ExitCode, string Output) Diagnostics, (int ExitCode, string Output) Malformed)> Build()
    {
        using var diagnostics = new ConsumerProject("Console.WriteLine(\"Unused\");", projectItems: """
            <EmbeddedResource Update="Resources/LogicalName.resx"><LogicalName>Custom.LogicalName.resources</LogicalName></EmbeddedResource>
            <EmbeddedResource Update="Resources/ManifestResourceName.resx"><ManifestResourceName>Custom.ManifestResourceName</ManifestResourceName></EmbeddedResource>
            <EmbeddedResource Update="Resources/Linked.resx"><Link>Other/Linked.resx</Link></EmbeddedResource>
            <EmbeddedResource Remove="Resources/Excluded.es.resx" />
            <EmbeddedResource Update="Resources/CustomName.es.resx"><LogicalName>Other.resources</LogicalName></EmbeddedResource>
            <EmbeddedResource Update="Resources/Neutral.es.resx"><WithCulture>false</WithCulture></EmbeddedResource>
            """);
        using var malformedResource = new ConsumerProject("""
            using Talby.Core.ResxAccess;
            Console.WriteLine("Unused");
            [GenerateResxAccess("Resources/Labels.resx")]
            public static class Texts
            {
            }
            """);

        RawTextConsumerTests.WriteInvalidResources(diagnostics);
        LocalizedResourceConsumerTests.WriteUnsupportedCultures(diagnostics);
        LocalizedResourceConsumerTests.WriteInconsistentResources(diagnostics);
        LocalizedResourceConsumerTests.WriteExpectedCultures(diagnostics);
        LocalizedResourceConsumerTests.WriteInvalidSatelliteEmbedding(diagnostics);
        IndexedPlaceholderConsumerTests.WriteMalformedPlaceholders(diagnostics);
        IndexedPlaceholderConsumerTests.WriteChangedPlaceholderContracts(diagnostics);
        malformedResource.Write("Resources/Labels.resx", "<root><data>");

        // Exactly two isolated projects; neither builds or restores the shared library.
        var results = await Task.WhenAll(diagnostics.Build(), malformedResource.Build());
        return (results[0], results[1]);
    }
}
