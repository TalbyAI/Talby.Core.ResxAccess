param(
    [Parameter(Mandatory)] [string] $BaselineRoot,
    [Parameter(Mandatory)] [string] $CandidateRoot,
    [Parameter(Mandatory)] [string] $ResultsDirectory
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$baseline = (Resolve-Path -LiteralPath $BaselineRoot).Path
$candidate = (Resolve-Path -LiteralPath $CandidateRoot).Path
if ($baseline -eq $candidate) { throw 'Baseline and candidate must have separate output trees.' }
New-Item -ItemType Directory -Path $ResultsDirectory -Force | Out-Null
$results = (Resolve-Path -LiteralPath $ResultsDirectory).Path
$csvPath = Join-Path $results 'timings.csv'
$measurementSamples = [Collections.Generic.List[object]]::new()
if (Test-Path -LiteralPath $csvPath)
{
    Import-Csv -LiteralPath $csvPath | ForEach-Object { $measurementSamples.Add($_) }
}
$aspects = @('InvalidPaths','UnavailableProjectContext','UnsupportedTargets',
    'RawTextGeneration','ResourceKeyIdentifiers','ResourceValidationDiagnostics',
    'KeywordResourceKey','IndexedPlaceholderGeneration')
$absorbed = @('ResourceEntryValidationDiagnostics','LocalizedResourceDiagnostics',
    'ExpectedCultureDiagnostics','IndexedPlaceholderDiagnostics')

function Invoke-Sample([string] $variant, [string] $mode, [string] $stage, [int] $pair)
{
    if (@($measurementSamples | Where-Object { $_.Variant -eq $variant -and $_.Mode -eq $mode -and $_.Stage -eq $stage -and [int]$_.Pair -eq $pair }).Count -gt 0) { return }
    $root = if ($variant -eq 'baseline') { $baseline } else { $candidate }
    $stem = "$stage-$mode-$variant-$pair"
    $sampleDirectory = Join-Path $results $stem
    New-Item -ItemType Directory -Path $sampleDirectory -Force | Out-Null
    $buildSeconds = 0.0
    $buildExit = 0
    $overall = [Diagnostics.Stopwatch]::StartNew()
    Push-Location $root
    try
    {
        if ($mode -eq 'WarmFull')
        {
            $timer = [Diagnostics.Stopwatch]::StartNew()
            $buildOutput = & dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity minimal 2>&1
            $buildExit = $LASTEXITCODE
            $timer.Stop()
            $buildSeconds = $timer.Elapsed.TotalSeconds
            $buildOutput | Set-Content -LiteralPath (Join-Path $sampleDirectory 'build.log')
            if ($buildExit -ne 0) { $buildOutput | Write-Output; throw "Build failed: $stem" }
        }

        $timer = [Diagnostics.Stopwatch]::StartNew()
        if ($mode -eq 'Aspect')
        {
            $testOutput = & dotnet test tests/Talby.Core.ResxAccess.AspectTests/Talby.Core.ResxAccess.AspectTests.csproj --configuration Release --no-build --no-restore --verbosity normal --logger 'trx;LogFileName=aspect.trx' --results-directory $sampleDirectory 2>&1
        }
        else
        {
            $selection = if ($mode -eq 'Fast') { 'fast' } else { 'full' }
            $testOutput = & pwsh -NoProfile -File tests/run.ps1 -Mode $selection 2>&1
        }
        $testExit = $LASTEXITCODE
        $timer.Stop()
        $overall.Stop()
        $testSeconds = $timer.Elapsed.TotalSeconds
        $testOutput | Set-Content -LiteralPath (Join-Path $sampleDirectory 'test.log')
        if ($testExit -ne 0) { $testOutput | Write-Output; throw "Tests failed: $stem" }

        if ($mode -eq 'Aspect')
        {
            $trxDirectory = $sampleDirectory
            $expectedNames = if ($variant -eq 'baseline') { $aspects + $absorbed } else { $aspects }
            $expectedCount = $expectedNames.Count
        }
        else
        {
            $verified = [regex]::Match(($testOutput -join "`n"), '(?m)^Verified (?:fast|full) selection: (\d+) passed; \d+ integration tests deferred\. Results: (.+)$')
            if (-not $verified.Success) { throw "Runner did not verify its inventory: $stem" }
            $trxDirectory = $verified.Groups[2].Value.Trim()
            $expectedCount = if ($mode -eq 'Fast') { if ($variant -eq 'baseline') { 31 } else { 27 } } else { if ($variant -eq 'baseline') { 47 } else { 43 } }
            if ([int]$verified.Groups[1].Value -ne $expectedCount) { throw "Wrong runner count: $stem" }
        }
        $executed = @(Get-ChildItem -LiteralPath $trxDirectory -Filter '*.trx' | ForEach-Object {
            [xml]$trx = Get-Content -LiteralPath $_.FullName -Raw
            foreach ($test in $trx.TestRun.Results.UnitTestResult)
            {
                if ($test.outcome -ne 'Passed') { throw "Nonpassing result in ${stem}: $($test.testName)" }
                $test.testName
            }
        })
        if ($executed.Count -ne $expectedCount -or @($executed | Sort-Object -Unique).Count -ne $expectedCount) { throw "Wrong executed inventory: $stem" }
        if ($mode -eq 'Aspect' -and (Compare-Object $expectedNames $executed -CaseSensitive)) { throw "Wrong AspectTest identities: $stem" }

        $sample = [pscustomobject]@{
            Stage=$stage; Mode=$mode; Pair=$pair; Variant=$variant
            BuildSeconds=$buildSeconds; TestSeconds=$testSeconds; TotalSeconds=$overall.Elapsed.TotalSeconds
            BuildExit=$buildExit; TestExit=$testExit; Passed=$executed.Count
            TrxDirectory=$trxDirectory
        }
        $measurementSamples.Add($sample)
        $measurementSamples | Export-Csv -LiteralPath $csvPath -NoTypeInformation
        Write-Output ('{0}: build {1:F3} s, test {2:F3} s, total {3:F3} s; {4} passed' -f $stem,$sample.BuildSeconds,$sample.TestSeconds,$sample.TotalSeconds,$sample.Passed)
    }
    finally { Pop-Location }
}

foreach ($mode in @('Aspect','Fast','Full','WarmFull'))
{
    Invoke-Sample 'baseline' $mode 'warmup' 0
    Invoke-Sample 'candidate' $mode 'warmup' 0
    foreach ($pair in 1..5)
    {
        $order = if ($pair % 2 -eq 0) { @('candidate','baseline') } else { @('baseline','candidate') }
        foreach ($variant in $order) { Invoke-Sample $variant $mode 'paired' $pair }
    }
}

foreach ($variant in @('baseline','candidate'))
{
    if (@($measurementSamples | Where-Object { $_.Variant -eq $variant -and $_.Stage -eq 'clean' }).Count -gt 0) { continue }
    $root = if ($variant -eq 'baseline') { $baseline } else { $candidate }
    Push-Location $root
    try
    {
        $output = & dotnet clean Talby.Core.ResxAccess.slnx --configuration Release --verbosity minimal 2>&1
        $cleanExit = $LASTEXITCODE
        $output | Set-Content -LiteralPath (Join-Path $results "clean-$variant.log")
        if ($cleanExit -ne 0) { throw "Clean failed: $variant" }
    }
    finally { Pop-Location }
    Invoke-Sample $variant 'WarmFull' 'clean' 1
}

$paired = @($measurementSamples | Where-Object Stage -eq 'paired')
if ($paired.Count -ne 40) { throw 'Expected forty paired observations.' }
Write-Output 'Verified forty paired observations, eight excluded warm-ups and two clean-output observations.'
