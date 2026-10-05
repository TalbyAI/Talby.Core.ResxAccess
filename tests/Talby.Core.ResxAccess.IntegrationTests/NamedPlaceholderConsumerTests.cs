using System.Globalization;
using System.Reflection;
using Customer.Api;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class NamedPlaceholderConsumerTests
{
    private readonly ConsumerDiagnosticsFixture diagnostics;

    public NamedPlaceholderConsumerTests(ConsumerDiagnosticsFixture diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    private static readonly (string Text, string Message)[] InvalidReferences =
    [
        ("{bad-name}", "Invalid Named Placeholder identifier 'bad-name'"),
        ("{name.part}", "Invalid Named Placeholder identifier 'name.part'"),
        ("{@name}", "Invalid Named Placeholder identifier ''"),
        ("{name\n}", "Invalid Named Placeholder identifier"),
        ("{name@object}", "Unsupported Argument Type 'object'"),
        ("{name@float}", "Unsupported Argument Type 'float'"),
        ("{name@Int32}", "Unsupported Argument Type 'Int32'"),
        ("{name@System.Int32}", "Unsupported Argument Type 'System.Int32'"),
        ("{name@datetime}", "Unsupported Argument Type 'datetime'"),
        ("{name@int??}", "Unsupported Argument Type 'int??'"),
        ("{name@}", "Unsupported Argument Type ''"),
        ("{name@int} {name@long}", "Conflicting Argument Types 'int' and 'long'"),
        ("{name@string} {name@string?}", "Conflicting Argument Types 'string' and 'string?'"),
        ("{name@int?} {name@int}", "Conflicting Argument Types 'int?' and 'int'"),
        ("{arg0} {0}", "collides with generated parameter 'arg0'"),
        ("{arg2} {02}", "collides with generated parameter 'arg2'"),
        ("{resourceCulture}", "collides with generated parameter 'resourceCulture'"),
        ("{formattingCulture@string?}", "collides with generated parameter 'formattingCulture'"),
        ("{name", "Unclosed Formatting Placeholder"),
        ("{name,+5}", "Expected an ASCII digit"),
        ("{name,}", "Expected an ASCII digit"),
        ("{name:{}}", "Nested opening brace"),
        ("{name other}", "Format item ends prematurely"),
        ("{0@int}", "Format item ends prematurely"),
        ("{a\u200Db} {ab}", "collides with generated parameter 'ab'"),
        ("{resource\u200DCulture}", "collides with generated parameter 'resource\\u200DCulture'"),
    ];

    private static readonly (
        string Reference,
        string Localized,
        string Message
    )[] InvalidTranslations =
    [
        ("{name@int}", "{name@long}", "the Reference Resource requires 'int'"),
        ("{name@int?}", "{name@int}", "the Reference Resource requires 'int?'"),
        ("{name@string}", "{name@string?}", "the Reference Resource requires 'string'"),
        ("{name@string?}", "{name@string}", "the Reference Resource requires 'string?'"),
        ("{name}", "{name@string}", "the Reference Resource requires 'object?'"),
        ("{name@string?}", "", "must use exactly the Reference Resource's Placeholder Contract"),
        (
            "{name} {2} {0}",
            "{name} {0}",
            "must use exactly the Reference Resource's Placeholder Contract"
        ),
        (
            "{name} {2} {0}",
            "{name} {0} {1} {2}",
            "must use exactly the Reference Resource's Placeholder Contract"
        ),
        (
            "{name} {2} {0}",
            "{0} {2}",
            "must use exactly the Reference Resource's Placeholder Contract"
        ),
        (
            "{name}",
            "{name} {extra}",
            "must use exactly the Reference Resource's Placeholder Contract"
        ),
        ("{name}", "{Name}", "must use exactly the Reference Resource's Placeholder Contract"),
        ("plain", "{name}", "must use exactly the Reference Resource's Placeholder Contract"),
        ("{name}", "{name@float}", "Unsupported Argument Type 'float'"),
        ("{name}", "{name", "Unclosed Formatting Placeholder"),
        ("{name@int}", "{name@int} {name@int?}", "Conflicting Argument Types 'int' and 'int?'"),
    ];

    [Fact]
    public void GeneratesNamedArgumentsBeforeOnlyUsedIndexedIdentities()
    {
        var method = typeof(NamedTexts).GetMethod(
            "FormatMixed",
            [typeof(string), typeof(object), typeof(object)]
        );
        Assert.NotNull(method);
        Assert.Equal(
            new[] { "name", "arg0", "arg2" },
            method.GetParameters().Select(parameter => parameter.Name)
        );
        Assert.All(method.GetParameters(), parameter => Assert.False(parameter.IsOptional));
        Assert.Equal(
            NullabilityState.NotNull,
            new NullabilityInfoContext().Create(method.GetParameters()[0]).ReadState
        );
        Assert.Equal(
            "Ada second first Ada",
            NamedTexts.FormatMixed("Ada", "first", "second", CultureInfo.InvariantCulture)
        );
        Assert.Equal(
            "first Ada second Ada",
            NamedTexts.FormatMixed("Ada", "first", "second", CultureInfo.GetCultureInfo("es"))
        );
        Assert.Equal(
            "{name@string} {2} {0} {name}",
            NamedTexts.Mixed(CultureInfo.InvariantCulture)
        );
        Assert.Equal(
            "{0} {name} {2} {name@string}",
            NamedTexts.Mixed(CultureInfo.GetCultureInfo("es"))
        );
        Assert.Equal(
            " second first ",
            NamedTexts.FormatMixed(null!, "first", "second", CultureInfo.InvariantCulture)
        );
    }

    [Fact]
    public void PreservesEverySupportedArgumentTypeAndNullableRequiredSignature()
    {
        Type[] types =
        [
            typeof(string),
            typeof(bool),
            typeof(int),
            typeof(long),
            typeof(double),
            typeof(decimal),
            typeof(DateTime),
            typeof(DateTimeOffset),
            typeof(Guid),
        ];
        foreach (var nullable in new[] { false, true })
        {
            var method = typeof(NamedTexts)
                .GetMethods()
                .Single(method =>
                    method.Name == (nullable ? "FormatNullableTypes" : "FormatTypes")
                    && method.GetParameters().Length == 9
                );
            var parameters = method.GetParameters();
            Assert.Equal(
                new[] { "text", "flag", "count", "big", "ratio", "amount", "when", "offset", "id" },
                parameters.Select(parameter => parameter.Name)
            );
            for (var index = 0; index < types.Length; index++)
            {
                Assert.Equal(
                    nullable && types[index].IsValueType
                        ? typeof(Nullable<>).MakeGenericType(types[index])
                        : types[index],
                    parameters[index].ParameterType
                );
                Assert.False(parameters[index].IsOptional);
                Assert.Equal(
                    nullable ? NullabilityState.Nullable : NullabilityState.NotNull,
                    new NullabilityInfoContext().Create(parameters[index]).ReadState
                );
            }
        }

        var date = new DateTime(2026, 10, 5);
        var offset = new DateTimeOffset(2026, 10, 5, 12, 0, 0, TimeSpan.FromHours(2));
        var id = Guid.Parse("12345678-1234-1234-1234-123456789abc");
        Assert.Equal(
            "text|True|3|4|1.5|12.50|2026-10-05|+02:00|12345678-1234-1234-1234-123456789abc",
            NamedTexts.FormatTypes(
                "text",
                true,
                3,
                4L,
                1.5,
                12.5m,
                date,
                offset,
                id,
                CultureInfo.InvariantCulture,
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal(
            "12345678-1234-1234-1234-123456789abc|+02:00|2026-10-05|12.5|1.50|4|3|True|text",
            NamedTexts.FormatNullableTypes(
                "text",
                true,
                3,
                4L,
                1.5,
                12.5m,
                date,
                offset,
                id,
                CultureInfo.GetCultureInfo("es"),
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal(
            "||||||||",
            NamedTexts.FormatNullableTypes(
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                CultureInfo.GetCultureInfo("es")
            )
        );
        Assert.Equal("7 7  7", NamedTexts.FormatRepeated(7, null, CultureInfo.InvariantCulture));
    }

    [Fact]
    public void FormatsUntypedNullableKeywordAndUnicodeArgumentsWithStandardSemantics()
    {
        Assert.Equal(
            "    x [    ] {  12.5} {literal}",
            NamedTexts.FormatLayout(
                "x",
                null,
                12.5m,
                CultureInfo.InvariantCulture,
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal(
            "      [Ada ] {      } {literal}",
            NamedTexts.FormatLayout(
                null,
                "Ada",
                null,
                CultureInfo.GetCultureInfo("es"),
                CultureInfo.InvariantCulture
            )
        );
        Assert.Equal(
            "{unknown,5} [{name@string?,-4}] {{{amount@decimal?,6:N1}}} {{literal}}",
            NamedTexts.Layout(CultureInfo.InvariantCulture)
        );
        Assert.Equal(
            "3 café id",
            NamedTexts.FormatKeywords(
                @class: 3,
                Café: "café",
                _id: "id",
                resourceCulture: CultureInfo.InvariantCulture
            )
        );
        Assert.Equal(
            "id café 3",
            NamedTexts.FormatKeywords(3, "café", "id", CultureInfo.GetCultureInfo("es"))
        );
        Assert.Equal(
            "upper lower",
            NamedTexts.FormatCaseSensitive("lower", "upper", CultureInfo.GetCultureInfo("es"))
        );
        var parameters = typeof(NamedTexts)
            .GetMethod("FormatLayout", [typeof(object), typeof(string), typeof(decimal?)])!
            .GetParameters();
        Assert.Equal(
            NullabilityState.Nullable,
            new NullabilityInfoContext().Create(parameters[0]).ReadState
        );
        Assert.Throws<FormatException>(() =>
            NamedTexts.FormatBadFormat(1, CultureInfo.InvariantCulture)
        );
        Assert.Throws<ArgumentNullException>(() => NamedTexts.FormatAmount(1, null!));
        Assert.Throws<ArgumentNullException>(() =>
            NamedTexts.FormatAmount(1, CultureInfo.InvariantCulture, null!)
        );
    }

    [Fact]
    public void SelectsIndependentCulturesAndReorderedFormatsForEveryNamedOverload()
    {
        var resourceCulture = CultureInfo.CurrentUICulture;
        var formattingCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("es-MX");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            Assert.Equal("ES 12,5 12,5", NamedTexts.FormatAmount(12.5m));
            Assert.Equal(
                "REF 12,50",
                NamedTexts.FormatAmount(12.5m, CultureInfo.GetCultureInfo("de-DE"))
            );
            Assert.Equal(
                "ES 12.5 12.5",
                NamedTexts.FormatAmount(
                    12.5m,
                    CultureInfo.GetCultureInfo("es-AR"),
                    CultureInfo.InvariantCulture
                )
            );
            Assert.Equal(
                new[] { 1, 2, 3 },
                typeof(NamedTexts)
                    .GetMethods()
                    .Where(method => method.Name == "FormatAmount")
                    .Select(method => method.GetParameters().Length)
                    .Order()
            );
        }
        finally
        {
            CultureInfo.CurrentUICulture = resourceCulture;
            CultureInfo.CurrentCulture = formattingCulture;
        }
    }

    [Fact]
    public async Task RejectsInvalidNamedDeclarationsAndParameterCollisionsPrecisely()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        for (var index = 0; index < InvalidReferences.Length; index++)
        {
            AssertDiagnostic(
                build.Output,
                $"NamedInvalid{index}",
                "TRESX001",
                $"Resources/NamedInvalid{index}.resx",
                InvalidReferences[index].Message
            );
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task RejectsChangedNamedAndMixedContractsIncludingNullabilityAndOmittedKeys()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        for (var index = 0; index < InvalidTranslations.Length; index++)
        {
            AssertDiagnostic(
                build.Output,
                $"NamedChanged{index}",
                "TRESX004",
                $"Resources/NamedChanged{index}.fr.resx",
                InvalidTranslations[index].Message
            );
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    [Fact]
    public async Task ConsumerCompilerEnforcesNamedTypesNullabilityAndRequiredArguments()
    {
        using var consumer = new ConsumerProject(
            """
            using System.Globalization;
            using Customer.Api;
            Console.WriteLine("Unused");
            """,
            projectItems: $"""
            <Reference Include="ConsumerFixture"><HintPath>{System.Security.SecurityElement.Escape(
                typeof(NamedTexts).Assembly.Location
            )}</HintPath></Reference>
            """,
            projectProperties: "<WarningsAsErrors>nullable</WarningsAsErrors>"
        );
        consumer.Write(
            "WrongType.cs",
            "using Customer.Api; public class WrongType { public string Invoke() => NamedTexts.FormatAmount(\"wrong\"); }"
        );
        consumer.Write(
            "NullReference.cs",
            "using Customer.Api; public class NullReference { public string Invoke() => NamedTexts.FormatMixed(null, 0, 2); }"
        );
        consumer.Write(
            "RequiredNullable.cs",
            "using Customer.Api; public class RequiredNullable { public string Invoke() => NamedTexts.FormatNullableTypes(); }"
        );
        var build = await consumer.Build();
        Assert.NotEqual(0, build.ExitCode);
        foreach (
            var (file, code) in new[]
            {
                ("WrongType", "CS1503"),
                ("NullReference", "CS8625"),
                ("RequiredNullable", "CS1501"),
            }
        )
        {
            Assert.True(
                build
                    .Output.Split('\n')
                    .Any(line =>
                        line.Contains(file + ".cs(", StringComparison.Ordinal)
                        && line.Contains("error " + code, StringComparison.Ordinal)
                    ),
                build.Output
            );
        }
    }

    private static void AssertDiagnostic(
        string output,
        string target,
        string code,
        string resource,
        string message
    ) =>
        Assert.True(
            output
                .Split('\n')
                .Any(line =>
                    line.Contains(target + ".cs(", StringComparison.Ordinal)
                    && line.Contains("error " + code, StringComparison.Ordinal)
                    && line.Contains(resource, StringComparison.Ordinal)
                    && line.Contains("Resource Key 'omitted-key'", StringComparison.Ordinal)
                    && line.Contains(message, StringComparison.Ordinal)
                ),
            $"Expected {target}: {code}, {resource}, {message}.\n{output}"
        );

    internal static void WriteInvalidNamedPlaceholders(ConsumerProject consumer)
    {
        for (var index = 0; index < InvalidReferences.Length; index++)
        {
            WriteResourceSet(consumer, "NamedInvalid" + index, InvalidReferences[index].Text);
        }
        for (var index = 0; index < InvalidTranslations.Length; index++)
        {
            WriteResourceSet(
                consumer,
                "NamedChanged" + index,
                InvalidTranslations[index].Reference,
                InvalidTranslations[index].Localized
            );
        }
    }

    private static void WriteResourceSet(
        ConsumerProject consumer,
        string target,
        string reference,
        string? localized = null
    )
    {
        consumer.Write(
            target + ".cs",
            $$"""
            using Talby.Core.ResxAccess;
            [GenerateResxAccess("Resources/{{target}}.resx", ExpectedCultures = new[] { "es" })]
            public static class {{target}} { }
            """
        );
        consumer.Write($"Resources/{target}.resx", ResourceXml(reference));
        consumer.Write($"Resources/{target}.es.resx", ResourceXml(reference));
        if (localized is not null)
        {
            consumer.Write($"Resources/{target}.fr.resx", ResourceXml(localized));
        }
    }

    private static string ResourceXml(string text) =>
        new System.Xml.Linq.XElement(
            "root",
            new System.Xml.Linq.XElement(
                "data",
                new System.Xml.Linq.XAttribute("name", "omitted-key"),
                new System.Xml.Linq.XElement("value", text)
            )
        ).ToString();
}
