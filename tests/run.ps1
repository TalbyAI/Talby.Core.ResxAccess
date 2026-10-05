param(
    [Parameter(Mandatory)]
    [ValidateSet('fast', 'full')]
    [string] $Mode
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = Split-Path $PSScriptRoot -Parent
$fastProjects = @(
    'tests/Talby.Core.ResxAccess.UnitTests/Talby.Core.ResxAccess.UnitTests.csproj'
    'tests/Talby.Core.ResxAccess.AspectTests/Talby.Core.ResxAccess.AspectTests.csproj'
)
$projects = if ($Mode -eq 'fast')
{
    Write-Host 'FAST: running the selected test projects; other solution test projects are deferred (SDK embedding and runtime lookup are not checked):'
    $fastProjects | ForEach-Object { Write-Host "  $_" }
    $fastProjects
}
else
{
    'Talby.Core.ResxAccess.slnx'
}
if (-not $projects) { throw "No test projects selected for $Mode." }
$results = Join-Path $root "test-results/execution/$Mode-$([Guid]::NewGuid().ToString('N'))"
$arguments = @(
    '--configuration', 'Release', '--no-build', '--no-restore'
    '--verbosity', 'normal', '--logger', 'trx;LogFilePrefix=execution'
)
$passed = 0
Write-Host "Results: $results"

Push-Location $root
try
{
    foreach ($project in $projects)
    {
        $projectResults = Join-Path $results ([IO.Path]::GetFileNameWithoutExtension($project))
        $null = New-Item -ItemType Directory -Path $projectResults
        & dotnet test $project @arguments --results-directory $projectResults
        if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

        $trxFiles = @(Get-ChildItem -LiteralPath $projectResults -Filter '*.trx')
        if ($trxFiles.Count -eq 0) { throw "No TRX results for $project." }
        foreach ($file in $trxFiles)
        {
            [xml] $trx = Get-Content -LiteralPath $file.FullName -Raw
            if (-not $trx.TestRun.Results.UnitTestResult) { throw "No test results in $($file.FullName)." }
            foreach ($result in $trx.TestRun.Results.UnitTestResult)
            {
                if ($result.outcome -ne 'Passed') { throw "Test did not pass: $($result.testName) ($($result.outcome))" }
                $passed++
            }
        }
    }
    Write-Host "Verified $Mode results: $passed passed. Results: $results"
}
finally
{
    Pop-Location
}
exit 0
