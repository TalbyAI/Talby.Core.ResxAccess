param([Parameter(Mandatory)] [string] $ExperimentRoot)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$experiment = (Resolve-Path -LiteralPath $ExperimentRoot).Path
$evidence = Join-Path $PSScriptRoot '../../.scratch/integration-test-performance/results'
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Summarize-Comparisons.ps1') -ExperimentRoot $experiment -EvidenceDirectory $evidence -AllowPartial
if ($LASTEXITCODE -ne 0) { throw 'Isolated comparison summary failed.' }
$isolated = @(Import-Csv -LiteralPath (Join-Path $evidence 'summary.csv') | Where-Object Mode -eq 'Integration')
foreach ($option in @('A', 'B'))
{
    $result = @($isolated | Where-Object Comparison -eq $option)
    if ($result.Count -ne 1 -or [double] $result[0].SavedPercent -le 0)
    { throw "Isolated $option results do not support combination; review evidence before proceeding." }
}
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Run-Comparison.ps1') -ExperimentRoot $experiment -Candidate AB -Phase Integration
if ($LASTEXITCODE -ne 0) { throw 'A+B Integration comparison failed.' }
foreach ($option in @('A', 'B', 'AB'))
{
    & pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Run-Comparison.ps1') -ExperimentRoot $experiment -Candidate $option -Phase Validation
    if ($LASTEXITCODE -ne 0) { throw "$option full validation failed." }
}
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Summarize-Comparisons.ps1') -ExperimentRoot $experiment -EvidenceDirectory $evidence
if ($LASTEXITCODE -ne 0) { throw 'Complete comparison summary failed.' }
& pwsh -NoProfile -File (Join-Path $PSScriptRoot 'Analyze-Traces.ps1') -ExperimentRoot $experiment -EvidenceDirectory $evidence
if ($LASTEXITCODE -ne 0) { throw 'Command trace verification failed.' }
Write-Output "COMPLETE EXPERIMENT: $experiment"
