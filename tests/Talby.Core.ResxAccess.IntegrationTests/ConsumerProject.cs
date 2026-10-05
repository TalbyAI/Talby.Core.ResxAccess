using System.Diagnostics;
using System.Security;

namespace Talby.Core.ResxAccess.IntegrationTests;

internal sealed class ConsumerProject : IDisposable
{
    public string DirectoryPath { get; } =
        Path.Combine(Path.GetTempPath(), "ResxAccessTests", Guid.NewGuid().ToString("N"));

    public ConsumerProject(
        string source,
        string projectItems = "",
        string projectProperties = "",
        string projectTargets = ""
    )
    {
        var repository = FindRepository();
        Write(
            "Consumer.csproj",
            $$"""
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <OutputType>Exe</OutputType>
                <TargetFramework>net10.0</TargetFramework>
                <ImplicitUsings>enable</ImplicitUsings>
                <Nullable>enable</Nullable>
                <RootNamespace>ConsumerRoot</RootNamespace>
                {{projectProperties}}
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="{{SecurityElement.Escape(
                Path.Combine(repository, "src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj")
            )}}" />
                {{projectItems}}
              </ItemGroup>
              <Import Project="{{SecurityElement.Escape(
                Path.Combine(
                    repository,
                    "src/Talby.Core.ResxAccess/buildTransitive/Talby.Core.ResxAccess.targets"
                )
            )}}" />
              {{projectTargets}}
            </Project>
            """
        );
        Write("Program.cs", source);
    }

    public void Write(string path, string text)
    {
        var fullPath = Path.Combine(DirectoryPath, path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, text);
    }

    public async Task<(int ExitCode, string Output)> Build()
        // The current Release solution build supplies the library and its Metalama outputs.
        // Restore only the fresh consumer so the reference's obj directory stays read-only.
        =>
        await Run(
            "build",
            Path.Combine(DirectoryPath, "Consumer.csproj"),
            "--configuration",
            "Release",
            "--nologo",
            "--verbosity",
            "quiet",
            "-p:BuildProjectReferences=false",
            "-p:RestoreRecursive=false"
        );

    public static async Task<(int ExitCode, string Output)> Invoke(
        string assemblyPath,
        params string[] arguments
    ) => await Run([assemblyPath, .. arguments]);

    public async Task<(int ExitCode, string Output)> DesignTimeBuild() =>
        await Run(
            "msbuild",
            Path.Combine(DirectoryPath, "Consumer.csproj"),
            "-target:Compile",
            "-property:Configuration=Release",
            "-property:DesignTimeBuild=true",
            "-property:BuildingProject=false",
            "-property:SkipCompilerExecution=true",
            "-property:ProvideCommandLineArgs=true",
            "-property:BuildProjectReferences=false",
            "-getProperty:TalbyResxResourceMap",
            "-getItem:CscCommandLineArgs,AdditionalDesignTimeBuildInput"
        );

    private static async Task<(int ExitCode, string Output)> Run(params string[] arguments)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = Path.GetTempPath(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };
        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)!;
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException("Consumer process exceeded two minutes.");
        }

        return (process.ExitCode, await output + await error);
    }

    private static string FindRepository()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (
            directory is not null
            && !File.Exists(Path.Combine(directory.FullName, "Talby.Core.ResxAccess.slnx"))
        )
        {
            directory = directory.Parent;
        }

        return directory?.FullName
            ?? throw new InvalidOperationException("Cannot find the repository.");
    }

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}
