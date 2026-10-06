[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string] $RepositoryRoot,
    [Parameter(Mandatory = $true)]
    [string] $ExperimentRoot
)

$ErrorActionPreference = "Stop"
$historyMethods = @(
    "RefreshesRawAndFormattedTextAfterReferenceAndLocalizedEdits",
    "RefreshesDiscoveryValidationAndFallbackAfterResourceAdditionsAndRemovals",
    "RefreshesExpectedCultureDiagnosticsAfterRemovalAndRestoration",
    "DetectsAssociatedResourcesExcludedFromSdkEmbedding",
    "RefreshesGeneratedKeysSignaturesAndPlaceholderDiagnostics",
    "RefreshesValidationForOmittedKeysWhileIgnoringUnrelatedResources"
)

function Write-Utf8File([string] $Path, [string] $Content) {
    [System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($Path)) | Out-Null
    [System.IO.File]::WriteAllText($Path, $Content, [System.Text.UTF8Encoding]::new($false))
}

function Replace-ExactlyOnce([string] $Text, [string] $Old, [string] $New, [string] $Description) {
    $Text = Normalize-Lf $Text
    $Old = Normalize-Lf $Old
    $New = Normalize-Lf $New
    $count = [regex]::Matches($Text, [regex]::Escape($Old)).Count
    if ($count -ne 1) {
        throw "Expected one $Description occurrence; found $count."
    }
    return $Text.Replace($Old, $New)
}

function Replace-Between([string] $Text, [string] $Start, [string] $End, [string] $Replacement, [string] $Description) {
    $Text = Normalize-Lf $Text
    $Start = Normalize-Lf $Start
    $End = Normalize-Lf $End
    $Replacement = Normalize-Lf $Replacement
    $startIndex = $Text.IndexOf($Start, [System.StringComparison]::Ordinal)
    $endIndex = $Text.IndexOf($End, [System.StringComparison]::Ordinal)
    if ($startIndex -lt 0 -or $endIndex -le $startIndex) {
        throw "Could not locate the $Description boundaries."
    }
    return $Text.Substring(0, $startIndex) + $Replacement + $Text.Substring($endIndex)
}

function Normalize-Lf([string] $Text) {
    $lf = [string][char]10
    $cr = [string][char]13
    return $Text.Replace($cr + $lf, $lf).Replace($cr, $lf)
}

function Get-Sha256([string] $Text) {
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes((Normalize-Lf $Text))
    return [Convert]::ToHexString([System.Security.Cryptography.SHA256]::HashData($bytes)).ToLowerInvariant()
}

function Copy-SourceTree([string] $Source, [string] $Destination) {
    [System.IO.Directory]::CreateDirectory($Destination) | Out-Null
    $excluded = @("bin", "obj", "packages", ".nuget", "node_modules", "artifacts", "test-results", ".git")
    foreach ($entry in [System.IO.Directory]::EnumerateFileSystemEntries($Source)) {
        $name = [System.IO.Path]::GetFileName($entry)
        if ([System.IO.Directory]::Exists($entry)) {
            if ($excluded -contains $name) { continue }
            Copy-SourceTree $entry (Join-Path $Destination $name)
        }
        else {
            [System.IO.File]::Copy($entry, (Join-Path $Destination $name))
        }
    }
}

function Apply-TraceInstrumentation([string] $TreeRoot) {
    $path = Join-Path $TreeRoot "tests/Talby.Core.ResxAccess.IntegrationTests/ConsumerProject.cs"
    $source = [System.IO.File]::ReadAllText($path)
    $lf = [char]10
    $source = Replace-ExactlyOnce $source ("using System.Security;" + $lf + $lf) ("using System.Security;" + $lf + "using System.Text.Json;" + $lf + $lf) "trace JSON using"

    $runner = @'
    private static async Task<(int ExitCode, string Output)> Run(params string[] arguments)
    {
        var projectPath = arguments.FirstOrDefault(argument =>
            argument.EndsWith(".csproj", StringComparison.OrdinalIgnoreCase)
        );
        if (
            projectPath is null
            && arguments.Length > 0
            && arguments[0].EndsWith(".dll", StringComparison.OrdinalIgnoreCase)
        )
        {
            projectPath = Path.GetFullPath(
                Path.Combine(Path.GetDirectoryName(arguments[0])!, "..", "..", "..", "Consumer.csproj")
            );
        }

        projectPath ??= string.Empty;
        if (projectPath.Length > 0)
        {
            projectPath = Path.GetFullPath(projectPath);
        }

        var startedAt = DateTimeOffset.UtcNow;
        int? exitCode = null;
        try
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
                await process.WaitForExitAsync();
                exitCode = process.ExitCode;
                throw new TimeoutException("Consumer process exceeded two minutes.");
            }

            exitCode = process.ExitCode;
            return (exitCode.Value, await output + await error);
        }
        finally
        {
            await WriteTrace(arguments, projectPath, startedAt, DateTimeOffset.UtcNow, exitCode);
        }
    }

    private static async Task WriteTrace(
        string[] arguments,
        string projectPath,
        DateTimeOffset startedAt,
        DateTimeOffset endedAt,
        int? exitCode
    )
    {
        var traceDirectory = Environment.GetEnvironmentVariable("TALBY_CONSUMER_TRACE_DIR");
        if (string.IsNullOrWhiteSpace(traceDirectory))
        {
            return;
        }

        Directory.CreateDirectory(traceDirectory);
        var trace = new
        {
            args = arguments,
            startTimeUtc = startedAt,
            endTimeUtc = endedAt,
            exitCode,
            projectPath,
        };
        var fileName = $"{startedAt.UtcDateTime:yyyyMMddTHHmmssfffffffZ}-{Guid.NewGuid():N}.json";
        await File.WriteAllTextAsync(Path.Combine(traceDirectory, fileName), JsonSerializer.Serialize(trace));
    }

'@
    $start = "    private static async Task<(int ExitCode, string Output)> Run(params string[] arguments)"
    $end = "    private static string FindRepository()"
    $source = Replace-Between $source $start $end $runner "process runner"
    Write-Utf8File $path (Normalize-Lf $source)
}

function Convert-HistoryToStaticHelper([string] $Original) {
    $lf = [char]10
    $oldClass = @'
[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class IncrementalBuildConsumerTests
'@
    $converted = Replace-ExactlyOnce $Original $oldClass "internal static class IncrementalBuildConsumerHistories" "incremental history class declaration"
    foreach ($method in $historyMethods) {
        $old = "    [Fact]" + $lf + "    public async Task " + $method + "("
        $new = "    public static async Task " + $method + "("
        $converted = Replace-ExactlyOnce $converted $old $new ("Fact declaration for " + $method)
    }
    return $converted
}

function Restore-HistoryDeclarations([string] $StaticHelper) {
    $lf = [char]10
    $originalClass = "internal static class IncrementalBuildConsumerHistories"
    $restoredClass = @'
[Trait("Category", "Integration")]
[Collection("SDK consumer builds")]
public class IncrementalBuildConsumerTests
'@
    $restored = Replace-ExactlyOnce $StaticHelper $originalClass $restoredClass "history helper class declaration"
    foreach ($method in $historyMethods) {
        $old = "    public static async Task " + $method + "("
        $new = "    [Fact]" + $lf + "    public async Task " + $method + "("
        $restored = Replace-ExactlyOnce $restored $old $new ("static declaration for " + $method)
    }
    return $restored
}

function Apply-OptionA([string] $TreeRoot, [string] $OriginalHistory) {
    $testRoot = Join-Path $TreeRoot "tests/Talby.Core.ResxAccess.IntegrationTests"
    $historyPath = Join-Path $testRoot "IncrementalBuildConsumerTests.cs"
    $helper = Convert-HistoryToStaticHelper $OriginalHistory
    Write-Utf8File $historyPath (Normalize-Lf $helper)

    $quote = [string][char]34
    $lines = [System.Collections.Generic.List[string]]::new()
    $lines.Add("[assembly: CollectionBehavior(MaxParallelThreads = 2)]")
    $lines.Add("")
    $lines.Add("namespace Talby.Core.ResxAccess.IntegrationTests;")
    $lines.Add("")
    foreach ($method in $historyMethods) {
        $collection = "Incremental history: " + $method
        $collectionClass = $method + "Collection"
        $testClass = $method + "Tests"
        $lines.Add("[CollectionDefinition(" + $quote + $collection + $quote + ")]")
        $lines.Add("public sealed class " + $collectionClass + " { }")
        $lines.Add("")
        $lines.Add("[Trait(" + $quote + "Category" + $quote + ", " + $quote + "Integration" + $quote + ")]")
        $lines.Add("[Collection(" + $quote + $collection + $quote + ")]")
        $lines.Add("public sealed class " + $testClass)
        $lines.Add("{")
        $lines.Add("    [Fact]")
        $lines.Add("    public Task " + $method + "() =>")
        $lines.Add("        IncrementalBuildConsumerHistories." + $method + "();")
        $lines.Add("}")
        $lines.Add("")
    }
    Write-Utf8File (Join-Path $testRoot "IncrementalBuildHistoryTests.cs") (([string]::Join([char]10, $lines)) + [char]10)

    $designTimePath = Join-Path $testRoot "DesignTimeResourceConsumerTests.cs"
    $designTime = [System.IO.File]::ReadAllText($designTimePath)
    $designTime = Replace-ExactlyOnce $designTime '[Collection("SDK consumer builds")]' '[Collection("Design-time consumer builds")]' "DesignTime collection"
    Write-Utf8File $designTimePath (Normalize-Lf $designTime)

    $collectionPath = Join-Path $testRoot "ConsumerBuildCollection.cs"
    $collectionSource = [System.IO.File]::ReadAllText($collectionPath)
    $collectionSource += [char]10 + '[CollectionDefinition("Design-time consumer builds")]' + [char]10 + 'public sealed class DesignTimeBuildCollection { }' + [char]10
    Write-Utf8File $collectionPath (Normalize-Lf $collectionSource)

    if ((Get-Sha256 $OriginalHistory) -ne (Get-Sha256 (Restore-HistoryDeclarations $helper))) {
        throw "Option A changed the normalized incremental history source."
    }
}

function Apply-OptionB([string] $TreeRoot) {
    $path = Join-Path $TreeRoot "tests/Talby.Core.ResxAccess.IntegrationTests/ConsumerProject.cs"
    $source = [System.IO.File]::ReadAllText($path)
    $lf = [char]10
    $source = Replace-ExactlyOnce $source ("using System.Security;" + $lf + "using System.Text.Json;") ("using System.Security;" + $lf + "using System.Security.Cryptography;" + $lf + "using System.Text.Json;") "restore fingerprint using"

    $classOpening = "internal sealed class ConsumerProject : IDisposable" + $lf + "{"
    $state = @'
    private bool _restoreComplete;
    private string? _restoredProjectFingerprint;
    private const string RestoreSuccessTarget = """
        <Target Name="RecordConsumerRestoreSuccess" AfterTargets="Restore">
          <MakeDir Directories="$(MSBuildProjectExtensionsPath)" />
          <WriteLinesToFile
            File="$(MSBuildProjectExtensionsPath)ResxAccess.RestoreSucceeded"
            Lines="$(MSBuildProjectFullPath)"
            Overwrite="true" />
        </Target>
        """;
'@
    $source = Replace-ExactlyOnce $source $classOpening ($classOpening + $lf + $state) "restore state and marker target"

    $projectTargets = "              {{projectTargets}}"
    $projectTargetsWithRestoreTarget = "              {{projectTargets}}" + $lf + "              {{RestoreSuccessTarget}}"
    $source = Replace-ExactlyOnce $source $projectTargets $projectTargetsWithRestoreTarget "consumer restore marker target import"

    $writeLine = "        File.WriteAllText(fullPath, text);"
    $invalidate = @'
        File.WriteAllText(fullPath, text);
        if (IsRestoreInput(path))
        {
            _restoreComplete = false;
            _restoredProjectFingerprint = null;
            var restoreMarkerPath = Path.Combine(DirectoryPath, "obj", "ResxAccess.RestoreSucceeded");
            if (File.Exists(restoreMarkerPath))
            {
                File.Delete(restoreMarkerPath);
            }
        }
'@
    $source = Replace-ExactlyOnce $source $writeLine $invalidate "restore graph invalidation"

    $buildStart = "    public async Task<(int ExitCode, string Output)> Build()"
    $invokeStart = "    public static async Task<(int ExitCode, string Output)> Invoke("
    $buildMethod = @'
    public async Task<(int ExitCode, string Output)> Build()
    {
        // The current Release solution build supplies the library and its Metalama outputs.
        // A consumer-local target records successful implicit restore without inspecting assets.
        var projectPath = Path.Combine(DirectoryPath, "Consumer.csproj");
        var projectFingerprint = GetProjectFingerprint(projectPath);
        var restoreMarkerPath = Path.Combine(DirectoryPath, "obj", "ResxAccess.RestoreSucceeded");
        var skipRestore =
            _restoreComplete
            && File.Exists(restoreMarkerPath)
            && StringComparer.Ordinal.Equals(_restoredProjectFingerprint, projectFingerprint);

        if (!skipRestore && File.Exists(restoreMarkerPath))
        {
            File.Delete(restoreMarkerPath);
        }

        var buildArguments = new List<string>
        {
            "build",
            projectPath,
            "--configuration",
            "Release",
            "--nologo",
            "--verbosity",
            "quiet",
        };
        if (skipRestore)
        {
            buildArguments.Add("--no-restore");
        }
        buildArguments.Add("-p:BuildProjectReferences=false");
        buildArguments.Add("-p:RestoreRecursive=false");

        var build = await Run([.. buildArguments]);
        if (File.Exists(restoreMarkerPath))
        {
            _restoreComplete = true;
            _restoredProjectFingerprint = GetProjectFingerprint(projectPath);
        }
        return build;
    }

'@
    $source = Replace-Between $source $buildStart $invokeStart $buildMethod "Build method"

    $dispose = "    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);"
    $helpers = @'
    private static string GetProjectFingerprint(string projectPath) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(projectPath)));

    private static bool IsRestoreInput(string path)
    {
        var fileName = Path.GetFileName(path);
        var extension = Path.GetExtension(path);
        return extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".fsproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".vbproj", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".props", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".targets", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".sln", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".slnx", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("NuGet.Config", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("packages.config", StringComparison.OrdinalIgnoreCase)
            || fileName.Equals("packages.lock.json", StringComparison.OrdinalIgnoreCase);
    }

'@
    $source = Replace-ExactlyOnce $source $dispose ($helpers + $dispose) "restore helper insertion"
    Write-Utf8File $path (Normalize-Lf $source)
}

function Write-IdentityMap([string] $TreeRoot, [bool] $HasOptionA) {
    $map = [ordered]@{}
    if ($HasOptionA) {
        foreach ($method in $historyMethods) {
            $newName = "Talby.Core.ResxAccess.IntegrationTests." + $method + "Tests." + $method
            $oldName = "Talby.Core.ResxAccess.IntegrationTests.IncrementalBuildConsumerTests." + $method
            $map[$newName] = $oldName
        }
    }
    Write-Utf8File (Join-Path $TreeRoot "identity-map.json") ((ConvertTo-Json -InputObject $map -Depth 10) + [char]10)
}

function Write-CoverageVerification([string] $TreeRoot, [string] $Variant, [string] $OriginalHistory, [bool] $HasOptionA, [string] $Commit) {
    $hash = Get-Sha256 $OriginalHistory
    $historyPath = Join-Path $TreeRoot "tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerTests.cs"
    $variantHash = Get-Sha256 ([System.IO.File]::ReadAllText($historyPath))
    if ($HasOptionA) {
        $historyPath = Join-Path $TreeRoot "tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerTests.cs"
        $variantHash = Get-Sha256 (Restore-HistoryDeclarations ([System.IO.File]::ReadAllText($historyPath)))
    }
    $verification = [ordered]@{
        sourceCommit = $Commit
        variant = $Variant
        originalHistoryCodeSha256 = $hash
        normalizedVariantHistoryCodeSha256 = $variantHash
        matchesOriginal = ($hash -eq $variantHash)
        normalization = "Reverse only the class, Fact, and static-method declaration changes used to split the six histories."
    }
    Write-Utf8File (Join-Path $TreeRoot "coverage-verification.json") ((ConvertTo-Json -InputObject $verification -Depth 10) + [char]10)
}

function Get-TreeHashes([string] $TreeRoot) {
    $files = [ordered]@{}
    foreach ($file in Get-ChildItem -LiteralPath $TreeRoot -File -Recurse -Force | Sort-Object FullName) {
        $relative = [System.IO.Path]::GetRelativePath($TreeRoot, $file.FullName).Replace("\", "/")
        $files[$relative] = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    return $files
}

$repository = (Resolve-Path -LiteralPath $RepositoryRoot).Path
if (-not (Test-Path -LiteralPath (Join-Path $repository "Talby.Core.ResxAccess.slnx") -PathType Leaf)) {
    throw "RepositoryRoot must contain Talby.Core.ResxAccess.slnx."
}
$headCommit = (& git -C $repository rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($headCommit)) {
    throw "Could not resolve git HEAD in RepositoryRoot."
}

$experiment = [System.IO.Path]::GetFullPath($ExperimentRoot)
$relativeExperiment = [System.IO.Path]::GetRelativePath($repository, $experiment)
$outsideRepository =
    [System.IO.Path]::IsPathRooted($relativeExperiment) -or
    $relativeExperiment -eq "." -or
    $relativeExperiment.StartsWith(".." + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::Ordinal)
if ($outsideRepository) {
    throw "ExperimentRoot must be a child directory of RepositoryRoot."
}
[System.IO.Directory]::CreateDirectory($experiment) | Out-Null
& git -C $repository check-ignore --quiet --no-index -- $relativeExperiment
if ($LASTEXITCODE -ne 0) {
    throw "ExperimentRoot must be ignored by git."
}

$archivePath = Join-Path $experiment ("source-head-" + $headCommit + ".zip")
$manifestPath = Join-Path $experiment "source-hashes.json"
$treeNames = @("baseline", "A", "B", "AB")
$treePaths = @{}
foreach ($name in $treeNames) {
    $treePaths[$name] = Join-Path $experiment $name
    if (Test-Path -LiteralPath $treePaths[$name]) {
        throw "Variant destination already exists: $($treePaths[$name])"
    }
}
if ((Test-Path -LiteralPath $archivePath) -or (Test-Path -LiteralPath $manifestPath)) {
    throw "The source archive or hash manifest already exists in ExperimentRoot."
}

& git -C $repository archive --format=zip ("--output=" + $archivePath) $headCommit
if ($LASTEXITCODE -ne 0) {
    throw "git archive failed for HEAD $headCommit."
}
Expand-Archive -LiteralPath $archivePath -DestinationPath $treePaths["baseline"]
foreach ($name in @("A", "B", "AB")) {
    Copy-SourceTree $treePaths["baseline"] $treePaths[$name]
}

$historyPath = Join-Path $treePaths["baseline"] "tests/Talby.Core.ResxAccess.IntegrationTests/IncrementalBuildConsumerTests.cs"
$originalHistory = [System.IO.File]::ReadAllText($historyPath)
foreach ($name in $treeNames) {
    Apply-TraceInstrumentation $treePaths[$name]
}
Apply-OptionA $treePaths["A"] $originalHistory
Apply-OptionA $treePaths["AB"] $originalHistory
Apply-OptionB $treePaths["B"]
Apply-OptionB $treePaths["AB"]

foreach ($name in $treeNames) {
    $hasOptionA = $name -eq "A" -or $name -eq "AB"
    Write-IdentityMap $treePaths[$name] $hasOptionA
    Write-CoverageVerification $treePaths[$name] $name $originalHistory $hasOptionA $headCommit
}

$treeHashes = [ordered]@{}
foreach ($name in $treeNames) {
    $treeHashes[$name] = Get-TreeHashes $treePaths[$name]
}
$manifest = [ordered]@{
    sourceCommit = $headCommit
    archive = [ordered]@{
        path = [System.IO.Path]::GetFileName($archivePath)
        sha256 = (Get-FileHash -LiteralPath $archivePath -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    hashAlgorithm = "SHA-256"
    trees = $treeHashes
}
Write-Utf8File $manifestPath ((ConvertTo-Json -InputObject $manifest -Depth 100) + [char]10)

Write-Output ("Created frozen IntegrationTests trees at " + $experiment)
Write-Output ("Source commit: " + $headCommit)
Write-Output ("Source archive: " + $archivePath)
Write-Output ("Source hashes: " + $manifestPath)
