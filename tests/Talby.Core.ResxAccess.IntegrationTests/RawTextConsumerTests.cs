namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class RawTextConsumerTests
{
    internal const string ReferenceResource = """
        <root>
          <resheader name="resmimetype"><value>text/microsoft-resx</value></resheader>
          <resheader name="version"><value>2.0</value></resheader>
          <resheader name="reader"><value>System.Resources.ResXResourceReader, System.Windows.Forms</value></resheader>
          <resheader name="writer"><value>System.Resources.ResXResourceWriter, System.Windows.Forms</value></resheader>
          <data name="Welcome" xml:space="preserve"><value>  Hello {name@string}, {0:N2}!  </value></data>
          <data name="Plain"><value>Just text</value></data>
        </root>
        """;

    [Fact]
    public async Task CanCompileAndInvokeIndependentResourceSets()
    {
        var invocation = await ConsumerProject.Invoke(typeof(Customer.Api.AssociatedTexts).Assembly.Location);
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal("  Hello {name@string}, {0:N2}!  \n  Hello {name@string}, {0:N2}!  \nBasic text\nAssociated Texto\nAssociated Texte\nAssociated text\n  Associated hello {name@string}, {0:N2}!  \nPreserved\n", invocation.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task ReportsEachInvalidResourceAndEmbeddingInOneBuild()
    {
        using var consumer = new ConsumerProject("Console.WriteLine(\"Unused\");", projectItems: """
            <EmbeddedResource Update="Resources/LogicalName.resx"><LogicalName>Custom.LogicalName.resources</LogicalName></EmbeddedResource>
            <EmbeddedResource Update="Resources/ManifestResourceName.resx"><ManifestResourceName>Custom.ManifestResourceName</ManifestResourceName></EmbeddedResource>
            <EmbeddedResource Update="Resources/Linked.resx"><Link>Other/Linked.resx</Link></EmbeddedResource>
            """);
        var cases = new[]
        {
            (Target: "LogicalNameTarget", Resource: "Resources/LogicalName.resx", Code: "TRESX003", Message: "Unsupported Reference Resource embedding: 'Resources/LogicalName.resx' uses LogicalName, ManifestResourceName, or linked-resource configuration."),
            (Target: "ManifestResourceNameTarget", Resource: "Resources/ManifestResourceName.resx", Code: "TRESX003", Message: "Unsupported Reference Resource embedding: 'Resources/ManifestResourceName.resx' uses LogicalName, ManifestResourceName, or linked-resource configuration."),
            (Target: "LinkedTarget", Resource: "Resources/Linked.resx", Code: "TRESX003", Message: "Unsupported Reference Resource embedding: 'Resources/Linked.resx' uses LogicalName, ManifestResourceName, or linked-resource configuration."),
            (Target: "MissingResourceTarget", Resource: "Resources/Missing.resx", Code: "TRESX001", Message: "Invalid Reference Resource: 'Resources/Missing.resx' does not exist in the consumer project."),
            (Target: "LocalizedResourceTarget", Resource: "Resources/Localized.es.resx", Code: "TRESX001", Message: "Invalid Reference Resource: 'Resources/Localized.es.resx' is culture-specific; select the culture-neutral Reference Resource.")
        };
        foreach (var (target, resource, _, _) in cases)
        {
            consumer.Write($"{target}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("{{resource}}")]
                public static class {{target}}
                {
                }
                """);
            if (target != "MissingResourceTarget")
            {
                consumer.Write(resource, ReferenceResource);
            }
        }

        var build = await consumer.Build();
        Assert.NotEqual(0, build.ExitCode);
        var diagnosticLines = build.Output.Split('\n');
        foreach (var (target, _, code, message) in cases)
        {
            Assert.Contains(diagnosticLines, line => line.Contains($"{target}.cs(", StringComparison.Ordinal) && line.Contains($"error {code}: {message}", StringComparison.Ordinal));
        }
    }

    [Fact]
    public async Task ReportsMalformedReferenceResourceWithoutAspectCrash()
    {
        using var consumer = new ConsumerProject("""
            using Talby.Core.ResxAccess;
            Console.WriteLine("Unused");
            [GenerateResxAccess("Resources/Labels.resx")]
            public static class Texts
            {
            }
            """);
        consumer.Write("Resources/Labels.resx", "<root><data>");

        var build = await consumer.Build();
        Assert.NotEqual(0, build.ExitCode);
        Assert.Contains("error MSB3103", build.Output);
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task DescribesMissingRuntimeManifestAndResourceKey()
    {
        foreach (var scenario in new[] { "missing-manifest", "missing-key" })
        {
            var invocation = await ConsumerProject.Invoke(typeof(Customer.Api.AssociatedTexts).Assembly.Location, scenario);
            Assert.True(invocation.ExitCode == 0, invocation.Output);
            Assert.Equal("Descriptive failure", invocation.Output.Trim());
        }
    }
}
