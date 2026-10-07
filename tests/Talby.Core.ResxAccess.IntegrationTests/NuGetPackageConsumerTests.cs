using System.IO.Compression;
using System.Security;
using System.Xml.Linq;

namespace Talby.Core.ResxAccess.IntegrationTests;

[Trait("Category", "Integration")]
public sealed class NuGetPackageConsumerTests(NuGetPackageFixture package)
    : IClassFixture<NuGetPackageFixture>
{
    [Fact]
    public void IncludesConsumerDocumentationLicenseAndTransitiveTargets()
    {
        using var archive = ZipFile.OpenRead(package.PackagePath);
        var manifest = archive.Entries.Single(entry => entry.FullName.EndsWith(".nuspec"));
        using var manifestStream = manifest.Open();
        var document = XDocument.Load(manifestStream);
        var ns = document.Root!.Name.Namespace;
        var metadata = document.Root.Element(ns + "metadata")!;
        var project = XDocument.Load(
            Path.Combine(
                ConsumerProject.FindRepository(),
                "src",
                "Talby.Core.ResxAccess",
                "Talby.Core.ResxAccess.csproj"
            )
        );

        Assert.Equal("Talby.Core.ResxAccess", metadata.Element(ns + "id")!.Value);
        Assert.Equal(
            project.Descendants("Version").Single().Value,
            metadata.Element(ns + "version")!.Value
        );
        Assert.Equal(
            project.Descendants("Copyright").Single().Value,
            metadata.Element(ns + "copyright")?.Value
        );
        Assert.Equal("TalbyAI", metadata.Element(ns + "authors")!.Value);
        Assert.Equal("MIT", metadata.Element(ns + "license")?.Value);
        Assert.Equal("expression", metadata.Element(ns + "license")?.Attribute("type")?.Value);
        Assert.Equal("README.md", metadata.Element(ns + "readme")?.Value);
        Assert.Equal(
            "https://github.com/TalbyAI/Talby.Core.ResxAccess",
            metadata.Element(ns + "repository")?.Attribute("url")?.Value
        );
        Assert.NotNull(archive.GetEntry("README.md"));
        Assert.NotNull(archive.GetEntry("LICENSE"));
        Assert.NotNull(archive.GetEntry("lib/net10.0/Talby.Core.ResxAccess.dll"));
        Assert.NotNull(archive.GetEntry("buildTransitive/Talby.Core.ResxAccess.targets"));
        var metalamaDependency = Assert.Single(
            metadata.Descendants(ns + "dependency"),
            dependency => dependency.Attribute("id")?.Value == "Metalama.Framework"
        );
        Assert.Equal(
            project
                .Descendants("PackageReference")
                .Single(reference => reference.Attribute("Include")?.Value == "Metalama.Framework")
                .Attribute("Version")!
                .Value,
            metalamaDependency.Attribute("version")?.Value
        );
    }

    [Fact]
    public async Task CanGenerateAndInvokeLocalizedMethodsFromPackageReference()
    {
        using var consumer = new ConsumerProject(
            """
            using System.Globalization;
            using Talby.Core.ResxAccess;

            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            var es = CultureInfo.GetCultureInfo("es-AR");
            var fr = CultureInfo.GetCultureInfo("fr-FR");
            Console.WriteLine(Texts.Welcome());
            Console.WriteLine(Texts.Welcome(es));
            Console.WriteLine(Texts.FormatWelcome("Ada", 12.5m));
            Console.WriteLine(Texts.FormatWelcome("Ada", 12.5m, es, fr));
            Console.WriteLine(Texts.Plain(es));
            Console.WriteLine(Texts.Plain(CultureInfo.GetCultureInfo("de")));

            [GenerateResxAccess("Resources/Labels.resx", ExpectedCultures = new[] { "es" })]
            internal static class Texts
            {
            }
            """
        );
        consumer.Write(
            "Consumer.csproj",
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>PackageConsumer</RootNamespace>
                <RestorePackagesPath>{{SecurityElement.Escape(
                package.RestorePackagesPath
            )}}</RestorePackagesPath>
              </PropertyGroup>
              <ItemGroup>
                <PackageReference Include="Talby.Core.ResxAccess" Version="{{package.Version}}" />
              </ItemGroup>
            </Project>
            """
        );
        consumer.Write(
            "NuGet.Config",
            $$"""
            <configuration>
              <packageSources>
                <clear />
                <add key="local" value="{{SecurityElement.Escape(
                Path.GetDirectoryName(package.PackagePath)
            )}}" />
                <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
              </packageSources>
              <packageSourceMapping>
                <clear />
                <packageSource key="local"><package pattern="Talby.Core.ResxAccess" /></packageSource>
                <packageSource key="nuget.org"><package pattern="*" /></packageSource>
              </packageSourceMapping>
            </configuration>
            """
        );
        consumer.Write(
            "Resources/Labels.resx",
            """
            <root>
              <data name="Welcome"><value>Hello {name@string}, {amount@decimal:N2}!</value></data>
              <data name="Plain"><value>Hello</value></data>
            </root>
            """
        );
        consumer.Write(
            "Resources/Labels.es.resx",
            """
            <root>
              <data name="Welcome"><value>Hola {name}, {amount:N2}!</value></data>
              <data name="Plain"><value>Hola</value></data>
            </root>
            """
        );

        var build = await consumer.Build();
        Assert.True(build.ExitCode == 0, build.Output);
        Assert.True(
            File.Exists(
                Path.Combine(
                    consumer.DirectoryPath,
                    "obj",
                    "Release",
                    "net10.0",
                    "TalbyResxResources.txt"
                )
            ),
            "The package must import its transitive targets without an explicit consumer import."
        );
        Assert.True(
            File.Exists(
                Path.Combine(
                    consumer.DirectoryPath,
                    "bin",
                    "Release",
                    "net10.0",
                    "es",
                    "Consumer.resources.dll"
                )
            ),
            "The SDK must emit the Localized Resource satellite assembly."
        );
        var invocation = await ConsumerProject.Invoke(
            Path.Combine(consumer.DirectoryPath, "bin", "Release", "net10.0", "Consumer.dll")
        );
        Assert.True(invocation.ExitCode == 0, invocation.Output);
        Assert.Equal(
            "Hello {name@string}, {amount@decimal:N2}!\nHola {name}, {amount:N2}!\nHello Ada, 12.50!\nHola Ada, 12,50!\nHola\nHello\n",
            invocation.Output.Replace("\r\n", "\n")
        );
    }
}

public sealed class NuGetPackageFixture : IAsyncLifetime
{
    public string PackagePath { get; private set; } = "";

    public string Version { get; private set; } = "";

    public string RestorePackagesPath { get; private set; } = "";

    public async Task InitializeAsync()
    {
        var repository = ConsumerProject.FindRepository();
        var feed = Path.Combine(repository, "test-results", "nuget", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(feed);
        // Compiler servers may retain package assemblies after a consumer build.
        // Keep its isolated cache with the retained test artifacts, outside the disposable consumer.
        RestorePackagesPath = Path.Combine(feed, "packages");
        var providedPackage = Environment.GetEnvironmentVariable("TALBY_TEST_PACKAGE");
        if (string.IsNullOrEmpty(providedPackage))
        {
            var pack = await ConsumerProject.Run(
                "pack",
                Path.Combine(
                    repository,
                    "src",
                    "Talby.Core.ResxAccess",
                    "Talby.Core.ResxAccess.csproj"
                ),
                "--configuration",
                "Release",
                "--no-build",
                "--no-restore",
                "--output",
                feed
            );
            if (pack.ExitCode != 0)
            {
                throw new InvalidOperationException(pack.Output);
            }

            PackagePath = Directory.GetFiles(feed, "*.nupkg").Single();
        }
        else
        {
            PackagePath = Path.Combine(feed, Path.GetFileName(providedPackage));
            File.Copy(Path.GetFullPath(providedPackage), PackagePath);
        }

        using var archive = ZipFile.OpenRead(PackagePath);
        using var stream = archive
            .Entries.Single(entry => entry.FullName.EndsWith(".nuspec"))
            .Open();
        var document = XDocument.Load(stream);
        var ns = document.Root!.Name.Namespace;
        Version = document.Root.Element(ns + "metadata")!.Element(ns + "version")!.Value;
    }

    public Task DisposeAsync() => Task.CompletedTask;
}
