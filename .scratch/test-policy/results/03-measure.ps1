param(
    [string] $BaselineRevision = '9a0aa8b9cd4fe13213c4826acc0518bea25b4fbd'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$output = Join-Path $root 'test-results/03-aspect-logic'
$baseline = Join-Path $output 'baseline'
$logs = Join-Path $output 'measurements'
$csv = Join-Path $PSScriptRoot '03-timings.csv'
$samples = [Collections.Generic.List[object]]::new()
New-Item -ItemType Directory -Path $baseline, $logs -Force | Out-Null

function Invoke-Sample([string] $revision, [string] $phase, [string] $mode, [string] $route, [int] $pair, [int] $order)
{
    $directory = if ($revision -eq 'baseline') { $baseline } else { $root }
    $prefix = Join-Path $logs ('{0:D2}-{1}-{2}-{3}-{4}' -f ($samples.Count + 1), $revision, $phase, $mode, $route)
    $buildSeconds = 0.0
    $buildExit = 0
    Push-Location $directory
    try
    {
        if ($phase -eq 'clean-output')
        {
            & dotnet clean Talby.Core.ResxAccess.slnx --configuration Release --verbosity quiet *> "$prefix-clean.log"
            if ($LASTEXITCODE -ne 0) { throw "Clean failed: $prefix" }
        }
        $combined = [Diagnostics.Stopwatch]::StartNew()
        if ($mode -eq 'warm-build-test')
        {
            $build = [Diagnostics.Stopwatch]::StartNew()
            & dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity quiet *> "$prefix-build.log"
            $build.Stop()
            $buildSeconds = $build.Elapsed.TotalSeconds
            $buildExit = $LASTEXITCODE
            if ($buildExit -ne 0) { throw "Build failed: $prefix" }
        }
        $test = [Diagnostics.Stopwatch]::StartNew()
        & pwsh -NoProfile -File tests/run.ps1 -Mode $route *> "$prefix-test.log"
        $test.Stop()
        $testExit = $LASTEXITCODE
        $combined.Stop()
        if ($testExit -ne 0) { throw "Test failed: $prefix" }

        $summary = [regex]::Match([IO.File]::ReadAllText("$prefix-test.log"), "Verified $route selection: (\d+) passed;[^\r\n]*Results: ([^\r\n]+)")
        if (-not $summary.Success) { throw "Runner inventory verification missing: $prefix" }
        $selected = [int] $summary.Groups[1].Value
        $testRuns = @(Get-ChildItem -LiteralPath $summary.Groups[2].Value.Trim() -Filter '*.trx' | ForEach-Object {
            [xml] $trx = Get-Content -LiteralPath $_.FullName -Raw
            $trx.TestRun
        })
        $results = @($testRuns | ForEach-Object { $_.Results.UnitTestResult })
        $starts = @($testRuns | ForEach-Object { [DateTimeOffset]::Parse($_.Times.start) } | Sort-Object)
        $ends = @($testRuns | ForEach-Object { [DateTimeOffset]::Parse($_.Times.finish) } | Sort-Object)
        $activeSpan = ($ends[-1] - $starts[0]).TotalSeconds
        $passed = @($results | Where-Object outcome -eq 'Passed').Count
        if ($results.Count -ne $selected -or $passed -ne $selected) { throw "TRX inventory mismatch: $prefix" }
        $samples.Add([pscustomobject]@{
            Sequence = $samples.Count + 1; Phase = $phase; Mode = $mode; Route = $route; Pair = $pair; Order = $order; Revision = $revision
            BuildSeconds = [math]::Round($buildSeconds, 6); TestSeconds = [math]::Round($test.Elapsed.TotalSeconds, 6)
            CombinedSeconds = [math]::Round($combined.Elapsed.TotalSeconds, 6); BuildExit = $buildExit; TestExit = $testExit
            Selected = $selected; Executed = $results.Count; Passed = $passed; Failed = 0; Skipped = 0
            TestHostSpanSeconds = [math]::Round($activeSpan, 6)
            OutsideTestHostSpanSeconds = [math]::Round($test.Elapsed.TotalSeconds - $activeSpan, 6)
            TemporarySdkBuilds = 2 * [int]($route -eq 'full')
            FixtureBuildScheduled = [int]($mode -eq 'warm-build-test')
        })
        $samples | Export-Csv -LiteralPath $csv -NoTypeInformation
        Write-Host ("{0:D2} {1} {2} {3} {4}: {5:F2}s; {6} passed" -f $samples.Count, $phase, $mode, $route, $revision, $combined.Elapsed.TotalSeconds, $passed)
    }
    finally { Pop-Location }
}

Push-Location $root
try
{
    & git archive --format=tar "--output=$output/baseline.tar" $BaselineRevision
    if ($LASTEXITCODE -ne 0) { throw 'Baseline archive failed.' }
    & tar -xf "$output/baseline.tar" -C $baseline
    if ($LASTEXITCODE -ne 0) { throw 'Baseline extraction failed.' }
    foreach ($directory in @($baseline, $root))
    {
        Push-Location $directory
        try
        {
            & dotnet restore Talby.Core.ResxAccess.slnx *> "$logs/initial-$([IO.Path]::GetFileName($directory))-restore.log"
            if ($LASTEXITCODE -ne 0) { throw 'Initial restore failed.' }
            & dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity quiet *> "$logs/initial-$([IO.Path]::GetFileName($directory))-build.log"
            if ($LASTEXITCODE -ne 0) { throw 'Initial build failed.' }
        }
        finally { Pop-Location }
    }
    foreach ($mode in @('test-only', 'warm-build-test'))
    {
        foreach ($revision in @('baseline', 'candidate'))
        {
            foreach ($route in @('fast', 'full')) { Invoke-Sample $revision 'warmup' $mode $route 0 0 }
        }
        foreach ($pair in 1..5)
        {
            $revisions = if ($pair % 2 -eq 1) { @('baseline', 'candidate') } else { @('candidate', 'baseline') }
            $order = 0
            foreach ($revision in $revisions)
            {
                $order++
                foreach ($route in @('fast', 'full')) { Invoke-Sample $revision 'sample' $mode $route $pair $order }
            }
        }
    }
    foreach ($route in @('full', 'fast'))
    {
        $order = 0
        foreach ($revision in @('baseline', 'candidate')) { $order++; Invoke-Sample $revision 'clean-output' 'warm-build-test' $route 1 $order }
    }
}
finally { Pop-Location }
