param(
    [Parameter(Mandatory)] [string] $ExperimentRoot,
    [Parameter(Mandatory)] [string] $EvidenceDirectory
)

$ErrorActionPreference = 'Stop'
$experiment = (Resolve-Path -LiteralPath $ExperimentRoot).Path
$null = New-Item -ItemType Directory -Force -Path $EvidenceDirectory

function Get-Peak([object[]] $Intervals)
{
    if ($Intervals.Count -eq 0) { return 0 }
    $events = @(foreach ($interval in $Intervals)
    {
        [pscustomobject]@{ Time = [DateTimeOffset]::Parse($interval.Start); Change = 1 }
        [pscustomobject]@{ Time = [DateTimeOffset]::Parse($interval.End); Change = -1 }
    }) | Sort-Object Time, Change
    $active = 0
    $peak = 0
    foreach ($change in $events)
    {
        $active += $change.Change
        $peak = [Math]::Max($peak, $active)
    }
    return $peak
}

$rows = @(foreach ($comparison in @('A', 'B', 'AB'))
{
    foreach ($variant in @('baseline', $comparison))
    {
        $directory = Join-Path $experiment "comparisons/$comparison/warmup-Integration-$variant-0"
        $traceDirectory = Join-Path $directory 'trace'
        $inventoryPath = Join-Path $directory 'inventory.csv'
        if (-not (Test-Path -LiteralPath $inventoryPath)) { continue }
        $traces = @(Get-ChildItem -LiteralPath $traceDirectory -Filter '*.json' -File | ForEach-Object {
            Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
        })
        if ($traces.Count -eq 0) { throw "Missing command traces: $directory" }
        $builds = @($traces | Where-Object { $_.args[0] -eq 'build' })
        $designTime = @($traces | Where-Object { $_.args[0] -eq 'msbuild' })
        $runtimes = @($traces | Where-Object { $_.args[0].EndsWith('.dll', [StringComparison]::OrdinalIgnoreCase) })
        $firstBuilds = @(foreach ($group in @($builds | Group-Object projectPath))
        {
            $group.Group | Sort-Object { [DateTimeOffset]::Parse($_.startTimeUtc) } | Select-Object -First 1
        })
        $subsequentBuilds = @($builds | Where-Object { $_ -notin $firstBuilds })
        $firstSkipping = @($firstBuilds | Where-Object { $_.args -contains '--no-restore' }).Count
        $subsequentRestoring = @($subsequentBuilds | Where-Object { $_.args -notcontains '--no-restore' }).Count
        if ($firstSkipping -ne 0) { throw "A fresh consumer skipped restore: $directory" }
        if ($variant -in @('B', 'AB') -and $subsequentRestoring -ne 0)
        { throw "A restored unchanged consumer repeated restore: $directory" }
        $commandIntervals = @($builds + $designTime | ForEach-Object {
            [pscustomobject]@{ Start = $_.startTimeUtc.ToString('o'); End = $_.endTimeUtc.ToString('o') }
        })
        $histories = @(Import-Csv -LiteralPath $inventoryPath | Where-Object { $_.CanonicalIdentity -like '*.IncrementalBuildConsumerTests.*' })
        $historyIntervals = @($histories | ForEach-Object { [pscustomobject]@{ Start = $_.StartTime; End = $_.EndTime } })
        $peakHistories = Get-Peak $historyIntervals
        $peakBuilds = Get-Peak $commandIntervals
        if ($variant -in @('A', 'AB') -and $peakHistories -gt 2) { throw "History concurrency exceeded two: $directory" }
        [pscustomobject]@{
            Comparison = $comparison; Variant = $variant; Commands = $traces.Count
            BuildCommands = $builds.Count; DesignTimeCommands = $designTime.Count; RuntimeCommands = $runtimes.Count
            FreshConsumers = $firstBuilds.Count; SubsequentBuilds = $subsequentBuilds.Count
            FirstBuildsSkippingRestore = $firstSkipping; SubsequentBuildsWithRestore = $subsequentRestoring
            BuildCommandsNoRestore = @($builds | Where-Object { $_.args -contains '--no-restore' }).Count
            PeakIndependentHistories = $peakHistories; PeakBuildCommands = $peakBuilds
        }
    }
})
$rows | Export-Csv -LiteralPath (Join-Path $EvidenceDirectory 'command-traces.csv') -NoTypeInformation
$rows | Format-Table -AutoSize
