#requires -Version 7.0
# These checks never invoke the interactive wizard or publish a package.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$scriptPath = Join-Path $root 'scripts/release.ps1'
if (-not (Test-Path -LiteralPath $scriptPath)) { throw 'The release wizard is missing.' }
$tokens = $null
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile($scriptPath, [ref] $tokens, [ref] $errors)
if ($errors.Count) { throw ($errors | Out-String) }

# Load only the pure validation helpers, without running the script body.
foreach ($name in @('Get-ReleaseMetadata', 'Invoke-Native', 'Assert-PackageHash', 'Assert-PackageIdentity', 'Assert-PassedTests'))
{
    $function = $ast.Find({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $true)
    if (-not $function) { throw "Missing helper: $name" }
    Invoke-Expression $function.Extent.Text
}

function Assert-Rejected([scriptblock] $Action, [string] $Message)
{
    try { & $Action }
    catch
    {
        if ($_.Exception.Message -like "*$Message*") { return }
        throw
    }
    throw "Expected rejection containing: $Message"
}

foreach ($mode in @('Prepare', 'Website', 'Cli', 'Verify'))
{
    $plan = & $scriptPath -Mode $mode -Plan
    [xml] $library = Get-Content -LiteralPath (Join-Path $root 'src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj') -Raw
    if ($plan.Version -cne $library.Project.PropertyGroup.Version -or $plan.Mode -cne $mode)
    {
        throw 'Plan must use the project Version and requested Mode without interactive actions.'
    }
}
Assert-Rejected { Invoke-Native 'pwsh' @('-NoProfile', '-Command', 'exit 17') } 'exit code 17'

$temporary = Join-Path $root "test-results/release-script/$([Guid]::NewGuid().ToString('N'))"
$null = New-Item -ItemType Directory -Path $temporary
$project = Join-Path $temporary 'Library.csproj'
Set-Content -LiteralPath $project -Value '<Project><PropertyGroup><PackageId>Example.Library</PackageId><Version>2.3.4-beta.2</Version></PropertyGroup></Project>'
$metadata = Get-ReleaseMetadata $project
if ($metadata.Version -cne '2.3.4-beta.2' -or $metadata.PackageId -cne 'Example.Library') { throw 'Metadata was not read from XML.' }
Get-ReleaseMetadata $project '2.3.4-beta.2' | Out-Null
Assert-Rejected { Get-ReleaseMetadata $project '2.3.5' } 'does not match'
Set-Content -LiteralPath $project -Value '<Project><PropertyGroup><PackageId>Example.Library</PackageId></PropertyGroup></Project>'
Assert-Rejected { Get-ReleaseMetadata $project } 'one explicit'

$package = Join-Path $temporary 'Example.Library.2.3.4-beta.2.nupkg'
$archive = [IO.Compression.ZipFile]::Open($package, [IO.Compression.ZipArchiveMode]::Create)
try
{
    $entry = $archive.CreateEntry('Example.Library.nuspec')
    $writer = [IO.StreamWriter]::new($entry.Open())
    try { $writer.Write('<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd"><metadata><id>Example.Library</id><version>2.3.4-beta.2</version></metadata></package>') }
    finally { $writer.Dispose() }
}
finally { $archive.Dispose() }
Assert-PackageIdentity $package 'Example.Library' '2.3.4-beta.2'
Assert-Rejected { Assert-PackageIdentity $package 'Example.Library' '2.3.5' } 'identity'
$hash = (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash
Set-Content -LiteralPath "$package.sha256" -Value $hash
Assert-PackageHash $package $hash
Assert-Rejected { Assert-PackageHash $package ('0' * 64) } 'checksum'
Add-Content -LiteralPath $package -Value 'changed after validation'
Assert-Rejected { Assert-PackageHash $package $hash } 'checksum'

$results = Join-Path $temporary 'results'
$null = New-Item -ItemType Directory -Path $results
Assert-Rejected { Assert-PassedTests $results } 'No TRX'
$trx = Join-Path $results 'tests.trx'
Set-Content -LiteralPath $trx -Value '<TestRun><Results><UnitTestResult testName="Archive" outcome="Passed" /></Results></TestRun>'
Assert-PassedTests $results
Set-Content -LiteralPath $trx -Value '<TestRun><Results /></TestRun>'
Assert-Rejected { Assert-PassedTests $results } 'No test results'
Set-Content -LiteralPath $trx -Value '<TestRun><Results><UnitTestResult testName="Archive" outcome="NotExecuted" /></Results></TestRun>'
Assert-Rejected { Assert-PassedTests $results } 'did not pass'
Write-Host 'Release script checks passed: plans, XML version, mismatched version, archive identity, tampering, native failure, missing/empty/skipped test results.'
