using System.Text.Json;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class DesignTimeResourceConsumerTests
{
    [Fact]
    public async Task ExposesResourceChangesAsWatchedDesignTimeCompilationInputs()
    {
        const string source = """
            using Talby.Core.ResxAccess;

            [GenerateResxAccess("Resources/Texts.resx", ExpectedCultures = new[] { "es" })]
            internal static class Texts { }

            internal static class Program
            {
                private static void Main() { }
            }
            """;
        using var project = new ConsumerProject(
            source,
            projectItems: "<EmbeddedResource Remove=\"Resources/Texts.fr.resx\" />"
        );
        var reference = RawTextConsumerTests.ReferenceResource;
        var localized = reference.Replace("{name@string}", "{name}");
        project.Write("Resources/Texts.resx", reference);
        project.Write("Resources/Texts.es.resx", localized);
        var build = await project.Build();
        Assert.True(build.ExitCode == 0, build.Output);

        var initial = await ReadInputs(project, "Resources/Texts.resx", "Resources/Texts.es.resx");
        Assert.Equal(
            initial,
            await ReadInputs(project, "Resources/Texts.resx", "Resources/Texts.es.resx")
        );

        var referencePath = Path.Combine(project.DirectoryPath, "Resources/Texts.resx");
        var referenceWriteTime = File.GetLastWriteTimeUtc(referencePath);
        project.Write(
            "Resources/Texts.resx",
            reference.Replace("{name@string}", "{person@string?}")
        );
        File.SetLastWriteTimeUtc(referencePath, referenceWriteTime);
        var changed = await ReadInputs(project, "Resources/Texts.resx", "Resources/Texts.es.resx");
        Assert.NotEqual(initial.Text, changed.Text);

        project.Write("Resources/Texts.es.resx", localized.Replace("{name}", "{person}"));
        var corrected = await ReadInputs(
            project,
            "Resources/Texts.resx",
            "Resources/Texts.es.resx"
        );
        Assert.NotEqual(changed.Text, corrected.Text);

        project.Write("Resources/Texts.fr.resx", localized.Replace("{name}", "{other}"));
        var added = await ReadInputs(
            project,
            "Resources/Texts.resx",
            "Resources/Texts.es.resx",
            "Resources/Texts.fr.resx"
        );
        Assert.NotEqual(corrected.Text, added.Text);

        File.Delete(Path.Combine(project.DirectoryPath, "Resources/Texts.es.resx"));
        var removed = await ReadInputs(project, "Resources/Texts.resx", "Resources/Texts.fr.resx");
        Assert.NotEqual(added.Text, removed.Text);

        project.Write("Resources/Texts.es.resx", localized.Replace("{name}", "{person}"));
        var restored = await ReadInputs(
            project,
            "Resources/Texts.resx",
            "Resources/Texts.es.resx",
            "Resources/Texts.fr.resx"
        );
        Assert.Equal(added.Text, restored.Text);
        Assert.Equal(source, File.ReadAllText(Path.Combine(project.DirectoryPath, "Program.cs")));
    }

    private static async Task<(string Text, DateTime WriteTime)> ReadInputs(
        ConsumerProject project,
        params string[] resources
    )
    {
        var build = await project.DesignTimeBuild();
        Assert.True(build.ExitCode == 0, build.Output);
        using var json = JsonDocument.Parse(build.Output);
        var mapPath = json
            .RootElement.GetProperty("Properties")
            .GetProperty("TalbyResxResourceMap")
            .GetString()!;
        Assert.Contains(
            File.ReadLines(mapPath),
            row =>
                row
                == $"{Path.GetFullPath(Path.Combine(project.DirectoryPath, "Resources/Texts.resx"))}|ConsumerRoot.Resources.Texts||||false"
        );
        var items = json.RootElement.GetProperty("Items");
        var watched = items
            .GetProperty("AdditionalDesignTimeBuildInput")
            .EnumerateArray()
            .ToArray();
        foreach (var resource in resources)
        {
            Assert.Contains(
                watched,
                item =>
                    item.GetProperty("FullPath").GetString()
                        == Path.GetFullPath(Path.Combine(project.DirectoryPath, resource))
                    && item.GetProperty("ContentSensitive").GetString() == "true"
            );
        }
        Assert.Equal(
            resources.Length,
            watched.Count(item => item.GetProperty("Extension").GetString() == ".resx")
        );
        var arguments = items
            .GetProperty("CscCommandLineArgs")
            .EnumerateArray()
            .Select(item => item.GetProperty("Identity").GetString()!)
            .ToArray();
        var dependency = Assert.Single(
            arguments,
            argument => argument.EndsWith("TalbyResxDependency.g.cs", StringComparison.Ordinal)
        );
        var path = Path.GetFullPath(Path.Combine(project.DirectoryPath, dependency.Trim('"')));
        return (File.ReadAllText(path), File.GetLastWriteTimeUtc(path));
    }
}
