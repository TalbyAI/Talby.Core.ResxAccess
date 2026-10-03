param(
    [Parameter(Mandatory)]
    [ValidateSet('fast', 'full')]
    [string] $Mode
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = Split-Path $PSScriptRoot -Parent
$integration = @(
    'CanCompileAndInvokeIndependentResourceSets'
    'ReportsEachInvalidResourceAndEmbeddingInOneBuild'
    'ReportsMalformedReferenceResourceWithoutAspectCrash'
    'DescribesMissingRuntimeManifestAndResourceKey'
) | ForEach-Object { "Talby.Core.ResxAccess.Tests.RawTextConsumerTests.$_" }
$fast = @(
    'Talby.Core.ResxAccess.Tests.MetalamaSetupTests.CanCreateAndQueryCompilation'
    'InvalidPaths'
    'UnavailableProjectContext'
    'UnsupportedTargets'
    'RawTextGeneration'
    'ResourceKeyIdentifiers'
    'ResourceValidationDiagnostics'
    'KeywordResourceKey'
)
$fast += @(
    'ReadsTextEntriesAndSdkManifestName'
    'AcceptsExplicitStringTypesEmptyValuesAndCaseSensitiveKeys'
    'AcceptsAnEmptyReferenceResource'
    'RejectsMalformedXmlWithoutAnUnhandledXmlException'
    'RejectsInvalidRootElements'
    'RejectsDuplicateOrUnnamedResourceKeys'
    'RejectsInvalidValueAndTypeStructures'
    'RejectsInvalidPathsBeforeUnavailableProjectContext'
    'DistinguishesUnavailableProjectContextFromMissingFiles'
    'RejectsCultureSpecificReferenceResourcesBeforeMissingSdkMap'
    'RejectsUnavailableSdkMaps'
    'RejectsMissingOrMalformedEmbeddedResourceMetadata'
    'RejectsCustomNamesAndLinkedResourceMetadata'
    'RejectsResourcesOutsideTheProjectDirectory'
    'RejectsCultureSpecificSdkMetadata'
    'MatchesResourceMapPathsUsingPlatformComparison'
    'RecognizesKeywordAndUnicodeResourceKeyIdentifiers'
) | ForEach-Object { "Talby.Core.ResxAccess.Tests.ReferenceResourceTests.$_" }
$expected = if ($Mode -eq 'fast') { $fast } else { $fast + $integration }
$results = Join-Path $root "test-results/execution/$Mode-$([Guid]::NewGuid().ToString('N'))"
$arguments = @(
    'test', 'Talby.Core.ResxAccess.slnx', '--configuration', 'Release', '--no-build', '--no-restore'
    '--verbosity', 'normal', '--logger', 'trx;LogFilePrefix=execution', '--results-directory', $results
)
if ($Mode -eq 'fast')
{
    $arguments += '--filter', 'Category!=Integration'
    Write-Host "FAST: deferring $($integration.Count) integration tests (SDK embedding and runtime lookup are not checked):"
    $integration | ForEach-Object { Write-Host "  $_" }
}

Push-Location $root
try
{
    & dotnet @arguments
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    $executed = @(Get-ChildItem -LiteralPath $results -Filter '*.trx' | ForEach-Object {
        [xml] $trx = Get-Content -LiteralPath $_.FullName -Raw
        foreach ($result in $trx.TestRun.Results.UnitTestResult)
        {
            if ($result.outcome -ne 'Passed') { throw "Test did not pass: $($result.testName) ($($result.outcome))" }
            $result.testName
        }
    })
    # ponytail: fixed experiment inventory; review and update it when adding or renaming tests.
    if ($executed.Count -ne $expected.Count -or (Compare-Object $expected $executed -CaseSensitive))
    {
        throw "Unexpected $Mode selection. Expected: $($expected -join ', '). Executed: $($executed -join ', ')."
    }
    Write-Host "Verified $Mode selection: $($executed.Count) passed; $($integration.Count * [int]($Mode -eq 'fast')) integration tests deferred. Results: $results"
}
finally
{
    Pop-Location
}
exit 0
