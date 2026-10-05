namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class IncrementalBuildConsumerTests
{
    // Metalama's build signal is touched on every build. Exclude it from compiler inputs
    // so a successful regression proves resource dependencies, not incidental recompilation.
    private const string IsolateResourceInputs = """
        <Target Name="IsolateResourceInputs" AfterTargets="CreateMetalamaTouchFiles">
          <ItemGroup>
            <AdditionalFiles Remove="$(MetalamaBuildTouchFile)" />
          </ItemGroup>
        </Target>
        """;

    private const string Source = """
        using System.Globalization;
        using Talby.Core.ResxAccess;

        Console.WriteLine(Texts.Welcome(CultureInfo.InvariantCulture));
        Console.WriteLine(Texts.FormatWelcome("Ada", CultureInfo.InvariantCulture));
        Console.WriteLine(Texts.Welcome(CultureInfo.GetCultureInfo("es")));
        Console.WriteLine(Texts.FormatWelcome("Ada", CultureInfo.GetCultureInfo("es")));

        [GenerateResxAccess("Resources/Texts.resx")]
        public static partial class Texts { }
        """;

    [Fact]
    public async Task RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits()
    {
        using var project = new ConsumerProject(Source, projectTargets: IsolateResourceInputs);
        project.Write("Resources/Texts.resx", Resource("Hello {name@string}"));
        project.Write("Resources/Texts.es.resx", Resource("Hola {name}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHola {name}\nHola Ada\n"
        );

        var assemblyPath = Path.Combine(project.DirectoryPath, "bin/Release/net10.0/Consumer.dll");
        var lastWrite = File.GetLastWriteTimeUtc(assemblyPath);
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHola {name}\nHola Ada\n"
        );
        Assert.Equal(lastWrite, File.GetLastWriteTimeUtc(assemblyPath));

        project.Write("Resources/Texts.resx", Resource("Welcome {name@string}!"));
        await AssertBuildAndOutput(
            project,
            "Welcome {name@string}!\nWelcome Ada!\nHola {name}\nHola Ada\n"
        );

        project.Write("Resources/Texts.es.resx", Resource("Bienvenida {name}!"));
        await AssertBuildAndOutput(
            project,
            "Welcome {name@string}!\nWelcome Ada!\nBienvenida {name}!\nBienvenida Ada!\n"
        );
    }

    [Fact]
    public async Task RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals()
    {
        using var project = new ConsumerProject(
            Source.Replace(
                "[GenerateResxAccess(\"Resources/Texts.resx\")]",
                "[GenerateResxAccess(\"Resources/Texts.resx\", ExpectedCultures = new[] { \"en\" })]"
            ),
            projectTargets: IsolateResourceInputs
        );
        project.Write("Resources/Texts.resx", Resource("Hello {name@string}"));
        project.Write("Resources/Texts.en.resx", Resource("English {name}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHello {name@string}\nHello Ada\n"
        );

        project.Write("Resources/Texts.es.resx", Resource("Hola {name}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHola {name}\nHola Ada\n"
        );

        project.Write("Resources/Texts.fr.resx", Resource("Bonjour {other}"));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.fr.resx' Resource Key 'Welcome' must use exactly the Reference Resource's Placeholder Contract (arguments: name)."
        );
        project.Write("Resources/Texts.fr.resx", Resource("Bonjour {name}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHola {name}\nHola Ada\n"
        );

        File.Delete(Path.Combine(project.DirectoryPath, "Resources/Texts.es.resx"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHello {name@string}\nHello Ada\n"
        );
    }

    [Fact]
    public async Task RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration()
    {
        using var project = new ConsumerProject(
            Source.Replace(
                "[GenerateResxAccess(\"Resources/Texts.resx\")]",
                "[GenerateResxAccess(\"Resources/Texts.resx\", ExpectedCultures = new[] { \"es\" })]"
            ),
            projectTargets: IsolateResourceInputs
        );
        project.Write("Resources/Texts.resx", Resource("Hello {name@string}"));
        project.Write("Resources/Texts.es.resx", Resource("Hola {name}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHola {name}\nHola Ada\n"
        );
        File.Delete(Path.Combine(project.DirectoryPath, "Resources/Texts.es.resx"));
        await AssertDiagnostic(
            project,
            "TRESX005",
            "Invalid ExpectedCultures: Expected Culture 'es' requires an associated Localized Resource for 'Resources/Texts.resx'."
        );
        project.Write("Resources/Texts.es.resx", Resource("Nueva {name}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nNueva {name}\nNueva Ada\n"
        );
    }

    [Fact]
    public async Task DetectsAssociatedResourcesExcludedFromSdkEmbedding()
    {
        using var project = new ConsumerProject(
            Source,
            projectItems: "<EmbeddedResource Remove=\"Resources/Texts.fr.resx\" />",
            projectTargets: IsolateResourceInputs
        );
        project.Write("Resources/Texts.resx", Resource("Hello {name@string}"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHello {name@string}\nHello Ada\n"
        );
        project.Write("Resources/Texts.fr.resx", Resource("Bonjour {other}"));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.fr.resx' Resource Key 'Welcome' must use exactly the Reference Resource's Placeholder Contract (arguments: name)."
        );
        project.Write("Resources/Texts.fr.resx", Resource("Bonjour {name}"));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.fr.resx' must use standard SDK satellite embedding for this Resource Set."
        );
        File.Delete(Path.Combine(project.DirectoryPath, "Resources/Texts.fr.resx"));
        await AssertBuildAndOutput(
            project,
            "Hello {name@string}\nHello Ada\nHello {name@string}\nHello Ada\n"
        );
    }

    [Fact]
    public async Task RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics()
    {
        const string source = """
            using System.Globalization;
            using System.Reflection;
            using Talby.Core.ResxAccess;

            CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var methods = typeof(Texts).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
            foreach (var method in methods.Where(m => m.GetParameters().Length == 0).OrderBy(m => m.Name, StringComparer.Ordinal))
                Console.WriteLine($"{method.Name}={method.Invoke(null, null)}");
            foreach (var method in methods.Where(m => m.Name.StartsWith("Format", StringComparison.Ordinal) && m.GetParameters().All(p => p.ParameterType != typeof(CultureInfo))))
            {
                var parameters = method.GetParameters();
                Console.WriteLine(method.Name + "=" + string.Join(";", parameters.Select(p => $"{p.Name}:{p.ParameterType}:{new NullabilityInfoContext().Create(p).ReadState}")));
                var arguments = parameters.Select(p => p.ParameterType == typeof(string) ? (object)"Ada" : p.ParameterType == typeof(int) ? 7 : p.ParameterType == typeof(long?) ? 9L : "value").ToArray();
                Console.WriteLine(method.Invoke(null, arguments));
            }

            [GenerateResxAccess("Resources/Texts.resx")]
            public static partial class Texts { }
            """;
        using var project = new ConsumerProject(source, projectTargets: IsolateResourceInputs);
        const string reference = "Hello {name@string} {count@int} {2} {0}";
        project.Write("Resources/Texts.resx", Resource(reference));
        project.Write("Resources/Texts.es.resx", Resource("{count} {name} {0} {2}"));
        const string initialOutput =
            "Plain=Just text\nWelcome=Hello {name@string} {count@int} {2} {0}\nFormatWelcome=name:System.String:NotNull;count:System.Int32:NotNull;arg0:System.Object:Nullable;arg2:System.Object:Nullable\nHello Ada 7 value value\n";
        await AssertBuildAndOutput(project, initialOutput);

        const string changed = "Changed {total@long?} {person@string?} {5} {1}";
        project.Write("Resources/Texts.resx", Resource(changed));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.es.resx' Resource Key 'Welcome' must use exactly the Reference Resource's Placeholder Contract (arguments: total, person, 1, 5)."
        );
        project.Write("Resources/Texts.es.resx", Resource("{person} {total} {1} {5}"));
        const string changedOutput =
            "Plain=Just text\nWelcome=Changed {total@long?} {person@string?} {5} {1}\nFormatWelcome=total:System.Nullable`1[System.Int64]:Nullable;person:System.String:Nullable;arg1:System.Object:Nullable;arg5:System.Object:Nullable\nChanged 9 Ada value value\n";
        await AssertBuildAndOutput(project, changedOutput);

        project.Write("Resources/Texts.es.resx", Resource("{person@string} {total} {1} {5}"));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.es.resx' Resource Key 'Welcome' has a malformed Formatting Placeholder: Named Placeholder 'person' declares Argument Type 'string'; the Reference Resource requires 'string?'."
        );
        project.Write("Resources/Texts.es.resx", Resource("{person} {total} {1} {5}"));
        await AssertBuildAndOutput(project, changedOutput);

        project.Write("Resources/Texts.resx", Resource(changed, key: "Renamed"));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.es.resx' must contain exactly the Reference Resource's case-sensitive Resource Keys. Missing: 'Renamed'. Additional: 'Welcome'."
        );
        project.Write(
            "Resources/Texts.es.resx",
            Resource("{person} {total} {1} {5}", key: "Renamed")
        );
        await AssertBuildAndOutput(project, changedOutput.Replace("Welcome=", "Renamed="));
    }

    [Fact]
    public async Task RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources()
    {
        using var project = new ConsumerProject(Source, projectTargets: IsolateResourceInputs);
        var reference = Resource("Hello {name@string}")
            .Replace(
                "</root>",
                "<data name=\"omitted-key\"><value>{count@int}</value></data></root>"
            );
        var localized = Resource("Hola {name}")
            .Replace("</root>", "<data name=\"omitted-key\"><value>{count}</value></data></root>");
        project.Write("Resources/Texts.resx", reference);
        project.Write("Resources/Texts.es.resx", localized);
        const string output = "Hello {name@string}\nHello Ada\nHola {name}\nHola Ada\n";
        await AssertBuildAndOutput(project, output);

        project.Write("Resources/Unrelated.fr.resx", Resource("{unknown}"));
        await AssertBuildAndOutput(project, output);
        project.Write("Resources/Texts.es.resx", localized.Replace("{count}", "{other}"));
        await AssertDiagnostic(
            project,
            "TRESX004",
            "Invalid Localized Resource: 'Resources/Texts.es.resx' Resource Key 'omitted-key' must use exactly the Reference Resource's Placeholder Contract (arguments: count)."
        );
        project.Write("Resources/Texts.es.resx", localized);
        await AssertBuildAndOutput(project, output);
        project.Write("Resources/Texts.resx", reference.Replace("{count@int}", "{count@float}"));
        await AssertDiagnostic(
            project,
            "TRESX001",
            "Invalid Reference Resource: 'Resources/Texts.resx' Resource Key 'omitted-key' has a malformed Formatting Placeholder: Unsupported Argument Type 'float' for Named Placeholder 'count'."
        );
        project.Write("Resources/Texts.resx", reference);
        await AssertBuildAndOutput(project, output);
    }

    private static async Task AssertDiagnostic(ConsumerProject project, string code, string message)
    {
        var build = await project.Build();
        Assert.NotEqual(0, build.ExitCode);
        Assert.Contains(
            build.Output.Split('\n'),
            line =>
                line.Contains("Program.cs(", StringComparison.Ordinal)
                && line.Contains($"error {code}: {message}", StringComparison.Ordinal)
        );
    }

    private static string Resource(string text, string key = "Welcome") =>
        RawTextConsumerTests
            .ReferenceResource.Replace("  Hello {name@string}, {0:N2}!  ", text)
            .Replace("name=\"Welcome\"", $"name=\"{key}\"");

    private static async Task AssertBuildAndOutput(ConsumerProject project, string expected)
    {
        var build = await project.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        var invocation = await ConsumerProject.Invoke(
            Path.Combine(project.DirectoryPath, "bin/Release/net10.0/Consumer.dll")
        );
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal(expected, invocation.Output.Replace("\r\n", "\n"));
    }
}
