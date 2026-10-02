namespace Talby.Core.ResxAccess.Tests;

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
    public async Task CanCompileAndInvokeRawTextOnNonPartialStaticClass()
    {
        using var consumer = new ConsumerProject("""
            using System.Globalization;
            using Talby.Core.ResxAccess;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            Console.WriteLine(Customer.Api.Texts.Welcome());
            Console.WriteLine(Customer.Api.Texts.Welcome(CultureInfo.InvariantCulture));
            Console.WriteLine(Customer.Api.Texts.Plain());
            var type = typeof(Customer.Api.Texts);
            if (type.IsPublic || type.FullName != "Customer.Api.Texts") throw new Exception("Class identity changed.");
            var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.DeclaredOnly);
            if (methods.Length != 4 || methods.Any(m => m.Name.StartsWith("Format"))) throw new Exception("Unexpected API.");

            namespace Customer.Api
            {
                [GenerateResxAccess("Resources/Labels.resx")]
                internal static class Texts
                {
                }
            }
            """);
        consumer.Write("Resources/Labels.resx", ReferenceResource.Replace("</root>", "<data name=\"invalid-key\"><value>Not representable</value></data></root>"));

        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        var invocation = await consumer.Invoke();
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal("  Hello {name@string}, {0:N2}!  \n  Hello {name@string}, {0:N2}!  \nJust text\n", invocation.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task UsesSdkAssociatedTypeNameAndIndependentResourceCulture()
    {
        using var consumer = new ConsumerProject("""
            using System.Globalization;
            using Talby.Core.ResxAccess;

            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
            Console.WriteLine(Customer.Api.Texts.Plain());
            Console.WriteLine(Customer.Api.Texts.Plain(CultureInfo.GetCultureInfo("fr-CA")));
            Console.WriteLine(Customer.Api.Texts.Plain(CultureInfo.GetCultureInfo("de-DE")));
            Console.WriteLine(Customer.Api.Texts.Welcome(CultureInfo.InvariantCulture));
            if (!typeof(Customer.Api.Texts).IsPublic) throw new Exception("Accessibility changed.");
            try { Customer.Api.Texts.Plain(null!); throw new Exception("Null culture accepted."); }
            catch (ArgumentNullException) { }

            namespace Customer.Api
            {
                [GenerateResxAccess("Resources/Labels.resx")]
                public static class Texts
                {
                }
            }
            """);
        consumer.Write("Resources/Labels.cs", """
            namespace Unrelated.Namespace;
            public class ResourceAnchor
            {
            }
            """);
        consumer.Write("Resources/Labels.resx", ReferenceResource);
        consumer.Write("Resources/Labels.es.resx", ReferenceResource.Replace("Just text", "Texto"));
        consumer.Write("Resources/Labels.fr.resx", ReferenceResource.Replace("Just text", "Texte"));

        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        var invocation = await consumer.Invoke();
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal("Texto\nTexte\nJust text\n  Hello {name@string}, {0:N2}!  \n", invocation.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task ReportsUnsupportedEmbeddingMetadata()
    {
        foreach (var metadata in new[]
        {
            "<LogicalName>Custom.Labels.resources</LogicalName>",
            "<ManifestResourceName>Custom.Labels</ManifestResourceName>",
            "<Link>Other/Labels.resx</Link>"
        })
        {
            using var consumer = new ConsumerProject("""
                using Talby.Core.ResxAccess;
                Console.WriteLine("Unused");
                [GenerateResxAccess("Resources/Labels.resx")]
                public static class Texts
                {
                }
                """, $"<EmbeddedResource Update=\"Resources/Labels.resx\">{metadata}</EmbeddedResource>");
            consumer.Write("Resources/Labels.resx", ReferenceResource);

            var build = await consumer.Build();
            Assert.NotEqual(0, build.ExitCode);
            Assert.Contains("TRESX003", build.Output);
        }
    }

    [Fact]
    public async Task ReportsInvalidReferenceInputsAndTargetsInRealBuilds()
    {
        foreach (var (attributeArgument, target, resourceFile, diagnostic) in new[]
        {
            ("null!", "public static class Texts", "Resources/Labels.resx", "TRESX001"),
            ("\" \"", "public static class Texts", "Resources/Labels.resx", "TRESX001"),
            ("\"Labels.txt\"", "public static class Texts", "Resources/Labels.resx", "TRESX001"),
            ("\"Resources/Missing.resx\"", "public static class Texts", "Resources/Labels.resx", "TRESX001"),
            ("\"Resources/Labels.es.resx\"", "public static class Texts", "Resources/Labels.es.resx", "TRESX001"),
            ("\"Resources/Labels.resx\"", "public class Texts", "Resources/Labels.resx", "TRESX002"),
            ("\"Resources/Labels.resx\"", "public static class Texts<T>", "Resources/Labels.resx", "TRESX002")
        })
        {
            using var consumer = new ConsumerProject($$"""
                using Talby.Core.ResxAccess;
                Console.WriteLine("Unused");
                [GenerateResxAccess({{attributeArgument}})]
                {{target}}
                {
                }
                """);
            consumer.Write(resourceFile, ReferenceResource);

            var build = await consumer.Build();
            Assert.NotEqual(0, build.ExitCode);
            Assert.Contains(diagnostic, build.Output);
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

    [Fact]
    public async Task CultureNamedNeutralFilePreservesRepresentableKeysAndBlankText()
    {
        using var consumer = new ConsumerProject("""
            using Talby.Core.ResxAccess;
            if (Texts.@class() != "Keyword" || Texts.Café() != "Unicode" || Texts.Empty() != "" || Texts.Blank() != "   ") throw new Exception("Raw Text changed.");
            Console.WriteLine("Preserved");
            [GenerateResxAccess("Resources/en.resx")]
            public static class Texts
            {
            }
            """);
        consumer.Write("Resources/en.resx", ReferenceResource.Replace("</root>", """
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
        Assert.Equal("Preserved", invocation.Output.Trim());
    }
}
