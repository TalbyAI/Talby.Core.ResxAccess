#requires -Version 7.0
<#
.SYNOPSIS
Prepares, publishes, or verifies the NuGet release described in docs/releasing.md.
.PARAMETER Version
Optional assertion against the library project's Version. Never overrides it.
.PARAMETER Plan
Shows the resolved release and stages without commands, prompts, or browser opening.
#>
[CmdletBinding()]
param(
    [ValidateSet('Prepare', 'Website', 'Cli', 'Verify')]
    [string] $Mode = 'Prepare',
    [string] $Version,
    [switch] $Plan
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root 'src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj'
$solution = 'Talby.Core.ResxAccess.slnx'
$integrationProject = 'tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj'
$nugetSource = 'https://api.nuget.org/v3/index.json'
$owner = 'TalbyAI'
$stageIndex = 0

function Get-ReleaseMetadata([string] $ProjectPath, [string] $ExpectedVersion)
{
    [xml] $xml = Get-Content -LiteralPath $ProjectPath -Raw
    $versions = @($xml.SelectNodes('/Project/PropertyGroup/Version'))
    $ids = @($xml.SelectNodes('/Project/PropertyGroup/PackageId'))
    if ($versions.Count -ne 1 -or $ids.Count -ne 1 -or
        [string]::IsNullOrWhiteSpace($versions[0].InnerText) -or
        [string]::IsNullOrWhiteSpace($ids[0].InnerText))
    {
        throw 'The project must declare one explicit Version and PackageId.'
    }
    $actualVersion = $versions[0].InnerText.Trim()
    $packageId = $ids[0].InnerText.Trim()
    if ($actualVersion -notmatch '^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$' -or
        $packageId -notmatch '^[0-9A-Za-z_.-]+$')
    {
        throw 'Use a three-part NuGet Version with an optional prerelease suffix and a valid PackageId.'
    }
    if ($ExpectedVersion -and $ExpectedVersion -cne $actualVersion)
    {
        throw "Requested Version '$ExpectedVersion' does not match project Version '$actualVersion'. Update the project XML first."
    }
    [pscustomobject]@{ PackageId = $packageId; Version = $actualVersion }
}

function Invoke-Native([string] $Command, [string[]] $Arguments)
{
    Write-Host "> $Command $($Arguments -join ' ')" -ForegroundColor DarkGray
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE. Release stopped." }
}

function Show-Stage([string] $Name)
{
    $script:stageIndex++
    if (-not [Console]::IsOutputRedirected) { Clear-Host }
    Write-Host "Stage $stageIndex/$totalStages - $Name" -ForegroundColor Cyan
    Write-Host "$($metadata.PackageId) $($metadata.Version) | $Mode"
}

function Confirm-Step([string] $Prompt)
{
    if ((Read-Host "$Prompt [y/N]") -notmatch '^(y|yes)$')
    {
        throw 'Stopped by the publisher. No further stages were run.'
    }
}

function Open-Url([string] $Url)
{
    Write-Host "Open: $Url"
    try
    {
        if ($IsWindows) { Start-Process $Url }
        elseif ($IsMacOS) { Invoke-Native 'open' @($Url) | Out-Host }
        else { Invoke-Native 'xdg-open' @($Url) | Out-Host }
    }
    catch { Write-Warning "Could not open a browser. Visit $Url manually." }
}

function Assert-PackageHash([string] $PackagePath, [string] $ExpectedHash)
{
    $sidecar = (Get-Content -LiteralPath "$PackagePath.sha256" -Raw).Trim()
    $actual = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
    if ($ExpectedHash -notmatch '^[0-9a-fA-F]{64}$' -or
        $sidecar -ne $ExpectedHash -or $actual -ne $ExpectedHash)
    {
        throw 'The package checksum differs from the validated release artifact. Run Prepare again.'
    }
}

function Assert-PackageIdentity([string] $PackagePath, [string] $PackageId, [string] $PackageVersion)
{
    $archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
    try
    {
        $manifests = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec') })
        if ($manifests.Count -ne 1) { throw 'The package must contain one nuspec.' }
        $reader = [IO.StreamReader]::new($manifests[0].Open())
        try { [xml] $manifest = $reader.ReadToEnd() }
        finally { $reader.Dispose() }
        if ($manifest.package.metadata.id -cne $PackageId -or
            $manifest.package.metadata.version -cne $PackageVersion)
        {
            throw 'The archive identity does not match the project PackageId and Version.'
        }
    }
    finally { $archive.Dispose() }
}

function Assert-PassedTests([string] $ResultsDirectory)
{
    $files = @(Get-ChildItem -LiteralPath $ResultsDirectory -Filter '*.trx')
    if (-not $files.Count) { throw 'No TRX results from release archive validation.' }
    foreach ($file in $files)
    {
        [xml] $trx = Get-Content -LiteralPath $file.FullName -Raw
        if (-not $trx.TestRun.Results.UnitTestResult) { throw 'No test results from release archive validation.' }
        foreach ($result in $trx.TestRun.Results.UnitTestResult)
        {
            if ($result.outcome -ne 'Passed') { throw "Test did not pass: $($result.testName) ($($result.outcome))." }
        }
    }
}

function Test-PublishedConsumer
{
    # Each attempt has a new consumer, local cache, and a source configuration
    # containing only NuGet.org. A locally packed archive cannot satisfy restore.
    $attempt = Join-Path $root "artifacts/nuget/published-$([Guid]::NewGuid().ToString('N'))"
    $consumer = Join-Path $attempt 'consumer'
    $cache = Join-Path $attempt 'packages'
    Invoke-Native 'dotnet' @('new', 'console', '--framework', 'net10.0', '--output', $consumer, '--no-restore') | Out-Host
    $config = Join-Path $consumer 'NuGet.Config'
    Set-Content -LiteralPath $config -Value @'
<configuration>
  <packageSources><clear /><add key="nuget.org" value="https://api.nuget.org/v3/index.json" /></packageSources>
  <packageSourceMapping><clear /><packageSource key="nuget.org"><package pattern="*" /></packageSource></packageSourceMapping>
</configuration>
'@
    $consumerProject = Join-Path $consumer 'consumer.csproj'
    [xml] $xml = Get-Content -LiteralPath $consumerProject -Raw
    $cacheElement = $xml.CreateElement('RestorePackagesPath')
    $cacheElement.InnerText = $cache
    $null = $xml.Project.PropertyGroup.AppendChild($cacheElement)
    $items = $xml.CreateElement('ItemGroup')
    $reference = $xml.CreateElement('PackageReference')
    $reference.SetAttribute('Include', $metadata.PackageId)
    $reference.SetAttribute('Version', $metadata.Version)
    $null = $items.AppendChild($reference)
    $null = $xml.Project.AppendChild($items)
    $xml.Save($consumerProject)
    $resources = Join-Path $consumer 'Resources'
    $null = New-Item -ItemType Directory -Path $resources
    Set-Content -LiteralPath (Join-Path $resources 'Labels.resx') -Value @'
<root>
  <data name="Welcome"><value>Hello {name@string}, {amount@decimal:N2}!</value></data>
  <data name="Plain"><value>Hello</value></data>
</root>
'@
    Set-Content -LiteralPath (Join-Path $resources 'Labels.es.resx') -Value @'
<root>
  <data name="Welcome"><value>Hola {name}, {amount:N2}!</value></data>
  <data name="Plain"><value>Hola</value></data>
</root>
'@
    Set-Content -LiteralPath (Join-Path $consumer 'Texts.cs') -Value @'
using Talby.Core.ResxAccess;
[GenerateResxAccess("Resources/Labels.resx", ExpectedCultures = new[] { "es" })]
internal static class Texts { }
'@
    Set-Content -LiteralPath (Join-Path $consumer 'Program.cs') -Value @'
using System.Globalization;
CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("en-US");
CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
var es = CultureInfo.GetCultureInfo("es-AR");
var fr = CultureInfo.GetCultureInfo("fr-FR");
var actual = new[] {
    Texts.Welcome(), Texts.Welcome(es), Texts.FormatWelcome("Ada", 12.5m),
    Texts.FormatWelcome("Ada", 12.5m, es, fr), Texts.Plain(es),
    Texts.Plain(CultureInfo.GetCultureInfo("de"))
};
var expected = new[] {
    "Hello {name@string}, {amount@decimal:N2}!", "Hola {name}, {amount:N2}!",
    "Hello Ada, 12.50!", "Hola Ada, 12,50!", "Hola", "Hello"
};
if (!actual.SequenceEqual(expected)) throw new Exception(string.Join("\n", actual));
Console.WriteLine("Published package consumer passed.");
'@
    Invoke-Native 'dotnet' @('restore', $consumerProject, '--configfile', $config, '--packages', $cache, '--no-http-cache') | Out-Host
    Invoke-Native 'dotnet' @('build', $consumerProject, '--configuration', 'Release', '--no-restore') | Out-Host
    Invoke-Native 'dotnet' @('run', '--project', $consumerProject, '--configuration', 'Release', '--no-build', '--no-restore') | Out-Host
    Write-Host "Consumer and fresh cache retained at $attempt"
}

$metadata = Get-ReleaseMetadata $project $Version
$package = Join-Path $root "artifacts/nuget/$($metadata.PackageId).$($metadata.Version).nupkg"
$evidencePath = "$package.release.json"
$packageUrl = "https://www.nuget.org/packages/$($metadata.PackageId)/$($metadata.Version)"
$tag = "v$($metadata.Version)"
$stages = switch ($Mode)
{
    'Prepare' { @('Check release metadata', 'Restore, formatting, build and full tests', 'Pack and validate the exact archive', 'Record checksum and hand off for review') }
    'Website' { @('Check validated artifact', 'Confirm source publication and ownership', 'Upload and submit through NuGet.org', 'Verify the indexed package') }
    'Cli' { @('Check validated artifact', 'Confirm source publication and ownership', 'Publish with a scoped API key', 'Verify the indexed package') }
    'Verify' { @('Check release metadata', 'Confirm public version and owner', 'Verify with a fresh PackageReference consumer') }
}
$totalStages = $stages.Count
if ($Plan)
{
    [pscustomobject]@{ Mode = $Mode; PackageId = $metadata.PackageId; Version = $metadata.Version; Package = $package; Evidence = $evidencePath; Tag = $tag; Url = $packageUrl; Stages = $stages }
    return
}

Push-Location $root
try
{
    Show-Stage $stages[0]
    foreach ($command in @('dotnet', 'git')) { $null = Get-Command $command -ErrorAction Stop }
    # Detect imported/environment overrides instead of silently packing a different version.
    $evaluated = (Invoke-Native 'dotnet' @('msbuild', $project, '-nologo', '-property:Configuration=Release', '-getProperty:PackageId,PackageVersion,Version') | Out-String | ConvertFrom-Json).Properties
    if ($evaluated.PackageId -cne $metadata.PackageId -or $evaluated.PackageVersion -cne $metadata.Version -or $evaluated.Version -cne $metadata.Version)
    {
        throw 'Evaluated MSBuild metadata differs from the project XML. Remove Version/PackageVersion/PackageId overrides.'
    }
    $revision = (Invoke-Native 'git' @('rev-parse', 'HEAD') | Out-String).Trim()
    $clean = [string]::IsNullOrWhiteSpace((Invoke-Native 'git' @('status', '--porcelain') | Out-String))
    Write-Host "Package: $package"
    Write-Host "Source revision: $revision | Working tree clean: $clean"
    if ($Mode -ne 'Verify' -and (Test-Path -LiteralPath "$package.publication.json"))
    {
        throw 'A submission is already recorded for this version. Use -Mode Verify, or increment Version for a new release.'
    }

    if ($Mode -eq 'Prepare')
    {
        if (Test-Path Env:TALBY_TEST_PACKAGE) { throw 'Use a shell without an existing TALBY_TEST_PACKAGE override.' }
        foreach ($command in @('npm', 'node', 'pwsh')) { $null = Get-Command $command -ErrorAction Stop }
        $nodeVersion = (Invoke-Native 'node' @('--version') | Out-String).Trim()
        if ($nodeVersion -notmatch '^v24\.') { throw 'Node.js 24 is required.' }
        Confirm-Step 'Prepare this version, replacing its existing local archive and validation evidence?'
        # Invalidate previous proof before any new attempt, including a failed build.
        foreach ($proof in @("$package.sha256", $evidencePath))
        {
            if (Test-Path -LiteralPath $proof) { Remove-Item -LiteralPath $proof }
        }
        Show-Stage $stages[1]
        Invoke-Native 'dotnet' @('tool', 'restore') | Out-Host
        Invoke-Native 'npm' @('ci') | Out-Host
        Invoke-Native 'npm' @('run', 'format:check') | Out-Host
        Invoke-Native 'dotnet' @('restore', $solution) | Out-Host
        Invoke-Native 'dotnet' @('build', $solution, '--configuration', 'Release', '--no-restore') | Out-Host
        Invoke-Native 'pwsh' @('-NoProfile', '-File', 'tests/run.ps1', '-Mode', 'full') | Out-Host
        Show-Stage $stages[2]
        Invoke-Native 'dotnet' @('pack', $project, '--configuration', 'Release', '--no-build', '--no-restore', '--output', (Split-Path $package -Parent)) | Out-Host
        Assert-PackageIdentity $package $metadata.PackageId $metadata.Version
        $validatedHash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
        $results = Join-Path $root "test-results/release/$([Guid]::NewGuid().ToString('N'))"
        $null = New-Item -ItemType Directory -Path $results
        $env:TALBY_TEST_PACKAGE = $package
        try
        {
            Invoke-Native 'dotnet' @('test', $integrationProject, '--configuration', 'Release', '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~NuGetPackageConsumerTests', '--logger', 'trx', '--results-directory', $results) | Out-Host
            Assert-PassedTests $results
        }
        finally { Remove-Item Env:TALBY_TEST_PACKAGE }
        if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $validatedHash) { throw 'The archive changed during validation.' }
        $finalRevision = (Invoke-Native 'git' @('rev-parse', 'HEAD') | Out-String).Trim()
        if ($finalRevision -cne $revision) { throw 'HEAD changed during preparation. Run Prepare again at the intended revision.' }
        $clean = $clean -and [string]::IsNullOrWhiteSpace((Invoke-Native 'git' @('status', '--porcelain') | Out-String))
        Show-Stage $stages[3]
        Set-Content -LiteralPath "$package.sha256" -Value $validatedHash -Encoding ascii
        $evidence = [ordered]@{
            PackageId = $metadata.PackageId; Version = $metadata.Version
            SourceRevision = $revision; WorkingTreeClean = $clean
            SHA256 = $validatedHash; Size = (Get-Item -LiteralPath $package).Length
            ValidatedAtUtc = [DateTime]::UtcNow.ToString('o'); ArchiveTestResults = $results
            Validation = 'Formatting, Release build, full suite, exact archive consumer tests passed'
        }
        $evidence | ConvertTo-Json | Set-Content -LiteralPath $evidencePath -Encoding utf8
        Write-Host "Validated package: $package"
        Write-Host "SHA-256: $validatedHash"
        Write-Host "Evidence: $evidencePath"
        Write-Host 'Review the diff and retain changes on a working branch; follow the normal review/merge process.'
        Write-Host 'After merge, run Prepare again from the clean published source revision before publishing.'
        Write-Host "Record and push tag $tag at that revision. Update .scratch/nuget-release/validation.md with the evidence."
        if ($clean)
        {
            Write-Host "Tag commands after source publication: git tag -a $tag $revision -m 'Release $($metadata.Version)'"
            Write-Host "Then: git push origin $tag"
        }
        Write-Host 'Then run: pwsh -NoProfile -File scripts/release.ps1 -Mode Website (or -Mode Cli).'
        return
    }

    if ($Mode -in @('Website', 'Cli'))
    {
        $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
        if ($evidence.PackageId -cne $metadata.PackageId -or $evidence.Version -cne $metadata.Version) { throw 'Validation evidence is for another release.' }
        Assert-PackageIdentity $package $metadata.PackageId $metadata.Version
        Assert-PackageHash $package $evidence.SHA256
        if (-not $clean -or $evidence.WorkingTreeClean -ne $true -or $evidence.SourceRevision -cne $revision)
        {
            throw 'Publish requires a clean working tree and an artifact prepared at HEAD. Commit/merge changes and run Prepare again.'
        }
        Show-Stage $stages[1]
        $tagRevision = (Invoke-Native 'git' @('rev-parse', '--verify', "refs/tags/${tag}^{commit}") | Out-String).Trim()
        if ($tagRevision -cne $revision) { throw "Tag $tag must identify validated source revision $revision. Check the tag before publishing." }
        Confirm-Step "Have review/merge completed and source revision $revision and tag $tag been pushed publicly?"
        Open-Url 'https://www.nuget.org/'
        Write-Host "Sign in with your Microsoft-linked account and confirm control of $owner."
        Write-Host 'For an organization, check Manage Organizations and your package management membership.'
        Confirm-Step "Can you select the authorized $owner Package Owner?"
        Show-Stage $stages[2]
        if ($Mode -eq 'Website')
        {
            Open-Url 'https://www.nuget.org/packages/manage/upload'
            Write-Host "Select exactly: $package"
            Write-Host "Check Package ID $($metadata.PackageId), Version $($metadata.Version), Authors $owner, MIT, net10.0, repository URL and Metalama.Framework dependency."
            Write-Host "Select $owner as Package Owner, preview the README and check examples and links."
            Confirm-Step 'Are the owner, metadata and README correct, and are you ready to select Submit to publish this version?'
            Assert-PackageHash $package $evidence.SHA256
            Write-Host 'Select Submit in the browser. Use a new version for subsequent changes.'
            Confirm-Step 'Did NuGet accept your submission?'
        }
        else
        {
            if (Test-Path Env:NUGET_API_KEY) { throw 'Use a shell without an existing NUGET_API_KEY override.' }
            Open-Url 'https://www.nuget.org/account/apikeys'
            Write-Host "Create a scoped API key: Package Owner $owner, Push new packages and package versions, package pattern $($metadata.PackageId)."
            Confirm-Step "Publish $($metadata.PackageId) $($metadata.Version) to NuGet.org using a key owned by $owner?"
            $env:NUGET_API_KEY = Read-Host 'NuGet API key' -MaskInput
            try
            {
                if ([string]::IsNullOrWhiteSpace($env:NUGET_API_KEY)) { throw 'An API key is required.' }
                Assert-PackageHash $package $evidence.SHA256
                Invoke-Native 'dotnet' @('nuget', 'push', $package, '--source', $nugetSource) | Out-Host
            }
            finally { Remove-Item Env:NUGET_API_KEY }
        }
        # Persist publication evidence before indexing checks, so a delayed index
        # or failed consumer restore never leads to accidentally publishing twice.
        $publication = [ordered]@{ Url = $packageUrl; Owner = $owner; Version = $metadata.Version; SourceRevision = $revision; SHA256 = $evidence.SHA256; SubmittedAtUtc = [DateTime]::UtcNow.ToString('o') }
        $publication | ConvertTo-Json | Set-Content -LiteralPath "$package.publication.json" -Encoding utf8
        Show-Stage $stages[3]
    }
    else { Show-Stage $stages[1] }

    Open-Url $packageUrl
    Write-Host "Wait for NuGet validation and indexing. Confirm Version $($metadata.Version) and owner $owner on the public page."
    Write-Host 'If indexing is delayed, stop here and rerun with -Mode Verify later.'
    Confirm-Step 'Is this version indexed with the correct owner?'
    if ($Mode -eq 'Verify') { Show-Stage $stages[2] }
    Test-PublishedConsumer
    Write-Host "Verified public package: $packageUrl" -ForegroundColor Green
}
finally { Pop-Location }
