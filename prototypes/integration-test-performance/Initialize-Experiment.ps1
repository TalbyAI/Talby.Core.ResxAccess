param([string] $RepositoryRoot = (Join-Path $PSScriptRoot '../..'))

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$repository = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$experiment = Join-Path $repository "test-results/integration-exploration-$([Guid]::NewGuid().ToString('N').Substring(0, 8))"
& (Join-Path $PSScriptRoot 'New-Variants.ps1') -RepositoryRoot $repository -ExperimentRoot $experiment
$experiment | Set-Content -LiteralPath (Join-Path $repository 'test-results/integration-exploration-latest.txt')
$environmentRecord = [ordered]@{
    RecordedUtc = [DateTime]::UtcNow.ToString('o')
    SourceCommit = (& git -C $repository rev-parse HEAD)
    SDK = (& dotnet --version)
    OS = [Environment]::OSVersion.VersionString
    Architecture = [Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    LogicalProcessors = [Environment]::ProcessorCount
    NuGetPackages = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else { Join-Path $env:USERPROFILE '.nuget/packages' }
    ConsumerWorkingDirectory = [IO.Path]::GetTempPath()
    Configuration = 'Release'
    TimedCommandLogging = 'normal/TRX for tests; minimal for solution builds; quiet for child consumer builds'
}
$environmentRecord | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $experiment 'environment.json')
foreach ($variant in @('baseline', 'A', 'B', 'AB'))
{
    $tree = Join-Path $experiment $variant
    Push-Location $tree
    try
    {
        Write-Output "PREPARE $variant restore $(Get-Date -Format o)"
        & dotnet restore Talby.Core.ResxAccess.slnx *> (Join-Path $experiment "restore-$variant.log")
        if ($LASTEXITCODE -ne 0) { throw "Restore failed: $experiment/restore-$variant.log" }
        Write-Output "PREPARE $variant build $(Get-Date -Format o)"
        & dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity minimal *> (Join-Path $experiment "build-$variant.log")
        if ($LASTEXITCODE -ne 0) { throw "Build failed: $experiment/build-$variant.log" }
    }
    finally { Pop-Location }
}
Write-Output "READY $experiment"
