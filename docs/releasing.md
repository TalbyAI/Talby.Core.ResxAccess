# Manual NuGet release

The first release is `Talby.Core.ResxAccess` `0.1.0-beta.1`, targeting `net10.0`,
licensed under MIT and attributed to TalbyAI. It is intended for existing projects
and early adopters. The user performs the public upload after local validation.

## Prepared artifact

From the repository root, the package to upload is:

```text
artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg
```

Its adjacent `.nupkg.sha256` file records the validated archive's SHA-256 checksum.
The preparation evidence is in
[the validation report](../.scratch/nuget-release/validation.md).
Generated packages, package caches and test results are ignored by Git.

Check the file before uploading:

```powershell
$package = 'artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg'
$expectedHash = (Get-Content -LiteralPath "$package.sha256").Trim()
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'The archive differs from the validated release artifact.'
}
```

If you change source, resources, metadata or the package README, rebuild, repack
and repeat validation before replacing this checksum.

## Rebuild and validate

Prerequisites: Git, .NET SDK 10.0.401 or a later patch in the 10.0.4xx feature band,
Node.js 24, PowerShell 7 and access to NuGet.org. Run from the repository root.
Each native command must succeed before continuing:

```powershell
dotnet tool restore
if ($LASTEXITCODE -ne 0) { throw 'Tool restore failed.' }
npm ci
if ($LASTEXITCODE -ne 0) { throw 'Tooling installation failed.' }
npm run format:check
if ($LASTEXITCODE -ne 0) { throw 'Formatting validation failed.' }
dotnet restore Talby.Core.ResxAccess.slnx
if ($LASTEXITCODE -ne 0) { throw 'Solution restore failed.' }
dotnet build Talby.Core.ResxAccess.slnx --configuration Release --no-restore
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
pwsh -NoProfile -File tests/run.ps1 -Mode full
if ($LASTEXITCODE -ne 0) { throw 'Full test validation failed.' }
dotnet pack src/Talby.Core.ResxAccess/Talby.Core.ResxAccess.csproj --configuration Release --no-build --no-restore --output artifacts/nuget
if ($LASTEXITCODE -ne 0) { throw 'Packing failed.' }
```

Full includes two NuGet package tests. They inspect an actual `.nupkg` and build/run
a consumer using only `PackageReference` for this library. Each fixture has a fresh
package cache and source mapping that takes this package from its isolated local
feed. It checks automatic targets import, generated Raw Text and Formatted Text,
Localized Resource satellite loading, independent Formatting Culture and Resource
Culture fallback. Existing consumer, diagnostic and snapshot assertions remain.

Validate the exact archive that you will upload and record its checksum only after
the tests pass. `TALBY_TEST_PACKAGE` makes the fixture copy that archive into its
isolated feed instead of packing another copy:

```powershell
$package = 'artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg'
if (Test-Path Env:TALBY_TEST_PACKAGE) {
    throw 'Use a shell without an existing TALBY_TEST_PACKAGE override.'
}
$env:TALBY_TEST_PACKAGE = (Resolve-Path -LiteralPath $package).Path
try {
    dotnet test tests/Talby.Core.ResxAccess.IntegrationTests/Talby.Core.ResxAccess.IntegrationTests.csproj --configuration Release --no-build --no-restore --filter FullyQualifiedName~NuGetPackageConsumerTests
    if ($LASTEXITCODE -ne 0) { throw 'Release archive validation failed.' }
    (Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash |
        Set-Content -LiteralPath "$package.sha256" -Encoding ascii
}
finally {
    Remove-Item Env:TALBY_TEST_PACKAGE
}
```

Review the diff and retain the release changes on a working branch. Follow the
repository's normal review/merge process, then record the published source revision
with a `v0.1.0-beta.1` Git tag. Public repository changes should be available before
publication so that package README links point to the matching documentation.

## Confirm NuGet ownership

The NuGet public profile [TalbyAI](https://www.nuget.org/profiles/TalbyAI) returned
HTTP 200 on 2026-10-07; [Talby](https://www.nuget.org/profiles/Talby) returned HTTP 404.
The public profile does not identify whether TalbyAI is an individual or organization
account, or prove that the publisher controls it.

1. Sign in to [NuGet.org](https://www.nuget.org/) using your individual Microsoft-linked account. Follow the [account requirements](https://learn.microsoft.com/en-us/nuget/nuget-org/individual-accounts), including Microsoft account two-factor authentication.
2. Confirm that you control the TalbyAI identity. If it is an organization, check it under **Manage Organizations** and verify that your membership allows package management. If it is your individual identity, it can also own packages. Organization setup and permissions are documented in [NuGet organization accounts](https://learn.microsoft.com/en-us/nuget/nuget-org/organizations-on-nuget-org).
3. If the intended identity is missing from your account, resolve account creation or organization access before uploading. Public profile existence does not establish access or name availability.
4. Confirm `TalbyAI` as **Package Owner** during upload. The `.nuspec` Authors field is attribution; it does not assign NuGet ownership.

No package with ID `Talby.Core.ResxAccess` was publicly listed during preparation.
NuGet verifies the ID and any applicable prefix reservation when you upload; a
missing public listing does not reserve the name.

## First publish through the website

This is the recommended manual path and requires no API key.

1. Open [NuGet.org Upload](https://www.nuget.org/packages/manage/upload) after signing in.
2. Select exactly `artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg` after checking its checksum.
3. In the verification page, select the authorized **TalbyAI** owner. If it is unavailable, stop and resolve account access.
4. Check Package ID `Talby.Core.ResxAccess`, version `0.1.0-beta.1`, Authors `TalbyAI`, MIT license, `net10.0`, the repository URL and the Metalama.Framework dependency.
5. Preview the package README and check its examples and links.
6. Select **Submit** to publish. Record the package URL, owner, version, source revision and checksum. Allow NuGet validation and indexing to complete.

Microsoft documents this verification/submit flow in
[Publish NuGet packages](https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package).
Use a new version for subsequent changes rather than attempting to overwrite a
published version.

## Optional CLI publish

For later manual releases, you can use `dotnet nuget push`. Create a scoped API key
with **Package Owner** `TalbyAI`, **Push new packages and package versions**, and
package pattern `Talby.Core.ResxAccess`. The selected owner determines CLI publication
ownership. Follow [scoped API keys](https://learn.microsoft.com/en-us/nuget/nuget-org/scoped-api-keys).

The repository SDK supports `NUGET_API_KEY`, so you can supply a key for this process
without placing it in command history. In a fresh PowerShell 7 shell:

```powershell
$package = 'artifacts/nuget/Talby.Core.ResxAccess.0.1.0-beta.1.nupkg'
$expectedHash = (Get-Content -LiteralPath "$package.sha256").Trim()
if ((Get-FileHash -LiteralPath $package -Algorithm SHA256).Hash -ne $expectedHash) {
    throw 'The archive differs from the validated release artifact.'
}
if (Test-Path Env:NUGET_API_KEY) {
    throw 'Use a shell without an existing NUGET_API_KEY override.'
}
$env:NUGET_API_KEY = Read-Host 'NuGet API key' -MaskInput
try {
    dotnet nuget push $package --source https://api.nuget.org/v3/index.json
    if ($LASTEXITCODE -ne 0) { throw 'NuGet publication failed.' }
}
finally {
    Remove-Item Env:NUGET_API_KEY
}
```

## Check the published package

Confirm the public version page and its owner:

```text
https://www.nuget.org/packages/Talby.Core.ResxAccess/0.1.0-beta.1
```

After indexing, install from NuGet.org into a fresh consumer with a fresh package
cache. Use new directories on each attempt so an older locally restored archive
cannot satisfy this check:

```powershell
dotnet new console --framework net10.0 --output artifacts/nuget/published-consumer
if ($LASTEXITCODE -ne 0) { throw 'Consumer creation failed.' }
dotnet add artifacts/nuget/published-consumer package Talby.Core.ResxAccess --version 0.1.0-beta.1 --source https://api.nuget.org/v3/index.json --package-directory artifacts/nuget/published-packages
if ($LASTEXITCODE -ne 0) { throw 'Published package installation failed.' }
```

Use the resources, Resource Access class and method calls from the
[package README](../src/Talby.Core.ResxAccess/README.md) in that consumer. Put the
method calls in `Program.cs` and the class declaration in `Texts.cs`, then run:

```powershell
dotnet run --project artifacts/nuget/published-consumer --configuration Release --no-restore
```

Verify Raw Text, Formatted Text and Spanish Resource Culture fallback. If you use
the README's method calls as written, inspect their return values or wrap them in
`Console.WriteLine` to compare the documented results.
