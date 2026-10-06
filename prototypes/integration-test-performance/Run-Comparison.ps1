param(
    [Parameter(Mandatory)] [string] $ExperimentRoot,
    [Parameter(Mandatory)] [ValidateSet('A', 'B', 'AB')] [string] $Candidate,
    [ValidateSet('Integration', 'Validation', 'All', 'ResumeFullWarmup')] [string] $Phase = 'All'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$experiment = (Resolve-Path -LiteralPath $ExperimentRoot).Path
$sourceManifest = Get-Content -LiteralPath (Join-Path $experiment 'source-hashes.json') -Raw | ConvertFrom-Json -AsHashtable
foreach ($frozenVariant in @('baseline', $Candidate))
{
    foreach ($entry in $sourceManifest.trees[$frozenVariant].GetEnumerator())
    {
        $frozenFile = Join-Path (Join-Path $experiment $frozenVariant) $entry.Key
        if (-not (Test-Path -LiteralPath $frozenFile) -or (Get-FileHash -LiteralPath $frozenFile -Algorithm SHA256).Hash.ToLowerInvariant() -ne $entry.Value)
        { throw "Frozen source changed: $frozenVariant/$($entry.Key)" }
    }
}
$results = Join-Path $experiment "comparisons/$Candidate"
$null = New-Item -ItemType Directory -Force -Path $results
$csvPath = Join-Path $results 'timings.csv'
$samples = [Collections.Generic.List[object]]::new()
if (Test-Path -LiteralPath $csvPath)
{
    Import-Csv -LiteralPath $csvPath | ForEach-Object { $samples.Add($_) }
}

function Get-LibraryMetadata([string] $TreeRoot)
{
    $library = Join-Path $TreeRoot 'src/Talby.Core.ResxAccess'
    @(foreach ($directory in @('bin/Release', 'obj'))
    {
        $path = Join-Path $library $directory
        if (Test-Path -LiteralPath $path)
        {
            Get-ChildItem -LiteralPath $path -File -Recurse | ForEach-Object {
                '{0}|{1}|{2}' -f [IO.Path]::GetRelativePath($library, $_.FullName), $_.Length, $_.LastWriteTimeUtc.Ticks
            }
        }
    }) | Sort-Object
}

function Read-Inventory([string] $TrxDirectory, [hashtable] $IdentityMap, [string] $Destination)
{
    $files = @(Get-ChildItem -LiteralPath $TrxDirectory -Filter '*.trx' -File -Recurse)
    if ($files.Count -eq 0) { throw "Missing TRX files: $TrxDirectory" }
    $rows = @(foreach ($file in $files)
    {
        [xml] $trx = Get-Content -LiteralPath $file.FullName -Raw
        $tests = @($trx.TestRun.Results.UnitTestResult)
        if ($tests.Count -eq 0) { throw "Missing results: $($file.FullName)" }
        foreach ($test in $tests)
        {
            if ($test.outcome -ne 'Passed') { throw "Nonpassing result: $($test.testName) ($($test.outcome))" }
            $definition = @($trx.TestRun.TestDefinitions.UnitTest | Where-Object id -eq $test.testId)
            if ($definition.Count -ne 1) { throw "Missing test definition: $($test.testName)" }
            $assembly = [IO.Path]::GetFileNameWithoutExtension($definition[0].storage)
            $identity = [string] $test.testName
            $canonical = if ($IdentityMap.ContainsKey($identity)) { $IdentityMap[$identity] } else { $identity }
            [pscustomobject]@{
                Assembly = $assembly; Identity = $identity; CanonicalIdentity = $canonical
                Outcome = $test.outcome; StartTime = $test.startTime; EndTime = $test.endTime
                Duration = $test.duration; Canonical = "$assembly|$canonical"
            }
        }
    })
    if (@($rows.Canonical | Sort-Object -Unique -CaseSensitive).Count -ne $rows.Count) { throw 'Duplicate canonical test identities.' }
    $rows | Sort-Object Canonical | Export-Csv -LiteralPath $Destination -NoTypeInformation
    return ,@($rows.Canonical | Sort-Object)
}

function Invoke-Sample([string] $Variant, [string] $Mode, [string] $Stage, [int] $Pair)
{
    $existing = @($samples | Where-Object {
        $_.Variant -eq $Variant -and $_.Mode -eq $Mode -and $_.Stage -eq $Stage -and [int] $_.Pair -eq $Pair
    })
    if ($existing.Count -gt 0)
    {
        if ($existing.Count -ne 1 -or [int] $existing[0].BuildExit -ne 0 -or [int] $existing[0].TestExit -ne 0)
        { throw 'Invalid recorded sample.' }
        $recorded = Join-Path $results "$Stage-$Mode-$Variant-$Pair"
        foreach ($artifact in @('inventory.csv', 'test.log', 'library-before.txt', 'library-after.txt'))
        {
            if (-not (Test-Path -LiteralPath (Join-Path $recorded $artifact))) { throw "Missing recorded artifact: $recorded/$artifact" }
        }
        if ($Mode -eq 'WarmFull' -and -not (Test-Path -LiteralPath (Join-Path $recorded 'build.log')))
        { throw "Missing recorded build log: $recorded" }
        $recordedTree = Join-Path $experiment $Variant
        $recordedMap = Get-Content -LiteralPath (Join-Path $recordedTree 'identity-map.json') -Raw | ConvertFrom-Json -AsHashtable
        $recordedInventory = Read-Inventory $existing[0].TrxDirectory $recordedMap (Join-Path $recorded 'inventory.csv')
        $recordedMode = if ($Mode -eq 'WarmFull') { 'Full' } else { $Mode }
        $recordedExpected = @(Get-Content -LiteralPath (Join-Path $results "inventory-$recordedMode.txt"))
        if ($recordedInventory.Count -ne [int] $existing[0].Passed -or (Compare-Object $recordedExpected $recordedInventory -CaseSensitive))
        { throw 'Recorded inventory no longer matches its evidence.' }
        if (Compare-Object @(Get-Content -LiteralPath (Join-Path $recorded 'library-before.txt')) @(Get-Content -LiteralPath (Join-Path $recorded 'library-after.txt')) -CaseSensitive)
        { throw 'Recorded shared-library metadata changed.' }
        return
    }
    $tree = Join-Path $experiment $Variant
    $stem = "$Stage-$Mode-$Variant-$Pair"
    $directory = Join-Path $results $stem
    if (Test-Path -LiteralPath $directory) { throw "Incomplete sample exists; inspect it before retrying: $directory" }
    $null = New-Item -ItemType Directory -Path $directory
    $map = Get-Content -LiteralPath (Join-Path $tree 'identity-map.json') -Raw | ConvertFrom-Json -AsHashtable
    Write-Output "BEGIN $Candidate/$stem $(Get-Date -Format o)"
    $buildSeconds = 0.0
    $buildExit = 0
    $testExit = $null
    $testSeconds = $null
    $overall = [Diagnostics.Stopwatch]::StartNew()
    Push-Location $tree
    try
    {
        if ($Mode -eq 'WarmFull')
        {
            $timer = [Diagnostics.Stopwatch]::StartNew()
            $buildOutput = & dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore --verbosity minimal 2>&1
            $buildExit = $LASTEXITCODE
            $timer.Stop()
            $buildSeconds = $timer.Elapsed.TotalSeconds
            $buildOutput | Set-Content -LiteralPath (Join-Path $directory 'build.log')
            if ($buildExit -ne 0) { throw "Build failed: $directory/build.log" }
        }
        # Metadata inspection is outside command timers; complete-cycle time excludes it too.
        $overall.Stop()
        $before = @(Get-LibraryMetadata $tree)
        $before | Set-Content -LiteralPath (Join-Path $directory 'library-before.txt')
        if ($Stage -eq 'warmup') { $env:TALBY_CONSUMER_TRACE_DIR = Join-Path $directory 'trace' }
        else { Remove-Item Env:TALBY_CONSUMER_TRACE_DIR -ErrorAction SilentlyContinue }
        $overall.Start()
        $timer = [Diagnostics.Stopwatch]::StartNew()
        if ($Mode -in @('Integration', 'Aspect'))
        {
            $project = if ($Mode -eq 'Integration') { 'IntegrationTests' } else { 'AspectTests' }
            $testOutput = & dotnet test "tests/Talby.Core.ResxAccess.$project/Talby.Core.ResxAccess.$project.csproj" --configuration Release --no-build --no-restore --verbosity normal --logger 'trx;LogFileName=execution.trx' --results-directory $directory 2>&1
        }
        else
        {
            $selection = if ($Mode -eq 'Fast') { 'fast' } else { 'full' }
            $testOutput = & pwsh -NoProfile -File tests/run.ps1 -Mode $selection 2>&1
        }
        $testExit = $LASTEXITCODE
        $timer.Stop()
        $testSeconds = $timer.Elapsed.TotalSeconds
        $overall.Stop()
        Remove-Item Env:TALBY_CONSUMER_TRACE_DIR -ErrorAction SilentlyContinue
        $testOutput | Set-Content -LiteralPath (Join-Path $directory 'test.log')
        $after = @(Get-LibraryMetadata $tree)
        $after | Set-Content -LiteralPath (Join-Path $directory 'library-after.txt')
        $difference = @(Compare-Object $before $after -CaseSensitive)
        if ($difference.Count -ne 0)
        {
            $difference | Export-Csv -LiteralPath (Join-Path $directory 'library-changes.csv') -NoTypeInformation
            throw "Consumer tests wrote shared library outputs: $stem"
        }
        if ($testExit -ne 0) { throw "Tests failed: $directory/test.log" }
        if ($Mode -in @('Integration', 'Aspect')) { $trxDirectory = $directory }
        else
        {
            $verified = [regex]::Match(($testOutput -join "`n"), '(?m)^Verified (?:fast|full) results: (\d+) passed\. Results: (.+)$')
            if (-not $verified.Success) { throw "Runner did not verify results: $stem" }
            $trxDirectory = $verified.Groups[2].Value.Trim()
        }
        $inventory = Read-Inventory $trxDirectory $map (Join-Path $directory 'inventory.csv')
        $inventoryMode = if ($Mode -eq 'WarmFull') { 'Full' } else { $Mode }
        $expectedPath = Join-Path $results "inventory-$inventoryMode.txt"
        if (-not (Test-Path -LiteralPath $expectedPath))
        {
            if ($Variant -ne 'baseline') { throw 'Baseline must establish the inventory first.' }
            $inventory | Set-Content -LiteralPath $expectedPath
        }
        $expected = @(Get-Content -LiteralPath $expectedPath)
        if (Compare-Object $expected $inventory -CaseSensitive) { throw "Changed test inventory: $stem" }
        $sample = [pscustomobject]@{
            Comparison = $Candidate; Stage = $Stage; Mode = $Mode; Pair = $Pair; Variant = $Variant
            BuildSeconds = $buildSeconds; TestSeconds = $testSeconds
            TotalSeconds = $overall.Elapsed.TotalSeconds; BuildExit = $buildExit; TestExit = $testExit
            Passed = $inventory.Count; LibraryFilesUnchanged = $before.Count; TrxDirectory = $trxDirectory
        }
        $samples.Add($sample)
        $samples | Export-Csv -LiteralPath $csvPath -NoTypeInformation
        Write-Output ('END {0}/{1}: build {2:F3}s test {3:F3}s total {4:F3}s; {5} passed; {6} library files unchanged' -f $Candidate, $stem, $sample.BuildSeconds, $sample.TestSeconds, $sample.TotalSeconds, $sample.Passed, $sample.LibraryFilesUnchanged)
    }
    catch
    {
        $overall.Stop()
        [pscustomobject]@{
            Comparison = $Candidate; Stage = $Stage; Mode = $Mode; Pair = $Pair; Variant = $Variant
            BuildSeconds = $buildSeconds; TestSeconds = $testSeconds; TotalSeconds = $overall.Elapsed.TotalSeconds
            BuildExit = $buildExit; TestExit = $testExit; Error = $_.Exception.Message; SampleDirectory = $directory
        } | Export-Csv -LiteralPath (Join-Path $results 'failures.csv') -Append -NoTypeInformation
        throw
    }
    finally
    {
        Remove-Item Env:TALBY_CONSUMER_TRACE_DIR -ErrorAction SilentlyContinue
        Pop-Location
    }
}

if ($Phase -eq 'ResumeFullWarmup')
{
    Invoke-Sample 'baseline' 'Full' 'resume-warmup' 0
    Invoke-Sample $Candidate 'Full' 'resume-warmup' 0
    Write-Output "Completed $Candidate supplemental Full warm-ups; excluded from paired statistics."
    return
}

$modes = switch ($Phase)
{
    'Integration' { @('Integration') }
    'Validation' { @('Aspect', 'Fast', 'Full', 'WarmFull') }
    'All' { @('Integration', 'Aspect', 'Fast', 'Full', 'WarmFull') }
}
foreach ($mode in $modes)
{
    Invoke-Sample 'baseline' $mode 'warmup' 0
    Invoke-Sample $Candidate $mode 'warmup' 0
    foreach ($pair in 1..5)
    {
        $order = if ($pair % 2 -eq 0) { @($Candidate, 'baseline') } else { @('baseline', $Candidate) }
        foreach ($variant in $order) { Invoke-Sample $variant $mode 'paired' $pair }
    }
}
if ($Phase -ne 'Integration')
{
    # Continue the alternating order after paired observation five (baseline first).
    foreach ($variant in @($Candidate, 'baseline'))
    {
        if (@($samples | Where-Object { $_.Variant -eq $variant -and $_.Stage -eq 'clean' }).Count -gt 0) { continue }
        Push-Location (Join-Path $experiment $variant)
        try
        {
            $cleanOutput = & dotnet clean Talby.Core.ResxAccess.slnx --configuration Release --verbosity minimal 2>&1
            $cleanExit = $LASTEXITCODE
            $cleanOutput | Set-Content -LiteralPath (Join-Path $results "clean-$variant.log")
            if ($cleanExit -ne 0) { throw "Clean failed: $variant" }
        }
        finally { Pop-Location }
        Invoke-Sample $variant 'WarmFull' 'clean' 6
    }
}
Write-Output "Completed $Candidate/$Phase. Timings: $csvPath"
