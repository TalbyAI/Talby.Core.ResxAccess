using System.Globalization;
using System.Reflection;
using Customer.Api;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class IndexedPlaceholderConsumerTests
{
    private readonly ConsumerDiagnosticsFixture diagnostics;

    public IndexedPlaceholderConsumerTests(ConsumerDiagnosticsFixture diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    private static readonly string[] InvalidPlaceholderText = new[]
    {
        "{0",
        "text }",
        "{}",
        "{-1}",
        "{ 0}",
        "{0,+5}",
        "{0,}",
        "{0:{}}",
        "{0x}",
        "{2147483648}",
        "{0,999999999999999}",
        "{0\t}",
        "{0, 1\t}",
    };

    private static readonly (string Reference, string Localized)[] ChangedPlaceholderCases = new[]
    {
        (Reference: "{0} {2}", Localized: "{0}"),
        (Reference: "{0} {2}", Localized: "{0} {1} {2}"),
        (Reference: "{0}", Localized: ""),
        (Reference: "{0}", Localized: "   "),
        (Reference: "No arguments", Localized: "{0}"),
        (Reference: "{0}", Localized: "{name}"),
        (Reference: "{0}", Localized: "{0"),
        (Reference: "{{0}}", Localized: "{0}"),
    };

    [Fact]
    public void GeneratesRequiredNullableArgumentsInNumericOrderWithIndexGaps()
    {
        var method = typeof(IndexedTexts).GetMethod(
            "FormatSummary",
            [typeof(object), typeof(object)]
        );
        Assert.NotNull(method);
        var parameters = method.GetParameters();
        Assert.Equal(new[] { "arg0", "arg2" }, parameters.Select(parameter => parameter.Name));
        var nullability = new NullabilityInfoContext();
        Assert.All(
            parameters,
            parameter =>
            {
                Assert.False(parameter.IsOptional);
                Assert.Equal(NullabilityState.Nullable, nullability.Create(parameter).ReadState);
            }
        );
        Assert.Equal(
            "second / first / second",
            IndexedTexts.FormatSummary("first", "second", CultureInfo.InvariantCulture)
        );
        Assert.Equal("{2} / {0} / {2}", IndexedTexts.Summary(CultureInfo.InvariantCulture));
        Assert.DoesNotContain(
            typeof(IndexedTexts).GetMethods(),
            candidate => candidate.Name == "FormatPlain"
        );
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
            Assert.Equal(
                "ES 12,5",
                IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("es-AR"))
            );
            Assert.Equal(
                "FR 12,500",
                IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("fr-CA"))
            );
            Assert.Equal(
                "REF 12,50",
                IndexedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("de-DE"))
            );
            Assert.Equal(
                "ES 12.5",
                IndexedTexts.FormatAmount(
                    12.5m,
                    CultureInfo.GetCultureInfo("es"),
                    CultureInfo.InvariantCulture
                )
            );
            Assert.Equal(
                "REF 12.50",
                IndexedTexts.FormatAmount(
                    12.5m,
                    CultureInfo.InvariantCulture,
                    CultureInfo.GetCultureInfo("en-US")
                )
            );
            Assert.Equal(
                "first + second + first",
                IndexedTexts.FormatSummary("first", "second", CultureInfo.GetCultureInfo("es"))
            );

            var overloads = typeof(IndexedTexts)
                .GetMethods()
                .Where(method => method.Name == "FormatAmount")
                .ToArray();
            Assert.Equal(
                new[] { 1, 2, 3 },
                overloads.Select(method => method.GetParameters().Length).Order()
            );
            Assert.Equal(
                new[] { "arg0", "resourceCulture", "formattingCulture" },
                overloads
                    .Single(method => method.GetParameters().Length == 3)
                    .GetParameters()
                    .Select(parameter => parameter.Name)
            );
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
        Assert.Equal(
            "{  12.5} [     ]",
            IndexedTexts.FormatLayout(
                12.5m,
                null,
                CultureInfo.InvariantCulture,
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal(
            "{ value} [tail ]",
            IndexedTexts.FormatLayout(
                "value",
                "tail",
                CultureInfo.InvariantCulture,
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal("{{{0,6:N1}}} [{2,-5}]", IndexedTexts.Layout(CultureInfo.InvariantCulture));
        Assert.Equal("{{0}} and {{text}}", IndexedTexts.Literal());
        Assert.DoesNotContain(
            typeof(IndexedTexts).GetMethods(),
            method => method.Name == "FormatLiteral"
        );
        Assert.Equal("", IndexedTexts.Empty());
        Assert.Equal("   ", IndexedTexts.Blank());
        Assert.Equal(
            " /  / ",
            IndexedTexts.FormatSummary(null, null, CultureInfo.InvariantCulture)
        );
    }

    [Fact]
    public void PropagatesStandardFormattingFailuresAndRejectsNullCultures()
    {
        Assert.Throws<FormatException>(() =>
            IndexedTexts.FormatBadFormat(
                42,
                CultureInfo.InvariantCulture,
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal("", IndexedTexts.FormatBadFormat(null, CultureInfo.InvariantCulture));
        Assert.Throws<ArgumentNullException>(() => IndexedTexts.FormatSummary("one", "two", null!));
        Assert.Throws<ArgumentNullException>(() =>
            IndexedTexts.FormatSummary("one", "two", CultureInfo.InvariantCulture, null!)
        );
    }

    [Fact]
    public async Task RejectsMalformedIndexedPlaceholdersWithResourceAndKeyDiagnostics()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        for (var index = 0; index < InvalidPlaceholderText.Length; index++)
        {
            Assert.True(
                build
                    .Output.Split('\n')
                    .Any(line =>
                        line.Contains($"Malformed{index}.cs(", StringComparison.Ordinal)
                        && line.Contains(
                            $"error TRESX001: Invalid Reference Resource: 'Resources/Malformed{index}.resx' Resource Key 'Bad' has a malformed Formatting Placeholder",
                            StringComparison.Ordinal
                        )
                    ),
                build.Output
            );
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task RejectsChangedPlaceholderContractsInEveryLocalizedResourceAndOmittedKey()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        for (var index = 0; index < ChangedPlaceholderCases.Length; index++)
        {
            Assert.True(
                build
                    .Output.Split('\n')
                    .Any(line =>
                        line.Contains($"Changed{index}.cs(", StringComparison.Ordinal)
                        && line.Contains(
                            $"error TRESX004: Invalid Localized Resource: 'Resources/Changed{index}.fr.resx' Resource Key 'omitted-key'",
                            StringComparison.Ordinal
                        )
                    ),
                build.Output
            );
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    private static string ResourceXml(string key, string text) =>
        new System.Xml.Linq.XElement(
            "root",
            new System.Xml.Linq.XElement(
                "data",
                new System.Xml.Linq.XAttribute("name", key),
                new System.Xml.Linq.XElement("value", text)
            )
        ).ToString();

    internal static void WriteMalformedPlaceholders(ConsumerProject consumer)
    {
        for (var index = 0; index < InvalidPlaceholderText.Length; index++)
        {
            consumer.Write(
                $"Malformed{index}.cs",
                $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/Malformed{{index}}.resx")]
                public static class Malformed{{index}}
                {
                }
                """
            );
            consumer.Write(
                $"Resources/Malformed{index}.resx",
                ResourceXml("Bad", InvalidPlaceholderText[index])
            );
        }
    }

    internal static void WriteChangedPlaceholderContracts(ConsumerProject consumer)
    {
        for (var index = 0; index < ChangedPlaceholderCases.Length; index++)
        {
            consumer.Write(
                $"Changed{index}.cs",
                $$"""
                using Talby.Core.ResxAccess;
                [GenerateResxAccess("Resources/Changed{{index}}.resx", ExpectedCultures = new[] { "es" })]
                public static class Changed{{index}}
                {
                }
                """
            );
            consumer.Write(
                $"Resources/Changed{index}.resx",
                ResourceXml("omitted-key", ChangedPlaceholderCases[index].Reference)
            );
            consumer.Write(
                $"Resources/Changed{index}.es.resx",
                ResourceXml("omitted-key", ChangedPlaceholderCases[index].Reference)
            );
            consumer.Write(
                $"Resources/Changed{index}.fr.resx",
                ResourceXml("omitted-key", ChangedPlaceholderCases[index].Localized)
            );
        }
    }
}
