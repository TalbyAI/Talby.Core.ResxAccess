using System.Globalization;
using System.Reflection;
using Customer.Api;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class IndexedPlaceholderConsumerTests
{
    [Fact]
    public void GeneratesRequiredNullableArgumentsInNumericOrderWithIndexGaps()
    {
        var method = typeof(IndexedTexts).GetMethod("FormatSummary", [typeof(object), typeof(object)]);
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(new[] { "arg0", "arg2" }, parameters.Select(parameter => parameter.Name));
        var nullability = new NullabilityInfoContext();
        Assert.All(parameters, parameter =>
        {
            Assert.False(parameter.IsOptional);
            Assert.Equal(NullabilityState.Nullable, nullability.Create(parameter).ReadState);
        });
        Assert.Equal("second / first / second", IndexedTexts.FormatSummary("first", "second", CultureInfo.InvariantCulture));
        Assert.Equal("{2} / {0} / {2}", IndexedTexts.Summary(CultureInfo.InvariantCulture));
        Assert.DoesNotContain(typeof(IndexedTexts).GetMethods(), candidate => candidate.Name == "FormatPlain");
    }

    [Fact]
    public void SelectsResourceAndFormattingCulturesIndependentlyForAllOverloads()
    {
        var resourceCulture = CultureInfo.CurrentUICulture;
        var formattingCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("MX 12,50", IndexedTexts.FormatAmount(12.5m));
            Assert.Equal("ES 12,5", IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("es-AR")));
            Assert.Equal("FR 12,500", IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("fr-CA")));
            Assert.Equal("REF 12,50", IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal("ES 12.5", IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("es"), CultureInfo.InvariantCulture));
            Assert.Equal("REF 12.50", IndexedTexts.FormatAmount(12.5m, CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("en-US")));
            Assert.Equal("first + second + first", IndexedTexts.FormatSummary("first", "second", CultureInfo.GetCultureInfo("es")));

            var overloads = typeof(IndexedTexts).GetMethods().Where(method => method.Name == "FormatAmount").ToArray();
            Assert.Equal(new[] { 1, 2, 3 }, overloads.Select(method => method.GetParameters().Length).Order());
            Assert.Equal(new[] { "arg0", "resourceCulture", "formattingCulture" }, overloads.Single(method => method.GetParameters().Length == 3).GetParameters().Select(parameter => parameter.Name));
        }
        finally
        {
            CultureInfo.CurrentUICulture = resourceCulture;
            CultureInfo.CurrentCulture = formattingCulture;
        }
    }

    [Fact]
    public void FormatsAlignmentEscapedBracesAndNullArgumentsWhilePreservingRawText()
    {
        Assert.Equal("{  12.5} [     ]", IndexedTexts.FormatLayout(12.5m, null, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture));
        Assert.Equal("{ value} [tail ]", IndexedTexts.FormatLayout("value", "tail", CultureInfo.InvariantCulture, CultureInfo.InvariantCulture));
        Assert.Equal("{{{0,6:N1}}} [{2,-5}]", IndexedTexts.Layout(CultureInfo.InvariantCulture));
        Assert.Equal("{{0}} and {{text}}", IndexedTexts.Literal());
        Assert.DoesNotContain(typeof(IndexedTexts).GetMethods(), method => method.Name == "FormatLiteral");
        Assert.Equal("", IndexedTexts.Empty());
        Assert.Equal("   ", IndexedTexts.Blank());
        Assert.Equal(" /  / ", IndexedTexts.FormatSummary(null, null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void PropagatesStandardFormattingFailuresAndRejectsNullCultures()
    {
        Assert.Throws<FormatException>(() => IndexedTexts.FormatBadFormat(42, CultureInfo.InvariantCulture, CultureInfo.InvariantCulture));
        Assert.Equal("", IndexedTexts.FormatBadFormat(null, CultureInfo.InvariantCulture));
        Assert.Throws<ArgumentNullException>(() => IndexedTexts.FormatSummary("one", "two", null!));
        Assert.Throws<ArgumentNullException>(() => IndexedTexts.FormatSummary("one", "two", CultureInfo.InvariantCulture, null!));
    }

    [Fact]
    public async Task RejectsMalformedIndexedPlaceholdersWithResourceAndKeyDiagnostics()
    {
        using var consumer = new ConsumerProject("Console.WriteLine(\"Unused\");");
        var invalidText = new[] { "{0", "text }", "{}", "{-1}", "{ 0}", "{0,+5}", "{0,}", "{0:{}}", "{0x}", "{2147483648}", "{0,999999999999999}", "{0\t}", "{0, 1\t}" };
        for (var index = 0; index < invalidText.Length; index++)
        {
            consumer.Write($"Case{index}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/Case{{index}}.resx")]
                public static class Case{{index}}
                {
                }
                """);
            consumer.Write($"Resources/Case{index}.resx", ResourceXml("Bad", invalidText[index]));
        }

        var build = await consumer.Build();
        Assert.NotEqual(0, build.ExitCode);
        for (var index = 0; index < invalidText.Length; index++)
        {
            Assert.Contains($"error TRESX001: Invalid Reference Resource: 'Resources/Case{index}.resx' Resource Key 'Bad' has a malformed Formatting Placeholder", build.Output);
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task RejectsChangedPlaceholderContractsInEveryLocalizedResourceAndOmittedKey()
    {
        using var consumer = new ConsumerProject("Console.WriteLine(\"Unused\");");
        var cases = new[]
        {
            (Reference: "{0} {2}", Localized: "{0}"),
            (Reference: "{0} {2}", Localized: "{0} {1} {2}"),
            (Reference: "{0}", Localized: ""),
            (Reference: "{0}", Localized: "   "),
            (Reference: "No arguments", Localized: "{0}"),
            (Reference: "{0}", Localized: "{name}"),
            (Reference: "{0}", Localized: "{0"),
            (Reference: "{{0}}", Localized: "{0}")
        };
        for (var index = 0; index < cases.Length; index++)
        {
            consumer.Write($"Case{index}.cs", $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/Case{{index}}.resx", ExpectedCultures = new[] { "es" })]
                public static class Case{{index}}
                {
                }
                """);
            consumer.Write($"Resources/Case{index}.resx", ResourceXml("omitted-key", cases[index].Reference));
            consumer.Write($"Resources/Case{index}.es.resx", ResourceXml("omitted-key", cases[index].Reference));
            consumer.Write($"Resources/Case{index}.fr.resx", ResourceXml("omitted-key", cases[index].Localized));
        }

        var build = await consumer.Build();
        Assert.NotEqual(0, build.ExitCode);
        for (var index = 0; index < cases.Length; index++)
        {
            Assert.Contains($"error TRESX004: Invalid Localized Resource: 'Resources/Case{index}.fr.resx' Resource Key 'omitted-key'", build.Output);
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    private static string ResourceXml(string key, string text)
        => new System.Xml.Linq.XElement("root", new System.Xml.Linq.XElement("data", new System.Xml.Linq.XAttribute("name", key), new System.Xml.Linq.XElement("value", text))).ToString();
}
