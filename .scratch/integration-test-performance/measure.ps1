param(
    [ValidateSet('suite', 'probes')]
    [string] $Mode = 'suite'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$results = Join-Path $root 'test-results/integration-diagnosis'
$null = New-Item -ItemType Directory -Force -Path $results

function Invoke-MeasuredDotnet([string] $Name, [string[]] $Arguments)
{
    $log = Join-Path $results "$Name.log"
    $timer = [Diagnostics.Stopwatch]::StartNew()
    & dotnet @Arguments *> $log
    $resultCode = $LASTEXITCODE
    $timer.Stop()
    $sample = [pscustomobject]@{
        Sample = $Name
        Seconds = $timer.Elapsed.TotalSeconds
        ExitCode = $resultCode
    }
    $sample | Export-Csv (Join-Path $results "$Mode-samples.csv") -Append -NoTypeInformation
    Write-Host "$Name seconds=$($sample.Seconds) exit=$resultCode"
    if ($resultCode -ne 0) { throw "Failed: $Name. See $log." }
}

Push-Location $root
try
{
    if ($Mode -eq 'suite')
    {
        foreach ($index in 1..3)
        {
            Invoke-MeasuredDotnet "sample-$index" @(
                'test', 'tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj'
                '--configuration', 'Release', '--no-build', '--no-restore'
                '--logger', 'trx;LogFileName=integration.trx'
                '--results-directory', (Join-Path $results "sample-$index")
                '--verbosity', 'normal'
            )
        }
        return
    }

    # Same temporary-project location and working directory as ConsumerProject.Run.
    # Keep evidence instead of disposing the probe directory.
    $consumer = Join-Path ([IO.Path]::GetTempPath()) "ResxAccessTests/diagnosis-$([Guid]::NewGuid().ToString('N'))"
    $null = New-Item -ItemType Directory -Force -Path (Join-Path $consumer 'Resources')
    Set-Content (Join-Path $results 'probe-path.txt') $consumer
    $library = [Security.SecurityElement]::Escape((Join-Path $root 'src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj'))
    $targets = [Security.SecurityElement]::Escape((Join-Path $root 'src/Talby.Core.ResxAccess/buildTransitive/Talby.Core.ResxAccess.targets'))
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <RootNamespace>ConsumerRoot</RootNamespace>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="$library" /></ItemGroup>
  <Import Project="$targets" />
  <Target Name="IsolateResourceInputs" AfterTargets="CreateMetalamaTouchFiles">
    <ItemGroup><AdditionalFiles Remove="`$(MetalamaBuildTouchFile)" /></ItemGroup>
  </Target>
</Project>
"@ | Set-Content (Join-Path $consumer 'Consumer.csproj')
    @'
using System.Globalization;
using Talby.Core.ResxAccess;
Console.WriteLine(Texts.Welcome(CultureInfo.InvariantCulture));
Console.WriteLine(Texts.FormatWelcome("Ada", CultureInfo.InvariantCulture));
[GenerateResxAccess("Resources/Texts.resx")]
public static partial class Texts { }
'@ | Set-Content (Join-Path $consumer 'Program.cs')
    $resource = '<root><data name="Welcome"><value>Hello {name@string}</value></data></root>'
    Set-Content (Join-Path $consumer 'Resources/Texts.resx') $resource
    $project = Join-Path $consumer 'Consumer.csproj'
    $build = @('build', $project, '--configuration', 'Release', '--nologo', '--verbosity', 'quiet', '-p:BuildProjectReferences=false', '-p:RestoreRecursive=false')
    $assembly = Join-Path $consumer 'bin/Release/net10.0/Consumer.dll'
    $timestamps = [Collections.Generic.List[object]]::new()

    Push-Location ([IO.Path]::GetTempPath())
    try
    {
        Invoke-MeasuredDotnet 'probe-sdk' @('--version')
        Invoke-MeasuredDotnet 'probe-initial' $build
        foreach ($index in 1..3)
        {
            $restoreOrder = if ($index % 2 -eq 1) { @($false, $true) } else { @($true, $false) }
            foreach ($skipRestore in $restoreOrder)
            {
                $label = if ($skipRestore) { 'no-restore' } else { 'implicit-restore' }
                $before = [IO.File]::GetLastWriteTimeUtc($assembly)
                $arguments = if ($skipRestore) { @($build) + '--no-restore' } else { $build }
                Invoke-MeasuredDotnet "probe-unchanged-$label-$index" $arguments
                $timestamps.Add([pscustomobject]@{ Sample="probe-unchanged-$label-$index"; AssemblyUnchanged=($before -eq [IO.File]::GetLastWriteTimeUtc($assembly)) })
            }
            Set-Content (Join-Path $consumer 'Resources/Texts.resx') $resource.Replace('Hello', "Hello $index")
            Invoke-MeasuredDotnet "probe-edit-$index" $build
            Invoke-MeasuredDotnet "probe-invoke-$index" @($assembly)
        }
        $timestamps | Export-Csv (Join-Path $results 'probe-timestamps.csv') -NoTypeInformation
        # Diagnostic logging is kept outside ordinary timed comparisons.
        Invoke-MeasuredDotnet 'profile-unchanged' (@($build) + @('--no-restore', '--verbosity', 'diagnostic', '-clp:PerformanceSummary'))
        Set-Content (Join-Path $consumer 'Resources/Texts.resx') $resource.Replace('Hello', 'Profiled')
        Invoke-MeasuredDotnet 'profile-edit' (@($build) + @('--verbosity', 'diagnostic', '-clp:PerformanceSummary'))
        Invoke-MeasuredDotnet 'profile-designtime' @(
            'msbuild', $project, '-target:Compile', '-property:Configuration=Release'
            '-property:DesignTimeBuild=true', '-property:BuildingProject=false'
            '-property:SkipCompilerExecution=true', '-property:ProvideCommandLineArgs=true'
            '-property:BuildProjectReferences=false', '-verbosity:diagnostic', '-clp:PerformanceSummary'
        )
    }
    finally { Pop-Location }
}
finally { Pop-Location }
