param(
    [Parameter(Mandatory)] [string] $ExperimentRoot,
    [Parameter(Mandatory)] [string] $EvidenceDirectory,
    [switch] $AllowPartial
)

$ErrorActionPreference = 'Stop'
$experiment = (Resolve-Path -LiteralPath $ExperimentRoot).Path
$null = New-Item -ItemType Directory -Force -Path $EvidenceDirectory
$evidence = (Resolve-Path -LiteralPath $EvidenceDirectory).Path

function Get-Median([double[]] $Values)
{
    $sorted = @($Values | Sort-Object)
    if ($sorted.Count -eq 0) { throw 'No samples.' }
    $middle = [int] [Math]::Floor($sorted.Count / 2)
    if ($sorted.Count % 2) { return $sorted[$middle] }
    return ($sorted[$middle - 1] + $sorted[$middle]) / 2
}

$samples = @(Get-ChildItem -LiteralPath (Join-Path $experiment 'comparisons') -Filter 'timings.csv' -Recurse -File |
    ForEach-Object { Import-Csv -LiteralPath $_.FullName })
if ($samples.Count -eq 0) { throw 'No timing evidence.' }
if (-not $AllowPartial)
{
    foreach ($comparison in @('A', 'B', 'AB'))
    {
        foreach ($mode in @('Integration', 'Aspect', 'Fast', 'Full', 'WarmFull'))
        {
            foreach ($variant in @('baseline', $comparison))
            {
                $observed = @($samples | Where-Object {
                    $_.Comparison -eq $comparison -and $_.Mode -eq $mode -and $_.Variant -eq $variant -and $_.Stage -eq 'paired'
                })
                if ($observed.Count -ne 5 -or (Compare-Object @(1..5) @($observed | ForEach-Object { [int] $_.Pair })))
                { throw "Incomplete paired series: $comparison/$mode/$variant" }
            }
        }
        $clean = @($samples | Where-Object { $_.Comparison -eq $comparison -and $_.Stage -eq 'clean' })
        if ($clean.Count -ne 2 -or (Compare-Object @('baseline', $comparison) @($clean.Variant)))
        { throw "Incomplete clean-output comparison: $comparison" }
    }
}
else { Write-Output 'PARTIAL PROGRESS SUMMARY: incomplete modes are excluded; this is not acceptance evidence.' }
if (@($samples | Where-Object { [int] $_.BuildExit -ne 0 -or [int] $_.TestExit -ne 0 }).Count -ne 0)
{ throw 'Nonzero exit code in timing evidence.' }
$samples | Export-Csv -LiteralPath (Join-Path $evidence 'timings.csv') -NoTypeInformation
$pairedDeltas = [Collections.Generic.List[object]]::new()
$summary = @(foreach ($group in @($samples | Where-Object Stage -eq 'paired' | Group-Object Comparison, Mode))
{
    $comparison = $group.Group[0].Comparison
    $mode = $group.Group[0].Mode
    $baseline = @($group.Group | Where-Object Variant -eq 'baseline' | Sort-Object { [int] $_.Pair })
    $candidate = @($group.Group | Where-Object Variant -eq $comparison | Sort-Object { [int] $_.Pair })
    if ($baseline.Count -ne 5 -or $candidate.Count -ne 5)
    {
        if (-not $AllowPartial) { throw "Incomplete series: $comparison/$mode" }
        Write-Warning "Excluded incomplete series: $comparison/$mode"
        continue
    }
    foreach ($index in 0..4)
    {
        if ($baseline[$index].Pair -ne $candidate[$index].Pair) { throw "Unpaired samples: $comparison/$mode" }
        $baselineSeconds = [double] $baseline[$index].TotalSeconds
        $candidateSeconds = [double] $candidate[$index].TotalSeconds
        $pairedDeltas.Add([pscustomobject]@{
            Comparison = $comparison; Mode = $mode; Pair = $baseline[$index].Pair
            BaselineSeconds = $baselineSeconds; CandidateSeconds = $candidateSeconds
            SavedSeconds = $baselineSeconds - $candidateSeconds
            SavedPercent = 100 * ($baselineSeconds - $candidateSeconds) / $baselineSeconds
            BuildSavedSeconds = [double] $baseline[$index].BuildSeconds - [double] $candidate[$index].BuildSeconds
            TestSavedSeconds = [double] $baseline[$index].TestSeconds - [double] $candidate[$index].TestSeconds
        })
    }
    $baselineValues = @($baseline | ForEach-Object { [double] $_.TotalSeconds })
    $candidateValues = @($candidate | ForEach-Object { [double] $_.TotalSeconds })
    $baselineMedian = Get-Median $baselineValues
    $candidateMedian = Get-Median $candidateValues
    $deltas = @($pairedDeltas | Where-Object { $_.Comparison -eq $comparison -and $_.Mode -eq $mode })
    [pscustomobject]@{
        Comparison = $comparison; Mode = $mode; Pairs = 5
        BaselineMedianSeconds = $baselineMedian; CandidateMedianSeconds = $candidateMedian
        BaselineMinSeconds = ($baselineValues | Measure-Object -Minimum).Minimum
        BaselineMaxSeconds = ($baselineValues | Measure-Object -Maximum).Maximum
        CandidateMinSeconds = ($candidateValues | Measure-Object -Minimum).Minimum
        CandidateMaxSeconds = ($candidateValues | Measure-Object -Maximum).Maximum
        SavedSeconds = $baselineMedian - $candidateMedian
        SavedPercent = 100 * ($baselineMedian - $candidateMedian) / $baselineMedian
        PairedMedianSavedSeconds = Get-Median @($deltas.SavedSeconds)
        PairedMinSavedPercent = ($deltas.SavedPercent | Measure-Object -Minimum).Minimum
        PairedMaxSavedPercent = ($deltas.SavedPercent | Measure-Object -Maximum).Maximum
        BaselineMedianBuildSeconds = Get-Median @($baseline | ForEach-Object { [double] $_.BuildSeconds })
        CandidateMedianBuildSeconds = Get-Median @($candidate | ForEach-Object { [double] $_.BuildSeconds })
        BaselineMedianTestSeconds = Get-Median @($baseline | ForEach-Object { [double] $_.TestSeconds })
        CandidateMedianTestSeconds = Get-Median @($candidate | ForEach-Object { [double] $_.TestSeconds })
    }
})
$summary | Sort-Object Comparison, Mode | Export-Csv -LiteralPath (Join-Path $evidence 'summary.csv') -NoTypeInformation
$pairedDeltas | Export-Csv -LiteralPath (Join-Path $evidence 'paired-deltas.csv') -NoTypeInformation
$samples | Where-Object Stage -eq 'clean' | Export-Csv -LiteralPath (Join-Path $evidence 'clean-cycle.csv') -NoTypeInformation
$summary | Sort-Object Comparison, Mode | Format-Table Comparison, Mode, BaselineMedianSeconds, CandidateMedianSeconds, SavedPercent -AutoSize
