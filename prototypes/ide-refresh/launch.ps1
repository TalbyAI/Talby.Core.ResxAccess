param([switch] $CSharpOnly, [switch] $DesignTimeControl, [switch] $PrepareOnly)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path "$PSScriptRoot/../..").Path
$session = Join-Path $root "test-results/ide-refresh-$([Guid]::NewGuid().ToString('N'))"
$consumer = Join-Path $session 'consumer'
New-Item -ItemType Directory -Force "$consumer/Resources", "$consumer/.vscode", "$session/profile/User" | Out-Null
$library = Join-Path $root 'src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj'
$targets = Join-Path $root 'src/Talby.Core.ResxAccess/buildTransitive/Talby.Core.ResxAccess.targets'
@"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net10.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>IdeConsumer</RootNamespace>
  </PropertyGroup>
  <ItemGroup><ProjectReference Include="$library" /></ItemGroup>
  <Import Project="$targets" />
</Project>
"@ | Set-Content "$consumer/Consumer.csproj"
@'
using Talby.Core.ResxAccess;
Console.WriteLine(Texts.Welcome());
Console.WriteLine(Texts.FormatWelcome("Ada"));
[GenerateResxAccess("Resources/Texts.resx", ExpectedCultures = new[] { "es" })]
internal static partial class Texts { }
'@ | Set-Content "$consumer/Program.cs"
'<root><data name="Welcome"><value>Hello {name@string}</value></data></root>' | Set-Content "$consumer/Resources/Texts.resx"
'<root><data name="Welcome"><value>Hola {name}</value></data></root>' | Set-Content "$consumer/Resources/Texts.es.resx"
'{"dotnet.backgroundAnalysis.analyzerDiagnosticsScope":"fullSolution","dotnet.backgroundAnalysis.compilerDiagnosticsScope":"fullSolution"}' | Set-Content "$consumer/.vscode/settings.json"
'{"extensions.autoUpdate":false,"extensions.autoCheckUpdates":false}' | Set-Content "$session/profile/User/settings.json"

# Initial setup only. The extension-host probe never builds or edits C#.
dotnet build "$consumer/Consumer.csproj" --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) { throw 'Initial consumer build failed.' }
if ($PrepareOnly)
{
    Write-Host "Consumer for human observation: $consumer"
    return
}
$codeCommand = (Get-Command code).Source
$executable = Join-Path (Split-Path (Split-Path $codeCommand)) 'Code.exe'
$start = [Diagnostics.ProcessStartInfo]::new($executable)
$start.WindowStyle = 'Hidden'
$start.Environment.Remove('ELECTRON_RUN_AS_NODE') | Out-Null
if ($DesignTimeControl) { $start.Environment['TALBY_IDE_DESIGN_TIME_CONTROL'] = '1' }
foreach ($argument in @(
    '--new-window', '--disable-workspace-trust', '--skip-welcome', '--skip-release-notes',
    '--user-data-dir', "$session/profile",
    '--extensionDevelopmentPath', $PSScriptRoot,
    '--extensionTestsPath', "$PSScriptRoot/run.cjs", $consumer
)) { $start.ArgumentList.Add($argument) }
foreach ($extension in (& code --list-extensions))
{
    $allowed = $extension -match '^ms-dotnettools\.(csharp|vscode-dotnet-runtime)$' -or
        (-not $CSharpOnly -and $extension -eq 'ms-dotnettools.csdevkit')
    if (-not $allowed)
    {
        $start.ArgumentList.Add('--disable-extension')
        $start.ArgumentList.Add($extension)
    }
}
$process = [Diagnostics.Process]::Start($start)
Write-Host "Probe process: $($process.Id)"
Write-Host "Evidence: $consumer/evidence.json"
Write-Host "IDE logs: $session/profile/logs"
