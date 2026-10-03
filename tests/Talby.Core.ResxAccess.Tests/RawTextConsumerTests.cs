namespace Talby.Core.ResxAccess.Tests;

[Trait("Category", "Integration")]
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
        using var consumer = new ConsumerProject("""
            using System.Globalization;
            using Talby.Core.ResxAccess;

            var originalCulture = CultureInfo.CurrentCulture;
            var originalUICulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
                Console.WriteLine(Customer.Api.Texts.Welcome());
                Console.WriteLine(Customer.Api.Texts.Welcome(CultureInfo.InvariantCulture));
                Console.WriteLine(Customer.Api.Texts.Plain());
                var type = typeof(Customer.Api.Texts);
                if (type.IsPublic || type.FullName != "Customer.Api.Texts") throw new Exception("Class identity changed.");
                var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
                if (methods.Length != 4 || methods.Any(m => m.Name.StartsWith("Format"))) throw new Exception("Unexpected API.");

                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
                CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
                Console.WriteLine(Customer.Api.AssociatedTexts.Plain());
                Console.WriteLine(Customer.Api.AssociatedTexts.Plain(CultureInfo.GetCultureInfo("fr-CA")));
                Console.WriteLine(Customer.Api.AssociatedTexts.Plain(CultureInfo.GetCultureInfo("de-DE")));
                Console.WriteLine(Customer.Api.AssociatedTexts.Welcome(CultureInfo.InvariantCulture));
                if (!typeof(Customer.Api.AssociatedTexts).IsPublic) throw new Exception("Accessibility changed.");
                try { Customer.Api.AssociatedTexts.Plain(null!); throw new Exception("Null culture accepted."); }
                catch (ArgumentNullException) { }

                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
                if (Customer.Api.EdgeTexts.Plain() != "Edge text" || Customer.Api.EdgeTexts.@class() != "Keyword" || Customer.Api.EdgeTexts.Café() != "Unicode" || Customer.Api.EdgeTexts.Empty() != "" || Customer.Api.EdgeTexts.Blank() != "   ") throw new Exception("Raw Text changed.");
                Console.WriteLine("Preserved");
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
                CultureInfo.CurrentUICulture = originalUICulture;
            }

            namespace Customer.Api
            {
                [GenerateResxAccess("Resources/Basic.resx")]
                internal static class Texts
                {
                }

                [GenerateResxAccess("Resources/Associated.resx")]
                public static class AssociatedTexts
                {
                }

                [GenerateResxAccess("Resources/en.resx")]
                public static class EdgeTexts
                {
                }
            }
            """);
        consumer.Write("Resources/Basic.resx", ReferenceResource.Replace("Just text", "Basic text").Replace("</root>", "<data name=\"invalid-key\"><value>Not representable</value></data></root>"));
        consumer.Write("Resources/Associated.cs", """
            namespace Unrelated.Namespace;
            public class ResourceAnchor
            {
            }
            """);
        var associatedResource = ReferenceResource.Replace("Hello", "Associated hello");
        consumer.Write("Resources/Associated.resx", associatedResource.Replace("Just text", "Associated text"));
        consumer.Write("Resources/Associated.es.resx", associatedResource.Replace("Just text", "Associated Texto"));
        consumer.Write("Resources/Associated.fr.resx", associatedResource.Replace("Just text", "Associated Texte"));
        consumer.Write("Resources/en.resx", ReferenceResource.Replace("Just text", "Edge text").Replace("</root>", """
            <data name="class"><value>Keyword</value></data>
            <data name="Café"><value>Unicode</value></data>
            <data name="Empty"><value></value></data>
            <data name="Blank" xml:space="preserve"><value>   </value></data>
            </root>
            """));

        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        var invocation = await consumer.Invoke();
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
        foreach (var missingManifest in new[] { true, false })
        {
            var sabotage = missingManifest
                ? "<ItemGroup><_CoreCompileResourceInputs Remove=\"@(_CoreCompileResourceInputs)\" /></ItemGroup>"
                : "<Copy SourceFiles=\"Replacement.resources\" DestinationFiles=\"$(IntermediateOutputPath)ConsumerRoot.Resources.Labels.resources\" />";
            using var consumer = new ConsumerProject($$"""
                using System.Globalization;
                using System.Resources;
                using Talby.Core.ResxAccess;
                try { Texts.Plain(CultureInfo.GetCultureInfo("de-DE")); throw new Exception("Expected failure."); }
                catch (InvalidOperationException exception)
                {
                    if (!exception.Message.Contains("Plain") || !exception.Message.Contains("ConsumerRoot.Resources.Labels") || !exception.Message.Contains("de-DE")) throw;
                    if ({{missingManifest.ToString().ToLowerInvariant()}} && exception.InnerException is not MissingManifestResourceException) throw;
                    Console.WriteLine("Descriptive failure");
                }
                [GenerateResxAccess("Resources/Labels.resx")]
                public static class Texts
                {
                }
                """, projectTargets: $"<Target Name=\"SabotageRuntimeResources\" BeforeTargets=\"CoreCompile\">{sabotage}</Target>");
            consumer.Write("Resources/Labels.resx", ReferenceResource);
            using (var writer = new System.Resources.ResourceWriter(Path.Combine(consumer.DirectoryPath, "Replacement.resources")))
            {
                writer.AddResource("Other", "Other text");
            }

            var build = await consumer.Build();
            Assert.True(build.ExitCode == 0, build.Output);
            var invocation = await consumer.Invoke();
            Assert.True(invocation.ExitCode == 0, invocation.Output);
            Assert.Equal("Descriptive failure", invocation.Output.Trim());
        }
    }
}
