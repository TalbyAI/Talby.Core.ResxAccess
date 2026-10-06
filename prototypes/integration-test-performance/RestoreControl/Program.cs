using System.Reflection;
using System.Text.Json;

var type = Assembly.LoadFrom(args[0]).GetType(
    "Talby.Core.ResxAccess.IntegrationTests.ConsumerProject",
    throwOnError: true
)!;
using var project = (IDisposable)type.GetConstructors()[0].Invoke(
    [
        "Console.WriteLine(\"ok\");", "", "",
        """
        <Target Name="IsolateResourceInputs" AfterTargets="CreateMetalamaTouchFiles">
          <ItemGroup><AdditionalFiles Remove="$(MetalamaBuildTouchFile)" /></ItemGroup>
        </Target>
        """
    ]
);
var directory = (string)type.GetProperty("DirectoryPath")!.GetValue(project)!;
var projectPath = Path.Combine(directory, "Consumer.csproj");
var projectText = File.ReadAllText(projectPath);
var assemblyPath = Path.Combine(directory, "bin/Release/net10.0/Consumer.dll");
var observations = new List<object>();

async Task Build(string name, bool expectedSuccess, string? expectedDiagnostic = null)
{
    var task = (Task)type.GetMethod("Build")!.Invoke(project, null)!;
    await task;
    var result = ((int ExitCode, string Output))task.GetType().GetProperty("Result")!.GetValue(task)!;
    if ((result.ExitCode == 0) != expectedSuccess)
    {
        throw new InvalidOperationException($"Unexpected result for {name}: {result.Output}");
    }
    if (expectedDiagnostic is not null && !result.Output.Contains(expectedDiagnostic, StringComparison.Ordinal))
    {
        throw new InvalidOperationException($"Missing {expectedDiagnostic} for {name}: {result.Output}");
    }
    observations.Add(new { Name = name, result.ExitCode, DirectoryPath = directory });
}

void Write(string path, string text) => type.GetMethod("Write")!.Invoke(project, [path, text]);

await Build("fresh", true);
var unchangedWriteTime = File.GetLastWriteTimeUtc(assemblyPath);
await Build("unchanged", true);
if (File.GetLastWriteTimeUtc(assemblyPath) != unchangedWriteTime)
{
    throw new InvalidOperationException("The unchanged build rewrote the assembly.");
}
Write("Program.cs", "Console.WriteLine(UndefinedIdentifier);");
await Build("compilation-failure-after-successful-restore", false, "CS0103");
Write("Program.cs", "Console.WriteLine(\"ok\");");
await Build("corrected-compilation", true);
Write("Consumer.csproj", projectText + Environment.NewLine);
await Build("changed-project", true);
Write("Consumer.csproj", "<Project>");
await Build("restore-failure", false, "MSB4025");
await Build("repeated-restore-failure", false, "MSB4025");
Write("EmptyFeed/.keep", "");
var emptyFeed = System.Security.SecurityElement.Escape(Path.Combine(directory, "EmptyFeed"));
var missingPackageProject = projectText.Replace(
    "</Project>",
    $"<PropertyGroup><RestoreSources>{emptyFeed}</RestoreSources><NuGetAudit>false</NuGetAudit></PropertyGroup>"
    + "<ItemGroup><PackageReference Include=\"ResxAccess.RestoreControl.Missing\" Version=\"1.0.0\" /></ItemGroup></Project>"
);
Write("Consumer.csproj", missingPackageProject);
await Build("restore-task-package-failure", false, "NU1101");
await Build("repeated-restore-task-package-failure", false, "NU1101");
Write("Consumer.csproj", projectText);
await Build("corrected-project", true);
File.WriteAllText(args[1], JsonSerializer.Serialize(observations, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("Verified fresh restore, unchanged assembly, compilation-failure recovery, project invalidation and restore-failure recovery.");
