namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class LocalizedResourceConsumerTests
{
    private readonly ConsumerDiagnosticsFixture diagnostics;

    public LocalizedResourceConsumerTests(ConsumerDiagnosticsFixture diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    private static readonly (string Target, string Culture, string CanonicalCulture)[] UnsupportedCultureCases = new[]
    {
        (Target: "Uppercase", Culture: "ES", CanonicalCulture: "es"),
        (Target: "MixedCase", Culture: "Es-MX", CanonicalCulture: "es-MX"),
        (Target: "MixedRegion", Culture: "es-mX", CanonicalCulture: "es-MX")
    };

    private static readonly (string Target, string Xml, string Detail)[] InconsistentResourceCases = new[]
    {
        (Target: "MissingKey", Xml: RawTextConsumerTests.ReferenceResource.Replace("<data name=\"Plain\"><value>Just text</value></data>", ""), Detail: "Missing: 'Plain'. Additional: (none)."),
        (Target: "AdditionalKey", Xml: RawTextConsumerTests.ReferenceResource.Replace("</root>", "<data name=\"Extra\"><value>Extra</value></data></root>"), Detail: "Missing: (none). Additional: 'Extra'."),
        (Target: "CaseMismatch", Xml: RawTextConsumerTests.ReferenceResource.Replace("name=\"Plain\"", "name=\"plain\""), Detail: "Missing: 'Plain'. Additional: 'plain'."),
        (Target: "OmittedKey", Xml: RawTextConsumerTests.ReferenceResource, Detail: "Missing: 'omitted-key'. Additional: (none).")
    };

    private static readonly (string Target, string Cultures, string Message)[] ExpectedCultureCases = new[]
    {
        (Target: "Required", Cultures: "new[] { \"es\", \"fr\" }", Message: "Expected Culture 'fr' requires an associated Localized Resource for 'Resources/Required.resx'."),
        (Target: "Unknown", Cultures: "new[] { \"not-a-culture\" }", Message: "ExpectedCultures contains invalid Resource Culture 'not-a-culture'. Specify a non-empty culture name."),
        (Target: "Empty", Cultures: "new[] { \"\" }", Message: "ExpectedCultures contains invalid Resource Culture ''. Specify a non-empty culture name."),
        (Target: "Whitespace", Cultures: "new[] { \" \" }", Message: "ExpectedCultures contains invalid Resource Culture ' '. Specify a non-empty culture name."),
        (Target: "NullEntry", Cultures: "new string[] { null! }", Message: "ExpectedCultures contains invalid Resource Culture '(null)'. Specify a non-empty culture name.")
    };

    [Fact]
    public async Task CanInvokeSatelliteResourcesWithDefaultAndExplicitCulture()
    {
        var invocation = await ConsumerProject.Invoke(typeof(Customer.Api.AssociatedTexts).Assembly.Location, "localized");

        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal("Mexicano\nEspañol\nFrançais\nReference\nReference\n  Raw {name@string}, {0:N2}!  \n[]\n[   ]\n[]\n[  ]\n", invocation.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task RejectsUnsupportedLocalizedResourceCultureCasing()
    {
        var build = await diagnostics.BuildDiagnostics();

        Assert.NotEqual(0, build.ExitCode);
        foreach (var (target, culture, canonicalCulture) in UnsupportedCultureCases)
        {
            Assert.True(build.Output.Split('\n').Any(line => line.Contains($"{target}.cs(", StringComparison.Ordinal)
                && line.Contains($"error TRESX004: Invalid Localized Resource: 'Resources/{target}.{culture}.resx' must use the canonical Resource Culture suffix '{canonicalCulture}' or its lowercase form for runtime satellite probing.", StringComparison.Ordinal)), build.Output);
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task CanInvokeCanonicalAndLowercaseLocalizedResourceCultures()
    {
        var invocation = await ConsumerProject.Invoke(typeof(Customer.Api.AssociatedTexts).Assembly.Location, "culture-casing");
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal("Canonical\nLowercase\n", invocation.Output.Replace("\r\n", "\n"));
    }

    [Fact]
    public async Task RejectsInconsistentLocalizedResourcesOutsideExpectedCultures()
    {
        var build = await diagnostics.BuildDiagnostics();

        Assert.NotEqual(0, build.ExitCode);
        foreach (var (target, _, detail) in InconsistentResourceCases)
        {
            Assert.True(build.Output.Split('\n').Any(line => line.Contains($"{target}.cs(", StringComparison.Ordinal)
                && line.Contains($"error TRESX004: Invalid Localized Resource: 'Resources/{target}.fr.resx'", StringComparison.Ordinal)
                && line.Contains(detail, StringComparison.Ordinal)), build.Output);
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task ReportsMissingAndInvalidExpectedCultures()
    {
        var build = await diagnostics.BuildDiagnostics();

        Assert.NotEqual(0, build.ExitCode);
        foreach (var (target, _, message) in ExpectedCultureCases)
        {
            Assert.Contains(build.Output.Split('\n'), line => line.Contains($"{target}.cs(", StringComparison.Ordinal) && line.Contains($"error TRESX005: Invalid ExpectedCultures: {message}", StringComparison.Ordinal));
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task RejectsLocalizedResourcesWithoutStandardSatelliteEmbedding()
    {
        var build = await diagnostics.BuildDiagnostics();

        Assert.NotEqual(0, build.ExitCode);
        foreach (var target in new[] { "Excluded", "CustomName", "Neutral" })
        {
            Assert.True(build.Output.Split('\n').Any(line => line.Contains($"{target}.cs(", StringComparison.Ordinal)
                && line.Contains($"error TRESX004: Invalid Localized Resource: 'Resources/{target}.es.resx' must use standard SDK satellite embedding for this Resource Set.", StringComparison.Ordinal)), build.Output);
        }
    }

    internal static void WriteUnsupportedCultures(ConsumerProject consumer)
    {
        foreach (var (target, culture, _) in UnsupportedCultureCases)
        {
            consumer.Write($"{target}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/{{target}}.resx")]
                public static class {{target}}
                {
                }
                """);
            consumer.Write($"Resources/{target}.resx", RawTextConsumerTests.ReferenceResource);
            consumer.Write($"Resources/{target}.{culture}.resx", RawTextConsumerTests.ReferenceResource);
        }
    }

    internal static void WriteInconsistentResources(ConsumerProject consumer)
    {
        foreach (var (target, xml, _) in InconsistentResourceCases)
        {
            consumer.Write($"{target}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/{{target}}.resx", ExpectedCultures = new[] { "es" })]
                public static class {{target}}
                {
                }
                """);
            var reference = target == "OmittedKey"
                ? RawTextConsumerTests.ReferenceResource.Replace("</root>", "<data name=\"omitted-key\"><value>Reference</value></data></root>")
                : RawTextConsumerTests.ReferenceResource;
            consumer.Write($"Resources/{target}.resx", reference);
            consumer.Write($"Resources/{target}.es.resx", reference);
            consumer.Write($"Resources/{target}.fr.resx", xml);
        }
    }

    internal static void WriteExpectedCultures(ConsumerProject consumer)
    {
        foreach (var (target, cultures, _) in ExpectedCultureCases)
        {
            consumer.Write($"{target}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/{{target}}.resx", ExpectedCultures = {{cultures}})]
                public static class {{target}}
                {
                }
                """);
            consumer.Write($"Resources/{target}.resx", RawTextConsumerTests.ReferenceResource);
        }
        consumer.Write("Resources/Required.es.resx", RawTextConsumerTests.ReferenceResource);
        consumer.Write("Other/Required.fr.resx", RawTextConsumerTests.ReferenceResource);
        consumer.Write("Resources/Unrelated.fr.resx", RawTextConsumerTests.ReferenceResource);
    }

    internal static void WriteInvalidSatelliteEmbedding(ConsumerProject consumer)
    {
        foreach (var target in new[] { "Excluded", "CustomName", "Neutral" })
        {
            consumer.Write($"{target}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/{{target}}.resx", ExpectedCultures = new[] { "es" })]
                public static class {{target}}
                {
                }
                """);
            consumer.Write($"Resources/{target}.resx", RawTextConsumerTests.ReferenceResource);
            consumer.Write($"Resources/{target}.es.resx", RawTextConsumerTests.ReferenceResource);
        }
    }
}
