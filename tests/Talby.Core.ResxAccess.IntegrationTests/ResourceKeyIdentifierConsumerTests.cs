using System.Xml.Linq;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class ResourceKeyIdentifierConsumerTests
{
    private readonly ConsumerDiagnosticsFixture diagnostics;

    public ResourceKeyIdentifierConsumerTests(ConsumerDiagnosticsFixture diagnostics)
    {
        this.diagnostics = diagnostics;
    }

    private static readonly (
        string Target,
        string Key,
        string Text,
        string Member,
        string Declaration
    )[] ExistingCollisions =
    [
        (
            "ExistingRawMethod",
            "Plain",
            "text",
            "Plain",
            "public static string Plain() => \"existing\";"
        ),
        (
            "ExistingOverload",
            "Plain",
            "text",
            "Plain",
            "public static string Plain(int value) => \"existing\";"
        ),
        (
            "ExistingProperty",
            "Plain",
            "text",
            "Plain",
            "public static string Plain => \"existing\";"
        ),
        ("ExistingField", "Plain", "text", "Plain", "public static string Plain = \"existing\";"),
        (
            "ExistingEvent",
            "Plain",
            "text",
            "Plain",
            "public static event Action Plain { add { } remove { } }"
        ),
        ("ExistingNestedType", "Plain", "text", "Plain", "public class Plain { }"),
        (
            "ExistingFormattingMethod",
            "Plain",
            "{0}",
            "FormatPlain",
            "public static string FormatPlain(object value) => \"existing\";"
        ),
        (
            "NormalizedExisting",
            "has-dash",
            "{0}",
            "has_dash",
            "public static string has_dash() => \"existing\";"
        ),
        (
            "NormalizedFormattingExisting",
            "has-dash",
            "{name@string}",
            "Formathas_dash",
            "public static string Formathas_dash(string name) => \"existing\";"
        ),
        ("ClassNameCollision", "ClassNameCollision", "text", "ClassNameCollision", ""),
        (
            "UnicodeExisting",
            "a\u200Db",
            "text",
            "a\u200Db",
            "public static string ab() => \"existing\";"
        ),
    ];

    [Fact]
    public async Task NormalizesReproduciblyAndInvokesOriginalKeysAndEscapedKeywords()
    {
        using var consumer = new ConsumerProject(
            """
            using System.Globalization;
            using Talby.Core.ResxAccess;
            var culture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;
            Console.WriteLine(Normalized.a_b(culture));
            Console.WriteLine(Normalized.a_b_2(culture));
            Console.WriteLine(Normalized.a_b_3(culture));
            Console.WriteLine(Normalized.Formata_b_3("Ada", 7, culture, culture));
            Console.WriteLine(Normalized.a_b_4(culture));
            Console.WriteLine(Normalized.Formata_b_4(8, culture));
            Console.WriteLine(Normalized.a_b_5(culture));
            Console.WriteLine(Normalized.Formata_b_5("Bea", culture, culture));
            Console.WriteLine(Normalized._Text(culture));
            Console.WriteLine(Normalized._Text_2(culture));
            Console.WriteLine(Normalized.two_words(culture));
            Console.WriteLine(Normalized._(culture));
            Console.WriteLine(Normalized.__2(culture));
            Console.WriteLine(Normalized.End_(culture));
            Console.WriteLine(Normalized.@class(culture));
            Console.WriteLine(Normalized.Formatclass(9, culture, culture));
            Console.WriteLine(Normalized.record(culture));
            Console.WriteLine(Normalized.Café(culture));
            Console.WriteLine(Normalized.漢字(culture));
            Console.WriteLine(Normalized.ab_(culture));
            Console.WriteLine(Normalized.ab__2(culture));
            Console.WriteLine(Normalized.a_b_3(CultureInfo.GetCultureInfo("es")));
            Console.WriteLine(Normalized.Formata_b_3("Ada", 7, CultureInfo.GetCultureInfo("es"), culture));
            var names = typeof(Normalized).GetMethods().Select(m => m.ToString()).Order().ToArray();
            var reorderedNames = typeof(Reordered).GetMethods().Select(m => m.ToString()).Order().ToArray();
            if (!names.SequenceEqual(reorderedNames)) throw new Exception("Resource entry order changed the API.");
            foreach (var method in typeof(Normalized).GetMethods().Where(m => m.DeclaringType == typeof(Normalized)))
            {
                var other = typeof(Reordered).GetMethod(method.Name, method.GetParameters().Select(p => p.ParameterType).ToArray())!;
                var arguments = method.GetParameters().Select(p => p.ParameterType == typeof(CultureInfo) ? (object)culture : p.ParameterType == typeof(string) ? "Ada" : p.ParameterType == typeof(int) ? (object)7 : 8).ToArray();
                if (!Equals(method.Invoke(null, arguments), other.Invoke(null, arguments))) throw new Exception("Reordered lookup changed.");
            }
            [GenerateResxAccess("Resources/Names.resx", InvalidKeyHandling = InvalidKeyHandling.Normalize)]
            public static class Normalized { }
            [GenerateResxAccess("Resources/Reordered.resx", InvalidKeyHandling = InvalidKeyHandling.Normalize)]
            public static class Reordered { }
            """
        );
        var entries = new[]
        {
            ("a.b", "Dot {name@string}"),
            ("a-b", "Dash {0}"),
            ("a b", "Space {name@string} {2}"),
            ("a_b", "Valid"),
            ("a_b_2", "Reserved suffix"),
            ("1Text", "Digit"),
            ("_Text", "Reserved start"),
            ("two words", "Whitespace"),
            ("!", "Punctuation"),
            ("_", "Reserved underscore"),
            ("End\n", "Trailing newline"),
            ("class", "Keyword {0}"),
            ("record", "Contextual keyword"),
            ("Café", "Unicode letters"),
            ("漢字", "Unicode"),
            ("ab_", "Canonical reserved"),
            ("a\u200Db!", "Normalized Unicode format"),
        };
        consumer.Write("Resources/Names.resx", Resource(entries));
        consumer.Write("Resources/Reordered.resx", Resource(entries.Reverse()));
        consumer.Write(
            "Resources/Names.es.resx",
            Resource(
                entries.Select(entry =>
                    entry.Item1 == "a b" ? (entry.Item1, "ES {2} {name}") : entry
                )
            )
        );
        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        Assert.DoesNotContain("TRESX006", build.Output);
        var invocation = await ConsumerProject.Invoke(
            Path.Combine(consumer.DirectoryPath, "bin/Release/net10.0/Consumer.dll")
        );
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal(
            "Valid\nReserved suffix\nSpace {name@string} {2}\nSpace Ada 7\nDash {0}\nDash 8\nDot {name@string}\nDot Bea\nReserved start\nDigit\nWhitespace\nReserved underscore\nPunctuation\nTrailing newline\nKeyword {0}\nKeyword 9\nContextual keyword\nUnicode letters\nUnicode\nCanonical reserved\nNormalized Unicode format\nES {2} {name}\nES 7 Ada\n",
            invocation.Output.Replace("\r\n", "\n")
        );
    }

    [Fact]
    public async Task RejectsExistingMembersAndGeneratedMemberFamilyCollisions()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        foreach (var (target, key, _, member, _) in ExistingCollisions)
        {
            var displayKey = target == "UnicodeExisting" ? "a\\u200Db" : key;
            var displayMember = target == "UnicodeExisting" ? "a\\u200Db" : member;
            AssertDiagnostic(
                build.Output,
                target,
                "TRESX007",
                $"Resource Key '{displayKey}' in 'Resources/{target}.resx' generates member '{displayMember}', which collides with an existing target-class member."
            );
        }
        foreach (
            var (target, key, member, other) in new[]
            {
                ("GeneratedFamilyCollision", "Plain", "FormatPlain", "FormatPlain"),
                ("NormalizedFamilyCollision", "Plain!", "FormatPlain_", "FormatPlain!"),
            }
        )
        {
            AssertDiagnostic(
                build.Output,
                target,
                "TRESX007",
                $"Resource Key '{key}' in 'Resources/{target}.resx' generates member '{member}', which collides with the member family for Resource Key '{other}'."
            );
        }
        AssertDiagnostic(
            build.Output,
            "UnicodeFamilyCollision",
            "TRESX007",
            "Resource Key 'a\\u200Db' in 'Resources/UnicodeFamilyCollision.resx' generates member 'a\\u200Db', which collides with the member family for Resource Key 'ab'."
        );
        AssertDiagnostic(
            build.Output,
            "ResourceManagerKeyCollision",
            "TRESX007",
            "Resource Key '__resxResourceManager' in 'Resources/ResourceManagerKeyCollision.resx' generates member '__resxResourceManager', which collides with the generated ResourceManager field."
        );
        AssertDiagnostic(
            build.Output,
            "ExistingResourceManager",
            "TRESX007",
            "Generated ResourceManager field '__resxResourceManager' in 'Resources/ExistingResourceManager.resx' collides with an existing target-class member."
        );
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    private static void AssertDiagnostic(
        string output,
        string target,
        string code,
        string message
    ) =>
        Assert.True(
            output
                .Split('\n')
                .Any(line =>
                    line.Contains(target + ".cs(", StringComparison.Ordinal)
                    && line.Contains(
                        $"error {code}: Resource Access member collision: {message}",
                        StringComparison.Ordinal
                    )
                ),
            $"Expected {target}: {code}, {message}.\n{output}"
        );

    [Fact]
    public async Task WarnAndIgnoreOmitTheSameMembersWithDistinctDiagnostics()
    {
        using var consumer = new ConsumerProject(
            """
            using System.Globalization;
            using Talby.Core.ResxAccess;
            Console.WriteLine(Warned.Plain());
            Console.WriteLine(Ignored.Plain(CultureInfo.InvariantCulture));
            foreach (var type in new[] { typeof(Warned), typeof(Ignored) })
            {
                var methods = type.GetMethods().Where(m => m.DeclaringType == type).ToArray();
                if (methods.Length != 2 || methods.Any(m => m.Name != "Plain")) throw new Exception("Invalid members were not omitted.");
            }
            """
        );
        consumer.Write(
            "Warned.cs",
            """
            using Talby.Core.ResxAccess;
            [GenerateResxAccess("Resources/Policies.resx")]
            public static class Warned { }
            """
        );
        consumer.Write(
            "Ignored.cs",
            """
            using Talby.Core.ResxAccess;
            [GenerateResxAccess("Resources/Policies.resx", InvalidKeyHandling = InvalidKeyHandling.Ignore)]
            public static class Ignored { }
            """
        );
        consumer.Write(
            "Resources/Policies.resx",
            Resource([
                ("Plain", "Preserved"),
                ("invalid-key", "{name@string}"),
                ("1Text", "{0}"),
                ("End\n", "text"),
            ])
        );
        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        foreach (var key in new[] { "invalid-key", "1Text", "End\\u000A" })
        {
            Assert.True(
                build
                    .Output.Split('\n')
                    .Any(line =>
                        line.Contains("Warned.cs(", StringComparison.Ordinal)
                        && line.Contains(
                            $"warning TRESX006: Resource Key '{key}' in 'Resources/Policies.resx' cannot become a C# method identifier; its members are omitted.",
                            StringComparison.Ordinal
                        )
                    ),
                build.Output
            );
        }
        Assert.DoesNotContain(
            build.Output.Split('\n'),
            line =>
                line.Contains("Ignored.cs(", StringComparison.Ordinal)
                && line.Contains("TRESX006", StringComparison.Ordinal)
        );
        var invocation = await ConsumerProject.Invoke(
            Path.Combine(consumer.DirectoryPath, "bin/Release/net10.0/Consumer.dll")
        );
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal("Preserved\nPreserved\n", invocation.Output.Replace("\r\n", "\n"));
        foreach (var target in new[] { "Warned", "Ignored" })
        {
            consumer.Write(
                target + "Omitted.cs",
                $"public class {target}Omitted {{ public string Invoke() => {target}.invalid_key(); public string Format() => {target}.Formatinvalid_key(\"Ada\"); }}"
            );
        }
        var omittedBuild = await consumer.Build();
        Assert.NotEqual(0, omittedBuild.ExitCode);
        foreach (var target in new[] { "Warned", "Ignored" })
        {
            foreach (var member in new[] { "invalid_key", "Formatinvalid_key" })
            {
                Assert.True(
                    omittedBuild
                        .Output.Split('\n')
                        .Any(line =>
                            line.Contains(target + "Omitted.cs(", StringComparison.Ordinal)
                            && line.Contains(
                                $"error CS0117: '{target}' does not contain a definition for '{member}'",
                                StringComparison.Ordinal
                            )
                        ),
                    omittedBuild.Output
                );
            }
        }
    }

    [Fact]
    public async Task OmittedKeysStillReportResourceSetAndPlaceholderContractErrorsInBothPolicies()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        foreach (var policy in new[] { "Warn", "Ignore" })
        {
            foreach (
                var (behavior, code, message) in new[]
                {
                    (
                        "Reference",
                        "TRESX001",
                        "Invalid Reference Resource: 'Resources/OmittedReference"
                            + policy
                            + ".resx' Resource Key 'invalid-key' has a malformed Formatting Placeholder: Unclosed Formatting Placeholder."
                    ),
                    (
                        "Localized",
                        "TRESX004",
                        "Invalid Localized Resource: 'Resources/OmittedLocalized"
                            + policy
                            + ".fr.resx' Resource Key 'invalid-key' has a malformed Formatting Placeholder: Named Placeholder 'name' declares Argument Type 'string'; the Reference Resource requires 'int'."
                    ),
                    (
                        "Keys",
                        "TRESX004",
                        "Invalid Localized Resource: 'Resources/OmittedKeys"
                            + policy
                            + ".es.resx' must contain exactly the Reference Resource's case-sensitive Resource Keys. Missing: 'invalid-key'. Additional: (none)."
                    ),
                    (
                        "Expected",
                        "TRESX005",
                        "Invalid ExpectedCultures: Expected Culture 'es' requires an associated Localized Resource for 'Resources/OmittedExpected"
                            + policy
                            + ".resx'."
                    ),
                }
            )
            {
                var target = "Omitted" + behavior + policy;
                Assert.True(
                    build
                        .Output.Split('\n')
                        .Any(line =>
                            line.Contains(target + ".cs(", StringComparison.Ordinal)
                            && line.Contains($"error {code}: {message}", StringComparison.Ordinal)
                        ),
                    $"Expected {target}: {message}\n{build.Output}"
                );
            }
        }
        Assert.DoesNotContain("LAMA0041", build.Output);
    }

    internal static void WriteIdentifierDiagnostics(ConsumerProject consumer)
    {
        consumer.Write(
            "UnsupportedIdentifierPolicy.cs",
            """
            using Talby.Core.ResxAccess;
            [GenerateResxAccess("Resources/UnsupportedIdentifierPolicy.resx", InvalidKeyHandling = (InvalidKeyHandling)99)]
            public static class UnsupportedIdentifierPolicy { }
            """
        );
        consumer.Write(
            "Resources/UnsupportedIdentifierPolicy.resx",
            Resource([("invalid-key", "Text")])
        );
        foreach (var (target, key, text, _, declaration) in ExistingCollisions)
        {
            WriteCollision(consumer, target, Resource([(key, text)]), declaration);
        }
        WriteCollision(
            consumer,
            "GeneratedFamilyCollision",
            Resource([("Plain", "{0}"), ("FormatPlain", "text")])
        );
        WriteCollision(
            consumer,
            "NormalizedFamilyCollision",
            Resource([("Plain!", "{name@string}"), ("FormatPlain!", "text")])
        );
        WriteCollision(
            consumer,
            "UnicodeFamilyCollision",
            Resource([("a\u200Db", "one"), ("ab", "two")])
        );
        WriteCollision(
            consumer,
            "ResourceManagerKeyCollision",
            Resource([("__resxResourceManager", "text")])
        );
        WriteCollision(
            consumer,
            "ExistingResourceManager",
            Resource([("Plain", "text")]),
            "private static readonly object __resxResourceManager = new();"
        );
        foreach (var policy in new[] { "Warn", "Ignore" })
        {
            foreach (var behavior in new[] { "Reference", "Localized", "Keys", "Expected" })
            {
                var target = "Omitted" + behavior + policy;
                consumer.Write(
                    target + ".cs",
                    $$"""
                    using Talby.Core.ResxAccess;
                    [GenerateResxAccess("Resources/{{target}}.resx", InvalidKeyHandling = InvalidKeyHandling.{{policy}}, ExpectedCultures = new[] { "es" })]
                    public static class {{target}} { }
                    """
                );
                consumer.Write(
                    $"Resources/{target}.resx",
                    Resource([("invalid-key", behavior == "Reference" ? "{name" : "{name@int}")])
                );
                if (behavior != "Expected")
                {
                    consumer.Write(
                        $"Resources/{target}.es.resx",
                        behavior == "Keys" ? "<root />" : Resource([("invalid-key", "{name}")])
                    );
                }
                if (behavior == "Localized")
                {
                    consumer.Write(
                        $"Resources/{target}.fr.resx",
                        Resource([("invalid-key", "{name@string}")])
                    );
                }
            }
        }
    }

    private static void WriteCollision(
        ConsumerProject consumer,
        string target,
        string resource,
        string declaration = ""
    )
    {
        consumer.Write(
            target + ".cs",
            $$"""
            using Talby.Core.ResxAccess;
            [GenerateResxAccess("Resources/{{target}}.resx", InvalidKeyHandling = InvalidKeyHandling.Normalize)]
            public static class {{target}} { {{declaration}} }
            """
        );
        consumer.Write($"Resources/{target}.resx", resource);
    }

    [Fact]
    public async Task RejectsUnsupportedIdentifierPolicies()
    {
        var build = await diagnostics.BuildDiagnostics();
        Assert.NotEqual(0, build.ExitCode);
        Assert.True(
            build
                .Output.Split('\n')
                .Any(line =>
                    line.Contains("UnsupportedIdentifierPolicy.cs(", StringComparison.Ordinal)
                    && line.Contains(
                        "error TRESX008: InvalidKeyHandling value '99' is unsupported. Specify Warn, Ignore, or Normalize.",
                        StringComparison.Ordinal
                    )
                ),
            build.Output
        );
    }

    private static string Resource(IEnumerable<(string Key, string Text)> entries) =>
        new XElement(
            "root",
            entries.Select(entry => new XElement(
                "data",
                new XAttribute("name", entry.Key),
                new XElement("value", entry.Text)
            ))
        ).ToString();
}
